/**
 * Provides the API for managing "pings" for a game.
 *     - Path:
 *     - Method: Timer
 *
 * @param { object } context  The Azure Function execution context
 * @param { object } timer    The timer state at the time that scheduled execution was triggered
 *
 */
module.exports = function pong(context, timer) {
    context.log("Ping Manager execution was triggered.");

    // useless test code
    console.log(timer);

    context.done();
};