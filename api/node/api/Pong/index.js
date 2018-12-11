const GameStateManager   = require("../../shared/gameStateManager");
const GameActivity       = require("../../shared/models/gameActivity");
const DeviceCommunicator = require("../../shared/particleDeviceCommunicator");
const config             = require("../../shared/configuration/config");
const util               = require("util");

/** The manager of state to use for processing requests. */
const gameStateManager = new GameStateManager(config.getSetting(config.storageConnectionStringName), config.getSetting(config.storageContainerName));

/** The communicator to use for interacting with the game devices. */
const communicator = new DeviceCommunicator(config.getSetting(config.deviceWebHookUriTemplateName));

/** The maximum age of a ping, in seconds, before it expires. */
const maxPingAgeSeconds = parseInt(config.getSetting(config.pingMaxAgeName), 10);

/** The amount of time, in seconds, that a user should be given to respond to a ping on the device. */
const pingTimeout = parseInt(config.getSetting(config.pingTimeoutName), 10);

/** The error message to return in the event that the request was malformed. */
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
    ((body)                               &&
     (body.deviceId)                      &&
     (typeof(body.deviceId) === "string") &&
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
module.exports = async function pong(context, request) {
    context.log("Pong function processed a request.");

    // Verify that there was a valid game device received.

    if (!validateRequestBody(request.body)) {
        return {
            status  : 400,
            headers : { "Content-Type" : "application/json" },
            body    : { message : badRequestMessage }
        };
    }

    // Attempt to register the pong, which may or may not be accepted depending on the curent state.  If the pong was not accepted, then
    // take no further action.

    const [ pongAccepted, pongState ] = await gameStateManager.recordPong(request.body.deviceId, maxPingAgeSeconds);

    if (!pongAccepted) {
        return {
            status  : 200,
            headers : { "Content-Type" : "application/json" },
            body    : pongState
        };
    }

    // If the pong was accepted, manage the active ping.  This will allow the device sending the pong to update
    //  the status and then perform any tasks needed to generate a new ping or determine the game winner.
    //
    // NOTE: The Pong and ManageActivePing operations are distinct and not performed atomically; it is possible
    //       for another caller to have already performed the ping management between the two calls, by design.

    const [ newPing, expiredPing, currentState ] = await gameStateManager.manageActivePing(maxPingAgeSeconds);

    // If there was a device eliminated, then signal it.

    if (expiredPing) {
        communicator
            .sendEliminatedEventAsync(currentState.registeredDevices[expiredPing.deviceId])
            .catch(error => context.log(`An error occurred signaling device: ${ expiredPing.deviceId } of its elimination.  Details: [${ util.inspect(error) }]`));

        context.log(`A request to Pong has expired a ping for device: ${ expiredPing.deviceId } from event time: ${ expiredPing.eventTimeUtc }.`);
    }

    // If the game was completed when managing the active ping, signal completion and the winner.  If
    // the game was already complete when the pong was sent, then another function invocation completed it and
    // has responsibility for device communication.

    if ((currentState.activity === GameActivity.Complete) && (currentState.activity !== startState.activity)) {
        communicator
            .sendEndEventAsync(currentState.registeredDevices)
            .catch(error => context.log(`An error occurred signaling all registered devices that the game has ended.  Details: [${ util.inspect(error) }]`));

        if (currentState.registeredDevices.hasOwnProperty(currentState.winningDeviceId)) {
            communicator
                .sendWinEventAsync(currentState.registeredDevices[currentState.winningDeviceId])
                .catch(error => context.log(`An error occurred signaling device: ${ currentState.winningDeviceId } that it has won.  Details: [${ util.inspect(error) }]`));
        }

        context.log(`A request to Pong has marked the game complete.  Device: ${ currentState.winningDeviceId } was named the winner.`);
    }

    // If a new ping was generated, signal the associated device.

    if ((newPing) && (currentState.activeDevices.includes(newPing.deviceId))) {
        communicator
            .sendPingEventAsync(currentState.registeredDevices[newPing.deviceId], pingTimeout)
            .catch(error => context.log(`An error occurred signaling device: ${ newPing.deviceId } of being pinged.  Details: [${ util.inspect(error) }]`));

        context.log(`A request to Pong has triggered the following ping: [${ newPing.deviceId } // ${ newPing.eventTimeUtc }]`);
    }

    const response = {
        status  : 200,
        headers : { "Content-Type" : "application/json" },
        body    : currentState
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