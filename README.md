# ButtonPong

## Overview

"Button Pong" is a simple "ping pong" (hence the name) style game where signals are sent to connected devices and you need to press a button to send the signal back. The object is to be the last user/device standing.

This game was created to provide a starting point for coding events where attendees will be charged with fixing common problems or extending the solution to introduce new functionality to the game. It is built using [Microsoft Azure Functions](https://azure.microsoft.com) and [Particle's Internet Button](https://docs.particle.io/guide/tools-and-features/button/photon/). The internet button provides an excellent platform for learning about IOT programming without having to mess with wiring/breadboards. Serverless Azure Functions provide a simple and inexpensive way to host microservice based solutions. Together they are a fun way to explore the possibilities.

### How it works

The game, simply put is a type of virtual "ping pong" game. Pings get sent to a device, you press a button to respond with a ping which results in another device getting pinged.

When the game is set up, you start by pressing down on your button and activating all 4 of its switches. When done successfully, you'll get a positive tone and the right will light up briefly with a light blue color. When everyone that's going to play is ready, one person presses one of the switches on their device which results in all devices getting the "game ready" single and having a brief green ring.

A few seconds after the game starts, one of the devices will get a rainbow color which means they have been pinged. When the rainbow goes away they need to press a switch to "pong" the "ping" they just received. This will in turn send a new signal to random device. If you don't press a button fast enough, you will get a red light and a sad sound and be out of the game.

This process continues until only one device is left at which time it is notified it is the winner and the game ends.

### Using this for hacks and contributions

If you intend to use this for hacks, have each team fork this repository to their own so they can work on their code there. This allows them to make their changes independently. Additionally, we (the original authors of this repository) cannot help you secure Particle Buttons for your hack no matter how much we might like too. This is not a funded, official effort so we simply don't have anything that even resembles a budget.

This repository will accept pull requests, but only for the purpose of improving the hack experience. The intention was to provide a simple game that could be extended, not to create a production ready game with lots of complexity. Please keep this in mind when submitting issues or PRs.

## Setting Up the Game API

The game requires a Microsoft Azure subscription where the game API is hosted and two Particle Internet Buttons. Start by deploying the game API, then you can set up the

_**For operations in the particle web portals, its recommended you use the Chrome web browser.**_

### Azure Infrastructure

The first step is setup is to deploy the API using Microsoft Azure's serverless solution: Functions. We'll start by creating the Azure environment for the API as follows:

- Create an Azure Resource Group
- In the resource group, create a general use (tables, blobs, queues) storage account and save the connection string for later (it will be used by the API)
- create a consumption based App Service Plan to host your functions

_**TODO:** provide this as an ARM template with CLI and PowerShell cmds to deploy it._

### Deploy the API via Visual Studio

A C# version of the API has been provided as a Visual Studio 2017 solution. Using this Solution, any edition of the [Visual Studio 2017 IDE](https://www.visualstudio.com/downloads/), and the [Azure Functions and Web Jobs Tools extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioWebandAzureTools.AzureFunctionsandWebJobsTools), you can quickly deploy the function based API to Azure.

Simply open the solution (/ButtonPont/ButtonPong.sln) file in Visual Studio and then right click the "CloudAPI" project. Select "Publish" from the pop-up context menu and designate a new Azure Function App as your target.

### Deploy the API via the Azure Portal

_**TODO:** provide details of this method_

### Configure the API's application Settings

If you did not deploy the Azure infrastructure via command line, you will need to configure the Azure application settings using the Azure Portal. The API depends on two application settings:

**storageConnString** - the connection string of the storage account we created in the resource group

**storageContainer** - the blob container in that storage account where we'll store game data. we recommend using the name 'gamedata'.

### The API

The API consists of two functions, each implementing one method.

**RegisterDevice** - This method allows a device to POST and register itself for the game. It receives a JSON payload that contains the device Id and its access token and stores this information as an Azure blob. This is exposed along the route '/api/devices'.

**Pong** - This method allows a device to PUT a "pong" payload to the API that can either start a game or register the status of a previous "ping" and remove the device is it was not successful. This is exposed along the route 'api/pong'.

**StartGame** - This method starts the game by sending a "ping" to every device with a value of '0' (zero) to notify them to start listening for game signals. It can only be called when a game is not currently running. This is exposed as a PUT method along the route '/api/game'.

**GetGameStatus** - This method retrieves the current contents of the game "state bag" (the underlying blob). This is exposed as a GET method along the route 'api/game'.

**RestartGame** - This method clear the game state and all registered devices. Its intended to be use mainly to assist in debugging. This is exposed as a DELETE method along the route 'api/game'.

This API has intentionally been kept minimal to allow for plenty of opportunities for extension and enhancement.

### Testing the API

If you're running the API locally, you'll need to rename `sample.settings.json` to `local.settings.json` and update the placeholders with the connection strings to your storage account.  If you're seeing exceptions in the local function host window, this is a likely cause.

To make Testing the API easier, especially when just trying to learn what its doing, we've created a Postman collection (Button Pong.postman_collection.json) and exported it to this repository. This collection uses variables that should be defined in a corresponding Postman environment. A sample environment file, to outline what values should be provided, has also been placed in the repository ((Button PongSample-Local.postman_environment.json))

To use these files, simply [install Postman](https://www.getpostman.com/) and import the collection and environment files. Once imported, you'll need to customize the environment settings which will supply the variables used by the collection's methods.

For more on Postman environments and variables, please see the [Postman Online Documentation](https://www.getpostman.com/docs/postman/environments_and_globals/manage_environments).

## Setting up the Particle Button

The Particle Internet Button was chosen for this project for several reasons:

1. As previously mentioned, no wiring is required. The button is a self contained prototyping platform
2. Particle provides an online IDE for publishing to the devices with a comprehensive sample library
3. It has built-in WiFi so it's just "connect and go"

### Registering your device

We start by registering our button and setting up the WiFi. The Button's brain is a Particle Photon, so we'll follow the [setup instructions provided by Particle](https://docs.particle.io/guide/tools-and-features/button/core/). This can be done via the Apple or Android apps, USB, or [from your computer](https://setup.particle.io/). The Photon is a 'headless' device, so if your WiFi requires you to log in via a web page, you'll need to talk with your WiFi administrators about how to register your device's MAC address. For details on obtaining the device's MAC address, please refer to this [blog post](https://blog.jongallant.com/2015/08/particle-photon-mac-address/).

_**Note:** When at a hack with lots of Photon's around be sure you're connecting to **your** device. The Wifi SSID will be in the format of "Photon-nnnn". The 'nnnn' are from the 3 section of the devices serial number. The number should be visible on a sticker placed somewhere on your device's packaging._

During this process you will be prompted to create a Particle Account and to name your device. Be sure to name the device something unique but meaningful so you can tell your device apart from others.

Your photon can store the details of up to 5 WiFi networks. If you need/want to manage these, please refer to the [Particle Reference documentation](https://docs.particle.io/reference/firmware/photon/#setcredentials-).

### Setting up the firmware

Particle provides an online, web-based IDE that we can use to author and publish firmware to our device. So with the device set up, proceed to the [Particle Build Site](https://build.particle.io) to start adding the Button Pong firmware.  We'll start by creating an App, the project that will contain the firmware code we're going to flash to our device. Start by naming the App then press "save" (click on the Folder icon on the upper left of the IDE). Don't forget to press "save".

Next we add some community libraries that are used by our sample firmware: InternetButton, InternetButtonEvents. We'll add these by selecting "Libraries" in the IDE (looks like a medal in the left hand menu). Search for the two libraries one at a time and after selecting them, click on "Include in project". If our App isn't listed, as a project we can include these in, then we didn't press "save". So return code area and create our app again and be sure to save it this time.

With the libraries added, we need need to paste in the baseline copy of the firmware. Copy the contents of the buttonpong.ino file from this repository into the .ino file for your App/project. A default one was created for you when you created the project. You want to replace everything in that file with the contents from buttonpong.ino file.

With the firmware, and libraries in place, press the lightning bolt icon in the IDE to flash the firmware to your device.

If you prefer, Particle also provides a local IDE, called [Particle Dev](https://docs.particle.io/guide/tools-and-features/dev/) which is based on the Atom editor.  Out-of-the-box, Particle Dev will allow you to develop locally, while still using the Particle cloud services to compile and deploy to your Particle Button.  The above steps can also be performed using Particle Dev, should you so choose.

_**Note:** In the lower right corner of the IDE, you should see the name of your device listed with a slowly flashing blue icon, the "breathing" state of your Photon. If your device is not listed, click on the name that is to select your device (you may have multiple devices selected). If no devices are listed, then proceed back to the setup steps for the device and make sure its associated with your particle user account._

### Connecting the Device to the API

Particle provides a cloud back end to interact with devices. Button Leverages this to help the device call and be called by the API. The device publishes an event, and the cloud back end uses an integration webhook to perform an action. So to connect the API, we need to set up the integrations to call our Azure Function API.

Before we start, we need to capture some information. From the Azure infrastructure:

- The root URL of the App Service that our API is hosted in... \<myname\>.azurewebsites.net
- The master host key for our function API.  _(This can be obtained via the Azure portal by navigating to App Service resource that contains our Button Pong functions and selecting "Function App Settings" under "Configured features". This will open a new pane on the right-hand side, there will be a section for `Host Keys` under which there is an item named `_master`.   Using the `Click to Show` link will expose the master key for this Functions application and "copy" action will copy the value to your clipboard.)_

From the Particle Developer Id:

- The device Id (select Devices, then your device to display the numeric device Id)
- Your account Access Token (click on Settings) to see your alphanumeric access token

Or using the [Particle Command Line](https://docs.particle.io/guide/tools-and-features/cli/photon/):

- The device Id can be retrieved by running `particle list`
- Your account Access Token can be retrieved by running `particle token list` and locating the `user` token in the output

With this information in hand, we can proceed to the [Particle Console IDE](https://console.particle.io), and define our integrations. On the console IDE, select Integrations from the left hand menu, then web hook, then "Custom Template".

Using the register-webhook.json file provided in this repository, replace the contents of the webhook template with the contents of the file and replace the following values:

\<apidns\> - use the DNS name of your site

\<masterhostkey\> - the master host key for our API functions.

\<myparticleaccesstoken\> - your particle account access token

Once the values have been substituted, "Create" the web hook. You can test to make sure it was successfully created by pressing the "Test" button on the webhook page.

Repeat this process for the pong-webhook.json and startsignal-webhook.json files. These integration webhooks will apply to all devices on your account, so there's no need to set up multiple web hooks for each device on the same account.

_**Note:** Testing the webhook from the Particle Console IDE may result in a "Timed out" response. If this happens, refresh the page and look at the log to ensure that it did indeed fail and if so, why._

The [Particle Command Line](https://docs.particle.io/guide/tools-and-features/cli/photon/) can also be used to register the webhooks, using the command `particle webhook create <<FILENAME>>`, where `<<FILENAME>>` is the JSON template that was updated.

Our Azure Functions API has the ability to call any functions published by the device directly. So the "ping" from the API will go directly to the device using its Device Id and your account's Access Token. Therefore, there's no reason for us to set up a web hook for this integration point.

Additionally, this firmware stores the "state" of the game (not running, registered, running, over). To reset this state, you will need to press the reset button of your device. While doing work with the device and API, this will be a pretty common occurrence.

## Enhancement Ideas

As has been mentioned several times, this repo stops well short of implementing many features that would make the game more reliable, less prone to exploitation, and very possibly, more fun. However, sometimes hacks are helped by providing some guidance, so the following is a list of possible enhancements or improvements your hack group could explore:

### API Improvements

- Once a game is in progress, prevent any device from sending an "start game" pong (this results in multiple 'balls' in play)
- If a device tries to register while a game is ongoing, make sure it doesn't think its registered (add negative response to the API and handling to the device).
- Add additional error handling to the API to deal with situations like "device not active" when attempting to "ping" it
- Add a dashboard/scoring feature
- Help preventing "cheating" by adding in code that double checks the devices success/fail status the last "ping"
- Prevent "state clobbering" by adding locking to the state data store
- Add telemetry information or an app gateway to the API to allow for monitoring
- Experiment with state stores other then Azure Storage

#### Device Enhancements

- Give the ability to reset the game state stored on the device either via an integration webhook, or via the device itself. Perhaps integrate this with the reset-game API
- Modify the "ping" response so that the timer doesn't start until after the rainbow affect is done.

#### Gameplay Enhancements

- Add gameplay complexity
  - As the game progresses, reduce the response window
  - Vary the button that needs to be pressed
  - Allow multiple rounds to the game and keep score
- Improve the UI
  - Have the button's LED ring count down the response time
  - Flash on success and fail
  - Provide an audio indication of success/fail, or winning the game

#### General Suggestions

- Display a scoring dashboard on another IOT Device, a web site, or using Power BI
- Add scalability to the solution to allow for more devices
- Add multiple 'balls' to the game so you can get more pings/pongs going at once
