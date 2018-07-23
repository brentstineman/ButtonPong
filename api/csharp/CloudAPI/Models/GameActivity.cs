using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CloudApi.Models
{
    /// <summary>
    ///   An activity taking place in the game; this equates to the overall state of
    ///   the game as it progresses through its lifecycle.
    /// </summary>
    /// 
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GameActivity
    {
        /// <summary>The activity cannot be determined.</summary>
        Unknown,

        /// <summary>The game has not yet started; this is considered a pre-game phase where devices can join and leave.</summary>
        NotStarted,

        /// <summary>The game is currently taking place; participants may no longer join and leave.</summary>
        InProgress,

        /// <summary>The game is complete and a winner was determined.  It may be restarted using the same device participants.</summary>
        Complete
    }
}
