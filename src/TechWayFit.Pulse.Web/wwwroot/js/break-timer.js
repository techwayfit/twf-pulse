/**
 * Break countdown timer utility.
 * Called via Blazor JS interop: window.startBreakCountdown(elementId, remainingSeconds)
 * Counts down from remainingSeconds and shows the remaining time inside the element.
 */
(function () {
    'use strict';

    const activeTimers = new Map();

    window.startBreakCountdown = function (elementId, remainingSeconds) {
        // Clear any existing timer for this element
        if (activeTimers.has(elementId)) {
            clearInterval(activeTimers.get(elementId));
            activeTimers.delete(elementId);
        }

        const el = document.getElementById(elementId);
        if (!el) return;

        let totalSeconds = Math.max(0, Math.round(remainingSeconds));

        function formatTime(seconds) {
            const h = Math.floor(seconds / 3600);
            const m = Math.floor((seconds % 3600) / 60);
            const s = seconds % 60;
            if (h > 0) {
                return pad(h) + ':' + pad(m) + ':' + pad(s);
            }
            return pad(m) + ':' + pad(s);
        }

        function pad(n) {
            return n.toString().padStart(2, '0');
        }

        function tick() {
            if (totalSeconds <= 0) {
                clearInterval(timerId);
                activeTimers.delete(elementId);
                updateElement(el, '00:00', true);
                return;
            }
            totalSeconds--;
            updateElement(el, formatTime(totalSeconds), false);
        }

        function updateElement(el, timeStr, expired) {
            // Support both a container with a .timer-value span and a plain element
            const valueEl = el.querySelector('.timer-value');
            if (valueEl) {
                valueEl.textContent = timeStr;
            } else {
                el.textContent = timeStr;
            }
            if (expired) {
                el.classList.add('timer-expired');
            }
        }

        // Set initial value then start countdown
        updateElement(el, formatTime(totalSeconds), false);
        const timerId = setInterval(tick, 1000);
        activeTimers.set(elementId, timerId);
    };

    window.stopBreakCountdown = function (elementId) {
        if (activeTimers.has(elementId)) {
            clearInterval(activeTimers.get(elementId));
            activeTimers.delete(elementId);
        }
    };
})();
