/**
 * Activity Type Classes - OOP approach for managing different activity types
 */

// Base Activity Class
class Activity {
    constructor(data = {}) {
        this.id = data.id || this.generateId();
        this.type = data.type || this.constructor.name.replace('Activity', '');
        this.title = data.title || '';
        this.prompt = data.prompt || '';
        this.durationMinutes = data.durationMinutes || 5;
        this.order = data.order || 0;
        this.config = this.parseConfig(data.config);
    }

    generateId() {
        return `activity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }

    parseConfig(config) {
        if (!config) return {};
        if (typeof config === 'string') {
            try {
                return JSON.parse(config);
            } catch (e) {
                console.error('Failed to parse config:', e);
                return {};
            }
        }
        return config;
    }

    escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Abstract methods - must be implemented by subclasses
    getModalId() {
        throw new Error('getModalId() must be implemented');
    }

    getFieldPrefix() {
        throw new Error('getFieldPrefix() must be implemented');
    }

    populateModal() {
        const modalId = this.getModalId();
        const modal = document.getElementById(modalId);
        if (!modal) {
            console.error(`Modal ${modalId} not found`);
            return;
        }

        const prefix = this.getFieldPrefix();
        
        // Populate common fields
        this.setFieldValue(`${prefix}Title`, this.title);
        this.setFieldValue(`${prefix}Prompt`, this.prompt);
        this.setFieldValue(`${prefix}Duration`, this.durationMinutes);

        // Let subclass populate specific fields
        this.populateSpecificFields(prefix);
    }

    populateSpecificFields(prefix) {
        // Override in subclasses
    }

    setFieldValue(id, value) {
        const field = document.getElementById(id);
        if (field) {
            if (field.type === 'checkbox') {
                field.checked = !!value;
            } else {
                field.value = value || '';
            }
        }
    }

    getFieldValue(id) {
        const field = document.getElementById(id);
        if (!field) return null;
        return field.type === 'checkbox' ? field.checked : field.value;
    }

    collectCommonData(prefix) {
        return {
            title: this.getFieldValue(`${prefix}Title`),
            prompt: this.getFieldValue(`${prefix}Prompt`),
            durationMinutes: parseInt(this.getFieldValue(`${prefix}Duration`)) || 5
        };
    }

    // Abstract method for collecting activity-specific data
    collectData() {
        throw new Error('collectData() must be implemented');
    }

    toJSON() {
        return {
            id: this.id,
            type: this.type,
            title: this.title,
            prompt: this.prompt,
            durationMinutes: this.durationMinutes,
            order: this.order,
            config: this.config
        };
    }

    renderCard(index) {
        const emoji = this.getEmoji();
        const details = this.renderCardDetails();
        
        return `
            <div class="activity-card">
                <div class="activity-header">
                    <div class="activity-title">
                        <span class="activity-icon">${emoji}</span>
                        <h3>${this.escapeHtml(this.title)}</h3>
                    </div>
                </div>
                <p class="activity-description">${this.escapeHtml(this.prompt)}</p>
                ${details ? `<p class="activity-meta">${details}</p>` : ''}
                <div class="activity-actions-overlay">
                    <button class="btn btn-sm btn-primary" onclick="activityManager.editActivity(${index})">
                        Edit
                    </button>
                    <button class="btn btn-sm btn-secondary" onclick="activityManager.removeActivity(${index})">
                        Remove
                    </button>
                </div>
            </div>
        `;
    }

    getEmoji() {
        return '📋'; // Override in subclasses
    }

    renderCardDetails() {
        return ''; // Override in subclasses for activity-specific details
    }
}

// Poll Activity
class PollActivity extends Activity {
    constructor(data = {}) {
        super(data);
        this.options = this.config.options || [];
        this.allowMultiple = this.config.allowMultiple || false;
        this.maxResponses = this.config.maxResponses || 1;
    }

    getModalId() {
        return 'pollModal';
    }

    getFieldPrefix() {
        return 'poll';
    }

    getEmoji() {
        return '📊';
    }

    populateSpecificFields(prefix) {
        // Populate allow multiple checkbox
        this.setFieldValue(`${prefix}AllowMultiple`, this.allowMultiple);
        this.setFieldValue(`${prefix}MaxResponses`, this.maxResponses);

        // Populate options
        this.populateOptions();
    }

    populateOptions() {
        const container = document.getElementById('pollOptionsContainer');
        if (!container) return;

        container.innerHTML = '';
        window.pollOptionCount = 0;

        if (Array.isArray(this.options) && this.options.length > 0) {
            this.options.forEach((option) => {
                window.pollOptionCount++;
                const optionId = window.pollOptionCount;

                const label = typeof option === 'string' ? option : (option.label || option.Label || '');
                const description = typeof option === 'object' ? (option.description || option.Description || '') : '';

                const optionHtml = `
                    <div class="card mb-2" id="pollOption${optionId}">
                        <div class="card-body">
                            <div class="row g-2">
                                <div class="col-md-5">
                                    <input type="text" class="form-control poll-option-label" 
                                           placeholder="Option label" value="${this.escapeHtml(label)}" required />
                                </div>
                                <div class="col-md-6">
                                    <input type="text" class="form-control poll-option-desc" 
                                           placeholder="Description (optional)" value="${this.escapeHtml(description)}" />
                                </div>
                                <div class="col-md-1">
                                    ${optionId > 2 ? `<button type="button" class="btn btn-sm btn-danger w-100" onclick="removePollOption(${optionId})">×</button>` : ''}
                                </div>
                            </div>
                        </div>
                    </div>
                `;
                container.insertAdjacentHTML('beforeend', optionHtml);
            });
        }
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const commonData = this.collectCommonData(prefix);

        // Collect poll options
        const options = [];
        document.querySelectorAll('.poll-option-label').forEach((input, index) => {
            if (input.value) {
                const descInput = document.querySelectorAll('.poll-option-desc')[index];
                options.push({
                    label: input.value,
                    description: descInput?.value || null
                });
            }
        });

        if (options.length < 2) {
            throw new Error('Please add at least 2 poll options');
        }

        return {
            ...commonData,
            type: 'Poll',
            config: {
                options: options,
                allowMultiple: this.getFieldValue(`${prefix}AllowMultiple`),
                maxResponses: parseInt(this.getFieldValue(`${prefix}MaxResponses`)) || 1
            }
        };
    }

    renderCardDetails() {
        return `${this.options.length} option${this.options.length !== 1 ? 's' : ''}`;
    }

    toJSON() {
        return {
            ...super.toJSON(),
            config: {
                options: this.options,
                allowMultiple: this.allowMultiple,
                maxResponses: this.maxResponses
            }
        };
    }
}

// WordCloud Activity
class WordCloudActivity extends Activity {
    constructor(data = {}) {
        super(data);
        this.maxWords = this.config.maxWords || 3;
        this.allowMultiple = this.config.allowMultiple || false;
        this.maxSubmissions = this.config.maxSubmissions || 1;
    }

    getModalId() {
        return 'wordcloudModal';
    }

    getFieldPrefix() {
        return 'wc';
    }

    getEmoji() {
        return '☁️';
    }

    populateSpecificFields(prefix) {
        this.setFieldValue(`${prefix}MaxWords`, this.maxWords);
        this.setFieldValue(`${prefix}AllowMultiple`, this.allowMultiple);
        this.setFieldValue(`${prefix}MaxSubmissions`, this.maxSubmissions);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const commonData = this.collectCommonData(prefix);

        return {
            ...commonData,
            type: 'WordCloud',
            config: {
                maxWords: parseInt(this.getFieldValue(`${prefix}MaxWords`)) || 3,
                allowMultiple: this.getFieldValue(`${prefix}AllowMultiple`),
                maxSubmissions: parseInt(this.getFieldValue(`${prefix}MaxSubmissions`)) || 1
            }
        };
    }

    renderCardDetails() {
        return `Max ${this.maxWords} word${this.maxWords !== 1 ? 's' : ''} per submission`;
    }

    toJSON() {
        return {
            ...super.toJSON(),
            config: {
                maxWords: this.maxWords,
                allowMultiple: this.allowMultiple,
                maxSubmissions: this.maxSubmissions
            }
        };
    }
}

// Quadrant Activity
class QuadrantActivity extends Activity {
    constructor(data = {}) {
        super(data);
        this.xAxisLabel = this.config.xAxisLabel || 'X Axis';
        this.yAxisLabel = this.config.yAxisLabel || 'Y Axis';
        this.topLeft = this.config.topLeft || 'Top Left';
        this.topRight = this.config.topRight || 'Top Right';
        this.bottomLeft = this.config.bottomLeft || 'Bottom Left';
        this.bottomRight = this.config.bottomRight || 'Bottom Right';
    }

    getModalId() {
        return 'quadrantModal';
    }

    getFieldPrefix() {
        return 'quadrant';
    }

    getEmoji() {
        return '📐';
    }

    populateSpecificFields(prefix) {
        this.setFieldValue(`${prefix}XAxis`, this.xAxisLabel);
        this.setFieldValue(`${prefix}YAxis`, this.yAxisLabel);
        this.setFieldValue(`${prefix}TopLeft`, this.topLeft);
        this.setFieldValue(`${prefix}TopRight`, this.topRight);
        this.setFieldValue(`${prefix}BottomLeft`, this.bottomLeft);
        this.setFieldValue(`${prefix}BottomRight`, this.bottomRight);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const commonData = this.collectCommonData(prefix);

        return {
            ...commonData,
            type: 'Quadrant',
            config: {
                xAxisLabel: this.getFieldValue(`${prefix}XAxis`),
                yAxisLabel: this.getFieldValue(`${prefix}YAxis`),
                topLeft: this.getFieldValue(`${prefix}TopLeft`) || 'Top Left',
                topRight: this.getFieldValue(`${prefix}TopRight`) || 'Top Right',
                bottomLeft: this.getFieldValue(`${prefix}BottomLeft`) || 'Bottom Left',
                bottomRight: this.getFieldValue(`${prefix}BottomRight`) || 'Bottom Right'
            }
        };
    }

    renderCardDetails() {
        return `4 quadrants`;
    }

    toJSON() {
        return {
            ...super.toJSON(),
            config: {
                xAxisLabel: this.xAxisLabel,
                yAxisLabel: this.yAxisLabel,
                topLeft: this.topLeft,
                topRight: this.topRight,
                bottomLeft: this.bottomLeft,
                bottomRight: this.bottomRight
            }
        };
    }
}

// Five Whys Activity
class FiveWhysActivity extends Activity {
    getModalId() {
        return 'fivewhysModal';
    }

    getFieldPrefix() {
        return 'fivewhys';
    }

    getEmoji() {
        return '🤔';
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const commonData = this.collectCommonData(prefix);

        return {
            ...commonData,
            type: 'FiveWhys',
            config: {}
        };
    }
}

// Rating Activity
class RatingActivity extends Activity {
    constructor(data = {}) {
        super(data);
        this.scale = this.config.scale || 5;
        this.lowLabel = this.config.lowLabel || '';
        this.highLabel = this.config.highLabel || '';
        this.maxResponses = this.config.maxResponses || 1;
    }

    getModalId() {
        return 'ratingModal';
    }

    getFieldPrefix() {
        return 'rating';
    }

    getEmoji() {
        return '⭐';
    }

    populateSpecificFields(prefix) {
        this.setFieldValue(`${prefix}Scale`, this.scale);
        this.setFieldValue(`${prefix}LowLabel`, this.lowLabel);
        this.setFieldValue(`${prefix}HighLabel`, this.highLabel);
        this.setFieldValue(`${prefix}MaxResponses`, this.maxResponses);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const commonData = this.collectCommonData(prefix);

        return {
            ...commonData,
            type: 'Rating',
            config: {
                scale: parseInt(this.getFieldValue(`${prefix}Scale`)) || 5,
                lowLabel: this.getFieldValue(`${prefix}LowLabel`) || '',
                highLabel: this.getFieldValue(`${prefix}HighLabel`) || '',
                maxResponses: parseInt(this.getFieldValue(`${prefix}MaxResponses`)) || 1
            }
        };
    }

    renderCardDetails() {
        return `1-${this.scale} scale`;
    }

    toJSON() {
        return {
            ...super.toJSON(),
            config: {
                scale: this.scale,
                lowLabel: this.lowLabel,
                highLabel: this.highLabel,
                maxResponses: this.maxResponses
            }
        };
    }
}

// Feedback Activity
class FeedbackActivity extends Activity {
    constructor(data = {}) {
        super(data);
        this.maxResponses = this.config.maxResponses || 3;
    }

    getModalId() {
        return 'feedbackModal';
    }

    getFieldPrefix() {
        return 'feedback';
    }

    getEmoji() {
        return '💬';
    }

    populateSpecificFields(prefix) {
        this.setFieldValue(`${prefix}MaxResponses`, this.maxResponses);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const commonData = this.collectCommonData(prefix);

        return {
            ...commonData,
            type: 'Feedback',
            config: {
                maxResponses: parseInt(this.getFieldValue(`${prefix}MaxResponses`)) || 3
            }
        };
    }

    renderCardDetails() {
        return `<span style="display: inline-flex; align-items: center; gap: 0.25rem; color: #6b7280; font-size: 0.9rem; font-weight: 600;">
            <i class="far fa-comments" style="color: #6366f1;"></i>
            Max ${this.maxResponses} responses
        </span>`;
    }

    toJSON() {
        return {
            ...super.toJSON(),
            config: {
                maxResponses: this.maxResponses
            }
        };
    }
}

// Activity Factory - creates appropriate activity instance based on type
class ActivityFactory {
    static create(data) {
        const type = (data.type || '').toLowerCase();
        
        switch (type) {
            case 'poll':
                return new PollActivity(data);
            case 'wordcloud':
                return new WordCloudActivity(data);
            case 'quadrant':
                return new QuadrantActivity(data);
            case 'fivewhys':
                return new FiveWhysActivity(data);
            case 'rating':
                return new RatingActivity(data);
            case 'feedback':
                return new FeedbackActivity(data);
            default:
                console.warn(`Unknown activity type: ${type}`);
                return new Activity(data);
        }
    }

    static getAvailableTypes() {
        return [
            { type: 'Poll', class: PollActivity, emoji: '📊', description: 'Multiple choice questions' },
            { type: 'WordCloud', class: WordCloudActivity, emoji: '☁️', description: 'Collect words or short phrases' },
            { type: 'Quadrant', class: QuadrantActivity, emoji: '📐', description: '2x2 matrix for categorization' },
            { type: 'FiveWhys', class: FiveWhysActivity, emoji: '🤔', description: 'Root cause analysis' },
            { type: 'Rating', class: RatingActivity, emoji: '⭐', description: 'Star or numeric ratings' },
            { type: 'Feedback', class: FeedbackActivity, emoji: '💬', description: 'Open-ended feedback' }
        ];
    }
}

// Export for use in other scripts
window.Activity = Activity;
window.PollActivity = PollActivity;
window.WordCloudActivity = WordCloudActivity;
window.QuadrantActivity = QuadrantActivity;
window.FiveWhysActivity = FiveWhysActivity;
window.RatingActivity = RatingActivity;
window.FeedbackActivity = FeedbackActivity;
window.ActivityFactory = ActivityFactory;
