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
        this.participantTypeSelect = null;
        
        // Define all participant types organized by industry
        this.participantTypes = [
            {
                optgroup: 'Technology & Engineering',
                options: [
                    'Software Engineers',
                    'Frontend Developers',
                    'Backend Developers',
                    'Full Stack Developers',
                    'DevOps Engineers',
                    'Site Reliability Engineers (SRE)',
                    'QA Engineers',
                    'Data Engineers',
                    'Machine Learning Engineers',
                    'Data Scientists',
                    'Security Engineers',
                    'Cloud Architects',
                    'System Administrators',
                    'Database Administrators',
                    'Technical Architects'
                ]
            },
            {
                optgroup: 'Product & Design',
                options: [
                    'Product Managers',
                    'Product Owners',
                    'UX Designers',
                    'UI Designers',
                    'UX Researchers',
                    'Product Designers',
                    'Graphic Designers'
                ]
            },
            {
                optgroup: 'Leadership & Management',
                options: [
                    'Engineering Managers',
                    'Product Leaders',
                    'CTOs',
                    'VPs of Engineering',
                    'CEOs',
                    'COOs',
                    'CFOs',
                    'Directors',
                    'Team Leads',
                    'Department Heads'
                ]
            },
            {
                optgroup: 'Sales & Marketing',
                options: [
                    'Sales Representatives',
                    'Account Executives',
                    'Sales Managers',
                    'Customer Success Managers',
                    'Marketing Managers',
                    'Digital Marketing Specialists',
                    'Content Marketers',
                    'Growth Marketers',
                    'Brand Managers'
                ]
            },
            {
                optgroup: 'Operations & Support',
                options: [
                    'Operations Managers',
                    'Customer Support Agents',
                    'Technical Support',
                    'IT Support',
                    'Business Analysts',
                    'Project Managers',
                    'Scrum Masters',
                    'Agile Coaches'
                ]
            },
            {
                optgroup: 'Healthcare',
                options: [
                    'Physicians',
                    'Nurses',
                    'Nurse Practitioners',
                    'Medical Assistants',
                    'Healthcare Administrators',
                    'Pharmacists',
                    'Physical Therapists',
                    'Lab Technicians'
                ]
            },
            {
                optgroup: 'Education',
                options: [
                    'Teachers',
                    'Professors',
                    'Students',
                    'School Administrators',
                    'Educational Coordinators',
                    'Instructional Designers',
                    'Academic Advisors'
                ]
            },
            {
                optgroup: 'Finance & Accounting',
                options: [
                    'Accountants',
                    'Financial Analysts',
                    'Investment Bankers',
                    'Financial Advisors',
                    'Controllers',
                    'Auditors',
                    'Tax Specialists'
                ]
            },
            {
                optgroup: 'Legal & Compliance',
                options: [
                    'Lawyers',
                    'Legal Counsel',
                    'Paralegals',
                    'Compliance Officers',
                    'Contract Managers'
                ]
            },
            {
                optgroup: 'Human Resources',
                options: [
                    'HR Managers',
                    'Recruiters',
                    'Talent Acquisition Specialists',
                    'HR Business Partners',
                    'Learning & Development Specialists'
                ]
            },
            {
                optgroup: 'Manufacturing & Supply Chain',
                options: [
                    'Production Managers',
                    'Supply Chain Managers',
                    'Logistics Coordinators',
                    'Quality Assurance Specialists',
                    'Warehouse Managers',
                    'Procurement Specialists'
                ]
            },
            {
                optgroup: 'Retail & Hospitality',
                options: [
                    'Store Managers',
                    'Retail Associates',
                    'Hotel Managers',
                    'Restaurant Managers',
                    'Front Desk Staff',
                    'Event Coordinators'
                ]
            },
            {
                optgroup: 'Construction & Real Estate',
                options: [
                    'Civil Engineers',
                    'Architects',
                    'Construction Managers',
                    'Real Estate Agents',
                    'Property Managers',
                    'Surveyors'
                ]
            }
        ];
        
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
        
        // Initialize tab switching for pill buttons
        this.initializeTabSwitching();
        
        // Load full session data from API
        await this.loadSessionData();
        
        // Initialize components
        this.initializeTemplates();
        this.initializeAIForm();
        this.initializeTemplateModal();
        
        // Setup global functions for HTML onclick handlers
        window.addActivity = (type) => this.addManualActivity(type);
        window.editActivity = (index) => this.editActivity(index);
        window.removeActivity = (index) => this.removeActivity(index);
        window.skipActivities = () => this.skipActivities();
        window.goLive = () => this.goLive();
        window.selectTemplate = (templateId) => this.selectTemplate(templateId);
        
        console.log('Add Activities Manager initialized');
    }

    initializeTabSwitching() {
        // Handle tab button clicks to update active state
        const tabButtons = document.querySelectorAll('[data-bs-toggle="tab"]');
        tabButtons.forEach(button => {
            button.addEventListener('shown.bs.tab', (event) => {
                // Remove active class from all buttons
                tabButtons.forEach(btn => btn.classList.remove('active'));
                // Add active class to clicked button
                event.target.classList.add('active');
            });
        });
    }

    initializeTemplateModal() {
        // Setup confirm button click handler
        const confirmBtn = document.getElementById('confirmTemplateBtn');
        if (confirmBtn) {
            confirmBtn.addEventListener('click', () => {
                if (this.pendingTemplateId) {
                    this.applyTemplate(this.pendingTemplateId);
                }
            });
        }
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
            
            // Load existing activities
            await this.loadActivities();
            
            console.log('Session data loaded:', this.sessionData);
        } catch (error) {
            console.error('Error loading session:', error);
            alert('Failed to load session data');
            window.location.href = '/facilitator/dashboard';
        }
    }

    async loadActivities() {
        try {
            const response = await fetch(`/api/sessions/${this.sessionCode}/activities`);
            if (!response.ok) {
                throw new Error('Failed to load activities');
            }
            
            const result = await response.json();
            const activities = result.data || [];
            
            // Map API activities to local format
            this.activities = activities.map(activity => ({
                id: activity.activityId,
                type: activity.type.toLowerCase(),
                title: activity.title,
                prompt: activity.prompt,
                config: activity.config,
                durationMinutes: activity.durationMinutes,
                order: activity.order
            }));
            
            this.updateActivityList();
            
            console.log(`Loaded ${this.activities.length} existing activities`);
        } catch (error) {
            console.error('Error loading activities:', error);
            // Don't fail - just start with empty activities
            this.activities = [];
        }
    }

    async initializeTemplates() {
        try {
            const response = await fetch('/api/templates');
            if (!response.ok) {
                throw new Error('Failed to load templates');
            }
            
            const result = await response.json();
            this.templates = result.templates || [];
            
            this.renderTemplates();
            this.initializeTemplateFilters();
        } catch (error) {
            console.error('Error loading templates:', error);
            document.getElementById('templateList').innerHTML = 
                '<div class="col-12 text-center text-danger py-4">Failed to load templates. Please try again.</div>';
        }
    }

    initializeTemplateFilters() {
        // Desktop: Button filtering
        document.querySelectorAll('[data-template-filter]').forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                // Update active button
                document.querySelectorAll('[data-template-filter]').forEach(btn => btn.classList.remove('active'));
                button.classList.add('active');
                
                this.filterTemplates(button.dataset.templateFilter);
            });
        });

        // Mobile: Dropdown filtering
        const mobileSelect = document.getElementById('templateCategoryFilterMobile');
        if (mobileSelect) {
            mobileSelect.addEventListener('change', (e) => {
                this.filterTemplates(e.target.value);
            });
        }
    }

    filterTemplates(category) {
        const cards = document.querySelectorAll('.template-card-container');
        cards.forEach(card => {
            if (category === 'all' || card.dataset.category === category) {
                card.style.display = '';
            } else {
                card.style.display = 'none';
            }
        });
    }

    renderTemplates() {
        const container = document.getElementById('templateList');
        
        if (this.templates.length === 0) {
            container.innerHTML = `
                <div class="col-12 text-center py-5">
                    <div class="fs-1 mb-3">📝</div>
                    <h5 class="text-muted">No templates available</h5>
                    <p class="text-muted">System templates will be initialized on first application startup.</p>
                </div>
            `;
            return;
        }
        
        // Separate system and custom templates
        const systemTemplates = this.templates.filter(t => t.isSystemTemplate);
        const customTemplates = this.templates.filter(t => !t.isSystemTemplate);
        
        let html = '';
        
        // Render system templates
        if (systemTemplates.length > 0) {
            html += systemTemplates.map(template => this.renderTemplateCard(template)).join('');
        }
        
        // Render custom templates
        if (customTemplates.length > 0) {
            html += customTemplates.map(template => this.renderTemplateCard(template)).join('');
        }
        
        container.innerHTML = html;
    }

    renderTemplateCard(template) {
        return `
            <div class="col-md-6 col-lg-4 template-card-container" data-category="${template.category}">
                <div class="activity-card template-card" style="cursor: pointer; position: relative;" onclick="activityManager.selectTemplate('${template.id}')">
                    ${template.isSystemTemplate ? '<span class="template-badge badge bg-primary-subtle text-primary">★ System</span>' : '<span class="template-badge badge bg-success-subtle text-success">✎ Custom</span>'}
                    <div class="activity-header">
                        <div class="activity-title">
                            <span class="activity-icon">${template.iconEmoji || '📋'}</span>
                            <h3>${this.escapeHtml(template.name)}</h3>
                        </div>
                    </div>
                    <p class="activity-description">${this.escapeHtml(template.description)}</p>
                    <p class="activity-meta">
                        <span class="badge bg-secondary-subtle text-secondary">${template.category}</span>
                    </p>
                    <div class="template-overlay">
                        <button class="btn btn-primary" onclick="event.stopPropagation(); activityManager.selectTemplate('${template.id}')">
                            Use Template
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    async selectTemplate(templateId) {
        // Store template ID for confirmation
        this.pendingTemplateId = templateId;
        
        // Show modal confirmation if activities exist
        if (this.activities.length > 0) {
            const modal = new bootstrap.Modal(document.getElementById('templateConfirmModal'));
            modal.show();
            return;
        }
        
        // No existing activities, apply template directly
        await this.applyTemplate(templateId);
    }

    async applyTemplate(templateId) {
        try {
            // Get the modal if it's open
            const modalElement = document.getElementById('templateConfirmModal');
            const modal = modalElement ? bootstrap.Modal.getInstance(modalElement) : null;
            
            // Show loading indicator on confirm button if modal is open
            const confirmBtn = document.getElementById('confirmTemplateBtn');
            if (confirmBtn) {
                confirmBtn.disabled = true;
                confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Loading...';
            }

            // Load template details
            const response = await fetch(`/api/templates/${templateId}`);
            if (!response.ok) {
                throw new Error('Failed to load template details');
            }
            
            const result = await response.json();
            const template = result.template;
            
            if (!template || !template.config || !template.config.activities) {
                throw new Error('Invalid template configuration');
            }
            
            // Clear existing activities from UI and server
            if (this.activities.length > 0) {
                // Delete all existing activities
                for (const activity of this.activities) {
                    if (activity.id) {
                        try {
                            await fetch(`/api/sessions/${this.sessionCode}/activities/${activity.id}`, {
                                method: 'DELETE'
                            });
                        } catch (err) {
                            console.warn('Failed to delete activity:', err);
                        }
                    }
                }
            }
            
            this.activities = [];
            
            // Add template activities to session
            const templateActivities = template.config.activities || [];
            
            console.log(`Applying ${templateActivities.length} activities from template...`);
            
            for (let i = 0; i < templateActivities.length; i++) {
                const activityConfig = templateActivities[i];
                console.log(`Creating activity ${i + 1}/${templateActivities.length}:`, activityConfig);
                
                try {
                    await this.createActivityFromTemplate(activityConfig);
                } catch (activityError) {
                    console.error(`Failed to create activity ${i + 1}:`, activityError);
                    throw new Error(`Failed to create activity "${activityConfig.title}": ${activityError.message}`);
                }
            }
            
            // Close modal if open
            if (modal) {
                modal.hide();
            }
            
            // Show success message
            alert(`Successfully added ${templateActivities.length} ${templateActivities.length === 1 ? 'activity' : 'activities'} from template!`);
            
            // Switch to manual tab to show activities
            const manualTab = document.getElementById('manual-tab');
            if (manualTab) {
                const tabInstance = new bootstrap.Tab(manualTab);
                tabInstance.show();
            }
            
        } catch (error) {
            console.error('Error applying template:', error);
            alert('Failed to apply template:\n\n' + error.message);
            
            // Close modal if open
            if (modal) {
                modal.hide();
            }
        } finally {
            // Restore confirm button
            if (confirmBtn) {
                confirmBtn.disabled = false;
                confirmBtn.innerHTML = 'OK';
            }
        }
    }

    async createActivityFromTemplate(templateActivity) {
        // Map template config to activity format
        const activityData = {
            type: templateActivity.type,
            title: templateActivity.title,
            prompt: templateActivity.prompt || '',
            durationMinutes: templateActivity.durationMinutes || 5,
            config: this.mapTemplateConfig(templateActivity.type, templateActivity.config)
        };

        return await this.createActivityFromData(activityData);
    }

    mapTemplateConfig(activityType, templateConfig) {
        if (!templateConfig) return {};

        const type = activityType.toLowerCase();
        
        switch (type) {
            case 'poll':
                return {
                    options: templateConfig.options || [],
                    allowMultiple: templateConfig.multipleChoice || false,
                    maxResponses: 1
                };
            
            case 'wordcloud':
                return {
                    maxWords: templateConfig.maxWords || 3,
                    allowMultiple: false,
                    maxSubmissions: 1
                };
            
            case 'quadrant':
                return {
                    xAxisLabel: templateConfig.xAxisLabel || 'X Axis',
                    yAxisLabel: templateConfig.yAxisLabel || 'Y Axis',
                    topLeft: templateConfig.topLeftLabel || 'Top Left',
                    topRight: templateConfig.topRightLabel || 'Top Right',
                    bottomLeft: templateConfig.bottomLeftLabel || 'Bottom Left',
                    bottomRight: templateConfig.bottomRightLabel || 'Bottom Right'
                };
            
            case 'fivewhys':
                return {
                    maxDepth: templateConfig.maxDepth || 5
                };
            
            case 'rating':
                return {
                    scale: templateConfig.maxRating || 5,
                    lowLabel: 'Low',
                    highLabel: 'High',
                    maxResponses: null
                };
            
            case 'feedback':
                return {
                    maxResponses: null
                };
            
            default:
                return templateConfig || {};
        }
    }

    async createActivityFromData(activityData) {
        try {
            // Ensure config is a JSON string
            const configString = typeof activityData.config === 'string' 
                ? activityData.config 
                : JSON.stringify(activityData.config || {});

            const requestBody = {
                order: activityData.order || (this.activities.length + 1),
                type: activityData.type,
                title: activityData.title,
                prompt: activityData.prompt || '',
                config: configString,
                durationMinutes: activityData.durationMinutes || 5
            };

            console.log('Creating activity:', requestBody);

            const response = await fetch(`/api/sessions/${this.sessionCode}/activities`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestBody)
            });
            
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                console.error('API Error:', errorData);
                throw new Error(errorData.message || `Failed to create activity (${response.status})`);
            }
            
            const result = await response.json();
            
            // Add to local activities array
            this.activities.push({
                id: result.data?.id,
                type: activityData.type.toLowerCase(),
                title: activityData.title,
                prompt: activityData.prompt,
                durationMinutes: activityData.durationMinutes,
                config: activityData.config
            });
            
            // Update UI
            this.updateActivityList();
            
            console.log('Activity created successfully:', result.data);
            return result.data;
            
        } catch (error) {
            console.error('Error creating activity:', error);
            throw error; // Re-throw to be caught by caller
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

    editActivity(index) {
        const activityData = this.activities[index];
        if (!activityData) return;
        
        // Create activity instance using factory
        const activity = ActivityFactory.create(activityData);
        
        // Get modal and check if it exists
        const modalId = activity.getModalId();
        const modal = document.getElementById(modalId);
        if (!modal) {
            console.error(`Modal ${modalId} not found`);
            return;
        }
        
        // Store for editing
        window.editingActivityIndex = index;
        window.editingActivityData = activityData;
        
        // Populate modal using activity class method
        activity.populateModal();
        
        // Show modal
        const modalElement = bootstrap.Modal.getOrCreateInstance(modal);
        modalElement.show();
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
            container.innerHTML = `
                <div class="text-center py-5">
                    <div class="mb-3">
                        <i class="fas fa-clipboard-list fa-3x text-muted"></i>
                    </div>
                    <p class="text-muted mb-0">No activities yet. Create your first activity to get started.</p>
                </div>
            `;
            goLiveBtn.disabled = true;
            badge.textContent = 'No activities added yet';
        } else {
            // Use activity classes to render cards in a column layout
            const html = `<div class="d-flex flex-column gap-3">` + 
                this.activities.map((activityData, index) => {
                    const activity = ActivityFactory.create(activityData);
                    return activity.renderCard(index);
                }).join('') + 
                `</div>`;
            
            container.innerHTML = html;
            goLiveBtn.disabled = false;
            badge.textContent = `${this.activities.length} ${this.activities.length === 1 ? 'activity' : 'activities'} added`;
        }
    }

    initializeAIForm() {
        const form = document.getElementById('aiGenerateForm');
        if (!form) return;
        
        // Initialize participant type multi-select
        this.initializeParticipantTypeSelector();
        
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const submitBtn = document.getElementById('aiGenerateBtn');
            const btnText = submitBtn.querySelector('.btn-text');
            const btnLoading = submitBtn.querySelector('.btn-loading');
            const statusMessage = document.getElementById('aiStatusMessage');
            
            // Validate duration is selected
            const duration = document.getElementById('aiDuration').value;
            if (!duration) {
                statusMessage.textContent = '⚠️ Please select a session duration';
                statusMessage.className = 'alert alert-warning';
                statusMessage.classList.remove('d-none');
                return;
            }
            
            // Show loading state
            btnText.classList.add('d-none');
            btnLoading.classList.remove('d-none');
            submitBtn.disabled = true;
            statusMessage.classList.add('d-none');
            
            try {
                const workshopType = document.getElementById('aiWorkshopType').value;
                const additionalContext = document.getElementById('aiAdditionalContext').value.trim();
                const participantCount = document.getElementById('aiParticipantCount').value;
                
                // Get selected participant types from Tom Select
                const selectedTypes = this.participantTypeSelect ? this.participantTypeSelect.getValue() : [];
                
                // Build AI generation request - activity count will be auto-calculated from duration
                const aiRequest = {
                    additionalContext: additionalContext || null,
                    workshopType: workshopType || null,
                    durationMinutes: parseInt(duration),
                    participantCount: participantCount ? parseInt(participantCount) : null,
                    participantType: selectedTypes.length > 0 ? selectedTypes.join(', ') : null
                };
                
                console.log('🧠 Requesting AI generation:', aiRequest);
                
                // Call new optimized endpoint - generates and adds activities in one call
                const response = await fetch(`/api/sessions/${this.sessionCode}/generate-activities`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(aiRequest)
                });
                
                if (!response.ok) {
                    const errorData = await response.json().catch(() => ({}));
                    
                    // Handle quota exceeded error
                    if (response.status === 429) {
                        throw new Error(errorData.error?.message || 'AI generation quota exceeded. Please add your own API key in settings.');
                    }
                    
                    throw new Error(errorData.error?.message || 'AI generation failed');
                }
                
                const result = await response.json();
                const generatedActivities = result.data || [];
                
                console.log('✅ Generated and saved activities:', generatedActivities);
                
                // Reload activities from server to get the complete list
                await this.loadActivities();
                
                // Show success message
                statusMessage.innerHTML = `
                    <div class="alert alert-success">
                        <strong>✅ Success!</strong> Generated ${generatedActivities.length} AI-powered activities
                    </div>
                `;
                statusMessage.classList.remove('d-none');
                
                // Switch to manual tab to see the generated activities after a brief delay
                setTimeout(() => {
                    document.getElementById('manual-tab').click();
                    
                    // Scroll to activity list
                    const activityList = document.getElementById('manualActivityList');
                    if (activityList) {
                        activityList.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                    }
                }, 1500);
                
            } catch (error) {
                console.error('❌ AI generation error:', error);
                
                // Show error message
                statusMessage.innerHTML = `
                    <div class="alert alert-danger">
                        <strong>❌ Error:</strong> ${this.escapeHtml(error.message)}
                    </div>
                `;
                statusMessage.classList.remove('d-none');
            } finally {
                btnText.classList.remove('d-none');
                btnLoading.classList.add('d-none');
                submitBtn.disabled = false;
            }
        });
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    initializeParticipantTypeSelector() {
        const dropdown = document.getElementById('participantTypeDropdown');
        const filterDropdown = document.getElementById('participantTypeFilter');
        if (!dropdown || !filterDropdown) return;
        
        // Populate industry filter dropdown
        this.participantTypes.forEach(group => {
            const option = document.createElement('option');
            option.value = group.optgroup;
            option.textContent = group.optgroup;
            filterDropdown.appendChild(option);
        });
        
        // Flatten participant types into options array for Tom Select
        const options = [];
        this.participantTypes.forEach(group => {
            group.options.forEach(option => {
                options.push({
                    value: option,
                    text: option,
                    optgroup: group.optgroup
                });
            });
        });
        
        // Initialize Tom Select with multi-select capability
        this.participantTypeSelect = new TomSelect(dropdown, {
            options: options,
            optgroups: this.participantTypes.map(g => ({ value: g.optgroup, label: g.optgroup })),
            optgroupField: 'optgroup',
            labelField: 'text',
            valueField: 'value',
            searchField: ['text', 'optgroup'],
            plugins: ['remove_button', 'dropdown_input'],
            maxOptions: null,
            closeAfterSelect: false,
            hideSelected: true,
            placeholder: 'Search and select...',
            render: {
                optgroup_header: function(data, escape) {
                    return '<div class="optgroup-header">' + escape(data.label) + '</div>';
                },
                option: function(data, escape) {
                    return '<div>' + escape(data.text) + '</div>';
                },
                item: function(data, escape) {
                    return '<div>' + escape(data.text) + '</div>';
                },
                no_results: function(data, escape) {
                    return '<div class="no-results">No participant types found matching "' + escape(data.input) + '"</div>';
                }
            },
            onInitialize: function() {
                this.dropdown.style.maxHeight = '300px';
                this.dropdown.style.overflowY = 'auto';
            }
        });
        
        // Add filter functionality
        filterDropdown.addEventListener('change', () => {
            const selectedCategory = filterDropdown.value;
            
            if (!selectedCategory) {
                // Show all options
                this.participantTypeSelect.clearOptions();
                this.participantTypeSelect.addOption(options);
                this.participantTypeSelect.refreshOptions(false);
            } else {
                // Filter options by selected category
                const filteredOptions = options.filter(opt => opt.optgroup === selectedCategory);
                this.participantTypeSelect.clearOptions();
                this.participantTypeSelect.addOption(filteredOptions);
                this.participantTypeSelect.refreshOptions(false);
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
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize when script loads and make it globally accessible
const activityManager = new AddActivitiesManager();
window.activityManager = activityManager;
