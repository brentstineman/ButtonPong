const GameStateManager   = require("../../shared/gameStateManager");
const GameActivity       = require("../../shared/models/gameActivity");
const DeviceCommunicator = require("../../shared/particleDeviceCommunicator");
const config             = require("../../shared/configuration/config");
const moment             = require("moment");
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
 * Provides the API for managing "pings" for a game.
 *     - Path:
 *     - Method: Timer (scheduled execution)
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } timer    The timer state at the time that scheduled execution was triggered
 *
 */
module.exports = async function pingManager(context, timer) {
    context.log("Ping Manager execution was triggered.");

    // Get a snapshot of the state before the pings are managed to allow for short-circuiting if a game
    // is not underway.

    const initialState = await gameStateManager.getGameStateAsync();

    if ((initialState.activity !== GameActivity.InProgress) || (initialState.activeDevices.length === 0)) {
        context.log(`Ping Manager detected game is not running or has no active devices at: ${ moment().format() }.`);
        return;
    }

    // Manage the pings.

    const [ newPing, expiredPing, currentState ] = await gameStateManager.manageActivePing(maxPingAgeSeconds);

    // If there was a device eliminated, then signal it.

    if (expiredPing) {
        communicator
            .sendEliminatedEventAsync(currentState.registeredDevices[expiredPing.deviceId])
            .catch(error => context.log(`An error occurred signaling device: ${ expiredPing.deviceId } of its elimination.  Details: [${ util.inspect(error) }]`));

        context.log(`Ping Manager expired a ping for device: ${ expiredPing.deviceId } from event time: ${ expiredPing.eventTimeUtc }.`);
    }

    // If the game was completed when managing the active ping, signal completion and the winner.  If
    // the game was already complete when the pong was sent, then another function invocation completed it and
    // has responsibility for device communication.

    if ((currentState.activity === GameActivity.Complete) && (currentState.activity !== initialState.activity)) {
        communicator
            .sendEndEventAsync(currentState.registeredDevices)
            .catch(error => context.log(`An error occurred signaling all registered devices that the game has ended.  Details: [${ util.inspect(error) }]`));

        if (currentState.registeredDevices.hasOwnProperty(currentState.winningDeviceId)) {
            communicator
                .sendWinEventAsync(currentState.registeredDevices[currentState.winningDeviceId])
                .catch(error => context.log(`An error occurred signaling device: ${ currentState.winningDeviceId } that it has won.  Details: [${ util.inspect(error) }]`));
        }

        context.log(`Ping Manager is marking the game complete.  Device: ${ currentState.winningDeviceId } was named the winner.`);
    }

    // If a new ping was generated, signal the associated device.

    if ((newPing) && (currentState.activeDevices.includes(newPing.deviceId))) {
        communicator
            .sendPingEventAsync(currentState.registeredDevices[newPing.deviceId], pingTimeout)
            .catch(error => context.log(`An error occurred signaling device: ${ newPing.deviceId } of being pinged.  Details: [${ util.inspect(error) }]`));

        context.log(`Ping Manager is sending the following ping: [${ newPing.deviceId } // ${ newPing.eventTimeUtc }]`);
    }

    // NOTE: Due to a bug in the Azure Functions host, because this function returns a promise, it should just return directly
    //       as the runtime will attempt to resolve the promise.
    //
    //       Calling "done" will result in an error message written to the console
    //       about calling "done" twice.  This is a known issue with using "async" and is due to be fixed in a forthcoming release.
    //       Function execution is not impacted; a graceful validation in the runtime is erroneously tripped.
    //
    //       See: https://github.com/Azure/azure-functions-nodejs-worker/pull/99
};