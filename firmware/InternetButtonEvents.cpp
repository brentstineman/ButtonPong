// NOTE: This will get published to the Particle Library seperately for easy integration

#include "InternetButtonEvents.h"

InternetButtonEvents::InternetButtonEvents(InternetButton internetButton) {
    b = internetButton;
    allButtonsClickedCallback = NULL;
    clickThreshold = 200;
    
    currentAllButtonsOn = false;
    previousAllButtonsOn = false;
}

InternetButtonEvents::InternetButtonEvents(InternetButton internetButton, int threshold) {
    b = internetButton;
    allButtonsClickedCallback = NULL;
    clickThreshold = threshold;
    
    currentAllButtonsOn = false;
    previousAllButtonsOn = false;
}

InternetButtonEvents::~InternetButtonEvents(void) {
}

bool InternetButtonEvents::allButtonsOn() {
    return currentAllButtonsOn;
}

void InternetButtonEvents::onAllButtonsClicked(CallbackType callback) {
    allButtonsClickedCallback = callback;
}

void InternetButtonEvents::onAllButtonsOn(CallbackType callback) {
    allButtonsDownCallback = callback;
}

void InternetButtonEvents::onAllButtonsOff(CallbackType callback) {
    allButtonsUpCallback = callback;
}

void InternetButtonEvents::update()
{
  if(b.allButtonsOn()){
        
        // start down timer
        if (!previousAllButtonsOn) {
            downStart = millis();
            previousAllButtonsOn = true;
        }
        
        // update down timer
        downTime = millis() - downStart;
        
        if (downTime > clickThreshold && !currentAllButtonsOn) {
            currentAllButtonsOn = true;
            
            if (allButtonsDownCallback) allButtonsDownCallback();
        }
    }
    else {
        
        // start up timer
        if (previousAllButtonsOn) {
            upStart = millis();
            previousAllButtonsOn = false;
        }
        
        // update up timer
        upTime = millis() - upStart;
        
        if (upTime > clickThreshold && currentAllButtonsOn) {
            currentAllButtonsOn = false;
            
            if (allButtonsUpCallback) allButtonsUpCallback();
            if (allButtonsClickedCallback) allButtonsClickedCallback();
        }
    }
}

