const axios = require("axios");
const qs    = require("querystring");

/**
 * Serves as an enumeration for the well-known web hook operations that can
 * be invoked.
 */
const WebHookOperations = {
    /** Signal that the game is starting. */
    startGame : "startGame",

    /** Signal that the game is ending. */
    endGame : "endGame",

    /** Signal a device that it holds the active ping. */
    ping : "ping",

    /** Signal a device that it has been eliminated. */
    eliminated : "eliminated",

    /** Signal a device that it has won the game. */
    gameWinner : "winner"
};

/**
 * Parses a string as an ES6 template, evaluating it against a given set of parameters.
 * @static
 * @private
 *
 * @param { string } templateString  The string to evaluate as a template.
 * @param { object } params          The set of parameters to evaluate the template against; the names of the attributes must match those in the string.
 *
 * @returns { string }  The string resulting from the template evaluation.
 *
 * NOTE: The template string has the same restrictions as an actual EcmaScript template; it
 *       cannot contain EcmaScript reserve words, such as "function".
 */
const parseStringAsTemplate = (templateString, params) => {
    const names = Object.keys(params);
    const vals  = Object.values(params);

    return new Function(...names, `return \`${ templateString }\`;`)(...vals);
}

/**
 * Invokes the requested webhook for the the specified device and operation.
 * @static
 * @private
 *
 * @param { string } urlTemplate  The template to use for building the web hook URL.
 * @param { string } operation    The name of the device operation being invoked.
 * @param { object } gameDevice   The game device that is the target of the web hook invocation.
 * @param { object } requestBody  The request body to send for the operation.  If not provided, no body will be sent.
 *
 * @returns { object }  A promise that represents the HTTP request.
 */
const invokeWebHookAsync = (urlTemplate, operation, gameDevice, requestBody = null) => {
    const uri = parseStringAsTemplate(urlTemplate, {
        device    : gameDevice.deviceId,
        operation : operation,
        token     : gameDevice.accessToken
    });

    return axios.post(uri, qs.stringify(requestBody));
};

/**
 * Performs any necessary communication to the game devices to facilitate game play.
 * @class
 */
class ParticleDeviceCommunicator {

    /**
     * Creates an instance of the class, optionally overriding the default
     * attribute values.
     * @constructor
     *
     * @param { string } deviceWebHookUriTemplate  A string specifying the URI of the Particle device web hook, in ES6 template string format.  "http://www.here.com/things/${ thing }".
     */
    constructor(deviceWebHookUriTemplate) {

        if ((typeof(deviceWebHookUriTemplate) !== "string") ||
            (deviceWebHookUriTemplate === null)             ||
            (deviceWebHookUriTemplate.length <= 0)) {

            throw new Error("deviceWebHookUriTemplate must be provided and be a valid URI that corresponds to the Particle Device web hook API.");
        }

        // Initialize the class read-only attributes.

        Object.defineProperties(this, {

            deviceWebHookUriTemplate : {
                value        : deviceWebHookUriTemplate,
                writable     : false,
                enumerable   : false,
                configurable : false
            }
        });
    }

    /**
     * Sends the event to signal devices that the game has started.
     *
     * @param { array } gameDevices The set of game device models that should be notified that the game is starting.
     */
    sendStartEventAsync(gameDevices) {
        let invocations = [];

        if (gameDevices) {
            invocations = Object.keys(gameDevices).map(deviceId => invokeWebHookAsync(this.deviceWebHookUriTemplate, WebHookOperations.startGame, gameDevices[deviceId]));
        }

        return axios.all(invocations);
    }

     /**
     * Sends the event to signal devices that the game has ended.
     *
     * @param { array } gameDevices The set of game device models that should be notified that the game is ending.
     */
    sendEndEventAsync(gameDevices) {
        let invocations = [];

        if (gameDevices) {
            invocations = Object.keys(gameDevices).map(deviceId => invokeWebHookAsync(this.deviceWebHookUriTemplate, WebHookOperations.endGame, gameDevices[deviceId]));
        }

        return axios.all(invocations);
    }

    /**
     * Sends the event to signal a device that it was chosen as the active ping.
     *
     * @param { object } gameDevice   The game device models that should be notified.
     * @param { number } pingTimeout  The amount of time, in seconds, that a user should be given to respond to a ping on the device.
     */
    sendPingEventAsync(gameDevice,
                       pingTimeout) {
        return invokeWebHookAsync(this.deviceWebHookUriTemplate, WebHookOperations.ping, gameDevice, parseInt((pingTimeout * 1000), 10));
    }

    /**
     * Sends the event to signal a device that it was eliminated from the game.
     *
     * @param { object } gameDevice The game device models that should be notified.
     */
    sendEliminatedEventAsync(gameDevice) {
        return invokeWebHookAsync(this.deviceWebHookUriTemplate, WebHookOperations.eliminated, gameDevice);
    }

    /**
     * Sends the event to signal a device that it was chosen as the winner of the game.
     *
     * @param { object } gameDevice The game device models that should be notified.
     */
    sendWinEventAsync(gameDevice) {
        return invokeWebHookAsync(this.deviceWebHookUriTemplate, WebHookOperations.gameWinner, gameDevice);
    }
}

module.exports = ParticleDeviceCommunicator;