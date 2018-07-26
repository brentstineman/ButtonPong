const GameActivity = require("../../shared/models/gameActivity");
const GameState    = require("../../shared/models/gameState");
const GameDevice   = require("../../shared/models/gameDevice");

/**
 * Provides the API for retrieving a game status.
 *     - Path: /api/game
 *     - Method: GET
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } request  The incoming HTTP request being processed
 *
 */
module.exports = function getStatus(context, request) {
    context.log("Get Status function processed a request.");

    // useless test code
    const state = new GameState({ activity: GameActivity.NotStarted });
    state.registeredDevices["123"] = new GameDevice({ deviceId: "123", accessToken: "ABC" });
    state.activeDevices.push(state.registeredDevices["123"].deviceId);
    state.activeDevices.push("456");

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : state
    };

    context.done(null, response);
};