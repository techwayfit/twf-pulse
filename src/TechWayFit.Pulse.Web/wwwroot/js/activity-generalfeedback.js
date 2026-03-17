/**
 * activity-generalfeedback.js
 * ─────────────────────────────────────────────────────────────────────────────
 * General Feedback activity — form handling, config mapping, card rendering.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs:
 *   feedbackModal, feedbackForm, feedbackTitle, feedbackPrompt,
 *   feedbackDuration, feedbackMaxResponses
 */
class GeneralFeedbackActivity extends Activity {

    static get activityType() { return 'generalfeedback'; }

    static get metadata() {
        return {
            icon:        '<i class="ics ics-chat ic-sm"></i>',
            displayName: 'Feedback',
            description: 'Open-ended free-text feedback from participants',
        };
    }

    constructor(data = {}) {
        super(data);
        this.maxResponses = this.config.maxResponses || this.config.maxResponsesPerParticipant || 3;
    }

    getModalId()    { return 'feedbackModal'; }
    getFieldPrefix(){ return 'feedback'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}MaxResponses`, this.maxResponses);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const common = this._collectCommon(prefix);
        return {
            ...common,
            type: 'GeneralFeedback',
            config: {
                maxResponsesPerParticipant: parseInt(this._getField(`${prefix}MaxResponses`)) || 3,
            },
        };
    }

    renderCardDetails() {
        return `<span class="d-inline-flex align-items-center gap-1 text-muted small fw-semibold">
            <i class="ics ics-chat ic-xs"></i> Max ${this.maxResponses} responses</span>`;
    }

    toJSON() {
        return { ...super.toJSON(), config: { maxResponses: this.maxResponses } };
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('feedbackTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const config = {
            maxResponsesPerParticipant: parseInt(document.getElementById('feedbackMaxResponses')?.value) || 3,
        };

        await ActivityBase.submitAndClose(
            { type: 'GeneralFeedback', title, prompt: document.getElementById('feedbackPrompt')?.value || null,
              durationMinutes: parseInt(document.getElementById('feedbackDuration')?.value) || 5,
              config: JSON.stringify(config) },
            'feedbackModal',
            GeneralFeedbackActivity.reset,
        );
    }

    static reset() {
        document.getElementById('feedbackForm')?.reset();
    }

    static mapTemplateConfig(c) {
        return { maxResponses: c.maxResponses || null };
    }
}

// Register under both the canonical key and the legacy 'feedback' alias used
// by add-activities.js template config mapping.
ActivityRegistry.register(GeneralFeedbackActivity);

// Alias so ActivityRegistry.getHandler('feedback') also works
class FeedbackActivity extends GeneralFeedbackActivity {
    static get activityType() { return 'feedback'; }
}
ActivityRegistry.register(FeedbackActivity);

window.GeneralFeedbackActivity = GeneralFeedbackActivity;
window.FeedbackActivity        = FeedbackActivity;

window.saveFeedbackActivity = () => GeneralFeedbackActivity.save();
