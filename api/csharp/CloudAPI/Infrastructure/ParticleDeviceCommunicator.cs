using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudApi.Models;

namespace CloudApi.Infrastructure
{
    /// <summary>
    ///  Performs any necessary communication to the game devices to facilitate game play.
    /// </summary>
    /// 
    internal class ParticleDeviceCommunicator : IDeviceCommunicator
    {
        /// <summary>The general-purpose HTTP client used for device communication.</summary>
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        ///   Sends the event to signal devices that the game has started.
        /// </summary>
        /// 
        /// <param name="devices">The devices to which the event should be communicated.</param>
        ///         
        public Task SendStartEventAsync(IEnumerable<GameDevice> devices)
        {
            return Task.WhenAll(devices.Select(device => 
            {
                var task = ParticleDeviceCommunicator.InvokeWebHook(device, "startGame");
                task.ConfigureAwait(false);

                return task;
            }));
        }

        /// <summary>
        ///   Sends the event to signal devices that the game has ended.
        /// </summary>
        /// 
        /// <param name="devices">The devices to which the event should be communicated.</param>
        ///         
        public Task SendEndEventAsync(IEnumerable<GameDevice> devices)
        {
            return Task.WhenAll(devices.Select(device => 
            {
                var task = ParticleDeviceCommunicator.InvokeWebHook(device, "endGame");
                task.ConfigureAwait(false);

                return task;
            }));
        }

        /// <summary>
        ///   Sends the event to signal a devices that it was chosen to as the active ping.
        /// </summary>
        /// 
        /// <param name="device">The device to which the event should be communicated.</param>
        /// <param name="timeoutSeconds">The duration of the timeout for the ping, in seconds.</param>
        ///   
        public Task SendPingEventAsync(GameDevice device,
                                       int        timeoutSeconds)
        {
            return ParticleDeviceCommunicator.InvokeWebHook(device, "ping", TimeSpan.FromSeconds(timeoutSeconds).TotalMilliseconds.ToString());
        }

        /// <summary>
        ///   Sends the event to signal a devices that it was eliminated from the game.
        /// </summary>
        /// 
        /// <param name="device">The device to which the event should be communicated.</param>
        ///  
        public Task SendEliminatedEventAsync(GameDevice device)
        {
            return ParticleDeviceCommunicator.InvokeWebHook(device, "eliminated");
        }

        /// <summary>
        ///   Sends the event to signal a devices that it was chosen to as the game winner.
        /// </summary>
        /// 
        /// <param name="device">The device to which the event should be communicated.</param>
        ///  
        public Task SendWinEventAsync(GameDevice device)
        {
            return ParticleDeviceCommunicator.InvokeWebHook(device, "winner");
        }
                
        /// <summary>
        ///  Invokes the requested webhook for the the specified device.
        /// </summary>
        /// 
        /// <param name="device">The device to to invoke the web hook for.</param>
        /// <param name="function">The name of the web hook function to invoke.</param>
        /// <param name="requestBody">The body of the request to send as the payload.</param>
        ///         
        private static async Task InvokeWebHook(GameDevice device, 
                                                string     function, 
                                                string     requestBody = null)
        {                      $"https://api.particle.io/v1/devices/${device}/${operation}?access_token=${token}"
            var uri  = new Uri($"https://api.particle.io/v1/devices/{device.DeviceId}/{function}?access_token={device.AccessToken}");
            var body = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("args", requestBody) });

            // In order to ensure that the static HttpClient sees DNS updates, the connection for each URI must be set to
            // expire; the default behavior is for it to be cached within the client until it is disposed.  Change the connection
            // lease to expire after 60 seconds to ensure that DNS changes are detected.

            try { ServicePointManager.FindServicePoint(uri).ConnectionLeaseTimeout = (60 * 1000); } catch {}

            // Invoke the web hook and ensure that the response was successful; a failed response will throw an exception.

            var response = await ParticleDeviceCommunicator.httpClient.PostAsync(uri, body).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }   
}
