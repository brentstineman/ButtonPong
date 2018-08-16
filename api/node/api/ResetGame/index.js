const GameStateManager = require("../../shared/gameStateManager");
const config           = require("../../shared/configuration/config");

/** The manager of state to use for processing requests. */
const gameStateManager = new GameStateManager(config.getSetting(config.storageConnectionStringName), config.getSetting(config.storageContainerName));

/**
 * Provides the API for resetting a game, including all registered devices.
 *     - Path: /api/game
 *     - Method: DELETE
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } request  The incoming HTTP request being processed
 *
 */
module.exports = async function resetGame(context, request) {
    context.log("Reset Game function processed a request.");

    // useless test code
    const state = await gameStateManager.resetGameAsync();

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : state
    };

    // NOTE: Due to a bug in the Azure Functions host, because this function returns a promise, the response should be returned directly
    //       as the runtime will attempt to resolve the promise.
    //
    //       Calling "done" will result in an error message written to the console
    //       about calling "done" twice.  This is a known issue with using "async" and is due to be fixed in a forthcoming release.
    //       Function execution is not impacted; a graceful validation in the runtime is erroneously tripped.
    //
    //       See: https://github.com/Azure/azure-functions-nodejs-worker/pull/99

    return response;
};