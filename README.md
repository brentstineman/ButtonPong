# ButtonPong
"Button Pong" is a simple "ping pong" (hence the name) style game where signals are sent to connected devices and you need to press a button to send the signal back. The object is to be the last user/device standing. 

This game was created to provide a starting point for coding events where attendees will be charged with fixing common problems or extending the solution to introduce new functionality to the game. It is built using [Microsoft Azure Functions](https://azure.microsoft.com) and [Patricle's Internet Button](https://docs.particle.io/guide/tools-and-features/button/photon/). The intent is to pro The internet button provides an excellent platform for learning about IOT programming without having to mess with wiring/breadboards. Serverless Azure Functions provide a simple and inexpensive way to host microservice based solutions. Together they are a fun way to explore the possibilities.

#### How it works

When a Particle Internet Button when activated with the game's firmware it will register with the game API hosted in Azure Functions. When all the devices that will participate in the game have registered, one person starts the game by depressing and holding down all four switches(buttons) on the device. This sends a single to the API to begin the game by sending a "ping" to a random device connected to the game. Upon recieving the "ping" the user of that device will get 2 seconds to depress any of the switches/buttons on their device. The device then sends a "pong" back to the Azure Function api to indicate if the user successfully depressed a button or not. If the user was not successful, the button will light up red and they are removed from the game. The API then "pings" a random device registered for the game. 

This process continues until only one device is left at which time it is notified it is the winner and the game ends. 

#### Using this for hacks and contributions
If you intend to use this for hacks, have each team fork this repository to their own so they can work on their code there. This allows them to make their changes independently. Additionally, we (the origional authors of this repository) cannot help you secure Particle Buttons for your hack no matter how much we might like too. This is not a funded, official effort so we simply don't have anything that even ressembles a budget.

This repository will accept pull requests, but only for the purpose of improving the hack experience. The intention was to provide a simple game that could be extended, not to create a production ready game with lots of complexity. Please keep this in mind when submitting issues or PRs. 

# Setting Up the Game API
The game requires a Microsoft Azure subscription where the game API is hosted and two Particle Internet Buttons. Start by deploying the game API, then you can set up the 

#### Azure Infrastructure
The first step is setup is to deploy the API using Microsoft Azure's serverless solution: Functions. We'll start by creating the Azure environment for the API as follows:
- Create an Azure Resource Group
- In the resource group, create a general use (tables, blobs, queues) storage account and save the connection string for later (it will be used by the API)
- create a consumption based App Service Plan to host your functions

_**TODO:** provide this as an ARM template with CLI and PowerShell cmds to deploy it._

#### Deploy the API via Visual Studio
A C# version of the API has been provided as a Visual Studio 2017 solution. Using this Solution, any edition of the [Visual Studio 2017 IDE](https://www.visualstudio.com/downloads/), and the [Azure Functions and Web Jobs Tools extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioWebandAzureTools.AzureFunctionsandWebJobsTools), you can quickly deploy the function based API to Azure. 

Simply open the solution (/ButtonPont/ButtonPong.sln) file Visual Studio and then right click the "CloudAPI" project. Select "Publish" from the pop-up context menu and designate a new Azure Function App as your target. 

#### Deploy the API via the Azure Portal
_**TODO:** provide details of this method_

#### Configure the API's application Settings
If you did not deploy the Azure infrastructure via command line, you will need to configure the Azure application settings using the Azure Portal. The API depends on two application settings:

**storageConnString** - the connection string of the storage account we created in the resource group

**storageContainer** - the blob container in that storage account where we'll store game data. we recommend usign the name 'gamedata'. 

#### The API 
The API consists of two functions, each implementing one method. 

**RegisterDevice** - This method allows a device to POST and register itself for the game. It recieves a JSON payload that contains the device ID and its access token and stores this information as an Azure blob. This is exposed along the route '/api/devices'.

**PongDevice** - This method allows a device to PUT a "pong" payload to the API that can either start a game or register the status of a previous "ping" and remove the device is it was not successful.

This API has intentially been kept minimal to allow for plenty of opportunties for extension and enhancement. 

#### Testing the API
To make Testing the API easier, especially when just trying to learn what its doing. We've created a Postman collection (Button Pong.postman_collection.json) and exported it to this repository. This collection uses variables that should be defined in a cooresponding Postman environment. A sample environment file, to outline what values should be provided, has also been placed in the repository ((Button PongSample-Local.postman_environment.json))

To use these files, simply [install Postman](https://www.getpostman.com/) and import the collection and environment files. Once imported, you'll need to customize the environment settings which will supply the variables used by the collection's methods.

For more on Postman environments and variables, please see the [Postman Online Documentation](https://www.getpostman.com/docs/postman/environments_and_globals/manage_environments).

# Setting up the Buttons
_**TODO:** provide details of this_

# Enhancement Ideas
As has been mentioned several times, this repo stops well short of implementing many features that would make the game more reliable, less prone to exploitation, and very possibly, more fun. However, sometimes hacks are helped by providing some guidance, so the following is a list of possible enhancements or improvements your hack group could explore:

#### API Improvements
- once a game is in progress, prevent more devices from registering
- once a game is in progress, prevent any device from sending an "start game" pong
- add additional error handling to the API to deal with situations like "device not active"

#### Gameplay Enhancements
- Add gameplay complexity
    - as the game progresses, reduce the response window
    - vary the button that needs to be pressed
    - Allow multiple rounds to the game and keep score
- Improve the UI
    - have the button's LED ring count down the response time
    - flash on success and fail
    - provide an audio indication of success/fail, or winning the game
