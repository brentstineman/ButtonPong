#include <InternetButton.h>
#include "InternetButtonEvents.h"

#define INTERNET_BUTTON_TYPE 0

bool isRegistered = false;

InternetButton b = InternetButton();
InternetButtonEvents buttonEvents = InternetButtonEvents(b);

//// TODO
// Implement state change button X
// Factor state change button into seperate class lib X
// implement all 4 buttons
// publish to particle libraries
// particle function ping(time ms) 

// call pong(true/false) 
/// if  failed registred is 

void setup() {
    Serial.begin(9600);
    Particle.function("ping", ping);
    Particle.subscribe("hook-response/pong", registrationHandler);
    
    buttonEvents.onAllButtonsClicked(allButtonsClickedHandler);
    buttonEvents.onAllButtonsOn(allButtonsOnHandler);

    b.begin(INTERNET_BUTTON_TYPE);
    
}

void loop(){
    buttonEvents.update();
    
    if (buttonEvents.allButtonsOn()) {
        b.allLedsOn(0,20,20);
    } else {
        b.allLedsOff();
    }
}

void allButtonsOnHandler() {
    Serial.println("down");
}

void allButtonsClickedHandler() {
    b.allLedsOn(20,20,0);
    
    Particle.publish("register", "TRUE");
    Serial.println("register");
    delay(500);
}

void registrationHandler(const char *event, const char *data) {
    isRegistered = true;
}

int ping(String countdown) {
    
    // TODO
    
    return 1;
}
