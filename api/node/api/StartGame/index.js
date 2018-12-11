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

    const [ started, startState ] = await gameStateManager.startGameAsync();

    // If the game was not started, the state failed validation.

    if (!started) {
        return {
            status  : 409,
            headers : { "Content-Type" : "application/json" },
            body    : startState
        };
    }

    // Signal Devices that the game has started.

    communicator
        .sendStartEventAsync(startState.registeredDevices)
        .catch(error => context.log(`An error occurred signaling all registered devices that the game has started.  Details: [${ util.inspect(error) }]`));

    const [ newPing, expiredPing, currentState ] = await gameStateManager.manageActivePing(maxPingAgeSeconds);

    // If there was a device eliminated, then signal it.

    if (expiredPing) {
        communicator
            .sendEliminatedEventAsync(currentState.registeredDevices[expiredPing.deviceId])
            .catch(error => context.log(`An error occurred signaling device: ${ expiredPing.deviceId } of its elimination.  Details: [${ util.inspect(error) }]`));

        context.log(`A request to Start Game has expired a ping for device: ${ expiredPing.deviceId } from event time: ${ expiredPing.eventTimeUtc }.`);
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

        context.log(`A request to Start Game has marked the game complete.  Device: ${ currentState.winningDeviceId } was named the winner.`);
    }

    // If a new ping was generated, signal the associated device.

    if ((newPing) && (currentState.activeDevices.includes(newPing.deviceId))) {
        communicator
            .sendPingEventAsync(currentState.registeredDevices[newPing.deviceId], pingTimeout)
            .catch(error => context.log(`An error occurred signaling device: ${ newPing.deviceId } of being pinged.  Details: [${ util.inspect(error) }]`));

        context.log(`A request to Start Game has triggered the following ping: [${ newPing.deviceId } // ${ newPing.eventTimeUtc }]`);
    }

    // The game has been started and initial ping management performed; return the current state of the game.

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