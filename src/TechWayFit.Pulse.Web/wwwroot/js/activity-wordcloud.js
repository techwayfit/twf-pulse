/**
 * activity-wordcloud.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Word Cloud activity — form handling, config mapping, card rendering.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs:
 *   wordcloudModal, wordcloudForm, wcTitle, wcPrompt, wcDuration,
 *   wcMaxWords, wcAllowMultiple, wcMaxSubmissions, wcMaxSubmissionsContainer
 */
class WordCloudActivity extends Activity {

    static get activityType() { return 'wordcloud'; }

    static get metadata() {
        return {
            icon:        '<i class="ics ics-thought-balloon ic-sm"></i>',
            displayName: 'Word Cloud',
            description: 'Collect words or short phrases from participants',
        };
    }

    constructor(data = {}) {
        super(data);
        this.maxWords      = this.config.maxWords      || 3;
        this.allowMultiple = this.config.allowMultiple || false;
        this.maxSubmissions = this.config.maxSubmissions || this.config.maxSubmissionsPerParticipant || 1;
    }

    getModalId()    { return 'wordcloudModal'; }
    getFieldPrefix(){ return 'wc'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}MaxWords`,      this.maxWords);
        this._setField(`${prefix}AllowMultiple`, this.allowMultiple);
        this._setField(`${prefix}MaxSubmissions`, this.maxSubmissions);
        const container = document.getElementById(`${prefix}MaxSubmissionsContainer`);
        if (container) container.style.display = this.allowMultiple ? 'block' : 'none';
    }

    collectData() {
        const prefix   = this.getFieldPrefix();
        const common   = this._collectCommon(prefix);
        const allowMul = this._getField(`${prefix}AllowMultiple`);
        return {
            ...common,
            type: 'WordCloud',
            config: {
                maxWords:       parseInt(this._getField(`${prefix}MaxWords`)) || 3,
                allowMultiple:  allowMul,
                maxSubmissions: allowMul ? (parseInt(this._getField(`${prefix}MaxSubmissions`)) || 1) : 1,
            },
        };
    }

    renderCardDetails() {
        return `Max ${this.maxWords} word${this.maxWords !== 1 ? 's' : ''} per submission`;
    }

    toJSON() {
        return { ...super.toJSON(), config: { maxWords: this.maxWords, allowMultiple: this.allowMultiple, maxSubmissions: this.maxSubmissions } };
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('wcTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const allowMultiple = document.getElementById('wcAllowMultiple')?.checked;
        const config = {
            maxSubmissionsPerParticipant: allowMultiple
                ? (parseInt(document.getElementById('wcMaxSubmissions')?.value) || 1) : 1,
        };

        await ActivityBase.submitAndClose(
            { type: 'WordCloud', title, prompt: document.getElementById('wcPrompt')?.value || null,
              durationMinutes: parseInt(document.getElementById('wcDuration')?.value) || 5,
              config: JSON.stringify(config) },
            'wordcloudModal',
            WordCloudActivity.reset,
        );
    }

    static reset() {
        document.getElementById('wordcloudForm')?.reset();
        const container = document.getElementById('wcMaxSubmissionsContainer');
        if (container) container.style.display = 'none';
    }

    static mapTemplateConfig(c) {
        return {
            maxWords:       c.maxWords || 3,
            allowMultiple:  false,
            maxSubmissions: 1,
        };
    }
}

// ── Modal init — toggle max-submissions on allowMultiple change ────────────────
(function initWordCloudModal() {
    function attach() {
        const modal = document.getElementById('wordcloudModal');
        if (!modal || modal.__wcInit) return;
        modal.__wcInit = true;
        modal.addEventListener('shown.bs.modal', () => {
            const allow = document.getElementById('wcAllowMultiple');
            if (allow && !allow.__wcListener) {
                allow.__wcListener = true;
                allow.addEventListener('change', e => {
                    const container = document.getElementById('wcMaxSubmissionsContainer');
                    if (container) container.style.display = e.target.checked ? 'block' : 'none';
                });
            }
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', attach);
    else attach();
})();

ActivityRegistry.register(WordCloudActivity);
window.WordCloudActivity = WordCloudActivity;

window.saveWordCloudActivity = () => WordCloudActivity.save();
