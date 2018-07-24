namespace CloudApi.Infrastructure
{
    /// <summary>
    ///   The set of names for the application's configuration values.
    /// </summary>
    /// 
    internal static class ConfigurationNames
    {
        /// <summary>The name of the configuration key for the connection string to game state storage;</summary>
        public const string StateStorageConnectionString  = "storageConnString";

        /// <summary>The name of the configuration key for the storage container that holds game state.</summary>
        public const string StateStorageContainer  = "storageContainer";

        /// <summary>The name of the configuration key for the maximum age, in seconds, of a ping before expiration.</summary>
        public const string PingMaxAgeSeconds = "pingMaxAgeSeconds";

        /// <summary>The amount of time, in seconds, that a device has to respond to a ping event.</summary>
        public const string PingTimeout = "pingTimeoutSeconds";

        /// <summary>The CRON expression to use for the timer associated with the ping manager.</summary>
        public const string PingManagerSchedule = "pingManagerSchedule";

    }
}
