namespace CloudApi.Models
{
    /// <summary>
    ///   An internet button device that may participate in a game of
    ///   Button Pong.
    /// </summary>
    /// 
    public class GameDevice
    {
        /// <summary>The unique identifier of the device.</summary>
        public string DeviceId;

        /// <summary>The access token to be used for communicating with the device for game operations.</summary>
        public string AccessToken;
    }
}
