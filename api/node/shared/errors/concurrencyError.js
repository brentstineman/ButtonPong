/**
 * An error that represents a concurrency violation when managing the
 * game state.
 * @class
 */
class ConcurrencyError extends Error {

    /**
     * Creates an instance of the class, optionally providing a message to associate with the scenario.
     *
     * @param { string } message         The message to associate with the exception scenario.
     * @param { object } innerException  The exception that indicated the source of the concurrency violation.
     */
    constructor (message        = null,
                 innerException = null) {
        super(message);

        this.name           = "ConcurrencyError";
        this.innerException = innerException;
    }
};