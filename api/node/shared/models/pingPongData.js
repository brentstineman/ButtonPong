/**
 * The state of a game of Button Pong.
 * @class
 */
class PingPongData {

    /**
     * Creates an instance of the class, optionally overriding the default
     * attribute values.
     *
     * @param { object } attributes  The set of values for populating the instance attributes.
     */
    constructor(attributes = null) {
        this.deviceId     = null;
        this.eventTimeUtc = null;

        if (attributes) {
            Object.assign(this, attributes);
        }
    }

    /**
     * Determines if another object is structurally equivalent to the current PingPongData
     * instance.
     * @method
     *
     * @param { object } other  The object instance to compare for equality.
     *
     * @returns { bool } true if other is structurally equal to the current instance; otherwise, false.
     */
    isEqualTo(other) {

        if (other === null) {
            return false;
        }

        let result = true;

       this.getOwnPropertyNames().forEach(member => {
            if ((!other.hasOwnProperty(member)) || (this[member] !== other[member])) {
                result = false;
                return;
            }
        });

        return result;
    }
}

module.exports = PingPongData;