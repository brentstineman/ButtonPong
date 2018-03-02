
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Text;
using System;

namespace CloudAPI
{
    public class bpDevice
    {
        public string deviceId { get; set; }
        public string accessToken { get; set; }
    }

    public static class Register
    {
        [FunctionName("RegisterDevice")]
        public async static Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "devices")]HttpRequestMessage req, TraceWriter log)
        {
            List<bpDevice> devicelist = null;

            log.Info("RegisterDevice processed a request.");

            HttpResponseMessage rtnResponse = null;

            try
            {
                // Get request body, should contain deviceId and accessToken
                //bpDevice deviceData = JsonConvert.DeserializeObject<bpDevice>(await req.Content.ReadAsStringAsync()); 
                bpDevice deviceData = await req.Content.ReadAsAsync<bpDevice>();

                // get device list from blob
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
                    {                        
                        container.CreateIfNotExistsAsync().Wait(); // create container
                        devicelist = new List<bpDevice>(); // create an empty device list
                    }
                }

                // add device to the list
                devicelist.Add(deviceData);

                // save back to blob
                MemoryStream tmpStream2 = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(devicelist, Formatting.Indented)));
                blob.UploadFromStreamAsync(tmpStream2).Wait();

                // respond to request
                rtnResponse = req.CreateResponse(HttpStatusCode.OK);
            }
            catch (RuntimeBinderException ex)
            {
                //  "name property doesn't exist in request body
                rtnResponse = req.CreateResponse(HttpStatusCode.BadRequest, @"Please pass the device ID and Access Token in the body of your request: { ""deviceID"" : ""value"", ""accesstoken"" : ""value""}");
            }

            return rtnResponse;
        }
    }
}
