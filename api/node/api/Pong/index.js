const GameActivity = require("../../shared/models/gameActivity");
const GameState    = require("../../shared/models/gameState");

const badRequestMessage = "Please pass the device identifier in the body of your request: { \"deviceId\" : \"value\" }";

/**
 * Validates that the HTTP Request body is correctly structured and
 * populated.
 *
 * @param { object } body  The HTTP request body to validate
 *
 * @returns { bool } true, if the body is valid; otherwise, false
 */
const validateRequestBody = body =>
    ((typeof(body) !== "undefined")       &&
     (body !== null)                      &&
     (typeof(body.deviceId) === "string") &&
     (body.deviceId !== null)             &&
     (body.deviceId.length > 0));

/**
 * Provides the API for registering a device "pong" response.
 *     - Path: /api/pong
 *     - Method: PUT
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } request  The incoming HTTP request being processed
 *
 */
module.exports = function pong(context, request) {
    context.log("Pong function processed a request.");

    // Verify that there was a valid game device received.

    if (!validateRequestBody(request.body)) {
        const response = {
            status  : 400,
            headers : { "Content-Type" : "application/json" },
            body    : { message : badRequestMessage }
        };

        context.done(null, response);
    }

    // useless test code
    const state = new GameState({ activity: GameActivity.NotStarted });

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : state
    };

    context.done(null, response);
};