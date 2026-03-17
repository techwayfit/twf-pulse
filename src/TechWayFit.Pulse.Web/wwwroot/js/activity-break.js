/**
 * activity-break.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Break activity — form handling, config mapping, card rendering.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs:
 *   breakModal, breakForm, breakTitle, breakMessage, breakDurationMinutes,
 *   breakShowCountdown, breakAllowReadySignal
 */
class BreakActivity extends Activity {

    static get activityType() { return 'break'; }

    static get metadata() {
        return {
            icon:        '<i class="fas fa-coffee ic-sm"></i>',
            displayName: 'Break',
            description: 'Timed break with optional countdown and ready signal',
        };
    }

    constructor(data = {}) {
        super(data);
        this.message          = this.config.message          || "Take a short break. We'll resume shortly!";
        this.breakDuration    = this.config.durationMinutes  || 15;
        this.showCountdown    = this.config.showCountdown    !== false;
        this.allowReadySignal = this.config.allowReadySignal !== false;
    }

    getModalId()    { return 'breakModal'; }
    getFieldPrefix(){ return 'break'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}Message`,       this.message);
        this._setField(`${prefix}Duration`,      this.breakDuration);
        this._setField(`${prefix}ShowCountdown`, this.showCountdown);
        this._setField(`${prefix}AllowReady`,    this.allowReadySignal);
    }

    collectData() {
        const prefix   = this.getFieldPrefix();
        const common   = this._collectCommon(prefix);
        const duration = parseInt(this._getField(`${prefix}Duration`)) || 15;
        return {
            ...common,
            durationMinutes: duration,
            type: 'Break',
            config: {
                message:          this._getField(`${prefix}Message`)       || "Take a short break. We'll resume shortly!",
                durationMinutes:  duration,
                showCountdown:    this._getField(`${prefix}ShowCountdown`) !== false,
                allowReadySignal: this._getField(`${prefix}AllowReady`)    !== false,
            },
        };
    }

    renderCardDetails() {
        return `<i class="fas fa-coffee me-1"></i>${this.breakDuration} min break`;
    }

    toJSON() {
        return { ...super.toJSON(), config: {
            message: this.message, durationMinutes: this.breakDuration,
            showCountdown: this.showCountdown, allowReadySignal: this.allowReadySignal,
        }};
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('breakTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const breakDuration = parseInt(document.getElementById('breakDurationMinutes')?.value) || 15;
        const config = {
            message:          document.getElementById('breakMessage')?.value || "Take a short break. We'll resume shortly!",
            durationMinutes:  breakDuration,
            showCountdown:    document.getElementById('breakShowCountdown')?.checked  !== false,
            allowReadySignal: document.getElementById('breakAllowReadySignal')?.checked !== false,
        };

        await ActivityBase.submitAndClose(
            { type: 'Break', title, prompt: config.message, durationMinutes: breakDuration,
              config: JSON.stringify(config) },
            'breakModal',
            BreakActivity.reset,
        );
    }

    static reset() {
        document.getElementById('breakForm')?.reset();
        const dur = document.getElementById('breakDurationMinutes');
        if (dur) dur.value = '15';
        const setChk = (id, v) => { const el = document.getElementById(id); if (el) el.checked = v; };
        setChk('breakShowCountdown',   true);
        setChk('breakAllowReadySignal', true);
    }

    static mapTemplateConfig(c) {
        return {
            message:          c.message          || "Take a short break. We'll resume shortly!",
            durationMinutes:  c.durationMinutes  || 15,
            showCountdown:    c.showCountdown    !== false,
            allowReadySignal: c.allowReadySignal !== false,
        };
    }
}

ActivityRegistry.register(BreakActivity);
window.BreakActivity = BreakActivity;

window.saveBreakActivity = () => BreakActivity.save();
