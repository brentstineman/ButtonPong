using System;
using System.Net;
using System.Net.Http;
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
    ///   Provides the API for initiating the start of a game.
    /// </summary>
    /// 
    public static class StartGame
    {
        /// <summary>The manager of state to use for processing requests.</summary>
        private static GameStateManager stateManager = new GameStateManager(
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageConnectionString), 
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageContainer));

        /// <summary>The maximum age of a ping before it expires.</summary>
        private static TimeSpan maxPingAge = TimeSpan.FromSeconds(
            Int32.Parse(Environment.GetEnvironmentVariable(ConfigurationNames.PingMaxAgeSeconds)));

        /// <summary>The amount of time that a user should be given to respond to a ping on the device.</summary>
        private static int pingTimeout = 
            Int32.Parse(Environment.GetEnvironmentVariable(ConfigurationNames.PingTimeout));

        /// <summary>The communicator to use for interacting with the game devices.</summary>
        private static IDeviceCommunicator communicator = new ParticleDeviceCommunicator();

        ///   Exposes the function API.
        /// </summary>
        /// 
        /// <param name="request">The incoming HTTP request.</param>
        /// <param name="logger">The logger to use for writing log information.</param>
        /// 
        /// <returns>The action result detailing the HTTP response.</returns>
        /// 
        [FunctionName(nameof(StartGame))]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "PUT", Route = "game")]HttpRequestMessage request, 
                                                     ILogger logger)
        {
            logger.LogInformation($"{ nameof(StartGame) } processed a request.");

            // Attempt to start the game, validating the current state meets the required
            // conditions.

            var (started, startState) = await StartGame.stateManager.StartGameAsync();

            // If the game was not started, the state failed validation. 
             
            if (!started)               
            { 
                return new ObjectResult(startState) 
                { 
                    StatusCode = (int)HttpStatusCode.Conflict 
                };
            }

            // Signal devices that the game has started.

            StartGame.communicator
                .SendStartEventAsync(startState.RegisteredDevices.Values)
                .FireAndForget(exception => logger.LogError(exception, "An exception occurred signaling all registered devices that the game has started."));

            // Manage active pings in an effort to trigger the first ping of the game.  Note that this may have happend concurrently
            // so all ping outcomes should be considered.

            var (newPing, expired, currentState) = await StartGame.stateManager.ManageActivePing(StartGame.maxPingAge);

            // If there was a device eliminated, then signal it.

            if (((expired != null) && (currentState.RegisteredDevices.ContainsKey(expired.DeviceId ?? String.Empty))))
            {
                StartGame.communicator
                    .SendEliminatedEventAsync(currentState.RegisteredDevices[expired.DeviceId])
                    .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { expired.DeviceId } of its elimination"));
            }

            // If the game was completed when managing the active ping, signal completion and the winner.  If 
            // the game was already complete when the pong was sent, then another function invocation completed it and
            // has responsibility for device communication.

            if ((currentState.Activity == GameActivity.Complete) && (currentState.Activity != startState.Activity))
            {
                StartGame.communicator
                    .SendEndEventAsync(currentState.RegisteredDevices.Values)
                    .FireAndForget(exception => logger.LogError(exception, "An exception occurred signaling all registered devices that the game has ended."));

                if (currentState.RegisteredDevices.ContainsKey(currentState.WinningDeviceId))
                {
                    StartGame.communicator
                        .SendWinEventAsync(currentState.RegisteredDevices[currentState.WinningDeviceId])
                        .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { currentState.WinningDeviceId } that it has won."));
               }
            }

            // If a new ping was generated, signal the associated device.

            if ((newPing != null) && (currentState.ActiveDevices.Contains(newPing.DeviceId ?? String.Empty)))
            {
                StartGame.communicator
                    .SendPingEventAsync(currentState.RegisteredDevices[newPing.DeviceId], StartGame.pingTimeout)
                    .FireAndForget(exception => logger.LogError(exception, $"An exception occurred signaling device: { expired.DeviceId } of being pinged."));
            }

            return new OkObjectResult(currentState);
        }
    }
}
