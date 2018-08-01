using System;
using System.Net.Http;
using System.Threading.Tasks;
using CloudApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace CloudApi
{
    /// <summary>
    ///   Provides the API for resetting a game, including all registered devices.
    /// </summary>
    /// 
    public static class ResetGame
    {
        /// <summary>The manager of state to use for processing requests.</summary>
        private static GameStateManager stateManager = new GameStateManager(
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageConnectionString), 
            Environment.GetEnvironmentVariable(ConfigurationNames.StateStorageContainer));

        /// <summary>
        ///   Exposes the function API.
        /// </summary>
        /// 
        /// <param name="request">The incoming HTTP request.</param>
        /// <param name="logger">The logger to use for writing log information.</param>
        /// 
        /// <returns>The action result detailing the HTTP response.</returns>
        /// 
        [FunctionName(nameof(ResetGame))]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "DELETE", Route = "game")]HttpRequestMessage request, ILogger logger)
        {
            logger.LogInformation($"{ nameof(ResetGame) } processed a request.");

            var gameState = await ResetGame.stateManager.ResetGameAsync();

            return new OkObjectResult(gameState);
        }
    }
}
