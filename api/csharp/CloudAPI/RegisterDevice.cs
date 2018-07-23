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
    ///   Provides the API for registering a device with the game.
    /// </summary>
    /// 
    public static class RegisterDevice
    {
        /// <summary>The manager of state to use for processing requests.</summary>
        private static GameStateManager stateManager = new GameStateManager(
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageConnectionString), 
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageContainer));

        /// <summary>
        ///   Exposes the function API.
        /// </summary>
        /// 
        /// <param name="device">The device to be registered.</param>
        /// <param name="logger">The logger to use for writing log information.</param>
        /// 
        /// <returns>The action result detailing the HTTP response.</returns>
        /// 
        [FunctionName(nameof(RegisterDevice))]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "devices")]GameDevice device, 
                                                    ILogger logger)
        {
            logger.LogInformation($"{ nameof(RegisterDevice) } processed a request.");
                        
            if ((String.IsNullOrEmpty(device?.DeviceId)) || (String.IsNullOrEmpty(device?.AccessToken)))
            {
                return new BadRequestObjectResult(new ErrorMessage(@"Please pass the device identifier and access token in the body of your request: { ""deviceId"" : ""value"", ""accesstoken"" : ""value""}"));
            }

            var deviceState = await RegisterDevice.stateManager.RegisterDeviceAsync(device);

            return new OkObjectResult(new DeviceStatus(device, deviceState));
        }
    }
}
