using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CloudApi.Models
{
    /// <summary>
    ///   The state of a device, with respect to a game of Button Pong.
    /// </summary>
    /// 
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeviceState
    {
        /// <summary>The state of the device cannot be determined.</summary>
        Unknown,

        /// <summary>The device was registered for the game, and is still an active participant.</summary>
        RegisteredActive,

        /// <summary>The device was registered for the game, but has been eliminated an is inactive.</summary>
        RegisteredInactive,

        /// <summary>The device was not registered for the game.</summary>
        NotInGame
    }
}
