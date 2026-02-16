// Presentation Mode JavaScript
// Handles fullscreen, keyboard navigation, and other presentation features

(function() {
    'use strict';

    // Fullscreen Management
    window.enterPresentationFullscreen = function() {
        const elem = document.documentElement;
        
        if (elem.requestFullscreen) {
            return elem.requestFullscreen();
        } else if (elem.webkitRequestFullscreen) {
            return elem.webkitRequestFullscreen();
        } else if (elem.mozRequestFullScreen) {
            return elem.mozRequestFullScreen();
        } else if (elem.msRequestFullscreen) {
            return elem.msRequestFullscreen();
        }
        
        return Promise.resolve();
    };

    window.exitPresentationFullscreen = function() {
        if (document.exitFullscreen) {
            return document.exitFullscreen();
        } else if (document.webkitExitFullscreen) {
            return document.webkitExitFullscreen();
        } else if (document.mozCancelFullScreen) {
            return document.mozCancelFullScreen();
        } else if (document.msExitFullscreen) {
            return document.msExitFullscreen();
        }
        
        return Promise.resolve();
    };

    window.isPresentationFullscreen = function() {
        return !!(document.fullscreenElement || 
                  document.webkitFullscreenElement || 
                  document.mozFullScreenElement || 
                  document.msFullscreenElement);
    };

    // Keyboard Navigation
    let keyboardHandlersAttached = false;

    window.initializePresentationKeyboard = function(onPrevious, onNext, onExit) {
        if (keyboardHandlersAttached) return;

        const handleKeyPress = (e) => {
            // Arrow keys for navigation
            if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
                e.preventDefault();
                if (onPrevious && typeof onPrevious.invokeMethodAsync === 'function') {
                    onPrevious.invokeMethodAsync('Invoke');
                }
            } else if (e.key === 'ArrowRight' || e.key === 'ArrowDown' || e.key === ' ') {
                e.preventDefault();
                if (onNext && typeof onNext.invokeMethodAsync === 'function') {
                    onNext.invokeMethodAsync('Invoke');
                }
            }
            // Escape to exit
            else if (e.key === 'Escape') {
                if (window.isPresentationFullscreen()) {
                    window.exitPresentationFullscreen();
                } else if (onExit && typeof onExit.invokeMethodAsync === 'function') {
                    onExit.invokeMethodAsync('Invoke');
                }
            }
            // F key to toggle fullscreen
            else if (e.key === 'f' || e.key === 'F') {
                e.preventDefault();
                if (window.isPresentationFullscreen()) {
                    window.exitPresentationFullscreen();
                } else {
                    window.enterPresentationFullscreen();
                }
            }
        };

        document.addEventListener('keydown', handleKeyPress);
        keyboardHandlersAttached = true;

        // Return cleanup function
        return () => {
            document.removeEventListener('keydown', handleKeyPress);
            keyboardHandlersAttached = false;
        };
    };

    // Prevent context menu in presentation mode
    window.disablePresentationContextMenu = function() {
        document.addEventListener('contextmenu', (e) => {
            if (document.querySelector('.presentation-container')) {
                e.preventDefault();
            }
        });
    };

    // Monitor fullscreen changes
    window.monitorFullscreenChanges = function(callback) {
        const events = ['fullscreenchange', 'webkitfullscreenchange', 'mozfullscreenchange', 'MSFullscreenChange'];
        
        const handler = () => {
            const isFullscreen = window.isPresentationFullscreen();
            if (callback && typeof callback.invokeMethodAsync === 'function') {
                callback.invokeMethodAsync('Invoke', isFullscreen);
            }
        };

        events.forEach(event => {
            document.addEventListener(event, handler);
        });

        return () => {
            events.forEach(event => {
                document.removeEventListener(event, handler);
            });
        };
    };

    // Auto-hide cursor after inactivity
    let cursorTimeout;
    let cursorHidden = false;

    window.enableCursorAutoHide = function(delayMs = 3000) {
        const hideCursor = () => {
            if (!cursorHidden) {
                document.body.style.cursor = 'none';
                cursorHidden = true;
            }
        };

        const showCursor = () => {
            if (cursorHidden) {
                document.body.style.cursor = '';
                cursorHidden = false;
            }
            
            clearTimeout(cursorTimeout);
            cursorTimeout = setTimeout(hideCursor, delayMs);
        };

        document.addEventListener('mousemove', showCursor);
        document.addEventListener('mousedown', showCursor);

        // Initial timeout
        cursorTimeout = setTimeout(hideCursor, delayMs);

        return () => {
            clearTimeout(cursorTimeout);
            document.removeEventListener('mousemove', showCursor);
            document.removeEventListener('mousedown', showCursor);
            document.body.style.cursor = '';
            cursorHidden = false;
        };
    };

    // Wake lock to prevent screen from sleeping during presentation
    let wakeLock = null;

    window.requestPresentationWakeLock = async function() {
        if ('wakeLock' in navigator) {
            try {
                wakeLock = await navigator.wakeLock.request('screen');
                console.log('Wake lock activated');
                
                wakeLock.addEventListener('release', () => {
                    console.log('Wake lock released');
                });
                
                return true;
            } catch (err) {
                console.error('Wake lock request failed:', err);
                return false;
            }
        }
        return false;
    };

    window.releasePresentationWakeLock = async function() {
        if (wakeLock !== null) {
            try {
                await wakeLock.release();
                wakeLock = null;
                console.log('Wake lock manually released');
            } catch (err) {
                console.error('Wake lock release failed:', err);
            }
        }
    };

    // Cleanup on page unload
    window.addEventListener('beforeunload', () => {
        window.releasePresentationWakeLock();
    });

    // Export for debugging
    window.PresentationMode = {
        enterFullscreen: window.enterPresentationFullscreen,
        exitFullscreen: window.exitPresentationFullscreen,
        isFullscreen: window.isPresentationFullscreen,
        requestWakeLock: window.requestPresentationWakeLock,
        releaseWakeLock: window.releasePresentationWakeLock
    };

    console.log('Presentation Mode JavaScript initialized');
})();
