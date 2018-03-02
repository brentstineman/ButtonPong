// This #include statement was automatically added by the Particle IDE.
#include <InternetButtonEvents.h>
#include <InternetButton.h>

#define STATE_GAME_OVER         0
#define STATE_GAME_WAITING      1
#define STATE_GAME_RESPONDING   2

int gameState = STATE_GAME_OVER;
unsigned long respondingTimeout;

InternetButton b = InternetButton();
InternetButtonEvents buttonEvents = InternetButtonEvents(b);

void setup() {
    Serial.begin(9600);
    
    Particle.function("ping", ping);
    Particle.subscribe("hook-sent/register", registrationHandler, MY_DEVICES);
    
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
    } else if (gameState == STATE_GAME_RESPONDING) {
        if (buttonEvents.buttonOn(1)) {
            b.ledOn(1,0,20,0);
            b.ledOn(11,0,20,0);
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
            b.ledOn(5,20,0,0);
            b.ledOn(6,20,0,0);
            b.ledOn(7,20,0,0);
        } else {
            b.ledOff(5);
            b.ledOff(6);
            b.ledOff(7);
        }
        
        if (buttonEvents.buttonOn(4)) {
            b.ledOn(8,20,20,0);
            b.ledOn(9,20,20,0);
            b.ledOn(10,20,20,0);
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
    }

}

void registrationHandler(const char *event, const char *data) {
    Serial.println("Registration succeeded");

    gameState = STATE_GAME_WAITING;
    b.allLedsOn(0,20,0);
    delay(500);
}

int ping(String timeout) {
    Serial.println("Received move: " + timeout + " milliseconds");
    
    if (gameState == STATE_GAME_WAITING) {
        int timeoutVal = 5000;
        if (timeout != NULL) {
            timeoutVal = timeout.toInt();
        }
        
        b.rainbow(3);
        respondingTimeout = millis() + timeoutVal;
        
        gameState = STATE_GAME_RESPONDING;
    }
    
    return 1;
}

void buttonClickedHandler(int buttonNumber) {
    if (gameState == STATE_GAME_RESPONDING) {
        Serial.println("Playing a move");

        if (millis() < respondingTimeout) {
            Serial.println("Successful move");
            Particle.publish("pong", "TRUE");
            gameState = STATE_GAME_WAITING;
            b.allLedsOn(0,20,0);
        } else {
            // Game over
            Serial.println("Game over");
            Particle.publish("pong", "FALSE");           
            gameState = STATE_GAME_OVER;
            b.allLedsOn(20,0,0);
        }
        
        respondingTimeout = 0;
        delay(500);
    }
}