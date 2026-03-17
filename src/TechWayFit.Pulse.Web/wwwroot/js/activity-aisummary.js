/**
 * activity-aisummary.js
 * ─────────────────────────────────────────────────────────────────────────────
 * AI Summary activity — form handling, config mapping, card rendering.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs:
 *   aisummaryModal, aisummaryForm, aisummaryTitle, aisummarySubtitle,
 *   aisummaryDuration, aisummaryCustomPromptAddition, aisummaryShowActivityBreakdown
 */
class AiSummaryActivity extends Activity {

    static get activityType() { return 'aisummary'; }

    static get metadata() {
        return {
            icon:        '<i class="fas fa-robot ic-sm"></i>',
            displayName: 'AI Summary',
            description: 'AI-generated summary of all session responses',
        };
    }

    constructor(data = {}) {
        super(data);
        this.customPromptAddition   = this.config.customPromptAddition   || '';
        this.showActivityBreakdown  = this.config.showActivityBreakdown  !== false;
    }

    getModalId()    { return 'aisummaryModal'; }
    getFieldPrefix(){ return 'aisummary'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}CustomPrompt`,    this.customPromptAddition);
        this._setField(`${prefix}ShowBreakdown`,   this.showActivityBreakdown);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const common = this._collectCommon(prefix);
        return {
            ...common,
            type: 'AiSummary',
            config: {
                customPromptAddition:  this._getField(`${prefix}CustomPrompt`) || '',
                showActivityBreakdown: this._getField(`${prefix}ShowBreakdown`) !== false,
                generatedSummary:      '',
                isGenerating:          false,
            },
        };
    }

    renderCardDetails() {
        return '<i class="fas fa-brain me-1"></i>AI-generated session summary';
    }

    toJSON() {
        return { ...super.toJSON(), config: {
            customPromptAddition:  this.customPromptAddition,
            showActivityBreakdown: this.showActivityBreakdown,
            generatedSummary:      '',
            isGenerating:          false,
        }};
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('aisummaryTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const config = {
            customPromptAddition:  document.getElementById('aisummaryCustomPromptAddition')?.value || '',
            showActivityBreakdown: document.getElementById('aisummaryShowActivityBreakdown')?.checked !== false,
            generatedSummary:      '',
            isGenerating:          false,
        };

        await ActivityBase.submitAndClose(
            { type: 'AiSummary', title, prompt: document.getElementById('aisummarySubtitle')?.value || null,
              durationMinutes: parseInt(document.getElementById('aisummaryDuration')?.value) || 5,
              config: JSON.stringify(config) },
            'aisummaryModal',
            AiSummaryActivity.reset,
        );
    }

    static reset() {
        document.getElementById('aisummaryForm')?.reset();
        const chk = document.getElementById('aisummaryShowActivityBreakdown');
        if (chk) chk.checked = true;
    }

    static mapTemplateConfig(c) {
        return {
            customPromptAddition:  c.customPromptAddition  || '',
            showActivityBreakdown: c.showActivityBreakdown !== false,
        };
    }
}

ActivityRegistry.register(AiSummaryActivity);
window.AiSummaryActivity = AiSummaryActivity;

window.saveAiSummaryActivity = () => AiSummaryActivity.save();
