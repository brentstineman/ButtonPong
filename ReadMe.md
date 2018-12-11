# Button Pong

## Overview

"Button Pong" is a simple "ping pong" (hence the name) style game where signals are sent to connected devices and you need to press a button in response to send the signal back. Failure to "pong" the signal back results in elimination from the game.  The object is to be the last user/device standing.

This game was created to provide a starting point for coding events where attendees will be charged with fixing common problems or extending the solution to introduce new functionality to the game. It is built using [Microsoft Azure Functions](https://azure.microsoft.com/en-us/services/functions/) and [Particle's Internet Button](https://docs.particle.io/guide/tools-and-features/button/photon/). The Internet Button provides an excellent platform for learning about IoT programming without having to mess with wiring/breadboards. Serverless Azure Functions provide a simple and inexpensive way to host microservice based solutions. Together they are a fun way to explore the possibilities.

## How It Works

The game, simply put, is a type of virtual "ping pong" game. Pings are sent to a device, after receiving one, you press a button to respond with a pong which results in another selection for being pinged.  When only one device remains in the game, that device is declared the winner and the game ends.

Once the game is set up, a device can be registered to join the game by pressing down on your button and activating all 4 of its switches. When done successfully, you'll get a positive tone and the device will light up briefly with a light blue color. When everyone that's going to play is ready, one person presses one of the switches on their device which results in all devices getting the "game ready" single and having a brief green ring.

A few seconds after the game starts, one of the devices will get a rainbow color which means they have been pinged. When the rainbow goes away, the player needs to press a switch to "pong" the "ping" they just received. This will in turn send a new signal to random device. If you don't press a button fast enough, you will get a red light and a sad sound and be out of the game.

This process continues until only one device is left at which time it is notified it is the winner and the game ends.

## Basic Requirements

In order for the game to work as designed, it requires a [Microsoft Azure](https://azure.microsoft.com) subscription for hosting the API, and a minimum of two Particle Internet Buttons to play.  Specific requirements for the API and firmware, including set-up and deployment, will appear in dedicated ReadMe files in their associated areas of the repository.  _(please see the [repository structure](#repository-structure) below for context)_

It is recommended that you start with building, deploying, and configuring the API.  Once the API has been smoke tested using [Postman](https://www.getpostman.com/) or a similar tool, then move to setting up the Particle Internet Buttons.

## Using The Assets for Hacks

If you intend to use this for hacks, we suggest having each team fork this repository to their own so they can work on their code there. This allows them to make their changes independently.  Each specific implementation of the API and firmware has a dedicated ReadMe file containing suggestions for enhancements, based on that particular code.  We recommend using these as seeds for hack participants in need of ideas.

Unfortunately, we are unable to help you secure Particle Buttons (or other devices) for your hack no matter how much we might like too. This is not a funded, official, effort so we simply don't have a budget that would allow us to do so.

## Enhancement Ideas

As has been mentioned, this game implementation stops short of many features that would make the game more reliable, less prone to exploitation, and very possibly, more fun. However, sometimes hacks are helped by providing some guidance, so the following is a list of possible enhancements or improvements your hack group could explore for the overall Button Pong game:

* As the game progresses, reduce the timeout for the "pong" response, making it more difficult the longer the game goes on.
* Add multiple rounds to the game and keep score.
* Add the concept of multiple "balls" to the game, allowing more than a single active "ping" at a time.

## Repository Structure

* **root**
  _The root contains the overall repository configuration files, license, and general structure._


  * **api**
    _The container for implementation of the game's server-side API, in various languages._

    * **testing**
      _Assets related to testing of the API, such as collection and parameter definitions for Postman._

    * **csharp**
      _The C# implementation of the game API.  Please see the accompanying ReadMe in this directory for information specific to this implementation._

  * **firmware**
    _The container for firmware implementing the game on connected devices._

    * **integrations**
      _Assets related to defining the event integrations for connecting the device with the API. For the Particle Internet Button, these integrations need to be registered via the Particle Portal or command line._

    * **src**
      _The implementation of the device game client.  Please see the accompanying ReadMe in this directory for information specific to this implementation._

## Licensing

The artifacts in this repository are offered under the MIT license, as described in the accompanying [license](./LICENSE "license") file.  Feel free to use the code in your own projects or the documents as templates as you see fit.  In general, formal attribution is not necessary, though it is always appreciated.  We do ask, however, that you not copy an item verbatim and pass it off as your own work.

## Contributing

This repository accepts pull requests, but only for the purpose of improving the hack experience. The intention is to provide a simple game that could be extended, not to create a production ready game with lots of complexity. Please keep this in mind when submitting issues or pull requests.

Contributors are expected to observe a code of conduct at all times, as described in the accompanying [conduct](./CONDUCT.md "code of conduct") file.