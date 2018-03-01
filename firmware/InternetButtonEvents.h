// NOTE: This will get published to the Particle Library seperately for easy integration

#include <InternetButton.h>

typedef void (*CallbackType)(void);

class InternetButtonEvents {

    public:
        InternetButtonEvents(InternetButton button);
        InternetButtonEvents(InternetButton button, int threshold);

        ~InternetButtonEvents();

        void 
            onAllButtonsClicked(CallbackType callback),
            onAllButtonsOn(CallbackType callback),
            onAllButtonsOff(CallbackType callback),
            update();
            
        bool allButtonsOn();


    private:
        InternetButton b;
        CallbackType allButtonsClickedCallback;
        CallbackType allButtonsDownCallback;
        CallbackType allButtonsUpCallback;

        unsigned long downStart;
        unsigned long downTime;
        
        unsigned long upStart;
        unsigned long upTime;
                    
        int clickThreshold;
        bool currentAllButtonsOn;
        bool previousAllButtonsOn;

};


