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
# Description: This is an HTTP triggered Azure function to handle game device registration
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
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using System.Net;
using System;

namespace CloudAPI
{
    public static class Register
    {
        [FunctionName("RegisterDevice")]
        public async static Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "devices")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("RegisterDevice processed a request.");

            HttpResponseMessage rtnResponse = null;

            try
            {
                // Get request body, should contain deviceId and accessToken
                //bpDevice deviceData = JsonConvert.DeserializeObject<bpDevice>(await req.Content.ReadAsStringAsync()); 
                bpDevice deviceData = await req.Content.ReadAsAsync<bpDevice>();

                GameStateManager gameState = new GameStateManager(); // get game state
                if (!gameState.IsRunning) // if game isn't running 
                {
                    gameState.RegisterDevice(deviceData); // register the device
                    log.Info($"Registering Device {deviceData.deviceId}");
                }

                // respond to request
                rtnResponse = req.CreateResponse(HttpStatusCode.OK);
            }
            catch (RuntimeBinderException ex)
            {
                //  "deviceData didn't deserialize properly
                rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, @"Please pass the device ID and Access Token in the body of your request: { ""deviceID"" : ""value"", ""accesstoken"" : ""value""}");
            }
            catch (Exception ex)
            {
                //  "name property doesn't exist in request body
                rtnResponse = req.CreateResponse(HttpStatusCode.InternalServerError, $"An unhandled exception occured: {ex.Message}");
            }

            return rtnResponse;
        }
    }
}
