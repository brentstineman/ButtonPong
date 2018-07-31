const GameStateManager = require("../../shared/gameStateManager");
const config           = require("../../shared/configuration/config");

/** The manager of state to use for processing requests. */
const gameStateManager = new GameStateManager(config.getSetting(config.storageConnectionStringName), config.getSetting(config.storageContainerName));

/** he maximum age of a ping, in seconds, before it expires. */
const maxPingAgeSeconds = parseInt(config.getSetting(config.maxPingAgeName), 10);

/** The amount of time, in seconds, that a user should be given to respond to a ping on the device. */
const pingTimeout = parseInt(config.getSetting(config.pingTimeoutName), 10);

/**
 * Provides the API for starting a game, if it is in the proper state to do so.
 *     - Path: /api/game
 *     - Method: PUT
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } request  The incoming HTTP request being processed
 *
 */
module.exports = async function startGame(context, request) {
    context.log("Start Game function processed a request.");

    const [ started, currentState ] = await gameStateManager.startGameAsync();

    // ==================================
    // TODO: SIGNAL DEVICE
    // TODO: MANAGE FIRST PING
    // ==================================

    // If the game was not started, then validation failed.  Signal that
    // the game is not in the correct state to be started.

    const response = {
        status  : ((started) ? 200 : 409),
        headers : { "Content-Type" : "application/json" },
        body    : currentState
    };

    // NOTE: Due to a bug in the Azure Functions host, because this function returns a promise, the response should be returned directly
    //       as the runtime will attempt to resolve the promise.
    //
    //       Calling "done" will result in an error message written o the console
    //       about calling "done" twice.  This is a known issue with using "async" and is due to be fixed in a forthcoming release.
    //       Function execution is not impacted; a graceful validation in the runtime is erroneously tripped.
    //
    //       See: https://github.com/Azure/azure-functions-nodejs-worker/pull/99

    return response;
};