/**
 * Serves as an enumeration for the well-known activities that a
 * game of Button Pong consists of.
 * @readonly
 * @enum { string }
 */
const GameActivity = Object.freeze({
    /** The activity cannot be determined. */
    Unknown : "Unknown",

    /** The game has not yet started; this is considered a pre-game phase where devices can join and leave. */
    NotStarted : "NotStarted",

    /** The game is currently taking place; participants may no longer join and leave. */
    InProgress : "InProgress",

    /** The game is complete and a winner was determined.  It may be restarted using the same device participants. */
    Complete : "Complete"
});

module.exports = GameActivity;