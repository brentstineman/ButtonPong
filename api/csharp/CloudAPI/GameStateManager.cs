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
# Description: This is a simple game state manager for the Button Pong game. 
#    For more info see: https://github.com/brentstineman/ButtonPong 
#
#
# Modifications
# 2018/03/03 : Initial publication
#
*/
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CloudAPI
{
    // represents a device registered for the game
    public class bpDevice
    {
        public string deviceId { get; set; }
        public string accessToken { get; set; }
    }

    /// <summary>
    /// A simple state manager for the game. Stores information in Azure Blob storage
    /// the first entry in the list stores if the game is running or not
    /// subsequent entries are the list of devices playing the game
    /// </summary>
    class GameStateManager
    {
        private static List<bpDevice> devicelist = null; // shared between instances of class, but not thread safe
        private CloudStorageAccount storageAccount = null;
        private CloudBlobClient blobClient = null;
        private CloudBlobContainer container = null;
        private CloudBlockBlob stateBlob = null;

        public GameStateManager()
        {
            // initialize the storage acount class variables
            storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("storageConnString"));
            blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(Environment.GetEnvironmentVariable("storageContainer"));
            stateBlob = container.GetBlockBlobReference("devices");
        }

        /// <summary>
        /// property for accessing registered devices
        /// </summary>
        public List<bpDevice> GameState
        {
            get
            {
                if (devicelist == null || (devicelist.Count == 0))
                    LoadState();

                return devicelist; 
            }
        }

        /// <summary>
        /// loads the game state from blob storage, initializes it if it doesn't already exist
        /// </summary>
        private void LoadState()
        {
            MemoryStream tmpStream = new MemoryStream();
            try
            {
                stateBlob.DownloadToStreamAsync(tmpStream).Wait();
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

            
            // initialize collection with game state "device" if it doesn't already exist       
            if (devicelist.Count <= 0)
            {
                
                bpDevice statedevice = new bpDevice()
                {
                    deviceId = "GameState",
                    accessToken = "false"
                };
                devicelist.Add(statedevice); // add to the list

            }
        }

        /// <summary>
        /// Saves the game's state to blob 
        /// </summary>
        private void SaveState()
        {
            MemoryStream tmpStream2 = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(devicelist, Formatting.Indented)));
            stateBlob.UploadFromStreamAsync(tmpStream2).Wait();
            tmpStream2.Close();
        }

        /// <summary>
        /// Registers a new device, returns true if successful. Won't allow an add if it already exists or a game is running
        /// </summary>
        /// <param name="newDevice"></param>
        /// <returns></returns>
        public bool RegisterDevice(bpDevice newDevice)
        {
            bool resultValue = false;

            if (!this.IsRunning) { // if a game isn't currently running
                // add the device if its not already in the list
                if (!GameState.Exists(i => i.deviceId == newDevice.deviceId))
                {
                    GameState.Add(newDevice);
                    this.SaveState();
                    resultValue = true;
                }
            }

            return resultValue;
        }

        /// <summary>
        /// removes the specified device if it exists
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public bool RemoveDevice(string deviceId)
        {
            bool resultValue = false;

            var itemToRemove = devicelist.Find(r => r.deviceId.Equals(deviceId));
            if (itemToRemove != null)
            {
                devicelist.Remove(itemToRemove);
                this.SaveState();
                resultValue = true;
            }
            return resultValue;
        }

        /// <summary>
        /// returns true if game is already running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                // the Devices getter will ensure we always have at least one item
                return GameState[0].accessToken == "true";
            }
        }

        /// <summary>
        /// Gets a count of the current number of registered devices
        /// </summary>
        public int DeviceCount
        {
            get
            {
                // get the count of the current number of devices
                return (GameState.Count - 1); // don't count the first one
            }
        }

        /// <summary>
        /// Starts the game by updating the first item in the device list
        /// </summary>
        public void StartGame()
        {
            GameState[0].accessToken = "true"; // change game state
            SaveState(); // save updated state
        }

        /// <summary>
        /// Stops he game by updating the first item in the device list
        /// </summary>
        public void StopGame()
        {
            GameState[0].accessToken = "false"; // change game state
            SaveState(); // save updated state
        }

        /// <summary>
        /// Wipes all game information and stops current game
        /// </summary>
        public void ResetGame()
        {
            GameState.Clear();
            SaveState(); 
        }

        /// <summary>
        /// Gets a list of all the devices currently in the game
        /// </summary>
        /// <returns></returns>
        public List<bpDevice> GetDeviceList()
        {
            if (GameState.Count > 1)
                return GameState.GetRange(1, devicelist.Count - 1);
            else
                return new List<bpDevice>();
        }

        /// <summary>
        /// Get a random device from the list
        /// </summary>
        /// <returns></returns>
        public bpDevice GetRandomDevice()
        {
            if (GameState.Count > 1)
            {
                int randomInt = new Random((int)DateTime.Now.Ticks).Next(1, devicelist.Count);
                return GameState[randomInt];
            }
            else
                return null;
        }
    }
}
