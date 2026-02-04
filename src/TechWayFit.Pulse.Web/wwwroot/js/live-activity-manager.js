/**
 * Live Activity Manager
 * Provides activity creation functionality for the facilitator live page
 */

window.initializeLiveActivityManager = function(sessionCode, facilitatorToken) {
    if (window.liveActivityManager) {
        console.log('Live activity manager already initialized');
        return;
    }
    
    window.liveActivityManager = {
        sessionCode: sessionCode,
        facilitatorToken: facilitatorToken,
        createActivityFromData: async function(activityData) {
            try {
                console.log('Creating activity with data:', activityData);
                
                const headers = {
                    'Content-Type': 'application/json'
                };
                
                // Add facilitator token if available
                if (this.facilitatorToken) {
                    headers['X-Facilitator-Token'] = this.facilitatorToken;
                }
                
                const response = await fetch('/api/sessions/' + this.sessionCode + '/activities', {
                    method: 'POST',
                    headers: headers,
                    body: JSON.stringify(activityData)
                });
                
                if (!response.ok) {
                    const errorText = await response.text();
                    console.error('API Error Response:', errorText);
                    let errorData;
                    try {
                        errorData = JSON.parse(errorText);
                    } catch (e) {
                        errorData = { message: errorText };
                    }
                    throw new Error(errorData.message || errorData.title || 'Failed to create activity');
                }
                
                const result = await response.json();
                console.log('Activity created successfully:', result);
                
                window.location.reload();
                
                return result.data || result;
            } catch (error) {
                console.error('Error creating activity:', error);
                alert('Failed to create activity: ' + error.message);
                throw error;
            }
        }
    };
    
    // Alias for compatibility with activity modals
    window.addActivitiesManager = window.liveActivityManager;
    console.log('Live activity manager initialized for session:', sessionCode);
};
