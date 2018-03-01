#include <InternetButton.h>

#define INTERNET_BUTTON_TYPE    1
#define PRESS_AND_HOLD          3*1000
#define CLICK_THRESHOLD         500

unsigned long downStart;
unsigned long downTime;

unsigned long upStart;
unsigned long upTime;

bool current = false;
bool previous = false;

InternetButton b = InternetButton();

//// TODO ///////
// Implement state change button
// Factor state change button into seperate class lib
// particle function ping(time ms)
// call pong(true/false) 

void setup() {
    Serial.begin(9600);
    Particle.function("ping", ping);

    b.begin(INTERNET_BUTTON_TYPE);
}

void loop(){
    
    if(b.allButtonsOn()){
        
        // start down timer
        if (!previous) {
            downStart = millis();
            previous = true;
        }
        
        // update down timer
        downTime = millis() - downStart;
        
        if (downTime > CLICK_THRESHOLD && !current) {
            current = true;
            Serial.println("down");
        }
        
        b.allLedsOn(0,20,20);
    }
    else {
        
        // start up timer
        if (previous) {
            upStart = millis();
            previous = false;
        }
        
        // update up timer
        upTime = millis() - upStart;
        
        if (upTime > CLICK_THRESHOLD && current) {
            current = false;
            
            Serial.println("up");

            // this is the click handler behavior
            b.allLedsOn(20,20,0);
            Particle.publish("pong", "TRUE");
            Serial.println("pong");
            delay(500);

        }
        
        b.allLedsOff();
    }
}

int ping(String countdown) {
    
    // TODO
    
    return 1;
}
