# Button Pong API - C# Version

## Deploy the API via Visual Studio

The C# version of the API has been provided as a Visual Studio 2017 solution. Using this solution, any edition of the [Visual Studio 2017 IDE](https://www.visualstudio.com/downloads/), and the [Azure Functions and Web Jobs Tools extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioWebandAzureTools.AzureFunctionsandWebJobsTools), you can quickly deploy the function based API to Azure.

Simply open the solution (/ButtonPong/ButtonPong.sln) file in Visual Studio and then right click the "CloudAPI" project. Select "Publish" from the pop-up context menu and designate a new Azure Function App as your target.

## Enhancement Ideas

As has been mentioned several times, this implementation stops well short of many features that would make the game more reliable, less prone to exploitation, and very possibly, more fun. However, sometimes hacks are helped by providing some guidance, so the following is a list of possible enhancements or improvements your hack group could explore for the Button Pong C# API:

- Add unit tests; the original authors intended to make the solution as testable as possible, but stopped short of adding tests.
  &nbsp;
- Add timeouts to external communications, including storage and button requests.  _(Tasks have already been extended with a "WithTimeout" method for this purpose)_
&nbsp;
- Add retries to external communications; consider using a library like [Polly](https://github.com/App-vNext/Polly, "Polly") to make this easier.
&nbsp;
- Signal devices when the game is reset, allowing them to correlate their local state to match the game state in the API.
&nbsp;
- Revise the device selection for pings to be less random, preventing a device from receiving multiple consecutive pings; this is especially desirable when there are fewer devices in the game.
&nbsp;
- Tie game state updates to Event Grid or another messaging service, allowing state to be kept locally in each function instance rather than sharing blob state.
&nbsp;
- Consider moving game state to a storage mechanism other than blob storage.  For example, Cosmos DB.  _(don't forget concurrency guards)_
&nbsp;
- Devise a better solution for managing active pings, preferably something that can be toggled on/off as the game state dictates, rather than running continuously.  Perhaps using Logic Apps or a Service Bus topic is worth considering.
&nbsp;
- Add telemetry information or an app gateway to the API to allow for monitoring.-