/**
 * An internet button device that may participate in a game of
 * Button Pong.
 * @class
 */
class GameDevice {

    /**
     * Creates an instance of the class, optionally overriding the default
     * attribute values.
     *
     * @param { object } attributes  The set of values for populating the instance attributes.
     */
    constructor(attributes = null) {
        this.deviceId    = null;
        this.accessToken = null;

        if (attributes) {
            Object.assign(this, attributes);
        }
    }
}

module.exports = GameDevice;