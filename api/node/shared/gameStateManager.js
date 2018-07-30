
const ConcurrencyError = require("./errors/concurrencyError");
const azureStorage     = require("azure-storage");
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

    client.acquireLease                    = promisify(client.acquireLease);
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
            lease             = await blobClient.acquireLease(storageContainer, stateBlobName, { leaseDuration : leaseDurationSeconds });
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
        return blobClient.getBlobToTextAsync(blobLease.container, blobLease.blob, { leaseId : blobLease.id });
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
            },
        });

        // Initialize the private class members.

        const client = createBlobClient(storageConnectionString);
        this[stateOperationExecutor] = async operation => performStateOperationWithLeaseAsync(client, storageContainerName, stateLeaseLengthSeconds, operation);
    }

    /**
     * Performs the actions needed to retrieve a copy of the current game state.
     *
     * @returns { object }  A snapshot of the current state of the game.
     */
    async getGameStateAsync() {
        let state = null;

        await this[stateOperationExecutor](gameState => {
            state = gameState;
            return [ StatePersistence.DoNotPersistState, gameState ];
        });

        return state;
    }
}

module.exports = GameStateManager;