const GameStateManager = require("../../shared/gameStateManager");

/**
 * Provides the API for retrieving a game status.
 *     - Path: /api/game
 *     - Method: GET
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } request  The incoming HTTP request being processed
 *
 */
module.exports = async function getStatus(context, request) {
    context.log("Get Status function processed a request.");

    const thing = new GameStateManager("DefaultEndpointsProtocol=https;AccountName=squirepong;AccountKey=Yu5mlOScPPhHmt830ms1EPp+NuA3E0rQ8a0+34311XKxuAycko5N6F320HB6Lhl1Ugw56iIBlP6C913CE4z6sA==;EndpointSuffix=core.windows.net", "jesselocal");

    const gameState = await thing.getGameStateAsync();

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : gameState || { state: "Nothing" }
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
}