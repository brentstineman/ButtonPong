using System.Collections.Generic;

namespace CloudApi.Models
{
    /// <summary>
    ///   The state of a game of Button Pong.
    /// </summary>
    /// 
    public class GameState
    {
        /// <summary>The current activity taking place in the game.</summary>        
        public GameActivity Activity;

        /// <summary>The set of devices registered for the game.</summary>
        public Dictionary<string, GameDevice> RegisteredDevices;
        
        /// <summary>The set of devices that have not been eliminated and are active in the game.</summary>
        public HashSet<string> ActiveDevices;

        /// <summary>The pings that have been sent to devices as part of the game.</summary>
        public List<PingPongData> PingsSent;

        /// <summary>The pongs received from devices in response to a game ping.</summary>
        public List<PingPongData> PongsReceived;

        /// <summary>The currently active ping awaiting a pong response, if any; otherwise, <c>null</c>.</summary>
        public PingPongData ActivePing;

        /// <summary>The identifier of the device that won the game.</summary>
        public string WinningDeviceId;

        /// <summary>
        ///   Initializes a new instance of the <see cref="GameState"/> class.
        /// </summary>
        /// 
        /// <param name="activity">The activity considered current for the game.</param>
        /// 
        public GameState(GameActivity activity = GameActivity.Unknown)
        {
            this.Activity          = activity;
            this.RegisteredDevices = new Dictionary<string, GameDevice>();
            this.ActiveDevices     = new HashSet<string>();            
            this.PingsSent         = new List<PingPongData>();
            this.PongsReceived     = new List<PingPongData>();
            this.ActivePing        = null;
            this.WinningDeviceId   = null;
        }
    }
}
