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
# Description: This helper class for sending 'ping' messages to Photon devices
#    For more info see: https://github.com/brentstineman/ButtonPong 
#
#
# Modifications
# 2018/03/03 : Initial publication
#
*/
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;

namespace CloudAPI
{
    /// <summary>
    /// "Ping" payload
    /// </summary>
    public class bpPing
    {
        /// <summary>
        /// in milliseconds, if -1 sent, then you are last player. Congratulations
        /// If 0, that means the game is ready to start
        /// any value > 0 is the time the device has to get a button push
        /// </summary>
        public int responseTimeout { get; set; } 
    }

    /// <summary>
    /// sends events to Photon devices
    /// </summary>
    class SendToDevice
    {
        /// <summary>
        /// Sends a ping event
        /// </summary>
        /// <param name="deviceToPing">the device ID and access token for the target device</param>
        /// <param name="value">the timeout value</param>
        public static void Ping(bpDevice deviceToPing, int value)
        {
            bpPing pingValue = new bpPing()
            {
                responseTimeout = value
            };

            Send(deviceToPing, "ping", JsonConvert.SerializeObject(pingValue));
        }

        /// <summary>
        /// Sends a start signal to a device
        /// </summary>
        /// <param name="targetDevice"></param>
        public static void Start(bpDevice targetDevice)
        {
            Send(targetDevice, "startGame");
        }

        private static async void Send(bpDevice targetDevice, string function, string requestBody = "")
        {
            // Posts a message to the device endpoint directly
            WebRequest request = WebRequest.Create($"https://api.particle.io/v1/devices/{targetDevice.deviceId}/{function}?access_token={targetDevice.accessToken}");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            Stream dataStream = request.GetRequestStream();
            byte[] requestbody = Encoding.UTF8.GetBytes(requestBody);
            dataStream.Write(requestbody, 0, requestbody.Length);
            dataStream.Close();
            WebResponse response = await request.GetResponseAsync();
            response.Close();
        }
    }
}
