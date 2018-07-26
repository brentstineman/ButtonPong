# Button Pong API

## API Details

The API consists of several Azure Functions, each implementing a single member of the API.  The API has intentionally been kept minimal to allow opportunities for extension and enhancement.  The available operations are:

* **GetStatus**
  _This endpoint allows callers to retrieve the current status of the game in JSON format, detailing the current game phase, devices participating, and ping/pong activities._

  &nbsp;&nbsp;_**Route:**_ /api/game
  &nbsp;&nbsp;_**Verb:**_ GET

  &nbsp;

* **RegisterDevice**
  _This endpoint allows callers to register a device as a participant of the game.  The registration requires both the device identifier and an access token that can be used for communicating with the device via its published integrations._

  &nbsp;&nbsp;_**Route:**_ /api/devices
  &nbsp;&nbsp;_**Verb:**_ POST

  &nbsp;

* **StartGame**
  _This endpoint allows callers to request that a game begin.  Validation will be performed to ensure that there are at least two registered devices and that no game is currently in progress._

  &nbsp;&nbsp;_**Route:**_ /api/game
  &nbsp;&nbsp;_**Verb:**_ PUT

  &nbsp;

* **ResetGame**
  _This endpoint allows callers to request that a game immediately be forced to reset; all activities will stop, all devices will be unregistered, and the game will return to an uninitialized state.  Game activities can be resumed by registering devices and starting a new game._

  &nbsp;&nbsp;_**Route:**_ /api/game
  &nbsp;&nbsp;_**Verb:**_ DELETE

  &nbsp;

* **Ping**
  _This endpoint allows callers to register a pong that is expected in response to a "ping."  Validation will be performed to ensure that the sender is the currently active ping and that a game is currently in progress._

  &nbsp;&nbsp;_**Route:**_ /api/pong
  &nbsp;&nbsp;_**Verb:**_ PUT

  &nbsp;

* **PingManager**
  _This function is not a callable endpoint, instead running on a schedule with responsibility for managing the active ping and transitioning game state if a pong has not be received in the expected response period.  This function ensures that communication or connection failures for a specific device do not crash the game as a whole, and that individual devices are not left responsible for understanding and influencing the state of the game for others._

## Azure Infrastructure Requirements

The first step is setup is to deploy the API using Azure's serverless solution: Functions. We'll start by creating the Azure environment for the API as follows:

1. Create an Azure Resource Group.
1. In the resource group, create a new general use (tables, blobs, queues) version 2 storage account.
1. Create a new Function App with a consumption-based App Service Plan to host your Azure Functions.

_**TODO:** provide this as an ARM template with CLI and PowerShell commands to deploy it._

## Deploy the API
This should work regardless of the underlying implementation technical stack, development environment, or language.

_**TODO:** provide details of this method_

## Configuring the API Application Settings

The API depends on the following application settings, which must be configured as part of the deployment or manually assigned within the Azure portal.  The settings are:

* **AzureWebJobsStorage**
  _The fully qualified connection string to the Azure Storage account to be used for storing system-owned infrastructure and diagnostics information._

  &nbsp;

* **AzureWebJobsDashboard**
  _The fully qualified connection string to the Azure Storage account to be used for storing system-generated dashboard information._

  &nbsp;

* **storageConnString**
  _The fully qualified connection string to the Azure Storage account to be used for storing game state information in blob storage._

  &nbsp;

* **storageContainer**
  _The name of the blob storage container in which to store game state information.  Recommended: "gamedata"._

  &nbsp;

* **pingManagerSchedule**
  _The CRON expression that specifies the schedule on which the ping manager should run.  Recommended: "*/45 * * * * *", which equates to "every 45 seconds"._

  &nbsp;

* **pingMaxAgeSeconds**
  _The maximum age, in seconds, of an active ping before it expires and the device is eliminated.  Recommended: "12"._

  &nbsp;

* **pingTimeoutSeconds**
  _The amount of time , in seconds, that is signaled to devices as the suggested timeout when awaiting for a pong to be triggered.  This value should be less than the configured "pingMaxAgeSeconds" in order to allow the button UI experience to make sense.  It should allow for some additional time for network latency.  Recommended: "5"._

## Testing the API

If you're running the API locally, you'll need to rename `sample.settings.json` to `local.settings.json` and update the placeholders with the connection strings to your storage account.  If you're seeing exceptions in the local function host window, this is a likely cause.

To make Testing the API easier, especially when just trying to learn what its doing, we've created a Postman collection (Button Pong.postman_collection.json) and exported it to this repository. This collection uses variables that should be defined in a corresponding Postman environment. A sample environment file, to outline what values should be provided, has also been placed in the repository ((Button PongSample-Local.postman_environment.json))

To use these files, simply [install Postman](https://www.getpostman.com/) and import the collection and environment files. Once imported, you'll need to customize the environment settings which will supply the variables used by the collection's methods.

For more on Postman environments and variables, please see the [Postman Online Documentation](https://www.getpostman.com/docs/postman/environments_and_globals/manage_environments).