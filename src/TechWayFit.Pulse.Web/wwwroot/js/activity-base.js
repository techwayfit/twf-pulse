/**
 * activity-base.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Provides the Activity base class, ActivityRegistry, and ActivityBase shared
 * helpers. Must be loaded BEFORE any activity-{name}.js file.
 *
 * Architecture
 * ────────────
 *  activity-base.js          ← this file: base class + registry + helpers
 *  activity-poll.js          ← extends Activity, registers with ActivityRegistry
 *  activity-wordcloud.js     ← same pattern
 *  …                         ← one file per activity type
 *  activity-types.js         ← backward-compat shim (re-exports classes)
 *  activity-modals.js        ← backward-compat shim (delegates to registry)
 *  add-activities.js         ← uses ActivityRegistry for icons and template config
 *
 * Adding a new activity type
 * ──────────────────────────
 * 1. Create activity-{name}.js following the pattern in activity-poll.js.
 * 2. Add <script src="~/js/activity-{name}.js"> to AddActivities.cshtml.
 * No other JS files need to change.
 */

// ── Shared modal/submit utilities ─────────────────────────────────────────────
class ActivityBase {
    /**
     * Hides a Bootstrap modal by ID.
     * @param {string} modalId
     */
    static hideModal(modalId) {
        const el = document.getElementById(modalId);
        if (!el) return;
        const inst = bootstrap.Modal.getInstance(el);
        if (inst) inst.hide();
        else bootstrap.Modal.getOrCreateInstance(el).hide();
    }

    /**
     * Posts the activity to the server via addActivitiesManager, then closes
     * the modal and resets the form.
     * @param {object}   activityData  Plain object matching the API contract.
     * @param {string}   modalId       ID of the modal to close on success.
     * @param {Function} resetFn       Called after a successful save.
     * @returns {Promise<boolean>}
     */
    static async submitAndClose(activityData, modalId, resetFn) {
        if (!window.addActivitiesManager) {
            alert('Activity manager not initialized. Please refresh the page.');
            return false;
        }
        try {
            await window.addActivitiesManager.createActivityFromData(activityData);
            ActivityBase.hideModal(modalId);
            if (typeof resetFn === 'function') resetFn();
            return true;
        } catch (err) {
            console.error('Failed to create activity:', err);
            alert('Failed to create activity: ' + err.message);
            return false;
        }
    }
}

// ── Central registry ──────────────────────────────────────────────────────────
/**
 * ActivityRegistry — each activity-{name}.js calls
 * ActivityRegistry.register(MyActivityClass) at module load time.
 *
 * Registered classes must expose:
 *   static activityType    {string}    lowercase key,  e.g. 'poll'
 *   static metadata        {object}    { icon, displayName, description }
 *   static async save()    {Function}  reads modal form → calls submit
 *   static reset()         {Function}  resets the modal form to defaults
 *   static mapTemplateConfig(cfg) {Function}  maps template JSON to config obj
 */
class ActivityRegistry {
    static #registry = new Map();

    /**
     * Register an activity class.
     * @param {typeof Activity} ActivityClass
     */
    static register(ActivityClass) {
        const type = ActivityClass.activityType?.toLowerCase();
        if (!type) {
            console.error('ActivityRegistry.register: class missing static activityType', ActivityClass);
            return;
        }
        ActivityRegistry.#registry.set(type, ActivityClass);
    }

    /**
     * Create an instance of the registered class for the given raw data object.
     * Falls back to the base Activity if the type is unregistered.
     * @param {object} data
     * @returns {Activity}
     */
    static create(data) {
        const type = (data.type || '').toLowerCase();
        const Cls = ActivityRegistry.#registry.get(type);
        if (!Cls) {
            console.warn(`ActivityRegistry: no handler for type "${type}". Using base Activity.`);
            return new Activity(data);
        }
        return new Cls(data);
    }

    /**
     * Return the registered class for a type key, or null.
     * @param {string} type
     * @returns {typeof Activity | null}
     */
    static getHandler(type) {
        return ActivityRegistry.#registry.get((type || '').toLowerCase()) ?? null;
    }

    /**
     * Return all registered activity classes in registration order.
     * @returns {(typeof Activity)[]}
     */
    static getAll() {
        return [...ActivityRegistry.#registry.values()];
    }

    /**
     * Invoke the registered class's static save() for the given type.
     * @param {string} type
     */
    static async save(type) {
        const Cls = ActivityRegistry.getHandler(type);
        if (!Cls) {
            console.error(`ActivityRegistry.save: no handler for type "${type}"`);
            return;
        }
        await Cls.save();
    }

    /**
     * Map a template activity config through the registered handler.
     * Falls back to the raw config object if the type is unregistered.
     * @param {string} type
     * @param {object} templateConfig
     * @returns {object}
     */
    static mapTemplateConfig(type, templateConfig) {
        const Cls = ActivityRegistry.getHandler(type);
        if (!Cls || typeof Cls.mapTemplateConfig !== 'function') {
            return templateConfig || {};
        }
        return Cls.mapTemplateConfig(templateConfig);
    }
}

// ── Base Activity (instance API used for edit / card rendering) ───────────────
/**
 * All per-activity classes extend this.
 *
 * Instance lifecycle:
 *   1. ActivityRegistry.create(data) → new XxxActivity(data)
 *   2. instance.populateModal()      → fills the edit modal
 *   3. instance.renderCard(index)    → renders the activity card HTML
 *
 * Static lifecycle (called by modal save buttons via global save functions):
 *   1. MyActivity.save()             → reads form, POSTs to server
 *   2. MyActivity.reset()            → resets modal form to defaults
 */
class Activity {
    constructor(data = {}) {
        this.id             = data.id             || this._generateId();
        this.type           = data.type           || '';
        this.title          = data.title          || '';
        this.prompt         = data.prompt         || '';
        this.durationMinutes = data.durationMinutes || 5;
        this.order          = data.order          || 0;
        this.config         = this._parseConfig(data.config);
    }

    _generateId() {
        return `activity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }

    _parseConfig(config) {
        if (!config) return {};
        if (typeof config === 'string') {
            try { return JSON.parse(config); } catch { return {}; }
        }
        return config;
    }

    escapeHtml(text) {
        if (!text) return '';
        const d = document.createElement('div');
        d.textContent = text;
        return d.innerHTML;
    }

    // ── Must override ──────────────────────────────────────────────────────
    getModalId()     { throw new Error(`${this.constructor.name}.getModalId() not implemented`); }
    getFieldPrefix() { throw new Error(`${this.constructor.name}.getFieldPrefix() not implemented`); }
    collectData()    { throw new Error(`${this.constructor.name}.collectData() not implemented`); }

    // ── Optional override ──────────────────────────────────────────────────
    /** Subclass fills activity-specific fields after common fields are set. */
    populateSpecificFields(_prefix) {}
    renderCardDetails() { return ''; }

    // ── Shared helpers ─────────────────────────────────────────────────────
    populateModal() {
        const modalId = this.getModalId();
        const modal = document.getElementById(modalId);
        if (!modal) { console.error(`Modal "${modalId}" not found`); return; }
        const prefix = this.getFieldPrefix();
        this._setField(`${prefix}Title`,    this.title);
        this._setField(`${prefix}Prompt`,   this.prompt);
        this._setField(`${prefix}Duration`, this.durationMinutes);
        this.populateSpecificFields(prefix);
    }

    _setField(id, value) {
        const el = document.getElementById(id);
        if (!el) return;
        if (el.type === 'checkbox') el.checked = !!value;
        else el.value = value ?? '';
    }

    _getField(id) {
        const el = document.getElementById(id);
        if (!el) return null;
        return el.type === 'checkbox' ? el.checked : el.value;
    }

    _collectCommon(prefix) {
        return {
            title:           this._getField(`${prefix}Title`),
            prompt:          this._getField(`${prefix}Prompt`),
            durationMinutes: parseInt(this._getField(`${prefix}Duration`)) || 5,
        };
    }

    renderCard(index) {
        const icon    = this.constructor.metadata?.icon ?? '<i class="ics ics-clipboard ic-sm"></i>';
        const details = this.renderCardDetails();
        const n       = index + 1;
        return `
<div class="card border-0 shadow-sm">
  <div class="card-body p-3">
    <div class="d-flex gap-3">
      <div class="activity-order-column d-flex flex-column align-items-center gap-2">
        <div class="badge bg-primary rounded-circle d-flex align-items-center justify-content-center activity-order-badge">#${n}</div>
        <div class="d-flex flex-column gap-1">
          <button class="btn btn-sm btn-outline-secondary py-0 activity-order-btn"
            onclick="activityManager.moveActivityUp(${index})" ${index === 0 ? 'disabled' : ''} title="Move up">▲</button>
          <button class="btn btn-sm btn-outline-secondary py-0 activity-order-btn"
            onclick="activityManager.moveActivityDown(${index})" title="Move down">▼</button>
        </div>
      </div>
      <div class="flex-grow-1 activity-content-col">
        <div class="d-flex align-items-start justify-content-between mb-2">
          <div class="d-flex align-items-center gap-2">
            <span class="text-primary fs-5">${icon}</span>
            <h5 class="mb-0 fw-semibold">${this.escapeHtml(this.title)}</h5>
          </div>
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-primary" onclick="activityManager.editActivity(${index})">
              <i class="ics ics-pencil ic-xs ic-mr"></i>Edit
            </button>
            <button class="btn btn-sm btn-outline-primary" onclick="activityManager.copyActivity(${index})" title="Copy Activity">
              <i class="ics ics-copy ic-xs ic-mr"></i>Copy
            </button>
            <button class="btn btn-sm btn-outline-danger" onclick="activityManager.removeActivity(${index})">
              <i class="ics ics-trash ic-xs ic-mr"></i>Remove
            </button>
          </div>
        </div>
        <p class="text-muted mb-2">${this.escapeHtml(this.prompt)}</p>
        ${details ? `<div class="small text-muted"><i class="ics ics-info ic-xs ic-mr"></i>${details}</div>` : ''}
      </div>
    </div>
  </div>
</div>`;
    }

    toJSON() {
        return {
            id: this.id, type: this.type, title: this.title,
            prompt: this.prompt, durationMinutes: this.durationMinutes,
            order: this.order, config: this.config,
        };
    }

    // ── Static metadata (subclasses must override) ─────────────────────────
    static get activityType()  { return ''; }
    static get metadata()      { return { icon: '', displayName: '', description: '' }; }

    // ── Static lifecycle (subclasses must override) ────────────────────────
    static async save()               { throw new Error('save() not implemented'); }
    static reset()                    {}
    static mapTemplateConfig(cfg)     { return cfg || {}; }
}

// ── Backward-compat ActivityFactory ───────────────────────────────────────────
/**
 * ActivityFactory is kept so any code that calls ActivityFactory.create(data)
 * or ActivityFactory.getAvailableTypes() continues to work unchanged.
 * It is now a thin wrapper around ActivityRegistry.
 */
class ActivityFactory {
    static create(data) {
        return ActivityRegistry.create(data);
    }

    static getAvailableTypes() {
        return ActivityRegistry.getAll().map(Cls => ({
            type: Cls.activityType,
            class: Cls,
            emoji: Cls.metadata?.icon ?? '',
            description: Cls.metadata?.description ?? '',
        }));
    }
}

// ── Globals ───────────────────────────────────────────────────────────────────
window.Activity         = Activity;
window.ActivityBase     = ActivityBase;
window.ActivityRegistry = ActivityRegistry;
window.ActivityFactory  = ActivityFactory;
