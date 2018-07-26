const GameDevice   = require("../shared/models/gameDevice");
const DeviceState  = require("../shared/models/deviceState");
const DeviceStatus = require("../shared/models/deviceStatus");

const badRequestMessage = "Please pass the device identifier and access token in the body of your request: { \"deviceId\" : \"value\", \"accesstoken\" : \"value\"}";
const templateDevice    = new GameDevice();

/**
 * Validates that the HTTP Request body is a correctly structured and
 * populated GameDevice.
 *
 * @param { object } body  The HTTP request body to validate
 *
 * @returns { bool } true, if the body is valid; otherwise, false
 */
const validateRequestBody = body => {
    if ((typeof(body) === "undefined") || (body === null)) {
        return false;
    }

    let x = Object.getOwnPropertyNames(templateDevice);
    let y=  Object.keys(templateObject);

debugger;
    for (let attribute in Object.getOwnPropertyNames(templateDevice)) {
        if ((!body.hasOwnProperty(attribute))      ||
            (typeof(body[attribute]) === "string") ||
            (body[attribute].length > 0)) {

            return false;
        }
    }

    return true;
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
module.exports = function registerDevice(context, request) {
    context.log("Register Device function processed a request.");

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
    const status = new DeviceStatus({ device: request.body, status: DeviceState.RegisteredActive });

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : status
    };

    context.done(null, response);
};