# ButtonPong
A sample lab that demonstrates a simple game using Azure Serverless and Particle Internet Button Devices

Game API's
- register device (POST): Adds a device to the game
- start game (PUT): starts a game, ends ability to register new devices and sends first "ping" to a device
- pong (PUT): register a "pong" back from a device that was "pinged". can be success or failure. failures deactivates/removes device in the game 
- end game (DELETE): stop game after there's assumed to be a winning player
- send game status (GET): send back ids of devices still in the game and state (active/inactive)

Button Actions:
- on startup it will register itself with the game, starts flashing yellow when registration is successful
- when all four buttons are pressed, the device will register with the game and go to no lights on
- when a "ping" is recieved (published function), will wait for nn seconds for any button to be pressed and respond with a "pong". If button is pressed before nn seconds pass, responds with a success, otherwise responds with a failure.
- when a game status is recieved will update with flashing red if failed (lost the game) or flashing green (won the game). Flashing will continue for 5 seconds before pressing any button will re-register with game
