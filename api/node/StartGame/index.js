const GameActivity = require("../shared/models/gameActivity");
const GameState    = require("../shared/models/gameState");

/**
 * Provides the API for starting a game, if it is in the proper state to do so.
 *     - Path: /api/game
 *     - Method: PUT
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } request  The incoming HTTP request being processed
 *
 */
module.exports = function startGame(context, request) {
    context.log("Start Game function processed a request.");

    // useless test code
    const state = new GameState({ activity: GameActivity.NotStarted });

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : state
    };

    context.done(null, response);
};