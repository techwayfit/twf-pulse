/**
 * Blazor Mobile Reconnection Handler
 * Detects when Blazor circuit cannot reconnect (expired) and forces a reload.
 * This prevents stale UI when mobile apps are backgrounded/resumed.
 */
(function () {
    let reconnectionAttempts = 0;
    const MAX_RECONNECTION_ATTEMPTS = 8;

    Blazor.start({
     reconnectionOptions: {
            maxRetries: MAX_RECONNECTION_ATTEMPTS,
      retryIntervalMilliseconds: (retryCount) => {
              // Exponential backoff: 0s, 2s, 10s, 30s, 60s, 120s, 240s, 300s (5 min)
       const delays = [0, 2000, 10000, 30000, 60000, 120000, 240000, 300000];
    return delays[Math.min(retryCount, delays.length - 1)];
   }
 },
        reconnectionHandler: {
            onConnectionDown: () => {
     console.log('[Pulse] Connection lost - attempting reconnect...');
                reconnectionAttempts++;

    // Show reconnecting UI (optional - you can create a modal)
  const indicator = document.getElementById('reconnecting-indicator');
           if (indicator) {
           indicator.classList.remove('d-none');
  }
       },
         onConnectionUp: () => {
       console.log('[Pulse] Connection restored');
           reconnectionAttempts = 0;

     // Hide reconnecting UI
      const indicator = document.getElementById('reconnecting-indicator');
    if (indicator) {
       indicator.classList.add('d-none');
       }
       },
   onReconnectionFailed: () => {
           console.error('[Pulse] Reconnection failed - circuit expired. Reloading page...');

     // Circuit expired - force reload to get fresh state
        setTimeout(() => {
               window.location.reload();
                }, 2000); // Give user 2 seconds to see the error message
  }
        }
    });

    // Mobile-specific: Detect app resume from background
    document.addEventListener('visibilitychange', function () {
     if (document.visibilityState === 'visible') {
 console.log('[Pulse] App resumed - checking connection...');

// If connection is down and we're past retention period, reload immediately
      if (reconnectionAttempts >= MAX_RECONNECTION_ATTEMPTS) {
    console.warn('[Pulse] Circuit likely expired - reloading...');
                window.location.reload();
      }
        }
    });
})();
