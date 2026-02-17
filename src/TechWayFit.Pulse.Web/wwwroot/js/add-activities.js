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
        window.goLive = () => this.goLive();
        window.selectTemplate = (templateId) => this.selectTemplate(templateId);
        
        // Auto-show template modal if templateId provided
        const hiddenTemplateId = document.getElementById('hiddenTemplateId');
        if (hiddenTemplateId && hiddenTemplateId.value) {
            const templateId = hiddenTemplateId.value;
            console.log('Auto-showing template modal for templateId:', templateId);
            
            // Switch to template tab
            const templateTab = document.getElementById('template-tab');
            if (templateTab) {
                const bsTab = new bootstrap.Tab(templateTab);
                bsTab.show();
            }
            
            // Auto-select the template
            setTimeout(() => {
                this.selectTemplate(templateId);
            }, 500); // Small delay to ensure tab switch completes
        }
        
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

                const targetPanel = event.target.getAttribute('data-bs-target');
                this.updateSaveSessionVisibility(targetPanel);
            });
        });

        const activeTab = document.querySelector('[data-bs-toggle="tab"].active');
        const activePanel = activeTab ? activeTab.getAttribute('data-bs-target') : '#manual-panel';
        this.updateSaveSessionVisibility(activePanel);
    }

    updateSaveSessionVisibility(activePanel) {
        const saveSessionSection = document.getElementById('saveSessionSection');
        if (!saveSessionSection) return;

        const isManual = activePanel === '#manual-panel';
        saveSessionSection.classList.toggle('d-none', !isManual);
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
                    this.showNotification(
                        'error',
                        'Access Denied',
                        'Session not found or access denied'
                    );
                    setTimeout(() => {
                        window.location.href = '/facilitator/dashboard';
                    }, 2000);
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
            this.showNotification(
                'error',
                'Failed to Load Session',
                'Failed to load session data'
            );
            setTimeout(() => {
                window.location.href = '/facilitator/dashboard';
            }, 2000);
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
                    <div class="mb-3"><i class="ics ics-pen ic-xl"></i></div>
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
        const iconHtml = window.IconHelper ? IconHelper.toFontAwesome(template.iconEmoji || 'ics-clipboard', 'ic-md') : '<i class="ics ics-clipboard ic-md"></i>';
        return `
            <div class="col-md-6 col-lg-4 template-card-container" data-category="${template.category}">
                <div class="card border-0 shadow-sm h-100 position-relative template-card-item" onclick="activityManager.selectTemplate('${template.id}')" role="button" tabindex="0">
                    ${template.isSystemTemplate ? '' : '<span class="badge bg-success-subtle text-success position-absolute top-0 start-0 m-3 template-badge">✎ Custom</span>'}
                    <div class="card-body p-4 pb-3 d-flex flex-column">
                        <div class="d-flex align-items-start gap-2 mb-3">
                            <span class="template-card-icon">${iconHtml}</span>
                            <h5 class="mb-0 fw-semibold template-card-title">${this.escapeHtml(template.name)}</h5>
                        </div>
                        <p class="text-muted mb-3 flex-grow-1">${this.escapeHtml(template.description)}</p>
                        <div>
                            <span class="badge bg-secondary-subtle text-secondary text-uppercase small">${template.category}</span>
                        </div>
                    </div>
                    <div class="template-card-overlay text-center">
                        <button type="button" class="btn btn-primary" onclick="event.stopPropagation(); activityManager.selectTemplate('${template.id}')">
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
        try {
            // Check activity limit before loading template
            if (this.activities.length >= 30) {
                this.showNotification(
                    'warning',
                    'Activity Limit Reached',
                    'Your session already has 30 activities. Sessions should not exceed 30 activities. Please remove some activities before adding a template.'
                );
                return;
            }
            
            // Store template ID for confirmation
            this.pendingTemplateId = templateId;
            
            // Load template details to show in modal
            const response = await fetch(`/api/templates/${templateId}`);
            if (!response.ok) {
                throw new Error('Failed to load template details');
            }
            
            const result = await response.json();
            const template = result.template;
            
            if (!template || !template.config || !template.config.activities) {
                throw new Error('Invalid template configuration');
            }
            
            // Update modal content
            const templateIconEl = document.getElementById('templateModalIcon');
            if (window.IconHelper) {
                templateIconEl.innerHTML = IconHelper.toFontAwesome(template.iconEmoji || 'ics-clipboard', 'ic-xl');
            } else {
                templateIconEl.innerHTML = '<i class="ics ics-clipboard ic-xl"></i>';
            }
            document.getElementById('templateModalName').textContent = template.name;
            document.getElementById('templateModalDescription').textContent = template.description || '';
            
            // Populate activities list
            const activitiesList = document.getElementById('templateActivitiesList');
            const activities = template.config.activities || [];
            
            activitiesList.innerHTML = activities.map((activity, index) => {
                const activityIcon = this.getActivityIcon(activity.type);
                const durationText = activity.durationMinutes ? `${activity.durationMinutes} min` : '';
                
                return `
                    <div class="list-group-item d-flex align-items-start gap-2">
                        <span class="me-1 template-activity-icon">${activityIcon}</span>
                        <div class="flex-grow-1">
                            <div class="fw-medium">${this.escapeHtml(activity.title)}</div>
                            ${activity.prompt ? `<small class="text-muted">${this.escapeHtml(activity.prompt)}</small>` : ''}
                        </div>
                        ${durationText ? `<span class="badge bg-secondary-subtle text-secondary">${durationText}</span>` : ''}
                    </div>
                `;
            }).join('');
            
            // Show modal
            const modal = new bootstrap.Modal(document.getElementById('templateConfirmModal'));
            modal.show();
            
        } catch (error) {
            console.error('Error loading template:', error);
            this.showNotification(
                'error',
                'Failed to Load Template',
                'Failed to load template details. Please try again.'
            );
        }
    }
    
    getActivityIcon(type) {
        const icons = {
            'poll': '<i class="ics ics-chart ic-sm"></i>',
            'wordcloud': '<i class="ics ics-thought-balloon ic-sm"></i>',
            'quadrant': '<i class="ics ics-chart-increasing ic-sm"></i>',
            'fivewhys': '<i class="ics ics-search ic-sm"></i>',
            'rating': '<i class="ics ics-star ic-sm"></i>',
            'feedback': '<i class="ics ics-chat ic-sm"></i>'
        };
        return icons[type.toLowerCase()] || '<i class="ics ics-clipboard ic-sm"></i>';
    }

    showNotification(type, title, message) {
        const modal = document.getElementById('notificationModal');
        const iconContainer = document.getElementById('notificationIconContainer');
        const icon = document.getElementById('notificationIcon');
        const titleEl = document.getElementById('notificationTitle');
        const messageEl = document.getElementById('notificationMessage');
        
        // Set icon and styling based on type
        if (type === 'success') {
            icon.innerHTML = '<i class="ics ics-check-mark ic-xl"></i>';
            iconContainer.style.background = 'linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%)';
        } else if (type === 'error') {
            icon.innerHTML = '<i class="ics ics-cross-mark ic-xl"></i>';
            iconContainer.style.background = 'linear-gradient(135deg, #fee2e2 0%, #fecaca 100%)';
        } else {
            icon.innerHTML = '<i class="ics ics-info ic-xl"></i>';
            iconContainer.style.background = 'linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%)';
        }
        
        // Set content
        titleEl.textContent = title;
        messageEl.textContent = message;
        
        // Show modal
        const notificationModal = new bootstrap.Modal(modal);
        notificationModal.show();
    }

    showConfirmation(title, message, onConfirm, options = {}) {
        const modal = document.getElementById('confirmModal');
        const icon = document.getElementById('confirmIcon');
        const titleEl = document.getElementById('confirmTitle');
        const messageEl = document.getElementById('confirmMessage');
        const okBtn = document.getElementById('confirmOkBtn');
        const cancelBtn = document.getElementById('confirmCancelBtn');
        
        // Set content
        icon.innerHTML = options.icon || '<i class="ics ics-warning ic-xl"></i>';
        titleEl.textContent = title;
        messageEl.textContent = message;
        okBtn.textContent = options.okText || 'OK';
        cancelBtn.textContent = options.cancelText || 'Cancel';
        
        // Set OK button style
        okBtn.className = `btn px-4 ${options.okClass || 'btn-primary'}`;
        
        // Remove any existing event listeners by cloning the button
        const newOkBtn = okBtn.cloneNode(true);
        okBtn.parentNode.replaceChild(newOkBtn, okBtn);
        
        // Add new event listener
        newOkBtn.addEventListener('click', () => {
            const confirmModal = bootstrap.Modal.getInstance(modal);
            if (confirmModal) {
                confirmModal.hide();
            }
            if (onConfirm) {
                onConfirm();
            }
        });
        
        // Show modal
        const confirmModal = new bootstrap.Modal(modal);
        confirmModal.show();
    }

    async applyTemplate(templateId) {
        try {
            // Check activity limit
            if (this.activities.length >= 30) {
                this.showNotification(
                    'warning',
                    'Activity Limit Reached',
                    'Your session already has 30 activities. Sessions should not exceed 30 activities. Please remove some activities before adding a template.'
                );
                return;
            }
            
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
            
            // Note: We are now appending activities instead of replacing them
            // Existing activities will remain in place
            
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
            this.showNotification(
                'success',
                'Template Applied Successfully!',
                `Successfully added ${templateActivities.length} ${templateActivities.length === 1 ? 'activity' : 'activities'} from template!`
            );
            
            // Remove templateId from URL to prevent re-triggering on page refresh
            const url = new URL(window.location);
            url.searchParams.delete('templateId');
            window.history.replaceState({}, '', url);
            
            // Remove hidden templateId field
            const hiddenTemplateId = document.getElementById('hiddenTemplateId');
            if (hiddenTemplateId) {
                hiddenTemplateId.remove();
            }
            
            // Switch to manual tab to show activities
            const manualTab = document.getElementById('manual-tab');
            if (manualTab) {
                const tabInstance = new bootstrap.Tab(manualTab);
                tabInstance.show();
            }
            
        } catch (error) {
            console.error('Error applying template:', error);
            this.showNotification(
                'error',
                'Failed to Apply Template',
                error.message
            );
            
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
        this.showConfirmation(
            'Remove Activity',
            'Are you sure you want to remove this activity?',
            async () => {
                try {
                    const activity = this.activities[index];
                    console.log(`Removing activity at index ${index}:`, activity);
                    
                    // If activity has an ID, delete from server
                    if (activity && activity.id) {
                        const response = await fetch(`/api/sessions/${this.sessionCode}/activities/${activity.id}`, {
                            method: 'DELETE'
                        });
                        
                        if (!response.ok) {
                            throw new Error('Failed to delete activity from server');
                        }
                    }
                    
                    // Remove from local array
                    this.activities.splice(index, 1);
                    console.log(`Activities remaining: ${this.activities.length}`);
                    
                    // Update the display
                    this.updateActivityList();
                } catch (error) {
                    console.error('Error removing activity:', error);
                    this.showNotification(
                        'error',
                        'Failed to Remove Activity',
                        'Could not remove the activity. Please try again.'
                    );
                }
            },
            {
                icon: '<i class="ics ics-trash ic-xl"></i>',
                okText: 'Remove',
                okClass: 'btn-danger'
            }
        );
    }

    updateActivityList() {
        const container = document.getElementById('manualActivityList');
        const goLiveBtn = document.getElementById('goLiveBtn');
        const badge = document.getElementById('activityCountBadge');
        
        console.log(`Updating activity list. Total activities: ${this.activities.length}`);
        
        if (!container) {
            console.error('Activity list container not found');
            return;
        }
        
        if (this.activities.length === 0) {
            container.innerHTML = `
                <div class="text-muted text-center small py-3">No activities yet. Choose an activity type below to add one.</div>
            `;
            if (goLiveBtn) goLiveBtn.disabled = true;
            if (badge) badge.textContent = 'No activities added yet';
        } else {
            // Use activity classes to render cards in a column layout
            const html = `<div class="d-flex flex-column gap-3">` + 
                this.activities.map((activityData, index) => {
                    try {
                        const activity = ActivityFactory.create(activityData);
                        return activity.renderCard(index);
                    } catch (error) {
                        console.error(`Error rendering activity at index ${index}:`, error, activityData);
                        return ''; // Skip this activity if there's an error
                    }
                }).join('') + 
                `</div>`;
            
            container.innerHTML = html;
            
            // Disable last down button since you can't move the last item down
            const downButtons = container.querySelectorAll('.activity-order-btn:last-child');
            if (downButtons.length > 0 && this.activities.length > 0) {
                downButtons[downButtons.length - 1].disabled = true;
            }
            
            if (goLiveBtn) goLiveBtn.disabled = false;
            if (badge) badge.textContent = `${this.activities.length} ${this.activities.length === 1 ? 'activity' : 'activities'} added`;
        }
    }
    
    moveActivityUp(index) {
        if (index <= 0) return; // Can't move first item up
        
        // Swap with previous activity
        const temp = this.activities[index];
        this.activities[index] = this.activities[index - 1];
        this.activities[index - 1] = temp;
        
        // Update order property
        this.activities.forEach((activity, i) => {
            activity.order = i;
        });
        
        this.updateActivityList();
        console.log(`Moved activity from index ${index} to ${index - 1}`);
    }
    
    moveActivityDown(index) {
        if (index >= this.activities.length - 1) return; // Can't move last item down
        
        // Swap with next activity
        const temp = this.activities[index];
        this.activities[index] = this.activities[index + 1];
        this.activities[index + 1] = temp;
        
        // Update order property
        this.activities.forEach((activity, i) => {
            activity.order = i;
        });
        
        this.updateActivityList();
        console.log(`Moved activity from index ${index} to ${index + 1}`);
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
            
            // Check activity limit
            if (this.activities.length >= 30) {
                statusMessage.innerHTML = '<strong><i class="ics ics-warning ic-sm ic-mr"></i>Activity Limit Reached:</strong> Your session already has 30 activities. Sessions should not exceed 30 activities.';
                statusMessage.className = 'alert alert-warning';
                statusMessage.classList.remove('d-none');
                return;
            }
            
            // Validate duration is selected
            const duration = document.getElementById('aiDuration').value;
            if (!duration) {
                statusMessage.innerHTML = '<i class="ics ics-warning ic-sm ic-mr"></i>Please select a session duration';
                statusMessage.className = 'alert alert-warning';
                statusMessage.classList.remove('d-none');
                return;
            }
            
            // Show loading state
            btnText.classList.add('d-none');
            btnText.classList.remove('d-inline');
            btnLoading.classList.remove('d-none');
            btnLoading.classList.add('d-inline');
            submitBtn.disabled = true;
            statusMessage.classList.add('d-none');
            
            try {
                const workshopType = document.getElementById('aiWorkshopType').value;
                const additionalContext = document.getElementById('aiAdditionalContext').value.trim();
                const participantCount = document.getElementById('aiParticipantCount').value;
                
                // Get selected participant types from Tom Select
                const selectedTypes = this.participantTypeSelect ? this.participantTypeSelect.getValue() : [];
                
                // Build list of existing activities to avoid duplicates
                const existingActivities = this.activities.map(a => `${a.title} | ${a.type}`).join('; ');
                
                // Build AI generation request - activity count will be auto-calculated from duration
                const aiRequest = {
                    additionalContext: additionalContext || null,
                    workshopType: workshopType || null,
                    durationMinutes: parseInt(duration),
                    participantCount: participantCount ? parseInt(participantCount) : null,
                    participantType: selectedTypes.length > 0 ? selectedTypes.join(', ') : null,
                    existingActivities: existingActivities || null
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
                        <strong><i class="ics ics-check-mark ic-sm ic-mr"></i>Success!</strong> Generated ${generatedActivities.length} AI-powered activities
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
                btnText.classList.add('d-inline');
                btnLoading.classList.add('d-none');
                btnLoading.classList.remove('d-inline');
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

    async goLive() {
        if (this.activities.length === 0) {
            this.showNotification(
                'error',
                'No Activities Added',
                'Please add at least one activity before going live'
            );
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
