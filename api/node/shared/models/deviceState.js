/**
 * The state of a device, with respect to a game of Button Pong.
 * @readonly
 * @enum { string }
 */
const DeviceState = Object.freeze({
    /** TThe state of the device cannot be determined. */
    Unknown : "Unknown",

    /** The device was registered for the game, and is still an active participant. */
    RegisteredActive : "RegisteredActive",

    /** The device was registered for the game, but has been eliminated an is inactive. */
    RegisteredInactive : "RegisteredInactive",

    /** The device was not registered for the game. */
    NotInGame : "NotInGame"
});

module.exports = DeviceState;