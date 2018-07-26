// This #include statement was automatically added by the Particle IDE.
#include <InternetButtonEvents.h>
#include <InternetButton.h>

#define STATE_GAME_OVER         0
#define STATE_GAME_READY        1
#define STATE_GAME_WAITING      2
#define STATE_GAME_RESPONDING   3

#define TONE_REGISTER           "C6,4\n"
#define TONE_GO                 "Ab6,8,C6,4\n"
#define TONE_START_GAME         "C6,8,C6,8,C6,8,C5,8\n"
#define TONE_PING               "Ab6,8\n"
#define TONE_PONG               "C6,8\n"
#define TONE_LOST               "C6,4,B4,1\n"
#define TONE_WINNER             "Eb6,8,Eb6,8,Eb6,8,G7,2\n"

int gameState = STATE_GAME_OVER;
unsigned long respondingTimeout;

InternetButton b = InternetButton();
InternetButtonEvents buttonEvents = InternetButtonEvents(b);

void setup() {
    Serial.begin(9600);

    Particle.function("startGame", startGame);
    Particle.function("ping", ping);
	Particle.function("winner", winner);

    buttonEvents.onButtonClicked(buttonClickedHandler);
    buttonEvents.onAllButtonsClicked(allButtonsClickedHandler);

    // If you have an original SparkButton, make sure to use `b.begin(1)`
    b.begin();
    b.setBPM(240);
}

void loop(){
    buttonEvents.update();
    updateGameLeds();
}

void updateGameLeds() {
    if (gameState == STATE_GAME_OVER && buttonEvents.allButtonsOn()) {
        b.allLedsOn(0,20,20);

    } else if (gameState == STATE_GAME_READY) {
        updateButtonPressLeds();
    } else if (gameState == STATE_GAME_RESPONDING) {
        updateButtonPressLeds();
        updateCoundown();
    }
    else {
        b.allLedsOff();
    }
}

void updateCoundown() {

    if (millis() > respondingTimeout) {
        // Game Over
        Particle.publish("pong", "FALSE");

        b.allLedsOn(20,0,0);
        b.playSong(TONE_LOST);
        delay(2000);
        gameState = STATE_GAME_OVER;

    } else {
        int secondsRemain = ((respondingTimeout - millis()) / 1000);

        for (int i = 0; i < secondsRemain && i < 11; i++) {
            b.ledOn(i+1, 0, 20, 0);
        }

    }

}

void updateButtonPressLeds() {
    if (buttonEvents.buttonOn(1)) {
            b.ledOn(1,0,0,20);
            b.ledOn(11,0,0,20);
    } else {
        b.ledOff(1);
        b.ledOff(11);
    }

    if (buttonEvents.buttonOn(2)) {
        b.ledOn(2,0,0,20);
        b.ledOn(3,0,0,20);
        b.ledOn(4,0,0,20);
    } else {
        b.ledOff(2);
        b.ledOff(3);
        b.ledOff(4);
    }

    if (buttonEvents.buttonOn(3)) {
        b.ledOn(5,0,0,20);
        b.ledOn(6,0,0,20);
        b.ledOn(7,0,0,20);
    } else {
        b.ledOff(5);
        b.ledOff(6);
        b.ledOff(7);
    }

    if (buttonEvents.buttonOn(4)) {
        b.ledOn(8,0,0,20);
        b.ledOn(9,0,0,20);
        b.ledOn(10,0,0,20);
    } else {
        b.ledOff(8);
        b.ledOff(9);
        b.ledOff(10);
    }
}

void allButtonsClickedHandler() {

    if (gameState == STATE_GAME_OVER) {
        Serial.println("Registering for new game");
        b.playSong(TONE_REGISTER);
        Particle.publish("register");
        gameState = STATE_GAME_READY;
    }

}

int startGame(String args) {
    Serial.println("New game started");

    int val = -1;

    if (gameState == STATE_GAME_READY) {
        gameState = STATE_GAME_WAITING;
        val = 1;

        b.allLedsOn(0,20,0);
        b.playSong(TONE_START_GAME);
        delay(2000);
    }

    return val;
}

int ping(String timeout) {
    Serial.println("Received move: " + timeout + " milliseconds");
    int val = -1;

    if (gameState == STATE_GAME_WAITING) {
        val = 1;

        int timeoutVal = 5000;
        if (timeout != NULL) {
            timeoutVal = timeout.toInt();
        }

        if (timeoutVal == -1) {
            // YOU WON!
            gameState = STATE_GAME_OVER;

            b.playSong(TONE_WINNER);
            b.rainbow(10);
        } else {
            b.playSong(TONE_PING);
            b.rainbow(3);
            respondingTimeout = millis() + timeoutVal;
            gameState = STATE_GAME_RESPONDING;
        }

    }

    return val;
}

int winner(String args) {
    Serial.println("Received winner event!");
    
    // YOU WON!
    gameState = STATE_GAME_OVER;
 
    b.playSong(TONE_WINNER);
    b.rainbow(10);
    
    return 1;
}

void buttonClickedHandler(int buttonNumber) {

    if (gameState == STATE_GAME_READY) {
        Serial.println("Kicking off a game");
        b.playSong(TONE_GO);
        Particle.publish("signalready");
    } else if (gameState == STATE_GAME_RESPONDING) {

        Serial.println("Successful move");
        b.playSong(TONE_PONG);
        Particle.publish("pong", "TRUE");
        gameState = STATE_GAME_WAITING;
        b.allLedsOn(0,20,0);
        delay(500);

        respondingTimeout = 0;
    }
}
