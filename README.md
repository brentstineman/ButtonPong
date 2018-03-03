# ButtonPong
"Button Pong" is a simple "ping pong" (hence the name) style game where signals are sent to connected devices and you need to press a button to send the signal back. The object is to be the last user/device standing. 

This game was created to provide a starting point for coding events where attendees will be charged with fixing common problems or extending the solution to introduce new functionality to the game. It is built using [Microsoft Azure Functions](https://azure.microsoft.com) and [Patricle's Internet Button](https://docs.particle.io/guide/tools-and-features/button/photon/). The intent is to pro The internet button provides an excellent platform for learning about IOT programming without having to mess with wiring/breadboards. Serverless Azure Functions provide a simple and inexpensive way to host microservice based solutions. Together they are a fun way to explore the possibilities.

## How it works

When a Particle Internet Button when activated with the game's firmware it will register with the game API hosted in Azure Functions. When all the devices that will participate in the game have registered, one person starts the game by depressing and holding down all four switches(buttons) on the device. This sends a single to the API to begin the game by sending a "ping" to a random device connected to the game. Upon recieving the "ping" the user of that device will get 2 seconds to depress any of the switches/buttons on their device. The device then sends a "pong" back to the Azure Function api to indicate if the user successfully depressed a button or not. If the user was not successful, the button will light up red and they are removed from the game. The API then "pings" a random device registered for the game. 

This process continues until only one device is left at which time it is notified it is the winner and the game ends. 

## Using this for hacks and contributions
If you intend to use this for hacks, have each team fork this repository to their own so they can work on their code there. This allows them to make their changes independently. Additionally, we (the origional authors of this repository) cannot help you secure Particle Buttons for your hack no matter how much we might like too. This is not a funded, official effort so we simply don't have anything that even ressembles a budget.

This repository will accept pull requests, but only for the purpose of improving the hack experience. The intention was to provide a simple game that could be extended, not to create a production ready game with lots of complexity. Please keep this in mind when submitting issues or PRs. 

# Setting Up the Game API
The game requires a Microsoft Azure subscription where the game API is hosted and two Particle Internet Buttons. Start by deploying the game API, then you can set up the 

## Azure Infrastructure
The first step is setup is to deploy the API using Microsoft Azure's serverless solution: Functions. We'll start by creating the Azure environment for the API as follows:
- Create an Azure Resource Group
- In the resource group, create a general use (tables, blobs, queues) storage account and save the connection string for later (it will be used by the API)
- create a consumption based App Service Plan to host your functions

_**TODO:** provide this as an ARM template with CLI and PowerShell cmds to deploy it._

## Deploy the API via Visual Studio
A C# version of the API has been provided as a Visual Studio 2017 solution. Using this Solution, any edition of the [Visual Studio 2017 IDE](https://www.visualstudio.com/downloads/), and the [Azure Functions and Web Jobs Tools extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioWebandAzureTools.AzureFunctionsandWebJobsTools), you can quickly deploy the function based API to Azure. 

Simply open the solution (/ButtonPont/ButtonPong.sln) file Visual Studio and then right click the "CloudAPI" project. Select "Publish" from the pop-up context menu and designate a new Azure Function App as your target. 

## Deploy the API via the Azure Portal
_**TODO:** provide details of this method_

## Configure the API's application Settings
If you did not deploy the Azure infrastructure via command line, you will need to configure the Azure application settings using the Azure Portal. The API depends on two application settings:

**storageConnString** - the connection string of the storage account we created in the resource group

**storageContainer** - the blob container in that storage account where we'll store game data. we recommend usign the name 'gamedata'. 

## The API 
The API consists of two functions, each implementing one method. 

**RegisterDevice** - This method allows a device to POST and register itself for the game. It recieves a JSON payload that contains the device ID and its access token and stores this information as an Azure blob. This is exposed along the route '/api/devices'.

**PongDevice** - This method allows a device to PUT a "pong" payload to the API that can either start a game or register the status of a previous "ping" and remove the device is it was not successful. This is exposed along the route 'api/pong'.

**StartGame** - This method starts the game by sending a "ping" to every device with a value of '0' (zero) to notify them to start listening for game signals. It can only be called when a game is not currently running. This is exposed as a PUT method along the route '/api/game'.

**RestGame** - This method clear the game state and all registered devices. Its intended to be use mainly to assist in debugging. This is exposed as a DELETE method along the route 'api/game'.

This API has intentially been kept minimal to allow for plenty of opportunties for extension and enhancement. 

## Testing the API
To make Testing the API easier, especially when just trying to learn what its doing. We've created a Postman collection (Button Pong.postman_collection.json) and exported it to this repository. This collection uses variables that should be defined in a cooresponding Postman environment. A sample environment file, to outline what values should be provided, has also been placed in the repository ((Button PongSample-Local.postman_environment.json))

To use these files, simply [install Postman](https://www.getpostman.com/) and import the collection and environment files. Once imported, you'll need to customize the environment settings which will supply the variables used by the collection's methods.

For more on Postman environments and variables, please see the [Postman Online Documentation](https://www.getpostman.com/docs/postman/environments_and_globals/manage_environments).

# Setting up the Particle Button

The Particle Internet Button was choosen for this project for several reasons:
1. as previously mentioned, no wiring is required. The button is a self contained prototyping platform
2. Particle provides an online IDE for publishing to the devices with a comprehensive sample library
3. it has built in wi-fi so its just "connect and go"

## Registering your device
We start by registering our button and setting up the Wifi. The Button's brain is a Particle Photon, so we'll follow the [setup instructions provided by Particle](https://docs.particle.io/guide/tools-and-features/button/core/). This can be done via the Apple or Android apps, USB, or [from your computer](https://setup.particle.io/). The Photon is a 'headless' device, so if your wifi requires you to log in via a web page, you'll need to talk with your wifi administrators about how to register your device's MAC address. For details on obtaining the device's MAC address, please refer to this [blog post](https://blog.jongallant.com/2015/08/particle-photon-mac-address/). 

_**Note:** When at a hack with lots of Photon's around be sure you're connecting to **your** device. The Wifi SSID will be in the format of "Photon-nnnn". The 'nnnn' are from the 3 section of the devices serial number. The number should be visible on a sticker placed somewhere on your device's packaging._

During this process you will be prompted to create a Particle Account and to name your device. Be sure to name the device something unique but meaningful so you can tell your device apart from others. 

## Setting up the firmware
Particle provides an online, web based IDE that we can use to author and publish firmware to our device. So with the device set up, proceed to [https://build.particle.io] to start adding the Button Pong firmware.

We start by creating an App, the project that will contain the firmware code we're goign to flash to our device. Start by naming the App then press "save" (click on the Folder icon on the upper left of the IDE). Don't forget to press "save". 

Next we add some community libraries that are used by our sample firmware: InternetButton, InternetButtonEvents. We'll add these by selecting "Libraries" in the IDE (looks like a medal in the left hand menu). Search for the two libraries one at a time and after selecting them, click on "Include in project". If our App isn't listed, as a project we can include these in, then we didn't press "save". So return code area and create our app again and be sure to save it this time. 

With the libraries added, we need need to paste in the baseline copy of the firmware. Copy the contents of the buttonpong.ino file from this repository into the .ino file for your App/project. A default one was created for you when you created the project. You want to replace everything in that file with the contents from buttonpong.ino file. 

With the fireware, and libraries in place, press the lightning bolt icon in the IDE to flash the firmware to your device. 

_**Note:** In the lower right corner of the IDE, you should see the name of your device listed with a slowly flashing blue icon, the "breathing" state of your Photon. If your device is not listed, click on the name that is to select your device (you may have multiple devices selected). If no devices are listed, then proceed back to the setup steps for the device and make sure its associated with your particle user account._

## Connecting the Device to the API
Particlel provides a cloud back end to interact with devices. Button Leverages this to help the device call and be called by the API. The device publishes an event, and the cloud back end uses an integration webhook to perform an action. So to connect the API, we need to set up the integrations to call our Azure Function API. 

Before we start, we need to capture some information. From the Azure infrastructure:
- the root URL of the App Service that our API is hosted in... \<myname\>.azurewebsites.net
- the master host key for our function API (available from the Function app settings)

From the Particle Developer ID:
- the device ID (select Devices, then your device to display the numeric device Id)
- your account Access Token (click on Settings) to see your alphanumeric access token

With this information in hand, we can proceed to the Particle Console IDE, [https://console.particle.io] and define our integrations. On the console IDE, select Integrations from the left hand menu, then web hook, then "Custom Template". 

Using the register-webhook.json file provided in this repository, replace the contents of teh webhook template with the contents of the file and replace the following values:

\<apidns\> - use the dns name of your site

\<masterhostkey\> - the master host key for our API functions

\<myparticleaccesstoken\> - your particle account access token

Once the values have been substituted, "Create" the web hook. You can test to make sure it was successfully created by pressing the "Test" button on the webhook page. 

Repeat this process for the pong-webhook.json file. They will apply to all devices on your account, so there's no need to set up multiple web hooks for each device if you're sharing accounts with someone else. 

_**Note:** Testing the webhook from the Particle Console IDE may result in a "Timed out" response. If this happens, refresh the page and look at the log to ensure that it did indeed fail and if so, why._

Our Azure Function API has the ability to call any functions published by the device directly. So the "ping" from the API will go directly to the device using its Device ID and your account's Access Token. Therefore, there's no reason for us to set up a web hook for this integration point. 

# Enhancement Ideas
As has been mentioned several times, this repo stops well short of implementing many features that would make the game more reliable, less prone to exploitation, and very possibly, more fun. However, sometimes hacks are helped by providing some guidance, so the following is a list of possible enhancements or improvements your hack group could explore:

#### API Improvements
- once a game is in progress, prevent any device from sending an "start game" pong
- add additional error handling to the API to deal with situations like "device not active"
- add a dashboard/scoring feature
- help preventing "cheating" by adding in code that double checks the devices success/fail status
- prevent "state clobbering" by adding locking to the state data store

#### Gameplay Enhancements
- Add gameplay complexity
    - as the game progresses, reduce the response window
    - vary the button that needs to be pressed
    - Allow multiple rounds to the game and keep score
- Improve the UI
    - have the button's LED ring count down the response time
    - flash on success and fail
    - provide an audio indication of success/fail, or winning the game

#### General Suggestions
- Display a scoring dashboard on another IOT Device, a web site, or using Power BI
- add scalability to the solution to allow for more devices
- add multiple 'balls' to the game so you can get more pings/pongs going at once

