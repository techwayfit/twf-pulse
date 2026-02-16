// ============================================
// PRESENTER MODE - JAVASCRIPT
// Handles fullscreen, timer, and keyboard controls
// ============================================

// Wrap everything in IIFE to avoid duplicate declarations
(function() {
    'use strict';

    // Check if already initialized
    if (window.PresenterMode && window.PresenterMode.initialized) {
        console.log('PresenterMode already initialized, skipping...');
        return;
    }

    let presenterTimerInterval = null;

    /**
     * Enters fullscreen presentation mode
     */
    window.enterPresenterFullscreen = function() {
    const elem = document.documentElement;
    
    if (elem.requestFullscreen) {
        elem.requestFullscreen().catch(err => {
            console.log('Fullscreen request denied:', err);
        });
    } else if (elem.webkitRequestFullscreen) {
        elem.webkitRequestFullscreen();
    } else if (elem.msRequestFullscreen) {
        elem.msRequestFullscreen();
    }
    };

    /**
     * Exits fullscreen presentation mode
     */
    window.exitPresenterFullscreen = function() {
        // Check if actually in fullscreen before trying to exit
        const isFullscreen = !!(document.fullscreenElement || 
                               document.webkitFullscreenElement || 
                               document.mozFullScreenElement || 
                               document.msFullscreenElement);
        
        if (!isFullscreen) {
            console.log('Not in fullscreen mode, nothing to exit');
            return;
        }

        if (document.exitFullscreen) {
            document.exitFullscreen().catch(err => {
                console.log('Exit fullscreen failed:', err);
            });
        } else if (document.webkitExitFullscreen) {
            document.webkitExitFullscreen();
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
        }
    };

    /**
     * Starts the activity timer
     */
    window.startPresenterTimer = function() {
    console.log('startPresenterTimer called');
    const timerEl = document.getElementById('activity-timer-presenter');
    console.log('Presenter timer element:', timerEl);

    if (!timerEl) {
        console.log('No presenter timer element found');
        return;
    }

    const durationMinutes = parseInt(timerEl.dataset.duration);
    const openedAt = new Date(timerEl.dataset.openedAt);
    const timerText = timerEl.querySelector('.timer-text');

    console.log('Duration:', durationMinutes, 'OpenedAt:', openedAt, 'TimerText:', timerText);

    if (presenterTimerInterval) {
        clearInterval(presenterTimerInterval);
    }

    function updateTimer() {
        const now = new Date();
        const elapsedMs = now - openedAt;
        const elapsedSeconds = Math.floor(elapsedMs / 1000);
        const totalSeconds = durationMinutes * 60;
        const remainingSeconds = Math.max(0, totalSeconds - elapsedSeconds);

        const minutes = Math.floor(remainingSeconds / 60);
        const seconds = remainingSeconds % 60;

        timerText.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;

        // Visual feedback
        if (remainingSeconds === 0) {
            timerEl.classList.add('timer-expired');
            timerText.textContent = "Time's Up!";
        } else if (remainingSeconds <= 60) {
            timerEl.classList.add('timer-warning');
            timerEl.classList.remove('timer-expired');
        } else {
            timerEl.classList.remove('timer-warning', 'timer-expired');
        }
    }

    updateTimer();
    presenterTimerInterval = setInterval(updateTimer, 1000);
    };

    /**
     * Sets up ESC key handler to exit presenter mode
     * @param {string} sessionCode - The session code to navigate to
     */
    window.setupPresenterKeyboardHandler = function(sessionCode) {
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            // Let browser handle ESC for exiting fullscreen naturally
            // Then navigate back to live view
            const isFullscreen = !!(document.fullscreenElement || 
                                   document.webkitFullscreenElement || 
                                   document.mozFullScreenElement || 
                                   document.msFullscreenElement);
            
            if (isFullscreen) {
                // Let browser exit fullscreen with ESC
                // Then navigate after a delay
                setTimeout(() => {
                    const stillFullscreen = !!(document.fullscreenElement || 
                                              document.webkitFullscreenElement || 
                                              document.mozFullScreenElement || 
                                              document.msFullscreenElement);
                    if (!stillFullscreen) {
                        window.location.href = '/facilitator/live?code=' + sessionCode;
                    }
                }, 300);
            }
        }
    });
    };

    /**
     * Initializes presenter mode features (timer, keyboard)
     * Note: Fullscreen must be triggered by user gesture (button click)
     * @param {string} sessionCode - The session code for navigation
     */
    window.initializePresenterMode = function(sessionCode) {
    // Don't auto-enter fullscreen (blocked by browsers)
    // User must click the "Enter Fullscreen" button
    
    // Start timer if element exists
    startPresenterTimer();
    
    // Setup keyboard handler
    window.setupPresenterKeyboardHandler(sessionCode);
    };

    /**
     * Cleans up presenter mode (clears timer)
     */
    window.cleanupPresenterMode = function() {
    if (presenterTimerInterval) {
        clearInterval(presenterTimerInterval);
        presenterTimerInterval = null;
    }
    };

    // Auto-initialize timer when page loads
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', window.startPresenterTimer);
    } else {
        window.startPresenterTimer();
    }

    // Monitor DOM for timer element changes (Blazor re-renders)
    let lastPresenterTimerElement = document.getElementById('activity-timer-presenter');
    const presenterObserver = new MutationObserver(() => {
    const currentTimerElement = document.getElementById('activity-timer-presenter');

    // Only restart timer if the element was added
    if (currentTimerElement && !lastPresenterTimerElement) {
        window.startPresenterTimer();
    }
    // Clear timer if element was removed
    else if (!currentTimerElement && lastPresenterTimerElement && presenterTimerInterval) {
        clearInterval(presenterTimerInterval);
        presenterTimerInterval = null;
    }

    lastPresenterTimerElement = currentTimerElement;
    });

    presenterObserver.observe(document.body, { childList: true, subtree: true });

    // Mark as initialized
    window.PresenterMode = {
        initialized: true,
        version: '1.0.0'
    };

})(); // End IIFE
