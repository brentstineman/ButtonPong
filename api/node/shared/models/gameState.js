const GameActivity = require("./gameActivity");

/**
 * The state of a game of Button Pong.
 * @class
 */
class GameState {

    /**
     * Creates an instance of the class, optionally overriding the default
     * attribute values.
     *
     * @param { object } attributes  The set of values for populating the instance attributes.
     */
    constructor(attributes = null) {
        this.activity          = GameActivity.Unknown;
        this.registeredDevices = {};
        this.pingsSent         = [];
        this.pongsReceived     = [];
        this.activeDevices     = [];
        this.activePing        = null;
        this.winningDeviceId   = null;

        if (attributes) {
            Object.assign(this, attributes);
        }
    }
}

module.exports = GameState;