/*
# Access granted under MIT Open Source License: https://en.wikipedia.org/wiki/MIT_License
#
# Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
# documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
# the rights to use, copy, modify, merge, publish, distribute, sublicense, # and/or sell copies of the Software, 
# and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all copies or substantial portions 
# of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
# TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
# THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
# CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
# DEALINGS IN THE SOFTWARE.
#
# Created by: Brent Stineman
#
# Description: This is an HTTP triggered Azure function to a "ping response" or pong event from a device
#    For more info see: https://github.com/brentstineman/ButtonPong 
#
#
# Modifications
# 2018/03/03 : Initial publication
#
*/
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using Microsoft.CSharp.RuntimeBinder;
using System.Net;
using System.Threading.Tasks;
using System;

namespace CloudAPI
{
    public class bpPong
    {
        public string deviceId { get; set; }
        public bool success { get; set; }
    }

    public static class Pong
    {
        static Random rnd = new Random();

        [FunctionName("Pong")]
        public async static Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "pong")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("Pong recieved from device.");

            HttpResponseMessage rtnResponse = null;

            try
            {
                // Get request body, should contain deviceId and accessToken
                bpPong pongData = await req.Content.ReadAsAsync<bpPong>();
                log.Info($"Pong received from device {pongData.deviceId}.");

                GameStateManager gameState = new GameStateManager(); // get game state
                if (gameState.IsRunning) // if game isn't running
                {
                    if (pongData.success == false) // last device failed to respond in time, remove it
                        gameState.RemoveDevice(pongData.deviceId);

                    // select next device to ping and prepare response (-1 if this is the last device, aka the winner)
                    bpDevice randomDevice = gameState.GetRandomDevice();
                    log.Info($"Sending initial ping to device {randomDevice.deviceId}.");
                    SendToDevice.Ping(randomDevice, (gameState.DeviceCount > 1 ? 5000 : -1));
                    if (gameState.DeviceCount <= 1) // game is over, stop game
                    {
                        gameState.StopGame();
                    }
                }

                // respond to request
                rtnResponse = req.CreateResponse(HttpStatusCode.OK);
            }
            catch (RuntimeBinderException ex)
            {
                //  "name property doesn't exist in request body
                rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, @"Please pass the device ID and status of the 'ping' in the body of your request: { ""deviceID"" : ""value"", ""status"" : true|false }");
            }

            return rtnResponse;
        }

    }
}
