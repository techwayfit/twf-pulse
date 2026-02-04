/**
 * Add Activities Page Handler
 */

class AddActivitiesManager {
    constructor() {
        this.sessionCode = null;
        this.sessionId = null;
        this.sessionData = null;
        this.activities = [];
        this.templates = [];
        
        // Expose globally for modal access
        window.addActivitiesManager = this;
        
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.init());
        } else {
            this.init();
        }
    }

    async init() {
        console.log('Initializing Add Activities Manager...');
        
        // Get session data from hidden fields (already validated server-side)
        const hiddenCode = document.getElementById('hiddenSessionCode');
        const hiddenId = document.getElementById('hiddenSessionId');
        
        if (!hiddenCode || !hiddenCode.value) {
            console.error('Session code not found in page');
            window.location.href = '/facilitator/dashboard';
            return;
        }
        
        this.sessionCode = hiddenCode.value;
        this.sessionId = hiddenId ? hiddenId.value : null;
        
        // Load full session data from API
        await this.loadSessionData();
        
        // Initialize components
        this.initializeTemplates();
        this.initializeAIForm();
        
        // Setup global functions for HTML onclick handlers
        window.addActivity = (type) => this.addManualActivity(type);
        window.removeActivity = (index) => this.removeActivity(index);
        window.skipActivities = () => this.skipActivities();
        window.goLive = () => this.goLive();
        window.selectTemplate = (templateId) => this.selectTemplate(templateId);
        
        console.log('Add Activities Manager initialized');
    }

    async loadSessionData() {
        try {
            const response = await fetch(`/api/sessions/${this.sessionCode}`);
            if (!response.ok) {
                if (response.status === 404 || response.status === 403) {
                    alert('Session not found or access denied');
                    window.location.href = '/facilitator/dashboard';
                    return;
                }
                throw new Error('Failed to load session');
            }
            
            const result = await response.json();
            this.sessionData = result.data;
            
            // Use sessionId from hidden field or API response
            if (!this.sessionId) {
                this.sessionId = this.sessionData.sessionId;
            }
            
            console.log('Session data loaded:', this.sessionData);
        } catch (error) {
            console.error('Error loading session:', error);
            alert('Failed to load session data');
            window.location.href = '/facilitator/dashboard';
        }
    }

    async initializeTemplates() {
        try {
            const response = await fetch('/api/templates');
            if (!response.ok) {
                throw new Error('Failed to load templates');
            }
            
            const result = await response.json();
            this.templates = result.data?.templates || [];
            
            this.renderTemplates();
        } catch (error) {
            console.error('Error loading templates:', error);
            document.getElementById('templateList').innerHTML = 
                '<div class="col-12 text-danger">Failed to load templates</div>';
        }
    }

    renderTemplates() {
        const container = document.getElementById('templateList');
        
        if (this.templates.length === 0) {
            container.innerHTML = '<div class="col-12 text-muted">No templates available</div>';
            return;
        }
        
        const html = this.templates.map(template => `
            <div class="col-md-6 col-lg-4">
                <div class="card template-card h-100 border-0 shadow-sm" onclick="selectTemplate('${template.id}')">
                    <div class="card-body text-center">
                        <div class="template-icon">${template.iconEmoji || '📋'}</div>
                        <div class="template-name">${template.name}</div>
                        <div class="template-description">${template.description}</div>
                        <div class="template-activity-count">
                            <span class="badge bg-light text-dark">${template.category}</span>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
        
        container.innerHTML = html;
    }

    async selectTemplate(templateId) {
        if (!confirm('This will replace any existing activities with the template activities. Continue?')) {
            return;
        }
        
        try {
            // Load template details
            const response = await fetch(`/api/templates/${templateId}`);
            if (!response.ok) {
                throw new Error('Failed to load template details');
            }
            
            const result = await response.json();
            const templateConfig = result.data?.template?.config;
            
            if (!templateConfig || !templateConfig.activities) {
                throw new Error('Invalid template configuration');
            }
            
            // Clear existing activities
            this.activities = [];
            
            // Add template activities to session
            for (const activityConfig of templateConfig.activities) {
                await this.createActivityFromConfig(activityConfig);
            }
            
            alert(`Added ${templateConfig.activities.length} activities from template!`);
            
            // Switch to manual tab to show activities
            const manualTab = document.getElementById('manual-tab');
            manualTab.click();
            
        } catch (error) {
            console.error('Error applying template:', error);
            alert('Failed to apply template: ' + error.message);
        }
    }

    async createActivityFromConfig(config) {
        return await this.createActivityFromData({
            order: config.order || (this.activities.length + 1),
            type: config.type,
            title: config.title,
            prompt: config.prompt,
            config: config.config ? JSON.stringify(config.config) : '{}',
            durationMinutes: config.durationMinutes || 5
        });
    }

    async createActivityFromData(activityData) {
        try {
            const response = await fetch(`/api/sessions/${this.sessionCode}/activities`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    order: activityData.order || (this.activities.length + 1),
                    type: activityData.type,
                    title: activityData.title,
                    prompt: activityData.prompt,
                    config: activityData.config || '{}',
                    durationMinutes: activityData.durationMinutes || 5
                })
            });
            
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || 'Failed to create activity');
            }
            
            const result = await response.json();
            
            // Add to local activities array
            this.activities.push({
                id: result.data?.id,
                type: activityData.type.toLowerCase(),
                title: activityData.title,
                prompt: activityData.prompt,
                durationMinutes: activityData.durationMinutes
            });
            
            // Update UI
            this.updateActivityList();
            
            console.log('Activity created successfully:', result.data);
            return result.data;
            
        } catch (error) {
            console.error('Error creating activity:', error);
            alert('Failed to create activity: ' + error.message);
            throw error;
        }
    }

    // This is now handled by modals - kept for backward compatibility
    addManualActivity(type) {
        console.log('addManualActivity called for type:', type);
        
        const activityNames = {
            poll: 'Poll',
            wordcloud: 'Word Cloud',
            rating: 'Rating',
            feedback: 'Feedback',
            quadrant: 'Quadrant Matrix',
            fivewhys: '5 Whys'
        };
        
        const title = prompt(`Enter activity title:`, activityNames[type] || 'Activity');
        if (!title) return;
        
        const prompt_text = prompt(`Enter prompt/question:`, `Enter your ${activityNames[type].toLowerCase()} prompt...`);
        if (!prompt_text) return;
        
        const config = {
            order: this.activities.length + 1,
            type: type,
            title: title,
            prompt: prompt_text,
            durationMinutes: 5
        };
        
        this.createActivityFromConfig(config);
    }

    removeActivity(index) {
        if (confirm('Remove this activity?')) {
            // TODO: Call API to delete activity
            this.activities.splice(index, 1);
            this.updateActivityList();
        }
    }

    updateActivityList() {
        const container = document.getElementById('manualActivityList');
        const goLiveBtn = document.getElementById('goLiveBtn');
        const badge = document.getElementById('activityCountBadge');
        
        if (this.activities.length === 0) {
            container.innerHTML = '<div class="text-muted small">No activities yet. Click a button above to add one.</div>';
            goLiveBtn.disabled = true;
            badge.textContent = 'No activities added yet';
        } else {
            const activityIcons = {
                poll: '📊',
                wordcloud: '☁️',
                quadrant: '📈',
                fivewhys: '🔍',
                rating: '⭐',
                feedback: '💬'
            };
            
            const html = this.activities.map((activity, index) => `
                <div class="activity-card">
                    <div class="activity-icon">${activityIcons[activity.type] || '📄'}</div>
                    <div class="activity-details">
                        <div class="activity-title">${activity.title}</div>
                        <div class="activity-prompt">${activity.prompt || 'No prompt'}</div>
                    </div>
                    <div class="activity-actions">
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeActivity(${index})">
                            Remove
                        </button>
                    </div>
                </div>
            `).join('');
            
            container.innerHTML = html;
            goLiveBtn.disabled = false;
            badge.textContent = `${this.activities.length} ${this.activities.length === 1 ? 'activity' : 'activities'} added`;
        }
    }

    initializeAIForm() {
        const form = document.getElementById('aiGenerateForm');
        if (!form) return;
        
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const submitBtn = form.querySelector('button[type="submit"]');
            const btnText = submitBtn.querySelector('.btn-text');
            const btnLoading = submitBtn.querySelector('.btn-loading');
            
            // Show loading state
            btnText.classList.add('d-none');
            btnLoading.classList.remove('d-none');
            submitBtn.disabled = true;
            
            try {
                const workshopType = document.getElementById('aiWorkshopType').value;
                const activityCount = parseInt(document.getElementById('aiActivityCount').value);
                const additionalContext = document.getElementById('aiAdditionalContext').value;
                
                // Build AI generation request
                const aiRequest = {
                    title: this.sessionData.title,
                    goal: this.sessionData.goal,
                    context: additionalContext || null,
                    generationContext: {
                        workshopType: workshopType || 'other',
                        durationMinutes: activityCount * 5  // Approximate: 5 mins per activity
                    }
                };
                
                // Call AI generation endpoint
                const response = await fetch('/api/sessions/generate', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(aiRequest)
                });
                
                if (!response.ok) {
                    throw new Error('AI generation failed');
                }
                
                const result = await response.json();
                const generatedActivities = result.data || result;
                
                console.log('Generated activities:', generatedActivities);
                
                // Clear existing and add generated activities
                this.activities = [];
                for (const activity of generatedActivities) {
                    await this.createActivityFromConfig(activity);
                }
                
                // Show success and switch to manual tab
                alert(`Successfully generated ${generatedActivities.length} activities!`);
                document.getElementById('manual-tab').click();
                
            } catch (error) {
                console.error('AI generation error:', error);
                alert('Failed to generate activities: ' + error.message);
            } finally {
                btnText.classList.remove('d-none');
                btnLoading.classList.add('d-none');
                submitBtn.disabled = false;
            }
        });
    }

    skipActivities() {
        if (confirm('Go live without activities? You can add them later from the facilitator view.')) {
            window.location.href = `/facilitator/live?code=${this.sessionCode}`;
        }
    }

    async goLive() {
        if (this.activities.length === 0) {
            alert('Please add at least one activity before going live');
            return;
        }
        
        // Navigate to live facilitator view
        window.location.href = `/facilitator/live?code=${this.sessionCode}`;
    }
}

// Initialize when script loads
new AddActivitiesManager();
