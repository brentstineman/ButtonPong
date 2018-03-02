// NOTE: This will get published to the Particle Library seperately for easy integration

#include "InternetButtonEvents.h"

InternetButtonEvents::InternetButtonEvents(InternetButton internetButton) {
    b = internetButton;
    clickThreshold = 200;

    buttonClickedCallback = NULL;
    buttonOnCallback = NULL;
    buttonOffCallback = NULL;
    allButtonsClickedCallback = NULL;
    allButtonsOnCallback = NULL;
    allButtonsOffCallback = NULL;

    button0CurrentOn = false;
    button0PreviousOn = false;
    
    button1CurrentOn = false;
    button1PreviousOn = false;
    
    button2CurrentOn = false;
    button2PreviousOn = false;
    
    button3CurrentOn = false;
    button3PreviousOn = false;

    allButtonsCurrentOn = false;
    allButtonsPreviousOn = false;
    
}

InternetButtonEvents::InternetButtonEvents(InternetButton internetButton, int threshold) {
    b = internetButton;
    clickThreshold = threshold;
    
    buttonClickedCallback = NULL;
    buttonOnCallback = NULL;
    buttonOffCallback = NULL;
    allButtonsClickedCallback = NULL;
    allButtonsOnCallback = NULL;
    allButtonsOffCallback = NULL;

    button0CurrentOn = false;
    button0PreviousOn = false;
    
    button1CurrentOn = false;
    button1PreviousOn = false;
    
    button2CurrentOn = false;
    button2PreviousOn = false;
    
    button3CurrentOn = false;
    button3PreviousOn = false;
    allButtonsCurrentOn = false;
    allButtonsPreviousOn = false;
}

InternetButtonEvents::~InternetButtonEvents(void) {
}

void InternetButtonEvents::onButtonClicked(CallbackIntType callback) {
    buttonClickedCallback = callback;
}

void InternetButtonEvents::onButtonOn(CallbackIntType callback) {
    buttonOnCallback = callback;
}

void InternetButtonEvents::onButtonOff(CallbackIntType callback) {
    buttonOffCallback = callback;
}

void InternetButtonEvents::onAllButtonsClicked(CallbackType callback) {
    allButtonsClickedCallback = callback;
}

void InternetButtonEvents::onAllButtonsOn(CallbackType callback) {
    allButtonsOnCallback = callback;
}

void InternetButtonEvents::onAllButtonsOff(CallbackType callback) {
    allButtonsOffCallback = callback;
}

void InternetButtonEvents::update()
{
    updateButton(b.buttonOn(1), 1);
    updateButton(b.buttonOn(2), 2);
    updateButton(b.buttonOn(3), 3);
    updateButton(b.buttonOn(4), 4);
    updateButton(b.allButtonsOn(), -1);
}

bool InternetButtonEvents::allButtonsOn() {
    return buttonOn(-1);
}

bool InternetButtonEvents::buttonOn(int buttonNumber) {
    bool val;

    switch (buttonNumber) {
        case -1:
            val = allButtonsCurrentOn;
            break;
        case 1:
            val = button0CurrentOn;
            break;
        case 2: 
            val = button1CurrentOn;
            break;
        case 3: 
            val = button2CurrentOn;
            break;
        case 4:
            val = button3CurrentOn;
            break;
        default:
            break;
    }
    
    return val;
}

void InternetButtonEvents::updateButton(bool buttonState, int buttonNumber) {

    if(buttonState){
        // start down timer
        if (!buttonPrevious(buttonNumber)) {
            setButtonOnStart(buttonNumber, millis());
            setButtonPrevious(buttonNumber, true);
        }
        
        // update down timer
        setButtonOnTime(buttonNumber, millis() - buttonOnStart(buttonNumber));
        

        if (buttonOnTime(buttonNumber) > clickThreshold && !buttonOn(buttonNumber)) {
            setButtonCurrent(buttonNumber, true);
            
            if (buttonNumber == -1) {
                if (allButtonsOnCallback) allButtonsOnCallback();
            } else {
                if (buttonOnCallback) buttonOnCallback(buttonNumber);
            }
        }
    }
    else {
        // start up timer
        if (buttonPrevious(buttonNumber)) {
            setButtonOffStart(buttonNumber, millis());
            setButtonPrevious(buttonNumber, false);
        }
        
        // update up timer
        setButtonOffTime(buttonNumber, millis() - buttonOffStart(buttonNumber));
        
        if (buttonOffTime(buttonNumber) > clickThreshold && buttonOn(buttonNumber)) {
            setButtonCurrent(buttonNumber, false);
            
            if (buttonNumber == -1) {
                if (allButtonsOffCallback) allButtonsOffCallback();
                if (allButtonsClickedCallback) allButtonsClickedCallback();
            } else {
                if (buttonOffCallback) buttonOffCallback(buttonNumber);
                if (buttonClickedCallback) buttonClickedCallback(buttonNumber);
            }
        }
    }
}

unsigned long InternetButtonEvents::buttonOnStart(int buttonNumber) {

    unsigned long val;
    
    switch (buttonNumber) {
        case -1:
            val = allButtonsOnStart;
            break;
        case 1:
            val = button0OnStart;
            break;
        case 2: 
            val = button1OnStart;
            break;
        case 3: 
            val = button2OnStart;
            break;
        case 4:
            val = button3OnStart;
            break;
        default:
            break;
    }
    
    return val;
}

unsigned long InternetButtonEvents::buttonOnTime(int buttonNumber) {

    unsigned long val;

    switch (buttonNumber) {
        case -1:
            val = allButtonsOnTime;
            break;
        case 1:
            val = button0OnTime;
            break;
        case 2: 
            val = button1OnTime;
            break;
        case 3: 
            val = button2OnTime;
            break;
        case 4:
            val = button3OnTime;
            break;
        default:
            break;
    }
    
    return val;
}

unsigned long InternetButtonEvents::buttonOffStart(int buttonNumber) {

    unsigned long val;
     
    switch (buttonNumber) {
        case -1:
            val = allButtonsOffStart;
            break;
        case 1:
            val = button0OffStart;
            break;
        case 2: 
            val = button1OffStart;
            break;
        case 3: 
            val = button2OffStart;
            break;
        case 4:
            val = button3OffStart;
            break;
        default:
            break;
    }
    
    return val;
}

unsigned long InternetButtonEvents::buttonOffTime(int buttonNumber) {
    unsigned long val;
    
    switch (buttonNumber) {
        case -1:
            val = allButtonsOffTime;
            break;
        case 1:
            val = button0OffStart;
            break;
        case 2: 
            val = button1OffStart;
            break;
        case 3: 
            val = button2OffStart;
            break;
        case 4:
            val = button3OffStart;
            break;
        default:
            break;
    }
    
    return val;
}

bool InternetButtonEvents::buttonPrevious(int buttonNumber) {
    bool val;
    
    switch (buttonNumber) {
        case -1:
            val = allButtonsPreviousOn;
            break;
        case 1:
            val = button0PreviousOn;
            break;
        case 2: 
            val = button1PreviousOn;
            break;
        case 3: 
            val = button2PreviousOn;
            break;
        case 4:
            val = button3PreviousOn;
            break;
        default:
            break;
    }
    
    return val;
}

void InternetButtonEvents::setButtonOnStart(int buttonNumber, unsigned long val) {

    switch (buttonNumber) {
        case -1:
            allButtonsOnStart = val;
            break;
        case 1:
            button0OnStart = val;
            break;
        case 2: 
            button1OnStart = val;
            break;
        case 3: 
            button2OnStart = val;
            break;
        case 4:
            button3OnStart = val;
            break;
        default:
            break;
    }
    }

void InternetButtonEvents::setButtonOnTime(int buttonNumber, unsigned long val) {

    switch (buttonNumber) {
        case -1:
            allButtonsOnTime = val;
            break;
        case 1:
            button0OnTime = val;
            break;
        case 2: 
            button1OnTime = val;
            break;
        case 3: 
            button2OnTime = val;
            break;
        case 4:
            button3OnTime = val;
            break;
        default:
            break;
    }
}

void InternetButtonEvents::setButtonOffStart(int buttonNumber, unsigned long val) {

    switch (buttonNumber) {
        case -1:
            allButtonsOffStart = val;
            break;
        case 1:
            button0OffStart = val;
            break;
        case 2: 
            button1OffStart = val;
            break;
        case 3: 
            button2OffStart = val;
            break;
        case 4:
            button3OffStart = val;
            break;
        default:
            break;
    }
}

void InternetButtonEvents::setButtonOffTime(int buttonNumber, unsigned long val) {

    switch (buttonNumber) {
        case -1:
            allButtonsOffTime = val;
            break;
        case 1:
            button0OffStart = val;
            break;
        case 2: 
            button1OffStart = val;
            break;
        case 3: 
            button2OffStart = val;
            break;
        case 4:
            button3OffStart = val;
            break;
        default:
            break;
    }
}

void InternetButtonEvents::setButtonCurrent(int buttonNumber, bool val) {

    switch (buttonNumber) {
        case -1:
            allButtonsCurrentOn = val;
            break;
        case 1:
            button0CurrentOn = val;
            break;
        case 2: 
            button1CurrentOn = val;
            break;
        case 3: 
            button2CurrentOn = val;
            break;
        case 4:
            button3CurrentOn = val;
            break;
        default:
            break;
    }
}

void InternetButtonEvents::setButtonPrevious(int buttonNumber, bool val) {

    switch (buttonNumber) {
        case -1:
            allButtonsPreviousOn = val;
            break;
        case 1:
            button0PreviousOn = val;
            break;
        case 2: 
            button1PreviousOn = val;
            break;
        case 3: 
            button2PreviousOn = val;
            break;
        case 4:
            button3PreviousOn = val;
            break;
        default:
            break;
    }
}

