
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.CSharp.RuntimeBinder;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System;

namespace CloudAPI
{
    public class bpPong
    {
        public string deviceId { get; set; }
        public bool success { get; set; }
    }

    public class bpPing
    {
        public int responseTimeout { get; set; } // in milliseconds, if -1 sent, then you are last player. Congratulations
    }


    public static class Pong
    {
        static Random rnd = new Random();

        [FunctionName("PongDevice")]
        public async static Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "pong")]HttpRequestMessage req, TraceWriter log)
        {
            List<bpDevice> devicelist = null;

            log.Info("Pong recieved from a device.");

            HttpResponseMessage rtnResponse = null;

            try
            {
                // Get request body, should contain deviceId and accessToken
                bpPong pongData = await req.Content.ReadAsAsync<bpPong>();

                //// get device list from blob
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("storageConnString"));
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Environment.GetEnvironmentVariable("storageContainer"));

                MemoryStream tmpStream = new MemoryStream();
                CloudBlockBlob blob = container.GetBlockBlobReference("devices");
                try
                {
                    blob.DownloadToStreamAsync(tmpStream).Wait();
                    devicelist = JsonConvert.DeserializeObject<List<bpDevice>>(Encoding.UTF8.GetString(tmpStream.ToArray()));
                }
                catch (Exception ex)
                {
                    // if the container doesn't already exist, create it and an empty device collection
                    if (ex.InnerException is StorageException && ex.InnerException.Message.Contains("does not exist"))
                        rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, "no devices are currently registerd");                    
                }

                if (pongData.success == false) // last device failed to respond in time, remove it
                {
                    // add device to the list
                    var itemToRemove = devicelist.Find(r => r.deviceId == pongData.deviceId);
                    devicelist.Remove(itemToRemove);

                    // save back to blob
                    MemoryStream tmpStream2 = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(devicelist, Formatting.Indented)));
                    blob.UploadFromStreamAsync(tmpStream2).Wait();
                }

                // select next device to ping and prepare response (-1 if this is the last device, aka the winner)
                bpDevice deviceToPing = devicelist[rnd.Next(devicelist.Count)];
                bpPing pingValue = new bpPing()
                {
                    responseTimeout = (devicelist.Count > 1 ? 2000 : -1)
                };
                // send operation
                WebRequest request = WebRequest.Create($"https://api.particle.io/v1/devices/{deviceToPing.deviceId}/ping?access_token={deviceToPing.accessToken}");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                Stream dataStream = request.GetRequestStream();
                byte[] requestbody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pingValue));
                dataStream.Write(requestbody, 0, requestbody.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                response.Close();

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
