/**
 * Activity Form Modals JavaScript
 * Handles the modal forms for creating different activity types
 */

(function() {
    // Poll option counter
    let pollOptionCount = 0;
    let pollModalInitialized = false;
    let wordCloudModalInitialized = false;

    // Initialize modals when they're shown (not on DOMContentLoaded, since modals are loaded by Blazor)
    document.addEventListener('DOMContentLoaded', () => {
        // Poll modal initialization
        const pollModal = document.getElementById('pollModal');
        if (pollModal) {
            pollModal.addEventListener('shown.bs.modal', () => {
                if (!pollModalInitialized) {
                    initializePollModal();
                    pollModalInitialized = true;
                }
            });
        }
        
        // Word cloud modal initialization
        const wcModal = document.getElementById('wordcloudModal');
        if (wcModal) {
            wcModal.addEventListener('shown.bs.modal', () => {
                if (!wordCloudModalInitialized) {
                    initializeWordCloudModal();
                    wordCloudModalInitialized = true;
                }
            });
        }
    });

    function initializePollModal() {
        // Only add initial options if container is empty (creating new activity)
        const container = document.getElementById('pollOptionsContainer');
        if (container && container.children.length === 0) {
            addPollOption();
            addPollOption();
        }
    }

    function initializeWordCloudModal() {
        // Toggle max submissions field
        const wcAllowMultiple = document.getElementById('wcAllowMultiple');
        if (wcAllowMultiple) {
            wcAllowMultiple.addEventListener('change', (e) => {
                const container = document.getElementById('wcMaxSubmissionsContainer');
                if (container) {
                    container.style.display = e.target.checked ? 'block' : 'none';
                }
            });
        }
    }

    window.addPollOption = function() {
    pollOptionCount++;
    const container = document.getElementById('pollOptionsContainer');
    const optionHtml = `
        <div class="card mb-2" id="pollOption${pollOptionCount}">
            <div class="card-body">
                <div class="row g-2">
                    <div class="col-md-5">
                        <input type="text" class="form-control poll-option-label" 
                               placeholder="Option label" required />
                    </div>
                    <div class="col-md-6">
                        <input type="text" class="form-control poll-option-desc" 
                               placeholder="Description (optional)" />
                    </div>
                    <div class="col-md-1">
                        ${pollOptionCount > 2 ? `<button type="button" class="btn btn-sm btn-danger w-100" onclick="removePollOption(${pollOptionCount})">×</button>` : ''}
                    </div>
                </div>
            </div>
        </div>
    `;
    container.insertAdjacentHTML('beforeend', optionHtml);
};

window.removePollOption = function(id) {
    document.getElementById(`pollOption${id}`)?.remove();
};

window.savePollActivity = async function() {
    const title = document.getElementById('pollTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }
    
    // Collect poll options
    const options = [];
    document.querySelectorAll('.poll-option-label').forEach((input, index) => {
        if (input.value) {
            const descInput = document.querySelectorAll('.poll-option-desc')[index];
            options.push({
                id: `option_${index}`,
                label: input.value,
                description: descInput?.value || null
            });
        }
    });
    
    if (options.length < 2) {
        alert('Please add at least 2 poll options');
        return;
    }
    
    const config = {
        options: options,
        allowMultiple: document.getElementById('pollAllowMultiple').checked,
        maxResponsesPerParticipant: parseInt(document.getElementById('pollMaxResponses').value)
    };
    
    const activity = {
        type: 'Poll',
        title: title,
        prompt: document.getElementById('pollPrompt').value || null,
        durationMinutes: parseInt(document.getElementById('pollDuration').value) || 5,
        config: JSON.stringify(config)
    };
    
    console.log('Saving poll activity:', activity);
    
    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }
    
    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        // Only hide modal if creation was successful (reload will happen)
        // bootstrap.Modal.getInstance(document.getElementById('pollModal'))?.hide();
    } catch (error) {
        console.error('Failed to save poll activity:', error);
        // Don't hide modal on error so user can retry
    }
    // Note: Modal will be hidden after page reload
    resetPollForm();
    resetPollForm();
};

window.saveWordCloudActivity = async function() {
    const title = document.getElementById('wcTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }
    
    const allowMultiple = document.getElementById('wcAllowMultiple').checked;
    const config = {
        maxSubmissionsPerParticipant: allowMultiple ? parseInt(document.getElementById('wcMaxSubmissions').value) : 1
    };
    
    const activity = {
        type: 'WordCloud',
        title: title,
        prompt: document.getElementById('wcPrompt').value || null,
        durationMinutes: parseInt(document.getElementById('wcDuration').value) || 5,
        config: JSON.stringify(config)
    };
    
    console.log('Saving word cloud activity:', activity);
    
    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }
    
    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        // Don't hide modal here - page will reload anyway
        // bootstrap.Modal.getInstance(document.getElementById('wordcloudModal')).hide();
    } catch (error) {
        console.error('Error creating word cloud activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        resetWordCloudForm();
    }
};

window.saveRatingActivity = async function() {
    const title = document.getElementById('ratingTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }
    
    const config = {
        maxRating: parseInt(document.getElementById('ratingScale').value),
        maxResponsesPerParticipant: parseInt(document.getElementById('ratingMaxResponses').value),
        lowLabel: document.getElementById('ratingLowLabel').value || null,
        highLabel: document.getElementById('ratingHighLabel').value || null
    };
    
    const activity = {
        type: 'Rating',
        title: title,
        prompt: document.getElementById('ratingPrompt').value || null,
        durationMinutes: parseInt(document.getElementById('ratingDuration').value) || 5,
        config: JSON.stringify(config)
    };
    
    console.log('Saving rating activity:', activity);
    
    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }
    
    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        // Don't hide modal here - page will reload anyway
        // bootstrap.Modal.getInstance(document.getElementById('ratingModal')).hide();
    } catch (error) {
        console.error('Error creating rating activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        resetRatingForm();
    }
};

window.saveFeedbackActivity = async function() {
    const title = document.getElementById('feedbackTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }
    
    const config = {
        maxResponsesPerParticipant: parseInt(document.getElementById('feedbackMaxResponses').value)
    };
    
    const activity = {
        type: 'GeneralFeedback',
        title: title,
        prompt: document.getElementById('feedbackPrompt').value || null,
        durationMinutes: parseInt(document.getElementById('feedbackDuration').value) || 5,
        config: JSON.stringify(config)
    };
    
    console.log('Saving feedback activity:', activity);
    
    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }
    
    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        // Don't hide modal here - page will reload anyway
        // bootstrap.Modal.getInstance(document.getElementById('feedbackModal')).hide();
    } catch (error) {
        console.error('Error creating feedback activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        resetFeedbackForm();
    }
};

window.saveQuadrantActivity = async function() {
    const title = document.getElementById('quadrantTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }
    
    const config = {
        xAxisLabel: document.getElementById('quadrantXAxis').value || 'X Axis',
        yAxisLabel: document.getElementById('quadrantYAxis').value || 'Y Axis',
        scale: parseInt(document.getElementById('quadrantScale').value) || 10,
        topLeftLabel: document.getElementById('quadrantTopLeft').value || 'Top Left',
        topRightLabel: document.getElementById('quadrantTopRight').value || 'Top Right',
        bottomLeftLabel: document.getElementById('quadrantBottomLeft').value || 'Bottom Left',
        bottomRightLabel: document.getElementById('quadrantBottomRight').value || 'Bottom Right'
    };
    
    const activity = {
        type: 'Quadrant',
        title: title,
        prompt: document.getElementById('quadrantPrompt').value || null,
        durationMinutes: parseInt(document.getElementById('quadrantDuration').value) || 10,
        config: JSON.stringify(config)
    };
    
    console.log('Saving quadrant activity:', activity);
    
    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }
    
    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        // Don't hide modal here - page will reload anyway
        // bootstrap.Modal.getInstance(document.getElementById('quadrantModal')).hide();
    } catch (error) {
        console.error('Error creating quadrant activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        resetQuadrantForm();
    }
};

window.saveFiveWhysActivity = async function() {
    const title = document.getElementById('fivewhysTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }
    
    const activity = {
        type: 'FiveWhys',
        title: title,
        prompt: document.getElementById('fivewhysPrompt').value || null,
        durationMinutes: parseInt(document.getElementById('fivewhysDuration').value) || 10,
        config: '{}'
    };
    
    console.log('Saving five whys activity:', activity);
    
    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }
    
    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        // Don't hide modal here - page will reload anyway
        // bootstrap.Modal.getInstance(document.getElementById('fivewhysModal')).hide();
    } catch (error) {
        console.error('Error creating five whys activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        resetFiveWhysForm();
    }
};

// Reset functions
function resetPollForm() {
    document.getElementById('pollForm').reset();
    document.getElementById('pollOptionsContainer').innerHTML = '';
    pollOptionCount = 0;
    addPollOption();
    addPollOption();
}

function resetWordCloudForm() {
    document.getElementById('wordcloudForm').reset();
    document.getElementById('wcMaxSubmissionsContainer').style.display = 'none';
}

function resetRatingForm() {
    document.getElementById('ratingForm').reset();
}

function resetFeedbackForm() {
    document.getElementById('feedbackForm').reset();
}

function resetQuadrantForm() {
    document.getElementById('quadrantForm').reset();
}

function resetFiveWhysForm() {
    document.getElementById('fivewhysForm').reset();
}

    // Expose initialization functions globally for external use
    window.initializePollModal = initializePollModal;
    window.initializeWordCloudModal = initializeWordCloudModal;
})();
