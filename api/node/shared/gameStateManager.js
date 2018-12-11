const GameState        = require("./models/gameState");
const GameActivity     = require("./models/gameActivity");
const PingPongData     = require("./models/pingPongData");
const DeviceState      = require("./models/deviceState");
const ConcurrencyError = require("./errors/concurrencyError");
const azureStorage     = require("azure-storage");
const moment           = require("moment");
const { promisify }    = require("util");

/** The name of the blob in storage, which holds the game state. */
const stateBlobName = "gameState";

/** The default length of time, in seconds, that the lease for storage operations is held.  */
const defaultStateLeaseLengthSeconds = 30;

/** The symbol to use for a reference to the instance function responsible for performaing a game state operation. */
const stateOperationExecutor = Symbol("State Operation Executor");

/**
 * Performs the tasks needed to create and initialize a client that can be used
 * for Azure Blob Storage operations.
 * @static
 * @private
 *
 * @param { string } connectionString The connection string to use for the Azure Storage account
 *
 * @returns { object }  An Azure Blob storage client configured for game-related operations.
 */
const createBlobClient = connectionString => {
    let client = azureStorage
        .createBlobService(connectionString)
        .withFilter(new azureStorage.ExponentialRetryPolicyFilter());

    // Extend the client by creating "async" versions of the operations used for
    // game state management instead of working with callbacks or doing manual
    // creation of promises.

    client.acquireLeaseAsync               = promisify(client.acquireLease);
    client.releaseLeaseAsync               = promisify(client.releaseLease);
    client.createContainerIfNotExistsAsync = promisify(client.createContainerIfNotExists);
    client.getBlobToTextAsync              = promisify(client.getBlobToText);
    client.createBlockBlobFromTextAsync    = promisify(client.createBlockBlobFromText);

    return client;
};

/**
 * Performs the tasks needed to acquire a lease on the specified container and state blob.  In the event that the container
 * does not yet exists, an attempt will be made to create it.
 * @static
 * @private
 *
 * @param { object } blobClient            The client ot use for Azure Storage blob operations.
 * @param { string } storageContainer      The name of the container in storage that contains the state blob.
 * @param { int }    leaseDurationSeconds  The length of time, in seconds, that the lease should be held for.
 *
 * @returns { object }  The lease that was requested.
 */
const acquireLeaseAsync = async (blobClient, storageContainer, leaseDurationSeconds) => {
    let executeCount      = 0;
    let operationComplete = false;
    let lease             = null;

    while ((!operationComplete) && (executeCount <= 1)) {
        try {
            lease             = await blobClient.acquireLeaseAsync(storageContainer, stateBlobName, { leaseDuration : leaseDurationSeconds });
            operationComplete = true;
        }

        catch (ex) {
            // If the lease couldn't be acquired because the container or blob was not found, attempt to create them
            // and then try again.  This is a benign race condition, as other callers may be doing the same.  This is
            // why any error observed during creation is ignored; another attempt will be made to acquire the lease, which
            // will trigger appropriate error propagation.

            if ((!operationComplete) && (executeCount <= 1) && (ex instanceof Error) && (ex.name === "StorageError") && (ex.statusCode === 404)) {
                try {
                    await blobClient.createContainerIfNotExistsAsync(storageContainer);
                    await blobClient.createBlockBlobFromTextAsync(storageContainer, stateBlobName, null);
                }

                catch {
                    // Ignore an exceptions here and allow retry or failure to occur.
                }
            }

            else if ((ex instanceof Error) && (ex.name === "StorageError") && (ex.statusCode === 409)) {
                throw new ConcurrencyError("The lease could not be acquired; it is likely is already held.", ex);
            }

            else {
                // This is not a special case, propagate the exception normally.

                throw ex;
            }
        }

        ++executeCount;
    }

    if (!lease) {
        throw new Error(`The lease could not be acquired for an unknown reason.  Check that the container, "${ storageContainer }" exists or that you have rights to create it.`);
    }

    return lease;
};

/**
 * Performs the actions needed to release a lease on the specific blob, assuming any triggered
 * exceptions are non-critical and ignoring them.
 * @static
 * @private
 *
 * @param { object } blobClient The client ot use for Azure Storage blob operations.
 * @param { object } blobLease  The lease held on the blob that holds the game state.
 */
const safeReleaseLeaseAsync = async (blobClient, lease) => {
    if (!lease) {
        return;
    }

    try {
        await blobClient.releaseLeaseAsync(lease.container, lease.blob, lease.id);
    }

    catch {
        // Take no action and consider this a non-critical exception.  The lease will
        // expire on it's own if it is not properly released.
    }
};

/**
 * Loads the game state from blob storage, if it exists.
 * @static
 * @private
 *
 * @param { object } blobClient The client ot use for Azure Storage blob operations.
 * @param { object } blobLease  The lease held on the blob that holds the game state.
 *
 * @returns { object }  The current state of the game, if it exists in storage; otherwise, null.
 *
 * NOTE: This function makes no attempt at synchronization; callers are responsible for
 *       acquiring and managing blob leases for their specific operation.
 */
const loadGameStateAsync = async (blobClient, blobLease) => {
    try {
        const stateBlob = await blobClient.getBlobToTextAsync(blobLease.container, blobLease.blob, { leaseId : blobLease.id });
        return ((stateBlob) && (stateBlob.length > 0)) ? JSON.parse(stateBlob) : null;
    }

    catch (ex) {
        // If the state was not found, do not consider it an exception; there is no state.

        if ((ex instanceof Error) && (ex.name === "StorageError") && (ex.statusCode === 404)) {
            return null;
        }

        // Bubble the exception.

        throw ex;
    }
};

/**
 * Saves the provided game state to a blob in Azure Storage.
 * @static
 * @private
 *
 * @param { object } blobClient The client ot use for Azure Storage blob operations.
 * @param { object } blobLease  The lease held on the blob that holds the game state.
 * @param { object } gameState  The state of the game to be saved to the blob.
 *
 * NOTE: This function makes no attempt at synchronization; callers are responsible for
 *       acquiring and managing blob leases for their specific operation.
 */
const saveGameStateAsync = async (blobClient, blobLease, gameState) => {
    let executeCount      = 0;
    let operationComplete = false;
    let lease             = null;

    while ((!operationComplete) && (executeCount <= 1)) {
        try {
            await blobClient.createBlockBlobFromTextAsync(blobLease.container, blobLease.blob, JSON.stringify(gameState), { leaseId : blobLease.id });
            operationComplete = true;
        }

        catch (ex) {
            // If the blob couldn't be created because the container was not found, attempt to create it
            // and then try again.  At this point, the lease is held, so no other callers should have the ability to
            // do so, but there may be manipulation happening outside this application.  That is why any error observed
            // during creation is ignored; another attempt will be made to write the blob, which will trigger appropriate
            // error propagation.

            if ((!operationComplete) && (executeCount <= 1) && (ex instanceof Error) && (ex.name === "StorageError") && (ex.statusCode === 404)) {
                try {
                    await blobClient.createContainerIfNotExistsAsync(lease.container);
                }

                catch {
                    // Ignore an exceptions here and allow retry or failure to occur.
                }
            }
            else {
                // This is not a special case, propagate the exception normally.

                throw ex;
            }
        }

        ++executeCount;
    }
};

/**
 * Handles lease management and game state persistence, yielding control to the specified
 * operation.
 * @static
 * @private
 *
 * @param { object }   blobClient            The client ot use for Azure Storage blob operations.
 * @param { string }   storageContainer      The name of the container in storage that contains the state blob.
 * @param { int }      leaseDurationSeconds  The length of time, in seconds, that the lease should be held for.
 * @param { function } operation             The synchronous operation to perform in the scope of the acquired lease.  Expected signature: [ StatePersistence, GameState ] function (GameState)
 */
const performStateOperationWithLeaseAsync = async (blobClient, storageContainer, leaseDurationSeconds, operation) => {
    let lease     = null;
    let gameState = null;

    try {
        // Acquire the lease and load the current game state.

        lease     = await acquireLeaseAsync(blobClient, storageContainer, leaseDurationSeconds);
        gameState = await loadGameStateAsync(blobClient, lease);

        // Perform the given operation, persisting the updated state if needed.

        const [ persistNeeded, updatedState ] = operation(gameState);

        if ((persistNeeded === StatePersistence.PersistState) && (gameState)) {
            await saveGameStateAsync(blobClient, lease, updatedState);
        }
    }

    finally {
        if (lease) {
            await safeReleaseLeaseAsync(blobClient, lease);
        }
    }
};

/**
 * Determines the state of the specified device.
 * @static
 * @private
 *
 * @param { object } gameState  The current state of the game.
 * @param { string } deviceId   The unique identifier of the device to determine the state of.
 *
 * @returns { enum }  The state of the device in the context of the game.
 */
const determineDeviceState = (gameState, deviceId) => {
    if ((!deviceId) || (!deviceId.length) || (deviceId.length <= 0)) {
        return DeviceState.Unknown;
    }

    const registered = (deviceId in gameState.registeredDevices);
    const active     = gameState.activeDevices.includes(deviceId);

    return
       (registered && active) ? DeviceState.RegisteredActive
     : (registered)           ? DeviceState.RegisteredInactive
     : DeviceState.NotInGame;
};

/**
 * Represents the need for persistence of game state; intended only for private, transient use
 * during state management operations.
 * @readonly
 * @enum { Symbol }
 */
const StatePersistence = Object.freeze({
    /** The state has been changed and should be persisted. */
    PersistState : Symbol("Persist"),

    /** The state has not changed or any changes should NOT be persistence. */
    DoNotPersistState : Symbol("DoNotPersist")
});

/**
 * A simple state manager for the game. Stores information in Azure Blob storage
 * the first entry in the list stores if the game is running or not
 * subsequent entries are the list of devices playing the game
 * @class
 */
class GameStateManager {

    /**
     * Creates an instance of the class, optionally overriding the default
     * attribute values.
     * @constructor
     *
     * @param { string } storageConnectionString  The connection string to use for the game state blob storage instance.
     * @param { string } storageContainerName     The nme of the blob storage container to use for holding game state.
     * @param { int }    stateLeaseLengthSeconds  The length of the lease held on storage for state operations, in seconds; if not provided, a default will be used.
     */
    constructor(storageConnectionString,
                storageContainerName,
                stateLeaseLengthSeconds = defaultStateLeaseLengthSeconds) {

        if ((typeof(storageConnectionString) !== "string") ||
            (storageConnectionString === null)             ||
            (storageConnectionString.length <= 0)) {

            throw new Error("storageConnectionString must be provided and be a valid Azure Storage connection string.");
        }

        if ((typeof(storageContainerName) !== "string") ||
            (storageContainerName === null)             ||
            (storageContainerName.length <= 0)) {

            throw new Error("storageContainerName must be provided and be a valid Azure Storage container name.");
        }

        if ((typeof(stateLeaseLengthSeconds) !== "number") ||
            (stateLeaseLengthSeconds < 15)                 ||
            (stateLeaseLengthSeconds > 60)) {

            throw new Error("stateLeaseLengthSeconds must be a number between 15 and 60.");
        }

        // Initialize the class read-only attributes.

        Object.defineProperties(this, {

            storageConnectionString : {
                value        : storageConnectionString,
                writable     : false,
                enumerable   : false,
                configurable : false
            },

            storageContainerName : {
                value        : storageContainerName,
                writable     : false,
                enumerable   : false,
                configurable : false
            },

            stateLeaseLengthSeconds : {
                value        : stateLeaseLengthSeconds,
                writable     : false,
                enumerable   : false,
                configurable : false
            }
        });

        // Initialize the private class members.

        const client = createBlobClient(storageConnectionString);
        this[stateOperationExecutor] = async operation => performStateOperationWithLeaseAsync(client, storageContainerName, stateLeaseLengthSeconds, operation);
    }

    /**
     * Performs the actions needed to retrieve a copy of the current game state.
     * @method
     *
     * @returns { object }  A snapshot of the current state of the game.
     */
    async getGameStateAsync() {
        let state = null;

        await this[stateOperationExecutor](gameState => {
            state = gameState;
            return [ StatePersistence.DoNotPersistState, gameState ];
        });

        return (state || new GameState({activity : GameActivity.NotStarted }));
    }

    /**
     * Performs the tasks needed to register a new device with the game.
     *
     * @param { object } newDevice  The device to register with the game.
     *
     * @returns { enum }  The status of the device in the game.
     */
    async registerDeviceAsync(newDevice) {
        let deviceState = DeviceState.Unknown;

        await this[stateOperationExecutor](gameState => {
            gameState = gameState || new GameState({ activity: GameActivity.NotStarted });

            // If the game has been started, then the device can't be registered; alert the
            // caller to the current state of the device.

            if (gameState.activity !== GameActivity.NotStarted) {
                deviceState = determineDeviceState(gameState, newDevice.deviceId);

                return [ StatePersistence.DoNotPersistState, gameState ];
            }

            let persist = StatePersistence.DoNotPersistState;

            // If the device wasn't registered, do so now; if it already exists, consider the call successful
            // but take no action.

            if (!(newDevice.deviceId in gameState.registeredDevices)) {
                gameState.registeredDevices[newDevice.deviceId] = newDevice
                persist = StatePersistence.PersistState;
            }

            if (!gameState.activeDevices.includes(newDevice.deviceId)) {
                gameState.activeDevices.push(newDevice.deviceId);
                persist = StatePersistence.PersistState;
            }

            deviceState = DeviceState.RegisteredActive;
            return [ persist, gameState ];
        });

        return deviceState;
    }

    /**
     * Performs the tasks needed to start the game, if the game can be started.
     * @method
     *
     * @param { bool } validate  Indicates whether validation should be performed; if >false, the state will be forced to InProgress regardless of current conditions.
     *
     * @returns { array } An array indicating if the game was started as the result of this request and a snapshot of the current state of the game.
     */
    async startGameAsync(validate = true) {
        let currentState = null;
        let started      = false;

        await this[stateOperationExecutor](gameState => {
            currentState = gameState || new GameState({ activity: GameActivity.NotStarted });

            // If validation is enabled and the game is already in progress or has less than 2 devices registered, then take no
            // action.

            if ((validate) && ((currentState === GameState.InProgress) || (Object.keys(currentState.activeDevices).length < 2))) {
                return [ StatePersistence.DoNotPersistState, currentState ];
            }

            // Mark the game as started and persist the state.

            currentState.activity = GameActivity.InProgress;
            started               = true;

            return [ StatePersistence.PersistState, currentState ];
        });

        return [ started, currentState ];
    }

    /**
     * Immediately clears all existing game state, cancelling any game in progress.
     * @method
     *
     * @returns  A snapshot of the current state of the game.
     */
    async resetGameAsync() {
        let currentState = null;

        await this[stateOperationExecutor](gameState => {
            currentState = new GameState({ activity: GameActivity.NotStarted });
            return [ StatePersistence.PersistState, currentState ];
        });

        return currentState;
    }

    /**
     * Manages the active ping for the game, expiring a ping that is over the age limit, if one exists.  In the case that
     * no ping exists or one is deemed expired, a new active ping is generated for the requested device, if specific.
     * If there was no device specified, a random device will be selected from those currently active in the game.
     * @method
     *
     * @param { int }    maxActivePingAge  The maximum age for an active ping before it expires, in seconds.  If null, any active ping will be immediately expired.
     * @param { string } deviceId          The device identifer to generate a new ping for; if not specified, a random active device will be selected.
     *
     * @returns { array }  An array containing any new ping generated, any active ping expired, and a snapshot of the current state of the game.
     */
    async manageActivePing(maxActivePingAge,
                           deviceId         = null) {
        let currentState = null;
        let expiredPing  = null;
        let newPing      = null;

        await this[stateOperationExecutor](gameState => {
            currentState = gameState || new GameState({ activity: GameActivity.NotStarted });

            // If the game is not already started or there are no active devices, then no
            // action can be taken.

            if ((currentState.activity !== GameActivity.InProgress) || (currentState.activeDevices.length <= 0)) {
                return [ StatePersistence.DoNotPersistState, currentState ];
            }

            // If there is a current ping and it has either expired or a forced expiration was requested, then
            // expire it now.

            let persist = StatePersistence.DoNotPersistState;

            const maxPingTime = (currentState.activePing)
              ? moment(currentState.activePing.eventTimeUtc).utc().add((maxActivePingAge || 0), "seconds")
              : moment().utc().add(1, "minutes");

            const now = moment().utc();

            if ((currentState.activePing !== null) &&
               ((maxActivePingAge === null) || (now.isSameOrAfter(maxPingTime)))) {

                expiredPing                = gameState.activePing;
                persist                    = StatePersistence.PersistState;
                currentState.activeDevices = currentState.activeDevices.filter(item => item !== expiredPing.deviceId);
                currentState.activePing    = null;
            }

            // If there is only a single remaining active device, it is the game's winner.  The game is now over.  Short circuit so
            // that no new ping is generated.

            if (currentState.activeDevices.length <= 1) {

                currentState.activity        = GameActivity.Complete;
                currentState.winningDeviceId = (currentState.activeDevices.pop() || null);
                currentState.activeDevices   = [];
                currentState.activePing      = null;

                return [ StatePersistence.PersistState, currentState ];
            }

            // If there was a requested device to ping, but that device is not active, no
            // ping can be generated.

            if ((deviceId) && (deviceId.length > 0) && (!currentState.activeDevices.includes(deviceId))) {
                return [ persist, currentState ];
            }

            // If a ping is needed, then generate one; otherwise, no further action
            // is necessary.

            if (currentState.activePing === null) {
                persist              = StatePersistence.PersistState;
                newPing              = this.generatePing(currentState, deviceId);
                gameState.activePing = newPing;

                gameState.pingsSent.push(newPing);
            }

            return [ persist, currentState ];
        });

        return [ newPing, expiredPing, currentState ];
    }

    /**
     * Records pong data in the game state for the specified device.  If the device is not associated with the
     * active ping or is not active in the game, then no action will be taken.
     * @method
     *
     * @param { string } deviceId          The identifier of the device to record a pong from.
     * @param { int }    maxActivePingAge  The maximum age for an active ping before it expires, in seconds.  If null, any active ping will be immediately expired.
     *
     * @returns { array }  An array containing a boolean that indicates if the ping was accepted and a snapshot of the current state of the game.
     */
    async recordPong(deviceId,
                    maxActivePingAge) {

        let currentState = null;
        let pongAccepted = false;

        await this[stateOperationExecutor](gameState => {
            currentState = gameState || new GameState({ activity: GameActivity.NotStarted });

            // If the game is not already started, there are no active devices, the requested device is not active,
            // the specified ping is no longer the current, or the active ping has expired, then no action can be taken.

            const maxPingTime = (currentState.activePing)
              ? moment(currentState.activePing.eventTimeUtc).utc().add((maxActivePingAge || 0), "seconds")
              : moment().utc().add(1, "minutes");

            const now = moment().utc();

            if ((currentState.activity !== GameActivity.InProgress) ||
                (!currentState.activePing)                          ||
                (currentState.activePing.deviceId !== deviceId)     ||
                (!currentState.activeDevices.includes(deviceId))    ||
                (now.isSameOrAfter(maxPingTime))) {

                return [ StatePersistence.DoNotPersistState, currentState ];
            }

            // Generate the Pong to record.

            currentState.pongsReceived.push(
                new PingPongData({
                    deviceId     : deviceId,
                    eventTimeUtc : moment().utc().toISOString()
                })
            );

            currentState.activePing = null;
            pongAccepted            = true;

            return [ StatePersistence.PersistState, currentState ];
        });

        return [ pongAccepted, currentState ];
    }

    /**
     * Generates ping data for use in the game state for the requested device.  If there was
     * no device specified, a random device will be selected from those currently active in the game.
     * @method
     *
     * @param { object } gameState  The current state of the game.
     * @param { string } deviceId   The device to generate the ping for; if not specified, a random active device will be chosen.
     *
     * @returns { object }  The generated ping data.
     *
     * NOTE: This method does not update the game state with the generated ping; responsiblity for state
     *       updates is assumed to be purview of the caller.
     */
    generatePing(gameState,
                 deviceId = null) {

        let pingDevice = deviceId;

        if ((!pingDevice) || (pingDevice.length <= 0)) {
            const randomDeviceIndex = Math.floor(Math.random() * gameState.activeDevices.length);
            pingDevice = gameState.activeDevices[randomDeviceIndex];
        }

        return new PingPongData({
            deviceId     : pingDevice,
            eventTimeUtc : moment().utc().toISOString()
        });
    }
}

module.exports = GameStateManager;