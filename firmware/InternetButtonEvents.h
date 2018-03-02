// NOTE: This will get published to the Particle Library seperately for easy integration

#include <InternetButton.h>

typedef void (*CallbackType)(void);
typedef void (*CallbackIntType)(int);

class InternetButtonEvents {

    public:
        InternetButtonEvents(InternetButton button);
        InternetButtonEvents(InternetButton button, int threshold);

        ~InternetButtonEvents();

        void 
            onAllButtonsClicked(CallbackType callback),
            onAllButtonsOn(CallbackType callback),
            onAllButtonsOff(CallbackType callback),
            onButtonClicked(CallbackIntType callback),
            onButtonOn(CallbackIntType callback),
            onButtonOff(CallbackIntType callback),
            update();
            
        bool 
            buttonOn(int buttonNumber),
            allButtonsOn();

    private:
        InternetButton b;
        int clickThreshold;
        
        unsigned long 
            previous(int buttonNumber);
        
        CallbackType allButtonsClickedCallback;
        CallbackType allButtonsOnCallback;
        CallbackType allButtonsOffCallback;
        CallbackIntType buttonClickedCallback;
        CallbackIntType buttonOnCallback;
        CallbackIntType buttonOffCallback;
        
        void 
            updateButton(bool buttonState, int buttonNumber),
            setButtonOnStart(int buttonNumber, unsigned long val),
            setButtonOnTime(int buttonNumber, unsigned long val),
            setButtonOffStart(int buttonNumber, unsigned long val),
            setButtonOffTime(int buttonNumber, unsigned long val),
            setButtonCurrent(int buttonNumber, bool val),
            setButtonPrevious(int buttonNumber, bool val);
        
        unsigned long 
            buttonOnStart(int buttonNumber),
            buttonOnTime(int buttonNumber),
            buttonOffStart(int buttonNumber),
            buttonOffTime(int buttonNumber);
        
        bool
            buttonPrevious(int buttonNumber);

        unsigned long 
            allButtonsOnStart,
            allButtonsOnTime,
            allButtonsOffStart,
            allButtonsOffTime;
        
        bool
            allButtonsCurrentOn,
            allButtonsPreviousOn;
     
        unsigned long 
            button0OnStart,
            button0OnTime,
            button0OffStart,
            button0OffTime;
            
        bool
            button0CurrentOn,
            button0PreviousOn;
        
        unsigned long 
            button1OnStart,
            button1OnTime,
            button1OffStart,
            button1OffTime;
            
        bool
            button1CurrentOn,
            button1PreviousOn;
            
        unsigned long 
            button2OnStart,
            button2OnTime,
            button2OffStart,
            button2OffTime;
            
        bool
            button2CurrentOn,
            button2PreviousOn;
        
        unsigned long 
            button3OnStart,
            button3OnTime,
            button3OffStart,
            button3OffTime;
            
        bool
            button3CurrentOn,
            button3PreviousOn;
};