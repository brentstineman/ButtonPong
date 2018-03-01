#include <InternetButton.h>
#define PRESS_AND_HOLD 3*1000;

unsigned long holdStart = millis();
bool buttonState;
bool previousState;

InternetButton b = InternetButton();

// Implement state change button
// call register
// particle function ping(time ms)
// call pong(true/false) 

void setup() {
    Serial.begin(9600);

    b.begin();
}

void loop(){
    
    if(b.allButtonsOn()){
        Particle.publish("allbuttons", NULL, 60, PRIVATE);
    
        
        b.allLedsOn(0,20,20);
    }
    else {
        b.allLedsOff();
    }


}
