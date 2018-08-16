const GameStateManager = require("../../shared/gameStateManager");
const GameDevice       = require("../../shared/models/gameDevice");
const DeviceStatus     = require("../../shared/models/deviceStatus");
const config           = require("../../shared/configuration/config");

/** The message to return for a falied request validation. */
const badRequestMessage = "Please pass the device identifier and access token in the body of your request: { \"deviceId\" : \"value\", \"accesstoken\" : \"value\"}";

/** The template game device instance to use for inspecting attributes. */
const templateDevice = new GameDevice();

/** The manager of state to use for processing requests. */
const gameStateManager = new GameStateManager(config.getSetting(config.storageConnectionStringName), config.getSetting(config.storageContainerName));

/**
 * Validates that the HTTP Request body is a correctly structured and
 * populated GameDevice.
 *
 * @param { object } body  The HTTP request body to validate
 *
 * @returns { bool } true, if the body is valid; otherwise, false
 */
const validateRequestBody = body => {
    if ((typeof(body) === "undefined") || (body === null))
    {
        return false;
    }

    let result = true;

    Object.getOwnPropertyNames(templateDevice).forEach(attribute => {

        if ((!body.hasOwnProperty(attribute))      ||
            (typeof(body[attribute]) !== "string") ||
            (body[attribute].length <= 0)) {

            result = false;
            return;
        }
    });

    return result;
};

/**
 * Provides the API for registering a device with the game.
 *     - Path: /api/devices
 *     - Method: POST
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } request  The incoming HTTP request being processed
 *
 */
module.exports = async function registerDevice(context, request) {
    context.log("Register Device function processed a request.");

    // Verify that there was a valid game device received.

    if (!validateRequestBody(request.body)) {
        return {
            status  : 400,
            headers : { "Content-Type" : "application/json" },
            body    : { message : badRequestMessage }
        };
    }

    // useless test code
    const status = await gameStateManager.registerDeviceAsync(request.body);

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : new DeviceStatus({ device : request.body, status : status })
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