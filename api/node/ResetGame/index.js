const GameActivity = require("../shared/models/gameActivity");
const GameState    = require("../shared/models/gameState");

/**
 * Provides the API for resetting a game, including all registered devices.
 *     - Path: /api/game
 *     - Method: DELETE
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } request  The incoming HTTP request being processed
 *
 */
module.exports = function resetGame(context, request) {
    context.log("Reset Game function processed a request.");

    // useless test code
    const state = new GameState({ activity: GameActivity.NotStarted });

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : state
    };

    context.done(null, response);
};