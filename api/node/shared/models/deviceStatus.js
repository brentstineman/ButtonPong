/**
 * The status of a device, normally occurring during a registration-related operation.
 * @class
 */
class DeviceStatus {

    /**
     * Creates an instance of the class, optionally overriding the default
     * attribute values.
     *
     * @param { object } attributes  The set of values for populating the instance attributes.
     */
    constructor(attributes = null) {
        this.device = null;
        this.status = null;

        if (attributes) {
            Object.assign(this, attributes);
        }
    }
}

module.exports = DeviceStatus;