// This #include statement was automatically added by the Particle IDE.
#include <InternetButtonEvents.h>
#include <InternetButton.h>

#define STATE_GAME_OVER         0
#define STATE_GAME_READY        1
#define STATE_GAME_WAITING      2
#define STATE_GAME_RESPONDING   3

int gameState = STATE_GAME_OVER;
unsigned long respondingTimeout;

InternetButton b = InternetButton();
InternetButtonEvents buttonEvents = InternetButtonEvents(b);

void setup() {
    Serial.begin(9600);
    
    Particle.function("startGame", startGame);
    Particle.function("ping", ping);
    
    buttonEvents.onButtonClicked(buttonClickedHandler);
    buttonEvents.onAllButtonsClicked(allButtonsClickedHandler);

    // If you have an original SparkButton, make sure to use `b.begin(1)` 
    b.begin();
}

void loop(){
    buttonEvents.update();
    updateGameLeds();
}

void updateGameLeds() {
    if (gameState == STATE_GAME_OVER && buttonEvents.allButtonsOn()) {
        b.allLedsOn(0,20,20);
        
    } else if (gameState == STATE_GAME_READY || 
               gameState == STATE_GAME_RESPONDING) {
        
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
    else {
        b.allLedsOff();
    }
}

void allButtonsClickedHandler() {
    
    if (gameState == STATE_GAME_OVER) {
        Serial.println("Registering for new game");
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
            
            b.rainbow(10);
            gameState == STATE_GAME_OVER;
        } else {
            b.rainbow(3);
            respondingTimeout = millis() + timeoutVal;
            gameState = STATE_GAME_RESPONDING;
        }
      
    }
    
    return val;
}

void buttonClickedHandler(int buttonNumber) {
    Serial.println(gameState);
    
    if (gameState == STATE_GAME_READY) {
        Serial.println("Kicking off a game");

        Particle.publish("go");
    } else if (gameState == STATE_GAME_RESPONDING) {

        if (millis() < respondingTimeout) {
            Serial.println("Successful move");
            Particle.publish("pong", "TRUE");
            gameState = STATE_GAME_WAITING;
            b.allLedsOn(0,20,0);
            delay(500);

        } else {
            // Game over
            Serial.println("Game over");
            Particle.publish("pong", "FALSE");           
            gameState = STATE_GAME_OVER;
            b.allLedsOn(20,0,0);
            delay(2000);
        }
        
        respondingTimeout = 0;
    }
}