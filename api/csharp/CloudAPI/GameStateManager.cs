using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CloudApi.Infrastructure;
using CloudApi.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CloudApi
{
    /// <summary>
    ///   A simple state manager for the game. Stores information in Azure Blob storage
    ///   the first entry in the list stores if the game is running or not
    ///   subsequent entries are the list of devices playing the game
    /// </summary>
    /// 
    internal class GameStateManager
    {
        /// <summary>The name of the blob in storage which holds the game state.</summary>
        private const string StateBlobName = "gameState";

        /// <summary>The default length of the lease taken on storage for operations.</summary>
        private static readonly TimeSpan DefaultLeaseLength = TimeSpan.FromSeconds(30);

        /// <summary>The random number generator to use for selecting active devices to ping.</summary>
        private static readonly Random RandomNumberGenerator = new Random();

        /// <summary>The settings to use for state serialization and deserialization.</summary>
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        /// <summary>A reference to the blob storage container that holds game state.</summary>
        private CloudBlobContainer stateContainer = null;

        /// <summary>A reference to the storage blob that holds game state.</summary>
        private ICloudBlob stateBlob = null;

        /// <summary>A reference to the blob storage container that holds game state.</summary>
        private TimeSpan stateLeaseLengthTime = TimeSpan.Zero;

        /// <summary>
        ///   Initializes a new instance of the <see cref="GameStateManager"/> class.
        /// </summary>
        /// 
        /// <param name="storageConnectionString">The connection string to use for the game state blob storage instance.</param>
        /// <param name="storageContainerName">The nme of the blob storage container to use for holding game state.</param>
        /// <param name="stateLeaseLengthTime">The length of the lease held on storage for state operations; if not provided, a default will be used.</param>
        /// 
        public GameStateManager(string    storageConnectionString,
                                string    storageContainerName,
                                TimeSpan? stateLeaseLengthTime = null)
        {            
            this.stateContainer = CloudStorageAccount
                .Parse(storageConnectionString)
                .CreateCloudBlobClient()
                .GetContainerReference(storageContainerName);

            this.stateBlob            = this.stateContainer.GetBlockBlobReference(GameStateManager.StateBlobName);
            this.stateLeaseLengthTime = stateLeaseLengthTime ?? GameStateManager.DefaultLeaseLength;
        }

        /// <summary>
        ///   Performs the actions needed to retrieve a copy of the current game state.
        /// </summary>
        /// 
        /// <returns>A snapshot of the current state of the game.</returns>
        ///         
        public async Task<GameState> GetGameStateAync()
        {
            var currentState = default(GameState);

            await this.PerformStateOperationWithLeaseAsync(gameState =>
            {
                currentState = gameState ?? new GameState(GameActivity.NotStarted);
                return (StatePersistence.DoNotPersistState, currentState);


            }).ConfigureAwait(false);

            return currentState;
        }

        /// <summary>
        ///   Performs the tasks needed to register a new device with the game.
        /// </summary>
        /// 
        /// <param name="newDevice">The device to register.</param>
        /// 
        /// <returns>An indication of whether the device exists in the game or not.</returns>
        /// 
        public async Task<DeviceState> RegisterDeviceAsync(GameDevice newDevice)
        {
            var deviceState = DeviceState.Unknown;
                        
            await this.PerformStateOperationWithLeaseAsync(gameState =>
            {   
                // If gamestate hasn't been initialized, do so now.

                gameState = gameState ?? new GameState(GameActivity.NotStarted);
                
                // If the game was already started, then the device cannot be registered.
                
                if (gameState.Activity != GameActivity.NotStarted)
                {
                    
                    deviceState = this.DetermineDeviceState(gameState, newDevice.DeviceId);
                    return (StatePersistence.DoNotPersistState, gameState);
                }

                var persist = StatePersistence.DoNotPersistState;

                // If the device wasn't registered, do so now; if it already exists, consider the registration successful, but take no
                // action.
                
                if ((gameState.RegisteredDevices.Count == 0) || (!gameState.RegisteredDevices.ContainsKey(newDevice.DeviceId)))
                {
                    gameState.RegisteredDevices.Add(newDevice.DeviceId, newDevice);
                    persist = StatePersistence.PersistState;
                }

                if ((gameState.ActiveDevices.Count == 0) || (!gameState.ActiveDevices.Contains(newDevice.DeviceId)))
                {
                    gameState.ActiveDevices.Add(newDevice.DeviceId);
                    persist = StatePersistence.PersistState;
                }
                
                deviceState = DeviceState.RegisteredActive;
                return (persist, gameState);

            }).ConfigureAwait(false);

            return deviceState;
        }

        /// <summary>
        ///   Performs the tasks needed to unregister a device from the game.
        /// </summary>
        /// 
        /// <param name="deviceId">The identifier of the device to remove.</param>
        /// 
        /// <returns>An indication of whether the device exists in the game or not.</returns>
        /// 
        public async Task<DeviceState> UnregisterDeviceAsync(string deviceId)
        {
            var deviceState = DeviceState.Unknown;

            await this.PerformStateOperationWithLeaseAsync(gameState =>
            {
                // If gamestate hasn't been initialized, do so now.

                gameState = gameState ?? new GameState(GameActivity.NotStarted);

                var persist = StatePersistence.DoNotPersistState;
                
                // If the game was already started, then the device cannot be unregistered.
                
                if (gameState.Activity == GameActivity.InProgress)
                {
                    deviceState = this.DetermineDeviceState(gameState, deviceId);
                    return (StatePersistence.DoNotPersistState, gameState);
                }

                // If there are registered devices, remove the specified one, if it exists. 
                
                if (gameState.ActiveDevices.Contains(deviceId))
                {
                    gameState.RegisteredDevices.Remove(deviceId);
                    persist = StatePersistence.PersistState;
                }

                if (gameState.RegisteredDevices.ContainsKey(deviceId))
                {
                    gameState.RegisteredDevices.Remove(deviceId);
                    persist = StatePersistence.PersistState;
                }

                deviceState = DeviceState.NotInGame;
                return (persist, gameState);

            }).ConfigureAwait(false);

            return deviceState;
        }

        /// <summary>
        ///   Performs the tasks needed to start the game, if the game can be started.
        /// </summary>
        /// 
        /// <param name="validate">Indicates whether validation should be performed; if <c>false</c>, the state will be forced to InProgress regardless of current conditions.</param>
        /// 
        /// <returns>A tuple indicating if the game was started as the result of this request and a snapshot of the current state of the game.</returns>
        /// 
        public async Task<(bool, GameState)> StartGameAsync(bool validate = true)
        {
            var currentState = default(GameState);
            var started      = false;

            await this.PerformStateOperationWithLeaseAsync(gameState =>
            {
                currentState = gameState ?? new GameState(GameActivity.NotStarted);

                // If validation is enabled and the game is already in progress or has less than 2 devices registered, then take no
                // action.

                if ((validate) && ((currentState.Activity == GameActivity.InProgress) || (currentState.RegisteredDevices.Count < 2)))
                {
                    return (StatePersistence.DoNotPersistState, currentState);
                }

                // Mark the game as started and persist the state.

                currentState.Activity = GameActivity.InProgress;

                started = true;
                return (StatePersistence.PersistState, currentState);


            }).ConfigureAwait(false);

            return (started, currentState);
        }

        /// <summary>
        ///   Performs the tasks needed to start the game, if the game can be started.
        /// </summary>
        ///
        /// <param
        /// <param name="validate">Indicates whether validation should be performed; if <c>false</c>, the state will be forced to InProgress regardless of current conditions.</param>
        /// 
        /// <returns>A tuple indicating if the game was completed as the result of this request and a snapshot of the current state of the game.</returns>
        /// 
        public async Task<(bool, GameState)> CompleteGameAsync(string winnningDeviceId = null,
                                                               bool   validate         = true)
        {
            var currentState = default(GameState);
            var completed    = false;

            await this.PerformStateOperationWithLeaseAsync(gameState =>
            {
                currentState = gameState ?? new GameState(GameActivity.NotStarted);

                // If validation is enabled and the game is already in progress or has no devices registered, then take no
                // action.

                if ((validate) && (currentState.Activity != GameActivity.InProgress))
                {
                    return (StatePersistence.DoNotPersistState, currentState);
                }

                // Transition state to completed and persist.

                currentState.Activity        = GameActivity.Complete;
                currentState.WinningDeviceId = (!String.IsNullOrEmpty(winnningDeviceId)) ? winnningDeviceId : currentState.WinningDeviceId;
                currentState.ActivePing      = null;


                currentState.ActiveDevices.Clear();

                completed = true;
                return (StatePersistence.PersistState, currentState);

            }).ConfigureAwait(false);

            return (completed, currentState);
        }

        /// <summary>
        ///   Immediately clears all existing game state, cancelling any game in progress.
        /// </summary>
        /// 
        /// <returns>A snapshot of the current state of the game.</returns>
        /// 
        public async Task<GameState> ResetGameAsync()
        {
            var currentState  = default(GameState);

            await this.PerformStateOperationWithLeaseAsync(gameState =>
            {
                currentState = new GameState(GameActivity.NotStarted);
                return (StatePersistence.PersistState, currentState);

            }).ConfigureAwait(false);

            return currentState;
        }

        /// <summary>
        ///   Manages the active ping for the game, expiring a ping that is over the age limit, if one exists.  In the case that
        ///   no ping exists or one is deemed expired, a new active ping is generated for the requested device, if specific.  
        ///   If there was no device specified, a random device will be selected from those currently active in the game.
        /// </summary>
        /// 
        /// <param name="maxActivePingAge">The maximum age for an active ping before it expires; if <c>TimeSpan.Zero</c>, any active ping will be immediately expired.</param>
        /// <param name="deviceId">The device identifier to generate ping data for.  If <c>null</c>; a random device will be chosen.</param>
        /// 
        /// <returns>A tuple containing the data for any ping that was generated, any ping that was expired and snapshot of the current state of the game.</returns>
        /// 
        public async Task<(PingPongData, PingPongData, GameState)> ManageActivePing(TimeSpan maxActivePingAge,
                                                                                    string   deviceId = null)
        {
            var currentState = default(GameState);
            var expiredPing  = default(PingPongData);
            var newPing      = default(PingPongData);
            
            await this.PerformStateOperationWithLeaseAsync(gameState =>
            {
                // If gamestate hasn't been initialized, do so now.

                gameState = gameState ?? new GameState(GameActivity.NotStarted);
                
                // If the game is not already started or there are no active devices then no action can be taken.
                
                if ((gameState.Activity != GameActivity.InProgress) || (gameState.ActiveDevices.Count == 0))
                    
                {                   
                    currentState = gameState;
                    return (StatePersistence.DoNotPersistState, gameState);
                }

                // If there is a current ping and it has either expired or forced expiration was requested, then 
                // expire it now.

                var persist = StatePersistence.DoNotPersistState;
                var now    = DateTime.UtcNow;
                
                if  ((gameState.ActivePing != null) && 
                    ((maxActivePingAge == TimeSpan.Zero) || (now.Subtract(gameState.ActivePing.EventTimeUtc) >= maxActivePingAge)))
                {
                    expiredPing = gameState.ActivePing;
                    persist     = StatePersistence.PersistState;

                    gameState.ActiveDevices.Remove(expiredPing.DeviceId);
                    gameState.ActivePing = null;
                }

                // If there is only a single remaining active device, it is the game's winner.  The game is now over.  Short circuit so 
                // that no new ping is generated.

                if (gameState.ActiveDevices.Count <= 1)
                {
                    gameState.Activity        = GameActivity.Complete;
                    gameState.WinningDeviceId = gameState.ActiveDevices.FirstOrDefault();                    
                    gameState.ActivePing      = null;

                    gameState.ActiveDevices.Clear();
                    
                    currentState = gameState;
                    return (StatePersistence.PersistState, gameState);                    
                }

                // If there was a requested device to ping, but that device is not active, then no ping can be generated.

                if ((!String.IsNullOrEmpty(deviceId)) && (!gameState.ActiveDevices.Contains(deviceId)))
                {
                    currentState = gameState;
                    return (persist, gameState);
                }

                // If a ping is needed then generate one; otherwise, no further action is needed.

                if (gameState.ActivePing == null)
                {
                    var pingData = this.GeneratePing(gameState, now, deviceId);
                                        
                    gameState.ActivePing = pingData;      
                    newPing              = pingData;
                    persist              = StatePersistence.PersistState;

                    gameState.PingsSent.Add(pingData);
                }
                    
                currentState = gameState;
                return (persist, gameState);

            }).ConfigureAwait(false);

            return (newPing, expiredPing, currentState);
        }

        /// <summary>
        ///   Records pong data in the game state for the specified device.  If the device is not associated with the
        ///   active ping or is not active in the game, then no action will be taken.
        /// </summary>
        /// 
        /// <param name="deviceId">The identifier of the device to record a pong from.</param>
        /// <param name="maxActivePingAge">The maximum age for an active ping before it expires.</param>
        /// 
        /// <returns>A tuple indicating of the pong was accepted and containing a snapshot of the current state of the game.</returns>
        /// 
        public async Task<(bool, GameState)> RecordPong(string   deviceId,
                                                        TimeSpan maxActivePingAge)
        {
            var currentState = default(GameState);
            var pongAccepted = false;
            var now          = DateTime.UtcNow;
            
            await this.PerformStateOperationWithLeaseAsync(gameState =>
            {
                // If gamestate hasn't been initialized, do so now.

                gameState = gameState ?? new GameState(GameActivity.NotStarted);
                
                // If the game is not already started, there are no active devices, the requested device is not active,
                // the specified ping is no longer the current, or the active ping has expired, then no action can be taken.
                
                if ((gameState.Activity != GameActivity.InProgress)       || 
                    (gameState.ActivePing == null)                        ||
                    (gameState.ActivePing.DeviceId != deviceId)           ||
                    (!gameState.ActiveDevices.Contains(deviceId))         ||
                    (now.Subtract(gameState.ActivePing.EventTimeUtc) >= maxActivePingAge))
                    
                {   
                    currentState = gameState;
                    return (StatePersistence.DoNotPersistState, gameState);
                }

                // Generate the pong data to record.
                                
                var pingPongData = new PingPongData
                {
                    DeviceId     = deviceId,
                    EventTimeUtc = now
                };
                                
                gameState.PongsReceived.Add(pingPongData);

                gameState.ActivePing = null;
                pongAccepted         = true;
                currentState         = gameState;                
                
                return (StatePersistence.PersistState, gameState);

            }).ConfigureAwait(false);

            return (pongAccepted, currentState);
        }

        /// <summary>
        ///   Handles lease management and game state persistence, yielding control to the specified 
        ///   operation.
        /// </summary>
        /// 
        /// <param name="operation">The operation to invoke once the state has been loaded.</param>
        ///         
        private async Task PerformStateOperationWithLeaseAsync(Func<GameState, (StatePersistence, GameState)> operation)
        {
            var leaseId   = default(string);
            var gameState = default(GameState);

            try
            {
                try
                { 
                    var condition = AccessCondition.GenerateEmptyCondition();
                    var options   = new BlobRequestOptions { RetryPolicy = new ExponentialRetry() };
                    var context   = new OperationContext();

                    leaseId   = await this.stateBlob.AcquireLeaseAsync(this.stateLeaseLengthTime, null, condition, options, context).ConfigureAwait(false); 
                    gameState = await this.LoadGameStateAsync(leaseId).ConfigureAwait(false);
                }

                catch (StorageException ex) when ((ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed) || (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict))
                {
                    // The lease could not be obtained.  This is most likely due to it being held (and not released in a timely manner) 
                    // by another process.  Wrap it as a known exception to indicate a concurrency collision.

                    throw new ConcurrencyException(ex);
                }

                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    // The storage item (or container) were not present.  This isn't considered an exception situation; simply indicate that there
                    // is no state available and allow the specified operation make the determination as to the correct course of action.

                    gameState = null;
                }

                // Invoke the operation.

                var (persistNeed, updatedState) = operation(gameState);

                if (persistNeed == StatePersistence.PersistState)
                {
                    await this.SaveStateAsync(updatedState, leaseId).ConfigureAwait(false);
                }
            }

            finally
            {
                if (!String.IsNullOrEmpty(leaseId))
                {
                    await this.SafeReleaseLeaseAsync(this.stateBlob, leaseId).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        ///   Loads the game state from blob storage, if it exists.
        /// </summary>
        /// 
        /// <param name="storageLeaseId">The identifier of the lease held on the game state storage item.</param>
        /// 
        /// <returns>The current state of the game, if it exists in storage; otherwise, <c>null</c>.</returns>
        /// 
        /// <remarks>
        ///     This method makes no attempt at synchronization; callers are responsible for 
        ///     aquiring and managing blob leases for their specific operation.
        /// </remarks>
        /// 
        private async Task<GameState> LoadGameStateAsync(string storageLeaseId)
        {
            using (var blobStream = new MemoryStream())
            {
                
                try
                {
                    var leaseCondition = AccessCondition.GenerateLeaseCondition(storageLeaseId);
                    var options        = new BlobRequestOptions { RetryPolicy = new ExponentialRetry() };
                    var context        = new OperationContext();

                    await this.stateBlob.DownloadToStreamAsync(blobStream, leaseCondition, options, context).ConfigureAwait(false);                
                    blobStream.Position = 0;

                    return JsonConvert.DeserializeObject<GameState>(Encoding.UTF8.GetString(blobStream.ToArray()), GameStateManager.SerializerSettings);
                }

                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }

                finally
                {
                    blobStream?.Close();
                }
            }
        }

        /// <summary>
        ///   Saves the game state to blob storage, if it exists.
        /// </summary>
        /// 
        /// <param name="gameState">The state to save;</param>
        /// <param name="storageLeaseId">The identifier of the lease held on the game state storage item.</param>
        /// 
        /// <remarks>
        ///     This method makes no attempt at synchronization; callers are responsible for 
        ///     aquiring and managing blob leases for their specific operation.
        /// </remarks>
        /// 
        private async Task SaveStateAsync(GameState gameState,
                                          string    storageLeaseId)
        {
            var executeCount = 0;
            var allowRetry   = true;
            
            using (var blobStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(gameState, Formatting.Indented, GameStateManager.SerializerSettings))))
            {                
                while ((allowRetry) && (executeCount <= 1))
                {    
                    try
                    {
                        blobStream.Position = 0;

                        var leaseCondition = AccessCondition.GenerateLeaseCondition(storageLeaseId);
                        var options        = new BlobRequestOptions { RetryPolicy = new ExponentialRetry() };
                        var context        = new OperationContext();

                        await this.stateBlob.UploadFromStreamAsync(blobStream, leaseCondition, options, context).ConfigureAwait(false);
                        
                        allowRetry = false;
                        blobStream.Close();
                    }

                    catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        await this.stateContainer.CreateIfNotExistsAsync();
                    } 

                    ++executeCount;
                }
            }
        }

        /// <summary>
        ///   Performs the actions needed to release a lease on the specific blob, assuming any triggered exceptions are 
        ///   non-critical and ignoring them.
        /// </summary>
        /// 
        /// <param name="blob">The blob on which the lease to release is held.</param>
        /// <param name="leaseId">The identifier of the lease to release.</param>
        ///         
        private async Task SafeReleaseLeaseAsync(ICloudBlob blob, 
                                                 string     leaseId)
        {
            // If there is no blob or valid lease identifier, then assume no lease is held to release.

            if ((blob == null) || (String.IsNullOrEmpty(leaseId)))
            {
              return;
            }

            try
            {
                await blob.ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(leaseId));
            }

            catch
            {
                // Take no action and consider this a non-critical exception.
            }


        }

        /// <summary>
        ///   Determines the state of the specified device.
        /// </summary>
        /// 
        /// <param name="gameState">State of the game.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// 
        /// <returns>The state of the device.</returns>
        /// 
        private DeviceState DetermineDeviceState(GameState gameState,
                                                 string    deviceId)
        {
            if ((String.IsNullOrEmpty(deviceId)) || (gameState == null))
            {
                return DeviceState.Unknown;
            }

             var registered = gameState.RegisteredDevices.ContainsKey(deviceId);
             var active     = gameState.ActiveDevices.Contains(deviceId);

             return
                 (registered && active) ? DeviceState.RegisteredActive
               : (registered)           ? DeviceState.RegisteredInactive
               : DeviceState.NotInGame;
        }

        /// <summary>
        ///   Generates ping data for use in the game state for the requested device.  If there was
        ///   no device specified, a random device will be selected from those currently active in the game.
        /// </summary>
        /// 
        /// <param name="gameState">The current state of the game.</param>
        /// <param name="eventTimeUtc">The date/time (in UTC) that the ping should consider the event occurrence.  If not specified, the current date/time will be used.</param>
        /// <param name="deviceId">The device identifier to generate ping data for.  If <c>null</c>; a random device will be chosen.</param>        
        /// 
        /// <returns>The generated ping data</returns>
        /// 
        /// <remarks>
        ///   This method does not update the game state with the generated ping; responsiblity for state
        ///   updates is assumed to be purview of the caller.
        /// </remarks>
        /// 
        private PingPongData GeneratePing(GameState gameState,
                                          DateTime? eventTimeUtc = null,
                                          string    deviceId     = null)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            var eventTime = eventTimeUtc ?? DateTime.UtcNow;

            if (String.IsNullOrEmpty(deviceId))
            {
                var selectedIndex = GameStateManager.RandomNumberGenerator.Next(0, gameState.ActiveDevices.Count);
                deviceId = gameState.ActiveDevices.ElementAt(selectedIndex);
            }

            return new PingPongData
            {
                DeviceId     = deviceId,
                EventTimeUtc = eventTime
            };
        }

        /// <summary>
        ///   Represents the need for persistence of game state; intended only for private
        ///   use.
        /// </summary>
        /// 
        private enum StatePersistence
        {
            PersistState,
            DoNotPersistState
        }
    }
}
