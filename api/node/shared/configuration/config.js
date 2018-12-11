/** The name of the configuration setting that holds the connection string to the Azure Storage account where game state is held. */
const storageConnectionStringName = "storageConnString";

/** The name of the configuration setting that holds the name of the Azure Storage blob container where game state is held. */
const storageContainerName = "storageContainer";

/** The name of the configuration setting that holds the value of the maximum age of an active ping, in seconds. */
const pingMaxAgeName = "pingMaxAgeSeconds";

/** The name of the configuration setting that holds the value of the timeout for a button to respond to an active ping, in seconds. */
const pingTimeoutName = "pingTimeoutSeconds";

/** The name of the Particle device web hook URI template string. */
const deviceWebHookUriTemplateName = "deviceWebHookUriTemplateName";

/**
 * Reads the requested configuration setting from the local settings file or from the
 * runtime environment.
 * @static
 *
 * @param { string } settingName  The name of the setting to read.
 *
 * @returns { string }  The value of the setting, if found; otherwise, null.
 */
const getSetting = settingName => process.env[settingName];

module.exports = {
    storageConnectionStringName,
    storageContainerName,
    pingMaxAgeName,
    pingTimeoutName,
    deviceWebHookUriTemplateName,
    getSetting
};
