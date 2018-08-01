using System;
using System.Threading.Tasks;
using CloudApi.Infrastructure;
using CloudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace CloudApi
{
    /// <summary>
    ///   Provides the API for registering a device "pong" response.
    /// </summary>
    /// 
    public static class Pong
    {
        /// <summary>The manager of state to use for processing requests.</summary>
        private static GameStateManager stateManager = new GameStateManager(
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageConnectionString), 
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageContainer));

        /// <summary>The maximum age of a ping before it expires.</summary>
        private static TimeSpan maxPingAge = TimeSpan.FromSeconds(
            Int32.Parse(Environment.GetEnvironmentVariable(ConfigurationNames.PingMaxAgeSeconds)));

        /// <summary>The maximum age of a ping before it expires.</summary>
        private static int pingTimeout = 
            Int32.Parse(Environment.GetEnvironmentVariable(ConfigurationNames.PingTimeout));

        /// <summary>The communicator to use for interacting with the game devices..</summary>
        private static IDeviceCommunicator communicator = new ParticleDeviceCommunicator();
    
        /// <summary>
        ///   Exposes the function API.
        /// </summary>
        /// 
        /// <param name="device">The device sending the "pong" response.</param>
        /// <param name="logger">The logger to use for writing log information.</param>
        /// 
        /// <returns>The action result detailing the HTTP response.</returns>
        /// 
        [FunctionName(nameof(Pong))]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "PUT", Route = "pong")]GameDevice device, 
                                                    ILogger logger)
        {
            logger.LogInformation($"{ nameof(Pong) } processed a request.");

            if (String.IsNullOrEmpty(device?.DeviceId))
            {
                return new BadRequestObjectResult(new ErrorMessage(@"Please pass the device identifier and a success indicator in the body of your request: { ""deviceId"" : ""value"", ""success"" : ""value""}"));
            }

            // Attempt to register the pong, which may or may not be accepted depending on the curent state.  If the pong was not accepted, then
            // take no further action.   

            var (pongAccepted, pongState) = await Pong.stateManager.RecordPong(device.DeviceId, Pong.maxPingAge);

            if (!pongAccepted)
            {
                return new OkObjectResult(pongState);
            }

            // If the pong was accepted, manage the active ping.  This will allow the device sending the pong to update
            //  the status and then perform any tasks needed to generate a new ping or determine the game winner.
            //
            // NOTE: The Pong and ManageActivePing operations are distinct and not performed atomically; it is possible 
            //       for another caller to have already performed the ping management between the two calls, by design.

            var (newPing, expired, currentState) = await Pong.stateManager.ManageActivePing(Pong.maxPingAge);

            // If there was a device eliminated, then signal it.

            if (((expired != null) && (currentState.RegisteredDevices.ContainsKey(expired.DeviceId ?? String.Empty))))
            {
                logger.LogInformation($"{ nameof(Pong) } signaling elimination to { expired?.DeviceId } at: { DateTime.Now }.");

                Pong.communicator
                    .SendEliminatedEventAsync(currentState.RegisteredDevices[expired.DeviceId])
                    .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { expired.DeviceId } of its elimination"));
            }

            // If the game was completed when managing the active ping, signal completion and the winner.  If 
            // the game was already complete when the pong was sent, then another function invocation completed it and
            // has responsibility for device communication.

            if ((currentState.Activity == GameActivity.Complete) && (currentState.Activity != pongState.Activity))
            {
                logger.LogInformation($"{ nameof(Pong) } signaling game complete at: { DateTime.Now }.");

                Pong.communicator
                    .SendEndEventAsync(currentState.RegisteredDevices.Values)
                    .FireAndForget(exception => logger.LogError(exception, "An exception occurred signaling all registered devices that the game has ended."));

                if (currentState.RegisteredDevices.ContainsKey(currentState.WinningDeviceId))
                {
                    Pong.communicator
                        .SendWinEventAsync(currentState.RegisteredDevices[currentState.WinningDeviceId])
                        .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { currentState.WinningDeviceId } that it has won."));
               }
            }

            // If a new ping was generated, signal the associated device.

            if ((newPing != null) && (currentState.ActiveDevices.Contains(newPing?.DeviceId ?? String.Empty)))
            {
                logger.LogInformation($"{ nameof(Pong) } sending ping to { newPing?.DeviceId } at: { DateTime.Now }.");

                Pong.communicator
                    .SendPingEventAsync(currentState.RegisteredDevices[newPing.DeviceId], Pong.pingTimeout)
                    .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { expired.DeviceId } of its elimination"));
            }

            return new OkObjectResult(currentState);
        }
    }
}
