/**
 * activity-fivewhys.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Five Whys activity — form handling, config mapping, card rendering.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs:
 *   fivewhysModal, fivewhysForm, fivewhysTitle, fivewhysRootQuestion,
 *   fivewhysContext, fivewhysMaxDepth, fivewhysDuration
 */
class FiveWhysActivity extends Activity {

    static get activityType() { return 'fivewhys'; }

    static get metadata() {
        return {
            icon:        '<i class="ics ics-question ic-sm"></i>',
            displayName: '5 Whys',
            description: 'AI-driven root cause analysis through iterative questioning',
        };
    }

    constructor(data = {}) {
        super(data);
        this.rootQuestion = this.config.rootQuestion || '';
        this.context      = this.config.context      || '';
        this.maxDepth     = this.config.maxDepth     || 5;
    }

    getModalId()    { return 'fivewhysModal'; }
    getFieldPrefix(){ return 'fivewhys'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}RootQuestion`, this.rootQuestion);
        this._setField(`${prefix}Context`,      this.context);
        this._setField(`${prefix}MaxDepth`,     this.maxDepth);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const common = this._collectCommon(prefix);
        return {
            ...common,
            type: 'FiveWhys',
            config: {
                rootQuestion: this._getField(`${prefix}RootQuestion`) || '',
                context:      this._getField(`${prefix}Context`)      || null,
                maxDepth:     parseInt(this._getField(`${prefix}MaxDepth`)) || 5,
            },
        };
    }

    renderCardDetails() {
        return this.rootQuestion
            ? `<i class="fas fa-quote-left me-1"></i>${this.escapeHtml(this.rootQuestion.slice(0, 60))}${this.rootQuestion.length > 60 ? '…' : ''}`
            : 'AI-driven root cause analysis';
    }

    toJSON() {
        return { ...super.toJSON(), config: { rootQuestion: this.rootQuestion, context: this.context, maxDepth: this.maxDepth } };
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('fivewhysTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const rootQuestion = document.getElementById('fivewhysRootQuestion')?.value?.trim();
        if (!rootQuestion) { alert('Please enter the initial problem question'); return; }

        const config = {
            rootQuestion,
            context:  document.getElementById('fivewhysContext')?.value  || null,
            maxDepth: parseInt(document.getElementById('fivewhysMaxDepth')?.value) || 5,
        };

        await ActivityBase.submitAndClose(
            { type: 'FiveWhys', title, prompt: rootQuestion,
              durationMinutes: parseInt(document.getElementById('fivewhysDuration')?.value) || 15,
              config: JSON.stringify(config) },
            'fivewhysModal',
            FiveWhysActivity.reset,
        );
    }

    static reset() {
        const form = document.getElementById('fivewhysForm');
        if (form) form.reset();
        const depth = document.getElementById('fivewhysMaxDepth');
        if (depth) depth.value = '5';
        const dur   = document.getElementById('fivewhysDuration');
        if (dur) dur.value = '15';
    }

    static mapTemplateConfig(c) {
        return { maxDepth: c.maxDepth || 5 };
    }
}

ActivityRegistry.register(FiveWhysActivity);
window.FiveWhysActivity = FiveWhysActivity;

window.saveFiveWhysActivity = () => FiveWhysActivity.save();
