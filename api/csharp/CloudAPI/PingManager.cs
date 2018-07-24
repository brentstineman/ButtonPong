using System;
using System.Threading.Tasks;
using CloudApi.Infrastructure;
using CloudApi.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CloudApi
{
    /// <summary>
    ///   Provides the API for managing "pings" for a game.
    /// </summary>
    /// 
    public static class PingManager
    {
        /// <summary>The manager of state to use for processing requests.</summary>
        private static GameStateManager stateManager = new GameStateManager(
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageConnectionString), 
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageContainer));

        /// <summary>The maximum age of a ping before it expires.</summary>
        private static TimeSpan maxPingAge = TimeSpan.FromSeconds(
            Int32.Parse(Environment.GetEnvironmentVariable(ConfigurationNames.PingMaxAgeSeconds)));

        /// <summary>The amount of time that is signaled to devices as the suggested timeout when awaiting for a pong to be triggered.</summary>
        private static int pingTimeoutSeconds = 
            Int32.Parse(Environment.GetEnvironmentVariable(ConfigurationNames.PingTimeout));

        /// <summary>The communicator to use for interacting with the game devices..</summary>
        private static IDeviceCommunicator communicator = new ParticleDeviceCommunicator();
    
        /// <summary>
        ///   Exposes the function API.
        /// </summary>
        /// 
        /// <param name="timer">The timer that triggered execution.</param>
        /// <param name="logger">The logger to use for writing log information.</param>
        ///
        [FunctionName(nameof(PingManager))]
        public async static Task Run([TimerTrigger("%" + ConfigurationNames.PingManagerSchedule + "%", RunOnStartup = false)]TimerInfo timer, 
                                                    ILogger logger)
        {            
            logger.LogInformation($"{ nameof(PingManager) } executed at: { DateTime.Now }.");            
                 
            // Get a snapshot of the state before the pings are managed to allow for short-circuiting if a game
            // is not underway.

            var initialState = await PingManager.stateManager.GetGameStateAync();

            // If the game is not currently active, then no further action is needed.

            if ((initialState.Activity != GameActivity.InProgress) || (initialState.ActiveDevices.Count == 0))                    
            {
                logger.LogInformation($"{ nameof(PingManager) } detected game is not running or has no active devices at: { DateTime.Now }.");
                return;
            }

            // Manage the pings.

            var (newPing, expired, currentState) = await PingManager.stateManager.ManageActivePing(PingManager.maxPingAge);

            // If there was a device eliminated, then signal it.

            if (((expired != null) && (currentState.RegisteredDevices.ContainsKey(expired.DeviceId ?? String.Empty))))
            {
                logger.LogInformation($"{ nameof(PingManager) } signaling elimination to { expired?.DeviceId } at: { DateTime.Now }.");

                PingManager.communicator
                    .SendEliminatedEventAsync(currentState.RegisteredDevices[expired.DeviceId])
                    .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { expired.DeviceId } of its elimination"));
            }

            // If the game was completed when managing the active ping, signal completion and the winner.  

            if ((currentState.Activity == GameActivity.Complete) && (currentState.Activity != initialState.Activity))
            {
                logger.LogInformation($"{ nameof(PingManager) } signaling game complete at: { DateTime.Now }.");

                PingManager.communicator
                    .SendEndEventAsync(currentState.RegisteredDevices.Values)
                    .FireAndForget(exception => logger.LogError(exception, "An exception occurred signaling all registered devices that the game has ended."));

                if (currentState.RegisteredDevices.ContainsKey(currentState.WinningDeviceId))
                {
                    PingManager.communicator
                        .SendWinEventAsync(currentState.RegisteredDevices[currentState.WinningDeviceId])
                        .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { currentState.WinningDeviceId } that it has won."));
               }
            }

            // If a new ping was generated, signal the associated device.

            if ((newPing != null) && (currentState.ActiveDevices.Contains(newPing?.DeviceId ?? String.Empty)))
            {
                logger.LogInformation($"{ nameof(PingManager) } sending ping to { newPing?.DeviceId } at: { DateTime.Now }.");

                PingManager.communicator
                    .SendPingEventAsync(currentState.RegisteredDevices[newPing.DeviceId], PingManager.pingTimeoutSeconds)
                    .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { expired.DeviceId } of its elimination"));
            }
        }
    }
}