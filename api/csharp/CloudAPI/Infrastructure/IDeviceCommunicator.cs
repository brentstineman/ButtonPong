using System.Collections.Generic;
using System.Threading.Tasks;
using CloudApi.Models;

namespace CloudApi.Infrastructure
{
    /// <summary>
    ///   Defines the contract to be fulilled by device communicators.
    /// </summary>
    /// 
    public interface IDeviceCommunicator
    {
        /// <summary>
        ///   Sends the event to signal devices that the game has started.
        /// </summary>
        /// 
        /// <param name="devices">The devices to which the event should be communicated.</param>
        ///         
        Task SendStartEventAsync(IEnumerable<GameDevice> devices);

        /// <summary>
        ///   Sends the event to signal devices that the game has ended.
        /// </summary>
        /// 
        /// <param name="devices">The devices to which the event should be communicated.</param>
        ///         
        Task SendEndEventAsync(IEnumerable<GameDevice> devices);
        
        /// <summary>
        ///   Sends the event to signal a devices that it was chosen to as the active ping.
        /// </summary>
        /// 
        /// <param name="device">The device to which the event should be communicated.</param>
        /// <param name="timeoutSeconds">The duration of the timeout for the ping, in seconds.</param>
        ///   
        Task SendPingEventAsync(GameDevice device,
                                int        timeoutSeconds);

        /// <summary>
        ///   Sends the event to signal a devices that it was eliminated from the game.
        /// </summary>
        /// 
        /// <param name="device">The device to which the event should be communicated.</param>
        ///  
        Task SendEliminatedEventAsync(GameDevice device);

        /// <summary>
        ///   Sends the event to signal a devices that it was chosen to as the game winner.
        /// </summary>
        /// 
        /// <param name="device">The device to which the event should be communicated.</param>
        ///  
        Task SendWinEventAsync(GameDevice device);
    }
}
