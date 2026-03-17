/**
 * activity-qna.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Q&A activity — form handling, config mapping, card rendering.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs:
 *   qnaModal, qnaForm, qnaTitle, qnaPrompt, qnaDuration,
 *   qnaMaxQuestions, qnaMaxLength, qnaAllowAnonymous, qnaAllowUpvoting
 */
class QnAActivity extends Activity {

    static get activityType() { return 'qna'; }

    static get metadata() {
        return {
            icon:        '<i class="fas fa-lightbulb ic-sm"></i>',
            displayName: 'Q&A',
            description: 'Live Q&A with optional upvoting and anonymous questions',
        };
    }

    constructor(data = {}) {
        super(data);
        this.maxQuestionsPerParticipant = this.config.maxQuestionsPerParticipant || 3;
        this.maxQuestionLength          = this.config.maxQuestionLength          || 300;
        this.allowAnonymous             = this.config.allowAnonymous             !== false;
        this.allowUpvoting              = this.config.allowUpvoting              !== false;
    }

    getModalId()    { return 'qnaModal'; }
    getFieldPrefix(){ return 'qna'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}MaxQuestions`,  this.maxQuestionsPerParticipant);
        this._setField(`${prefix}MaxLength`,     this.maxQuestionLength);
        this._setField(`${prefix}AllowAnonymous`,this.allowAnonymous);
        this._setField(`${prefix}AllowUpvoting`, this.allowUpvoting);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const common = this._collectCommon(prefix);
        return {
            ...common,
            type: 'QnA',
            config: {
                maxQuestionsPerParticipant: parseInt(this._getField(`${prefix}MaxQuestions`)) || 3,
                maxQuestionLength:          parseInt(this._getField(`${prefix}MaxLength`))    || 300,
                allowAnonymous:             this._getField(`${prefix}AllowAnonymous`)         !== false,
                allowUpvoting:              this._getField(`${prefix}AllowUpvoting`)          !== false,
            },
        };
    }

    renderCardDetails() {
        const upvote = this.allowUpvoting ? '<i class="fas fa-chevron-up me-1"></i>Upvoting on · ' : '';
        return `<span class="d-inline-flex align-items-center gap-1 text-muted small fw-semibold">
            ${upvote}Max ${this.maxQuestionsPerParticipant} questions per person</span>`;
    }

    toJSON() {
        return { ...super.toJSON(), config: {
            maxQuestionsPerParticipant: this.maxQuestionsPerParticipant,
            maxQuestionLength: this.maxQuestionLength,
            allowAnonymous: this.allowAnonymous,
            allowUpvoting: this.allowUpvoting,
        }};
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('qnaTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const config = {
            maxQuestionsPerParticipant: parseInt(document.getElementById('qnaMaxQuestions')?.value) || 3,
            maxQuestionLength:          parseInt(document.getElementById('qnaMaxLength')?.value)    || 300,
            allowAnonymous:             document.getElementById('qnaAllowAnonymous')?.checked !== false,
            allowUpvoting:              document.getElementById('qnaAllowUpvoting')?.checked  !== false,
        };

        await ActivityBase.submitAndClose(
            { type: 'QnA', title, prompt: document.getElementById('qnaPrompt')?.value || null,
              durationMinutes: parseInt(document.getElementById('qnaDuration')?.value) || 10,
              config: JSON.stringify(config) },
            'qnaModal',
            QnAActivity.reset,
        );
    }

    static reset() {
        const form = document.getElementById('qnaForm');
        if (form) form.reset();
        const setVal = (id, v) => { const el = document.getElementById(id); if (el) el.value = v; };
        setVal('qnaMaxQuestions', '3');
        setVal('qnaMaxLength',    '300');
        setVal('qnaDuration',     '10');
        const setChk = (id, v) => { const el = document.getElementById(id); if (el) el.checked = v; };
        setChk('qnaAllowAnonymous', true);
        setChk('qnaAllowUpvoting',  true);
    }

    static mapTemplateConfig(c) {
        return {
            maxQuestionsPerParticipant: c.maxQuestionsPerParticipant || 3,
            maxQuestionLength:          c.maxQuestionLength          || 300,
            allowAnonymous:             c.allowAnonymous             !== false,
            allowUpvoting:              c.allowUpvoting              !== false,
        };
    }
}

ActivityRegistry.register(QnAActivity);
window.QnAActivity = QnAActivity;

window.saveQnAActivity = () => QnAActivity.save();
