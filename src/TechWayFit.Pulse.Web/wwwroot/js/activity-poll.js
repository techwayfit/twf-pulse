/**
 * activity-poll.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Poll activity — form handling, config mapping, card rendering.
 * Depends on: activity-base.js (loaded first)
 *
 * Modal element IDs expected in HTML:
 *   pollModal, pollForm, pollTitle, pollPrompt, pollDuration,
 *   pollMaxResponses, pollAllowMultiple, pollOptionsContainer
 */
class PollActivity extends Activity {

    // ── Static identity ────────────────────────────────────────────────────
    static get activityType() { return 'poll'; }

    static get metadata() {
        return {
            icon:        '<i class="ics ics-chart ic-sm"></i>',
            displayName: 'Poll',
            description: 'Multiple choice questions with optional multiple selection',
        };
    }

    // ── Constructor ────────────────────────────────────────────────────────
    constructor(data = {}) {
        super(data);
        this.options       = this.config.options       || [];
        this.allowMultiple = this.config.allowMultiple || false;
        this.maxResponses  = this.config.maxResponses  || 1;
    }

    // ── Instance API ───────────────────────────────────────────────────────
    getModalId()    { return 'pollModal'; }
    getFieldPrefix(){ return 'poll'; }

    populateSpecificFields(prefix) {
        this._setField(`${prefix}AllowMultiple`, this.allowMultiple);
        this._setField(`${prefix}MaxResponses`,  this.maxResponses);
        this._populateOptions();
    }

    _populateOptions() {
        const container = document.getElementById('pollOptionsContainer');
        if (!container) return;
        container.innerHTML = '';
        window.pollOptionCount = 0;

        const opts = Array.isArray(this.options) ? this.options : [];
        if (opts.length === 0) {
            window.addPollOption?.();
            window.addPollOption?.();
            return;
        }
        opts.forEach(opt => {
            window.pollOptionCount = (window.pollOptionCount || 0) + 1;
            const id    = window.pollOptionCount;
            const label = typeof opt === 'string' ? opt : (opt.label || opt.Label || '');
            const desc  = typeof opt === 'object'  ? (opt.description || opt.Description || '') : '';
            container.insertAdjacentHTML('beforeend', `
<div class="card mb-2" id="pollOption${id}">
  <div class="card-body">
    <div class="row g-2">
      <div class="col-md-5">
        <input type="text" class="form-control poll-option-label" placeholder="Option label"
               value="${this.escapeHtml(label)}" required />
      </div>
      <div class="col-md-6">
        <input type="text" class="form-control poll-option-desc" placeholder="Description (optional)"
               value="${this.escapeHtml(desc)}" />
      </div>
      <div class="col-md-1">
        ${id > 2 ? `<button type="button" class="btn btn-sm btn-danger w-100" onclick="removePollOption(${id})">×</button>` : ''}
      </div>
    </div>
  </div>
</div>`);
        });
    }

    collectData() {
        const prefix     = this.getFieldPrefix();
        const common     = this._collectCommon(prefix);
        const options    = [];
        document.querySelectorAll('.poll-option-label').forEach((input, i) => {
            if (input.value) {
                const descEl = document.querySelectorAll('.poll-option-desc')[i];
                options.push({ id: `option_${i}`, label: input.value, description: descEl?.value || null });
            }
        });
        if (options.length < 2) throw new Error('Please add at least 2 poll options');
        return {
            ...common,
            type: 'Poll',
            config: {
                options,
                allowMultiple: this._getField(`${prefix}AllowMultiple`),
                maxResponses:  parseInt(this._getField(`${prefix}MaxResponses`)) || 1,
            },
        };
    }

    renderCardDetails() {
        return `${this.options.length} option${this.options.length !== 1 ? 's' : ''}`;
    }

    toJSON() {
        return { ...super.toJSON(), config: { options: this.options, allowMultiple: this.allowMultiple, maxResponses: this.maxResponses } };
    }

    // ── Static save / reset ────────────────────────────────────────────────
    static async save() {
        const title = document.getElementById('pollTitle')?.value?.trim();
        if (!title) { alert('Please enter an activity title'); return; }

        const options = [];
        document.querySelectorAll('.poll-option-label').forEach((input, i) => {
            if (input.value) {
                const descEl = document.querySelectorAll('.poll-option-desc')[i];
                options.push({ id: `option_${i}`, label: input.value, description: descEl?.value || null });
            }
        });
        if (options.length < 2) { alert('Please add at least 2 poll options'); return; }

        const config = {
            options,
            allowMultiple:             document.getElementById('pollAllowMultiple')?.checked,
            maxResponsesPerParticipant: parseInt(document.getElementById('pollMaxResponses')?.value) || 1,
        };

        await ActivityBase.submitAndClose(
            { type: 'Poll', title, prompt: document.getElementById('pollPrompt')?.value || null,
              durationMinutes: parseInt(document.getElementById('pollDuration')?.value) || 5,
              config: JSON.stringify(config) },
            'pollModal',
            PollActivity.reset,
        );
    }

    static reset() {
        document.getElementById('pollForm')?.reset();
        const container = document.getElementById('pollOptionsContainer');
        if (container) {
            container.innerHTML = '';
            window.pollOptionCount = 0;
            window.addPollOption?.();
            window.addPollOption?.();
        }
    }

    // ── Template config mapping ────────────────────────────────────────────
    static mapTemplateConfig(c) {
        return {
            options:       c.options       || [],
            allowMultiple: c.multipleChoice || false,
            maxResponses:  1,
        };
    }
}

// ── Poll option helpers (global, referenced by modal HTML onclick) ─────────────
window.pollOptionCount = 0;

window.addPollOption = function () {
    window.pollOptionCount++;
    const id        = window.pollOptionCount;
    const container = document.getElementById('pollOptionsContainer');
    if (!container) return;
    container.insertAdjacentHTML('beforeend', `
<div class="card mb-2" id="pollOption${id}">
  <div class="card-body">
    <div class="row g-2">
      <div class="col-md-5">
        <input type="text" class="form-control poll-option-label" placeholder="Option label" required />
      </div>
      <div class="col-md-6">
        <input type="text" class="form-control poll-option-desc" placeholder="Description (optional)" />
      </div>
      <div class="col-md-1">
        ${id > 2 ? `<button type="button" class="btn btn-sm btn-danger w-100" onclick="removePollOption(${id})">×</button>` : ''}
      </div>
    </div>
  </div>
</div>`);
};

window.removePollOption = function (id) {
    document.getElementById(`pollOption${id}`)?.remove();
};

// ── Modal init — seed two empty options when creating a new poll ───────────────
(function initPollModal() {
    function attach() {
        const modal = document.getElementById('pollModal');
        if (!modal || modal.__pollInit) return;
        modal.__pollInit = true;
        modal.addEventListener('show.bs.modal', () => {
            const container = document.getElementById('pollOptionsContainer');
            if (container && container.children.length === 0) {
                window.pollOptionCount = 0;
                window.addPollOption();
                window.addPollOption();
            }
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', attach);
    else attach();
})();

// ── Register & expose ─────────────────────────────────────────────────────────
ActivityRegistry.register(PollActivity);
window.PollActivity = PollActivity;

// Backward-compat global save function
window.savePollActivity = () => PollActivity.save();
