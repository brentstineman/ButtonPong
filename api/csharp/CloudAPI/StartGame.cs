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
# Description: This is an HTTP triggered Azure function to start the game. If not already running, it will notify
# all devices that game is beginning then send a ping to a random device
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
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudAPI
{
    public static class StartGame
    {
        private static Task notificationTask;
        private static List<Task> pingTasks;

        [FunctionName("StartGame")]
        public async static Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "game")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("StartGame received a request.");

            HttpResponseMessage rtnResponse = req.CreateResponse(HttpStatusCode.OK);

            // we'll only do this if we're not already doing start notifications
            if (notificationTask == null || notificationTask.Status != TaskStatus.Running)
            {
                GameStateManager gameState = new GameStateManager(); // get game state
                if (!gameState.IsRunning) // if game isn't running
                {
                    List<bpDevice> deviceList = gameState.GetDeviceList();
                    // if we have more than 1 device
                    if (deviceList.Count > 1)
                    {
                        gameState.StartGame();

                        // start device notification background task
                        notificationTask = new Task(() =>
                        {
                            log.Info("StartGame: Starting device notification.");

                            // get list of devices

                            pingTasks = new List<Task>();
                            foreach (bpDevice device in deviceList)
                            {
                                pingTasks.Add(Task.Run(() =>
                                {
                                    log.Info($"StartGame: Notifying device {device.deviceId} of start.");
                                    SendToDevice.Start(device);
                                }));
                            }
                            Task.WaitAll(pingTasks.ToArray());

                            Thread.Sleep(3000); // wait three seconds after all pings are complete

                            bpDevice randomDevice = gameState.GetRandomDevice();
                            log.Info($"StartGame: Sending initial ping to device {randomDevice.deviceId}.");
                            SendToDevice.Ping(randomDevice, 5000);
                        });

                        // spin off a thread to send the pings in the background so we can return immediately to caller
                        notificationTask.Start();
                    }
                    else
                        log.Info($"StartGame: Only one device. Don't you have any friends?");
                }
                else
                    log.Info("StartGame: Game already running. So I'm going to ignore this.");
            }
            else
                log.Info("StartGame: Notifications currently running. So I'm going to ignore this.");

            return rtnResponse;
        }
    }
}
