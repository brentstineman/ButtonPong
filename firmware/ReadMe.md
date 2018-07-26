# Button Pong Firmware

## Setting up the Particle Button

The Particle Internet Button was chosen for this project for several reasons:

1. The button is a self-contained prototyping platform, no wiring is required
1. Particle provides an online IDE for publishing to the devices with a comprehensive sample library
1. It has built-in WiFi so it's just "connect and go"

## Registering Your Devie

Start by registering your button and setting up the WiFi. The button's brain is a Particle Photon, so follow the [setup instructions provided by Particle](https://docs.particle.io/guide/tools-and-features/button/core/). This can be done via the Apple or Android apps, USB, or [from your computer](https://setup.particle.io/). The Photon is a 'headless' device, so if your WiFi requires you to log in via a web page, you'll need to talk with your WiFi administrators about how to register your device's MAC address. For details on obtaining the device's MAC address, please refer to this [blog post](https://blog.jongallant.com/2015/08/particle-photon-mac-address/).

_**Note:** When at a hack with lots of Photon's around be sure you're connecting to **your** device. The Wifi SSID will be in the format of "Photon-nnnn". The 'nnnn' are from the 3 section of the devices serial number. The number should be visible on a sticker placed somewhere on your device's packaging._

During this process you will be prompted to create a Particle Account and to name your device. Be sure to name the device something unique but meaningful so you can tell your device apart from others.

Your photon can store the details of up to 5 WiFi networks. If you need/want to manage these, please refer to the [Particle Reference documentation](https://docs.particle.io/reference/firmware/photon/#setcredentials-).

## Setting up the Firmware

Particle provides an online, web-based IDE that can be used to author and publish firmware to a device. So with your device set up, proceed to the [Particle Build Site](https://build.particle.io) to start adding the Button Pong firmware.  We'll start by creating an App, the project that will contain the firmware code we're going to flash to our device. Start by naming the App then press "save" (click on the Folder icon on the upper left of the IDE). Don't forget to press "save".

Next we add some community libraries that are used by our sample firmware: InternetButton, InternetButtonEvents. We'll add these by selecting "Libraries" in the IDE (looks like a medal in the left hand menu). Search for the two libraries one at a time and after selecting them, click on "Include in project". If our App isn't listed, as a project we can include these in, then we didn't press "save". So return to the code area and create our app again and be sure to save it this time.

With the libraries added, we need need to paste in the baseline copy of the firmware. Copy the contents of the buttonpong.ino file from this repository into the .ino file for your App/project. A default one was created for you when you created the project. You want to replace everything in that file with the contents from buttonpong.ino file.

With the firmware, and libraries in place, press the lightning bolt icon in the IDE to flash the firmware to your device.

If you prefer, Particle also provides a local IDE, called [Particle Dev](https://docs.particle.io/guide/tools-and-features/dev/) which is based on the Atom editor.  Out-of-the-box, Particle Dev will allow you to develop locally, while still using the Particle cloud services to compile and deploy to your Particle Button.  The above steps can also be performed using Particle Dev, should you so choose.

_**Note:** In the lower right corner of the IDE, you should see the name of your device listed with a slowly flashing blue icon, the "breathing" state of your Photon. If your device is not listed, click on the name that is to select your device (you may have multiple devices selected). If no devices are listed, then proceed back to the setup steps for the device and make sure its associated with your particle user account._

## Connecting the Device to the API

Particle provides a cloud back end to interact with devices. The Particle Internet Button leverages this to help the device call and be called by the API. The device publishes an event, and the cloud back end uses an integration webhook to perform an action. So to connect the API, we need to set up the integrations to call our Azure Function API.

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

## Troubleshooting the Unexpected Button Behavior with the Game

The most common problem that has been observed is when the firmware state gets out of sync with the game state in the API.  To correct this, press the reset button of all of the devices in the game.  Then invoke the _ResetGame_ API and confirm that the game state has returned to the activity "NotStarted".  While doing work with the device and API, this will be a pretty common occurrence.

## Enhancement Ideas

As has been mentioned several times, this implementation stops well short of many features that would make the game more reliable, less prone to exploitation, and very possibly, more fun. However, sometimes hacks are helped by providing some guidance, so the following is a list of possible enhancements or improvements your hack group could explore for the Button Pong firmware:

- Implement the "endGame" event, having the device clear and reset the game state.  _(The API already sends the signal)_
- Implement the "winner" event, having the button signal the user that it is the winner of the game.  _(The API already sends the signal)_
- Implement the "eliminated" event, having the button signal the user that it has been eliminated from the game. _(The API already sends the signal)_
- Remove code in the firmware that assumes elimination when a ping times out and accept the API's responsibility.
- Modify the "ping" response so that the timer doesn't start until after the rainbow affect is done.
- Vary the button that should be pushed to generate the "pong" response for each ping received.
- Improve the button's UI, with better countdown effect for ping timeouts and effects for other button events received.
- Add retries and timeouts for invoking API functions.
