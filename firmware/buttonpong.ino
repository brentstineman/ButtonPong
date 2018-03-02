// This #include statement was automatically added by the Particle IDE.
#include <InternetButtonEvents.h>
#include <InternetButton.h>

#define INTERNET_BUTTON_TYPE 0

bool isRegistered = false;

InternetButton b = InternetButton();
InternetButtonEvents buttonEvents = InternetButtonEvents(b);

//// TODO
// Implement state change button X
// Factor state change button into seperate class lib X
// implement all 4 buttons X
// publish to particle libraries X
// particle function ping(time ms) 

// call pong(true/false) 
/// if  failed registred is 

void setup() {
    Serial.begin(9600);
    Particle.function("ping", ping);
    Particle.subscribe("hook-response/pong", registrationHandler);
    
    buttonEvents.onButtonOn(buttonOnHandler);
    buttonEvents.onButtonClicked(buttonClickedHandler);
    buttonEvents.onAllButtonsClicked(allButtonsClickedHandler);

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

void buttonOnHandler(int buttonNumber) {
    Serial.print("down: ");
    Serial.println(buttonNumber);
}

void allButtonsClickedHandler() {
    b.allLedsOn(20,0,0);
    
    Particle.publish("register", "TRUE");
    Serial.println("all click");
    delay(500);
}

void buttonClickedHandler(int buttonNumber) {
    b.allLedsOn(20,20,0);
    
    //TBD
    //Particle.publish("ping", "TRUE");
    Serial.print("click: ");
    Serial.println(buttonNumber);
    delay(500);
}

void registrationHandler(const char *event, const char *data) {
    isRegistered = true;
}

int ping(String countdown) {
    
    // TODO
    
    return 1;
}
