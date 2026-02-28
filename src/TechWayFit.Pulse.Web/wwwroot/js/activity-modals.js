/**
 * Activity Form Modals JavaScript
 * Handles the modal forms for creating different activity types
 */

(function() {
    // Poll option counter
    let pollOptionCount = 0;
    let pollModalInitialized = false;
    let wordCloudModalInitialized = false;

    function hideActivityModal(id) {
        const el = document.getElementById(id);
        if (!el) return;
        const instance = bootstrap.Modal.getInstance(el);
        if (instance) { instance.hide(); } else { bootstrap.Modal.getOrCreateInstance(el).hide(); }
    }

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
        hideActivityModal('pollModal');
    } catch (error) {
        console.error('Failed to save poll activity:', error);
        alert('Failed to create activity: ' + error.message);
    }
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
        hideActivityModal('wordcloudModal');
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
        hideActivityModal('ratingModal');
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
        hideActivityModal('feedbackModal');
    } catch (error) {
        console.error('Error creating feedback activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        resetFeedbackForm();
    }
};

window.saveQuadrantActivity = async function() {
    const title = document.getElementById('quadrantTitle')?.value?.trim();
    if (!title) { alert('Please enter an activity title'); return; }

    const rawItems = document.getElementById('quadrantItems')?.value || '';
    const items = rawItems.split('\n').map(s => s.trim()).filter(s => s.length > 0);
    if (items.length === 0) { alert('Please add at least one item to score.'); return; }

    const sharesY = document.getElementById('quadrantYSharesX')?.checked ?? true;
    const config = {
        xAxisLabel: document.getElementById('quadrantXAxis')?.value?.trim() || 'Complexity',
        yAxisLabel: document.getElementById('quadrantYAxis')?.value?.trim() || 'Effort',
        xScoreOptions: window.quadrantModal_collectScoreTable('x'),
        yScoreOptions: sharesY ? [] : window.quadrantModal_collectScoreTable('y'),
        items,
        bubbleSizeMode: parseInt(document.getElementById('quadrantBubbleSize')?.value ?? '0', 10),
        allowNotes: document.getElementById('quadrantAllowNotes')?.checked ?? false,
        q1Label: document.getElementById('quadrantQ1Label')?.value?.trim() || '',
        q2Label: document.getElementById('quadrantQ2Label')?.value?.trim() || '',
        q3Label: document.getElementById('quadrantQ3Label')?.value?.trim() || '',
        q4Label: document.getElementById('quadrantQ4Label')?.value?.trim() || ''
    };

    if (config.xScoreOptions.length === 0) { alert('Please add at least one X-axis score option.'); return; }

    const activity = {
        type: 'Quadrant',
        title,
        prompt: document.getElementById('quadrantPrompt')?.value || null,
        durationMinutes: parseInt(document.getElementById('quadrantDuration')?.value) || 10,
        config: JSON.stringify(config)
    };

    if (!window.addActivitiesManager) { alert('Activity manager not initialized. Please refresh the page.'); return; }

    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        hideActivityModal('quadrantModal');
    } catch (error) {
        console.error('Error creating quadrant activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        resetQuadrantForm();
    }
};

// ── Score table helpers ──────────────────────────────────────────────────────

window.quadrantModal_renderScoreTable = function(axis, options) {
    const tbody = document.getElementById(`quadrant${axis.toUpperCase()}ScoreBody`);
    if (!tbody) return;
    tbody.innerHTML = '';
    (options || []).forEach((opt, idx) => {
        tbody.appendChild(quadrantModal_buildRow(axis, idx, opt.value, opt.label, opt.description));
    });
};

window.quadrantModal_collectScoreTable = function(axis) {
    const rows = document.querySelectorAll(`#quadrant${axis.toUpperCase()}ScoreBody tr`);
    const opts = [];
    rows.forEach(row => {
        const val = row.querySelector('.qscore-value')?.value?.trim() || '';
        const lbl = row.querySelector('.qscore-label')?.value?.trim() || '';
        const desc = row.querySelector('.qscore-desc')?.value?.trim() || null;
        if (val) opts.push({ value: val, label: lbl, description: desc || null });
    });
    return opts;
};

window.quadrantModal_toggleYPanel = function(hide) {
    const panel = document.getElementById('quadrantYScorePanel');
    if (panel) panel.style.display = hide ? 'none' : '';
};

window.quadrantModal_addRow = function(axis) {
    const tbody = document.getElementById(`quadrant${axis.toUpperCase()}ScoreBody`);
    if (!tbody) return;
    const idx = tbody.querySelectorAll('tr').length;
    tbody.appendChild(quadrantModal_buildRow(axis, idx, '', '', null));
};

window.quadrantModal_applyPreset = function(axis, preset) {
    let opts;
    if (preset === 'fibonacci') opts = QuadrantActivity.fibonacciPreset();
    else if (preset === 'odd')   opts = QuadrantActivity.oddPreset();
    else if (preset === '1-5')   opts = QuadrantActivity.defaultNumeric(1, 5);
    else                          opts = QuadrantActivity.defaultNumeric(1, 10);
    window.quadrantModal_renderScoreTable(axis, opts);
};

function quadrantModal_buildRow(axis, idx, value, label, description) {
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td><input type="text" class="form-control form-control-sm qscore-value" value="${escapeHtml(value)}" placeholder="e.g. 5" /></td>
        <td><input type="text" class="form-control form-control-sm qscore-label" value="${escapeHtml(label)}" placeholder="e.g. Medium" /></td>
        <td><input type="text" class="form-control form-control-sm qscore-desc" value="${escapeHtml(description || '')}" placeholder="Optional description" /></td>
        <td><button type="button" class="btn btn-sm btn-outline-danger" onclick="this.closest('tr').remove()"><i class="fas fa-times"></i></button></td>`;
    return tr;
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}


window.saveAiSummaryActivity = async function() {
    const title = document.getElementById('aisummaryTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }

    const config = {
        customPromptAddition: document.getElementById('aisummaryCustomPromptAddition').value || '',
        showActivityBreakdown: document.getElementById('aisummaryShowActivityBreakdown').checked,
        generatedSummary: '',
        isGenerating: false
    };

    const activity = {
        type: 'AiSummary',
        title: title,
        prompt: document.getElementById('aisummarySubtitle').value || null,
        durationMinutes: parseInt(document.getElementById('aisummaryDuration').value) || 5,
        config: JSON.stringify(config)
    };

    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }

    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        hideActivityModal('aisummaryModal');
    } catch (error) {
        console.error('Error creating AI Summary activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        document.getElementById('aisummaryForm').reset();
        document.getElementById('aisummaryShowActivityBreakdown').checked = true;
    }
};

window.saveQnAActivity = async function() {
    const title = document.getElementById('qnaTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }

    const config = {
        maxQuestionsPerParticipant: parseInt(document.getElementById('qnaMaxQuestions').value) || 3,
        maxQuestionLength: parseInt(document.getElementById('qnaMaxLength').value) || 300,
        allowAnonymous: document.getElementById('qnaAllowAnonymous').checked,
        allowUpvoting: document.getElementById('qnaAllowUpvoting').checked
    };

    const activity = {
        type: 'QnA',
        title: title,
        prompt: document.getElementById('qnaPrompt').value || null,
        durationMinutes: parseInt(document.getElementById('qnaDuration').value) || 10,
        config: JSON.stringify(config)
    };

    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }

    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        hideActivityModal('qnaModal');
    } catch (error) {
        console.error('Error creating Q&A activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        document.getElementById('qnaForm').reset();
        document.getElementById('qnaMaxQuestions').value = '3';
        document.getElementById('qnaMaxLength').value = '300';
        document.getElementById('qnaDuration').value = '10';
        document.getElementById('qnaAllowAnonymous').checked = true;
        document.getElementById('qnaAllowUpvoting').checked = true;
    }
};

window.saveFiveWhysActivity = async function() {
    const title = document.getElementById('fivewhysTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }

    const rootQuestion = document.getElementById('fivewhysRootQuestion').value;
    if (!rootQuestion) {
        alert('Please enter the initial problem question');
        return;
    }

    const config = {
        rootQuestion: rootQuestion,
        context: document.getElementById('fivewhysContext').value || null,
        maxDepth: parseInt(document.getElementById('fivewhysMaxDepth').value) || 5
    };

    const activity = {
        type: 'FiveWhys',
        title: title,
        prompt: rootQuestion,
        durationMinutes: parseInt(document.getElementById('fivewhysDuration').value) || 15,
        config: JSON.stringify(config)
    };

    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }

    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        hideActivityModal('fivewhysModal');
    } catch (error) {
        console.error('Error creating 5 Whys activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        resetFiveWhysForm();
    }
};

window.saveBreakActivity = async function() {
    const title = document.getElementById('breakTitle').value;
    if (!title) {
        alert('Please enter an activity title');
        return;
    }

    const breakDuration = parseInt(document.getElementById('breakDurationMinutes').value) || 15;
    const config = {
        message: document.getElementById('breakMessage').value || 'Take a short break. We\'ll resume shortly!',
        durationMinutes: breakDuration,
        showCountdown: document.getElementById('breakShowCountdown').checked,
        allowReadySignal: document.getElementById('breakAllowReadySignal').checked
    };

    const activity = {
        type: 'Break',
        title: title,
        prompt: config.message,
        durationMinutes: breakDuration,
        config: JSON.stringify(config)
    };

    if (!window.addActivitiesManager) {
        console.error('addActivitiesManager not found!');
        alert('Activity manager not initialized. Please refresh the page.');
        return;
    }

    try {
        await window.addActivitiesManager.createActivityFromData(activity);
        hideActivityModal('breakModal');
    } catch (error) {
        console.error('Error creating Break activity:', error);
        alert('Failed to create activity: ' + error.message);
    } finally {
        document.getElementById('breakForm').reset();
        document.getElementById('breakDurationMinutes').value = '15';
        document.getElementById('breakShowCountdown').checked = true;
        document.getElementById('breakAllowReadySignal').checked = true;
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
    document.getElementById('quadrantForm')?.reset();
    window.quadrantModal_renderScoreTable('x', QuadrantActivity.defaultNumeric(1, 10));
    window.quadrantModal_renderScoreTable('y', []);
    window.quadrantModal_toggleYPanel(true);
    const sharesY = document.getElementById('quadrantYSharesX');
    if (sharesY) sharesY.checked = true;
}

function resetFiveWhysForm() {
    const form = document.getElementById('fivewhysForm');
    if (form) form.reset();
    const depthSel = document.getElementById('fivewhysMaxDepth');
    if (depthSel) depthSel.value = '5';
    const durInput = document.getElementById('fivewhysDuration');
    if (durInput) durInput.value = '15';
}

    // Expose initialization functions globally for external use
    window.initializePollModal = initializePollModal;
    window.initializeWordCloudModal = initializeWordCloudModal;
})();
