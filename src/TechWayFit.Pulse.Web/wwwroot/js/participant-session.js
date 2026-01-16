// Participant session management for TechWayFit Pulse
window.pulseParticipantSession = {
    // Clean up old participant sessions (older than 8 hours)
    cleanupOldSessions: function() {
        try {
            const now = Math.floor(Date.now() / 1000); // Current Unix timestamp
            const maxAge = 8 * 60 * 60; // 8 hours in seconds
            
            for (let i = localStorage.length - 1; i >= 0; i--) {
                const key = localStorage.key(i);
                
                if (key && key.startsWith('pulse_session_time_')) {
                    const timestamp = parseInt(localStorage.getItem(key) || '0');
                    
                    if (now - timestamp > maxAge) {
                        // Remove old session data
                        const sessionCode = key.replace('pulse_session_time_', '');
                        localStorage.removeItem(key);
                        localStorage.removeItem(`pulse_participant_${sessionCode}`);
                        console.log(`Cleaned up old session: ${sessionCode}`);
                    }
                }
            }
        } catch (error) {
            console.warn('Failed to cleanup old sessions:', error);
        }
    },
    
    // Check if participant is already in a session
    hasActiveSession: function(sessionCode) {
        try {
            const participantKey = `pulse_participant_${sessionCode}`;
            const timestampKey = `pulse_session_time_${sessionCode}`;
            
            const participantId = localStorage.getItem(participantKey);
            const timestamp = localStorage.getItem(timestampKey);
            
            if (participantId && timestamp) {
                const now = Math.floor(Date.now() / 1000);
                const sessionAge = now - parseInt(timestamp);
                const maxAge = 8 * 60 * 60; // 8 hours
                
                if (sessionAge < maxAge) {
                    return participantId;
                }
            }
            
            return null;
        } catch (error) {
            console.warn('Failed to check active session:', error);
            return null;
        }
    },
    
    // Remove participant from session (for logout/leave functionality)
    removeFromSession: function(sessionCode) {
        try {
            localStorage.removeItem(`pulse_participant_${sessionCode}`);
            localStorage.removeItem(`pulse_session_time_${sessionCode}`);
        } catch (error) {
            console.warn('Failed to remove from session:', error);
        }
    }
};

// Clean up old sessions on page load
document.addEventListener('DOMContentLoaded', function() {
    window.pulseParticipantSession.cleanupOldSessions();
});