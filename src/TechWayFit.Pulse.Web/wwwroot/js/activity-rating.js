/**
 * activity-rating.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Rating activity — form handling, config mapping, card rendering.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs:
 *   ratingModal, ratingForm, ratingTitle, ratingPrompt, ratingDuration,
 *   ratingScale, ratingMaxResponses, ratingLowLabel, ratingHighLabel
 */
class RatingActivity extends Activity {

    static get activityType() { return 'rating'; }

    static get metadata() {
        return {
            icon:        '<i class="ics ics-star ic-sm"></i>',
            displayName: 'Rating',
            description: 'Star or numeric ratings with optional labels',
        };
    }

    constructor(data = {}) {
        super(data);
        this.scale        = this.config.scale        || this.config.maxRating || 5;
        this.lowLabel     = this.config.lowLabel     || '';
        this.highLabel    = this.config.highLabel    || '';
        this.maxResponses = this.config.maxResponses || this.config.maxResponsesPerParticipant || 1;
    }

    getModalId()    { return 'ratingModal'; }
    getFieldPrefix(){ return 'rating'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}Scale`,        this.scale);
        this._setField(`${prefix}LowLabel`,     this.lowLabel);
        this._setField(`${prefix}HighLabel`,    this.highLabel);
        this._setField(`${prefix}MaxResponses`, this.maxResponses);
    }

    collectData() {
        const prefix = this.getFieldPrefix();
        const common = this._collectCommon(prefix);
        return {
            ...common,
            type: 'Rating',
            config: {
                scale:        parseInt(this._getField(`${prefix}Scale`))        || 5,
                lowLabel:     this._getField(`${prefix}LowLabel`)               || '',
                highLabel:    this._getField(`${prefix}HighLabel`)              || '',
                maxResponses: parseInt(this._getField(`${prefix}MaxResponses`)) || 1,
            },
        };
    }

    renderCardDetails() {
        return `1–${this.scale} scale`;
    }

    toJSON() {
        return { ...super.toJSON(), config: { scale: this.scale, lowLabel: this.lowLabel, highLabel: this.highLabel, maxResponses: this.maxResponses } };
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('ratingTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const config = {
            maxRating:                  parseInt(document.getElementById('ratingScale')?.value)        || 5,
            maxResponsesPerParticipant: parseInt(document.getElementById('ratingMaxResponses')?.value) || 1,
            lowLabel:                   document.getElementById('ratingLowLabel')?.value  || null,
            highLabel:                  document.getElementById('ratingHighLabel')?.value || null,
        };

        await ActivityBase.submitAndClose(
            { type: 'Rating', title, prompt: document.getElementById('ratingPrompt')?.value || null,
              durationMinutes: parseInt(document.getElementById('ratingDuration')?.value) || 5,
              config: JSON.stringify(config) },
            'ratingModal',
            RatingActivity.reset,
        );
    }

    static reset() {
        document.getElementById('ratingForm')?.reset();
    }

    static mapTemplateConfig(c) {
        return {
            scale:        c.maxRating || 5,
            lowLabel:     'Low',
            highLabel:    'High',
            maxResponses: null,
        };
    }
}

ActivityRegistry.register(RatingActivity);
window.RatingActivity = RatingActivity;

window.saveRatingActivity = () => RatingActivity.save();
