/**
 * activity-quadrant.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Quadrant Matrix activity — form handling, config mapping, card rendering.
 * Includes all score-table helpers previously scattered in activity-modals.js.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs:
 *   quadrantModal, quadrantForm, quadrantTitle, quadrantPrompt, quadrantDuration,
 *   quadrantXAxis, quadrantYAxis, quadrantYSharesX,
 *   quadrantXScoreBody, quadrantYScoreBody, quadrantYScorePanel,
 *   quadrantItems, quadrantBubbleSize, quadrantAllowNotes,
 *   quadrantQ1Label, quadrantQ2Label, quadrantQ3Label, quadrantQ4Label
 */
class QuadrantActivity extends Activity {

    // ── Static identity ────────────────────────────────────────────────────
    static get activityType() { return 'quadrant'; }

    static get metadata() {
        return {
            icon:        '<i class="ics ics-chart-increasing ic-sm"></i>',
            displayName: 'Quadrant Matrix',
            description: 'Collaborative 2-axis scoring for prioritisation and evaluation',
        };
    }

    // ── Preset score tables ────────────────────────────────────────────────
    static defaultNumeric(min, max) {
        const opts = [];
        for (let i = min; i <= max; i++) opts.push({ value: String(i), label: '', description: null });
        return opts;
    }

    static fibonacciPreset() {
        return [1, 2, 3, 5, 8, 13].map(v => ({ value: String(v), label: '', description: null }));
    }

    static oddPreset() {
        return [1, 3, 5, 7, 9].map(v => ({ value: String(v), label: '', description: null }));
    }

    // ── Constructor ────────────────────────────────────────────────────────
    constructor(data = {}) {
        super(data);
        this.xAxisLabel    = this.config.xAxisLabel    || 'Complexity';
        this.yAxisLabel    = this.config.yAxisLabel    || 'Effort';
        this.xScoreOptions = this.config.xScoreOptions || QuadrantActivity.defaultNumeric(1, 10);
        this.yScoreOptions = this.config.yScoreOptions || [];
        this.items         = this.config.items         || [];
        this.bubbleSizeMode= this.config.bubbleSizeMode ?? 0;
        this.allowNotes    = this.config.allowNotes    ?? false;
        this.q1Label       = this.config.q1Label       || '';
        this.q2Label       = this.config.q2Label       || '';
        this.q3Label       = this.config.q3Label       || '';
        this.q4Label       = this.config.q4Label       || '';
    }

    // ── Instance API ───────────────────────────────────────────────────────
    getModalId()    { return 'quadrantModal'; }
    getFieldPrefix(){ return 'quadrant'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}XAxis`,      this.xAxisLabel);
        this._setField(`${prefix}YAxis`,      this.yAxisLabel);
        this._setField(`${prefix}Items`,      this.items.join('\n'));
        this._setField(`${prefix}BubbleSize`, String(this.bubbleSizeMode));
        this._setField(`${prefix}AllowNotes`, this.allowNotes);
        this._setField(`${prefix}Q1Label`,    this.q1Label);
        this._setField(`${prefix}Q2Label`,    this.q2Label);
        this._setField(`${prefix}Q3Label`,    this.q3Label);
        this._setField(`${prefix}Q4Label`,    this.q4Label);
        QuadrantActivity.renderScoreTable('x', this.xScoreOptions);
        const yOpts  = (this.yScoreOptions && this.yScoreOptions.length > 0) ? this.yScoreOptions : null;
        const shareY = document.getElementById(`${prefix}YSharesX`);
        if (shareY) shareY.checked = !yOpts;
        QuadrantActivity.toggleYPanel(!yOpts);
        if (yOpts) QuadrantActivity.renderScoreTable('y', yOpts);
    }

    collectData() {
        const prefix   = this.getFieldPrefix();
        const common   = this._collectCommon(prefix);
        const xOpts    = QuadrantActivity.collectScoreTable('x');
        const sharesY  = document.getElementById(`${prefix}YSharesX`)?.checked ?? true;
        const yOpts    = sharesY ? [] : QuadrantActivity.collectScoreTable('y');
        const rawItems = document.getElementById(`${prefix}Items`)?.value || '';
        const items    = rawItems.split('\n').map(s => s.trim()).filter(s => s.length > 0);
        return {
            ...common,
            type: 'Quadrant',
            config: {
                xAxisLabel:    this._getField(`${prefix}XAxis`)    || 'Complexity',
                yAxisLabel:    this._getField(`${prefix}YAxis`)    || 'Effort',
                xScoreOptions: xOpts,
                yScoreOptions: yOpts,
                items,
                bubbleSizeMode: parseInt(document.getElementById(`${prefix}BubbleSize`)?.value ?? '0', 10),
                allowNotes:     document.getElementById(`${prefix}AllowNotes`)?.checked ?? false,
                q1Label:        document.getElementById(`${prefix}Q1Label`)?.value?.trim() || '',
                q2Label:        document.getElementById(`${prefix}Q2Label`)?.value?.trim() || '',
                q3Label:        document.getElementById(`${prefix}Q3Label`)?.value?.trim() || '',
                q4Label:        document.getElementById(`${prefix}Q4Label`)?.value?.trim() || '',
            },
        };
    }

    renderCardDetails() {
        return `${this.items.length} item${this.items.length !== 1 ? 's' : ''} · ${this.xAxisLabel} vs ${this.yAxisLabel}`;
    }

    toJSON() {
        return { ...super.toJSON(), config: {
            xAxisLabel: this.xAxisLabel, yAxisLabel: this.yAxisLabel,
            xScoreOptions: this.xScoreOptions, yScoreOptions: this.yScoreOptions,
            items: this.items, bubbleSizeMode: this.bubbleSizeMode,
            allowNotes: this.allowNotes,
            q1Label: this.q1Label, q2Label: this.q2Label,
            q3Label: this.q3Label, q4Label: this.q4Label,
        }};
    }

    // ── Score-table static helpers ─────────────────────────────────────────
    /**
     * Render score options into the table body for the given axis ('x' or 'y').
     */
    static renderScoreTable(axis, options) {
        const tbody = document.getElementById(`quadrant${axis.toUpperCase()}ScoreBody`);
        if (!tbody) return;
        tbody.innerHTML = '';
        (options || []).forEach((opt) => {
            tbody.appendChild(QuadrantActivity._buildRow(axis, opt.value, opt.label, opt.description));
        });
    }

    /**
     * Collect the current score options from the table body.
     */
    static collectScoreTable(axis) {
        const rows = document.querySelectorAll(`#quadrant${axis.toUpperCase()}ScoreBody tr`);
        const opts = [];
        rows.forEach(row => {
            const val  = row.querySelector('.qscore-value')?.value?.trim() || '';
            const lbl  = row.querySelector('.qscore-label')?.value?.trim() || '';
            const desc = row.querySelector('.qscore-desc')?.value?.trim()  || null;
            if (val) opts.push({ value: val, label: lbl, description: desc || null });
        });
        return opts;
    }

    /**
     * Show or hide the Y-axis score panel.
     */
    static toggleYPanel(hide) {
        const panel = document.getElementById('quadrantYScorePanel');
        if (panel) panel.style.display = hide ? 'none' : '';
    }

    /**
     * Append a new empty row to the score table for axis ('x' or 'y').
     */
    static addRow(axis) {
        const tbody = document.getElementById(`quadrant${axis.toUpperCase()}ScoreBody`);
        if (!tbody) return;
        tbody.appendChild(QuadrantActivity._buildRow(axis, '', '', null));
    }

    /**
     * Populate the score table with a named preset.
     * preset: 'fibonacci' | 'odd' | '1-5' | '1-10'
     */
    static applyPreset(axis, preset) {
        let opts;
        if      (preset === 'fibonacci') opts = QuadrantActivity.fibonacciPreset();
        else if (preset === 'odd')       opts = QuadrantActivity.oddPreset();
        else if (preset === '1-5')       opts = QuadrantActivity.defaultNumeric(1, 5);
        else                             opts = QuadrantActivity.defaultNumeric(1, 10);
        QuadrantActivity.renderScoreTable(axis, opts);
    }

    /** Build a single <tr> for the score table. */
    static _buildRow(axis, value, label, description) {
        const tr = document.createElement('tr');
        tr.innerHTML = `
<td><input type="text" class="form-control form-control-sm qscore-value"
           value="${_escapeHtmlQ(value)}" placeholder="e.g. 5" /></td>
<td><input type="text" class="form-control form-control-sm qscore-label"
           value="${_escapeHtmlQ(label)}" placeholder="e.g. Medium" /></td>
<td><input type="text" class="form-control form-control-sm qscore-desc"
           value="${_escapeHtmlQ(description || '')}" placeholder="Optional description" /></td>
<td><button type="button" class="btn btn-sm btn-outline-danger"
            onclick="this.closest('tr').remove()"><i class="fas fa-times"></i></button></td>`;
        return tr;
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('quadrantTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const rawItems = document.getElementById('quadrantItems')?.value || '';
        const items    = rawItems.split('\n').map(s => s.trim()).filter(s => s.length > 0);
        if (items.length === 0) { alert('Please add at least one item to score.'); return; }

        const xOpts   = QuadrantActivity.collectScoreTable('x');
        if (xOpts.length === 0) { alert('Please add at least one X-axis score option.'); return; }

        const sharesY = document.getElementById('quadrantYSharesX')?.checked ?? true;
        const config = {
            xAxisLabel:    document.getElementById('quadrantXAxis')?.value?.trim()    || 'Complexity',
            yAxisLabel:    document.getElementById('quadrantYAxis')?.value?.trim()    || 'Effort',
            xScoreOptions: xOpts,
            yScoreOptions: sharesY ? [] : QuadrantActivity.collectScoreTable('y'),
            items,
            bubbleSizeMode: parseInt(document.getElementById('quadrantBubbleSize')?.value ?? '0', 10),
            allowNotes:     document.getElementById('quadrantAllowNotes')?.checked  ?? false,
            q1Label:        document.getElementById('quadrantQ1Label')?.value?.trim() || '',
            q2Label:        document.getElementById('quadrantQ2Label')?.value?.trim() || '',
            q3Label:        document.getElementById('quadrantQ3Label')?.value?.trim() || '',
            q4Label:        document.getElementById('quadrantQ4Label')?.value?.trim() || '',
        };

        await ActivityBase.submitAndClose(
            { type: 'Quadrant', title, prompt: document.getElementById('quadrantPrompt')?.value || null,
              durationMinutes: parseInt(document.getElementById('quadrantDuration')?.value) || 10,
              config: JSON.stringify(config) },
            'quadrantModal',
            QuadrantActivity.reset,
        );
    }

    static reset() {
        document.getElementById('quadrantForm')?.reset();
        QuadrantActivity.renderScoreTable('x', QuadrantActivity.defaultNumeric(1, 10));
        QuadrantActivity.renderScoreTable('y', []);
        QuadrantActivity.toggleYPanel(true);
        const sharesY = document.getElementById('quadrantYSharesX');
        if (sharesY) sharesY.checked = true;
    }

    // ── Template config mapping ────────────────────────────────────────────
    static mapTemplateConfig(c) {
        return {
            xAxisLabel: c.xAxisLabel    || 'X Axis',
            yAxisLabel: c.yAxisLabel    || 'Y Axis',
            q1Label:    c.topLeftLabel  || 'Top Left',
            q2Label:    c.topRightLabel || 'Top Right',
            q3Label:    c.bottomLeftLabel  || 'Bottom Left',
            q4Label:    c.bottomRightLabel || 'Bottom Right',
        };
    }
}

// ── Private HTML-escaper used by _buildRow ────────────────────────────────────
function _escapeHtmlQ(str) {
    if (!str) return '';
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

// ── Modal init: seed X score table + wire YSharesX toggle on first open ───────
(function initQuadrantModal() {
    function attach() {
        const modal = document.getElementById('quadrantModal');
        if (!modal || modal.__quadrantInit) return;
        modal.__quadrantInit = true;

        modal.addEventListener('show.bs.modal', () => {
            const xBody = document.getElementById('quadrantXScoreBody');
            if (xBody && xBody.children.length === 0) {
                QuadrantActivity.renderScoreTable('x', QuadrantActivity.defaultNumeric(1, 10));
            }
            QuadrantActivity.toggleYPanel(true);
        });

        modal.addEventListener('shown.bs.modal', () => {
            const sharesY = document.getElementById('quadrantYSharesX');
            if (sharesY && !sharesY.__qListener) {
                sharesY.__qListener = true;
                sharesY.addEventListener('change', e => {
                    QuadrantActivity.toggleYPanel(e.target.checked);
                    if (!e.target.checked) {
                        const yBody = document.getElementById('quadrantYScoreBody');
                        if (yBody && yBody.children.length === 0) {
                            QuadrantActivity.renderScoreTable('y', QuadrantActivity.defaultNumeric(1, 10));
                        }
                    }
                });
            }
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', attach);
    else attach();
})();

// ── Register & expose ─────────────────────────────────────────────────────────
ActivityRegistry.register(QuadrantActivity);
window.QuadrantActivity = QuadrantActivity;

// Backward-compat global helpers (called from modal HTML onclick attributes)
window.quadrantModal_renderScoreTable = (axis, options) => QuadrantActivity.renderScoreTable(axis, options);
window.quadrantModal_collectScoreTable = (axis)         => QuadrantActivity.collectScoreTable(axis);
window.quadrantModal_toggleYPanel      = (hide)         => QuadrantActivity.toggleYPanel(hide);
window.quadrantModal_addRow            = (axis)         => QuadrantActivity.addRow(axis);
window.quadrantModal_applyPreset       = (axis, preset) => QuadrantActivity.applyPreset(axis, preset);

// Backward-compat global save function
window.saveQuadrantActivity = () => QuadrantActivity.save();
