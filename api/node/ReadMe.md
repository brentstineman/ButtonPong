# Button Pong API - Node Version

## Prerequisites

The Button Pong API is built on the Azure Functions v2 platform.  In order to work with the code, the following prerequisites are needed:

* **Git** (https://git-scm.com/)
  &nbsp;&nbsp;You're free to use the command line or any client that you prefer.

* **Node.js** (https://nodejs.org)
  &nbsp;&nbsp;Active LTS and even-numbered Current Node.js versions are supported _(8.11.1 and 10.6.0 recommended)_
  
* **.NET Core 2.x** (https://www.microsoft.com/net/download)
  &nbsp;&nbsp;Be sure to install both the SDK and the runtime

* **Azure Functions Core Tools v2** (https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
  &nbsp;&nbsp;These can be installed multiple ways; personally, I'm running via NPM install on both Windows and Linux using:  
  &nbsp;&nbsp;&nbsp;&nbsp;`npm install -g azure-functions-core-tools@core --unsafe-perm true --allow-root`
  
* **Visual Studio Code** (https://code.visualstudio.com/)
  &nbsp;&nbsp;This is purely optional, but recommended as there are multiple extensions referred to by the Azure documentation _(see below)_ which automate much of the workflow.
  
  
## Deploying the API

There are multiple means of deplying to Azure Functions, depending on your preference.  If you're not used to working with Azure and Functions, I would recommend using the Continuous Deployment approach.

* [Continous Deployment](https://docs.microsoft.com/en-us/azure/azure-functions/functions-continuous-deployment)
* [Zip Deployment](https://docs.microsoft.com/en-us/azure/azure-functions/deployment-zip-push)
* [Run from Package](https://docs.microsoft.com/en-us/azure/azure-functions/run-functions-from-deployment-package)
* [Dev Ops Automation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-infrastructure-as-code)


## References

* [Azure Functions JavaScript Reference](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-node)
* [Visual Studio Code Azure Functions Extension](https://code.visualstudio.com/tutorials/functions-extension/deploy-app)
* [Azure Functions + Visual Studio Code Tutorial](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-function-vs-code)
* [Azure Functions v1 versus v2 Comparison Guide](https://docs.microsoft.com/en-us/azure/azure-functions/functions-versions)

## Enhancement Ideas

As has been mentioned, this implementation stops short of the level of polish and inclusion of some features that would make the game more reliable, less prone to exploitation, and very possibly, more fun. However, hacks are often helped by providing some guidance, so the following is a list of possible enhancements or improvements your hack group could explore for the Button Pong Node API:

* Enable ESLint and follow its recommendations
* Refactor structural equality checks to a shared module or the Object prototype
* Addition of unit tests
* Incorporate Babel to take advantage of ES6/7 language support
* Integrate state updates with Event Grid and maintain a static cache at the function level to remove the need for persistent storage
* Migrate storage from blob to Cosmos DB or another store with better concurrency support
* Make ping generation less random and offer better distribution (currently, one device can appear favored in small groups)
* Devise and implement a more robust solution for managing active pings, such as using Service Bus for better durability