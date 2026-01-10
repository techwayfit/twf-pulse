// TechWayFit Pulse - Form Builder
// Simple drag-drop form builder for join form configuration

class FormBuilder {
    constructor(containerId) {
        this.container = typeof containerId === 'string' 
            ? document.getElementById(containerId) 
            : containerId;
        this.fields = [];
        this.maxFields = 5;
        this.fieldIdCounter = 0;
        
        if (!this.container) {
            console.error('Form builder container not found:', containerId);
            return;
        }
        
        this.init();
    }

    init() {
        console.log('Form builder initializing...', this.container);
        this.renderFieldTypes();
        this.renderDropZone();
        this.setupDragAndDrop();
        console.log('Form builder initialized successfully');
    }

    renderFieldTypes() {
        const typesSection = this.container.querySelector('.field-types');
        console.log('Field types section:', typesSection);
        if (!typesSection) {
            console.error('Field types section not found!');
            return;
        }

        const fieldTypes = [
            { type: 'text', label: 'Text Box', icon: '📝' },
            { type: 'checkbox', label: 'Checkbox', icon: '☑️' },
            { type: 'dropdown', label: 'Dropdown', icon: '▼' },
            { type: 'textarea', label: 'Text Area', icon: '📄' },
            { type: 'radio', label: 'Radio Button', icon: '◉' }
        ];

        typesSection.innerHTML = fieldTypes.map(ft => `
            <div class="card field-type-item" draggable="true" data-field-type="${ft.type}">
                <div class="card-body p-2 d-flex align-items-center gap-2">
                    <span class="field-icon">${ft.icon}</span>
                    <span class="small">${ft.label}</span>
                </div>
            </div>
        `).join('');
    }

    renderDropZone() {
        const dropZone = this.container.querySelector('.form-drop-zone');
        console.log('Drop zone:', dropZone);
        if (!dropZone) {
            console.error('Drop zone not found!');
            return;
        }

        if (this.fields.length === 0) {
            dropZone.innerHTML = `
                <div class="drop-zone-empty text-center">
                    <p class="fw-semibold mb-2">Drag field types here (${this.fields.length}/${this.maxFields})</p>
                    <div class="text-muted small">Build your join form by dragging fields from the left</div>
                </div>
            `;
        } else {
            dropZone.innerHTML = `
                <div class="form-fields-list">
                    ${this.fields.map((field, index) => this.renderField(field, index)).join('')}
                </div>
            `;
        }
    }

    renderField(field, index) {
        const optionsHtml = (field.type === 'dropdown' || field.type === 'radio') ? `
            <div class="mt-3">
                <label class="form-label">Options (comma separated)</label>
                <input class="form-control" type="text" value="${field.options || ''}" 
                       onchange="formBuilder.updateFieldOptions(${index}, this.value)" 
                       placeholder="e.g., Option 1, Option 2, Option 3" />
            </div>
        ` : '';

        return `
            <div class="card form-field-item" data-field-id="${field.id}">
                <div class="card-header bg-light d-flex align-items-center gap-2 py-2">
                    <div class="field-drag-handle text-muted" style="cursor: grab;">⋮⋮</div>
                    <div class="flex-grow-1 d-flex align-items-center gap-2">
                        <strong class="small">${field.label || `Field ${index + 1}`}</strong>
                        <span class="badge bg-secondary">${this.getFieldTypeLabel(field.type)}</span>
                    </div>
                    <button class="btn btn-sm btn-outline-danger" onclick="formBuilder.removeField(${index})">Remove</button>
                </div>
                <div class="card-body">
                    <div class="row g-3">
                        <div class="col-md-8">
                            <label class="form-label">Field Label</label>
                            <input class="form-control" type="text" value="${field.label || ''}" 
                                   onchange="formBuilder.updateFieldLabel(${index}, this.value)" 
                                   placeholder="e.g., Team Name" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Required</label>
                            <select class="form-select" onchange="formBuilder.updateFieldRequired(${index}, this.value === 'true')">
                                <option value="true" ${field.required ? 'selected' : ''}>Yes</option>
                                <option value="false" ${!field.required ? 'selected' : ''}>No</option>
                            </select>
                        </div>
                    </div>
                    ${optionsHtml}
                </div>
            </div>
        `;
    }

    getFieldTypeLabel(type) {
        const labels = {
            'text': 'Text Box',
            'checkbox': 'Checkbox',
            'dropdown': 'Dropdown',
            'textarea': 'Text Area',
            'radio': 'Radio Button'
        };
        return labels[type] || type;
    }

    setupDragAndDrop() {
        // Drag from field types
        this.container.addEventListener('dragstart', (e) => {
            if (e.target.classList.contains('field-type-item')) {
                e.dataTransfer.effectAllowed = 'copy';
                e.dataTransfer.setData('fieldType', e.target.dataset.fieldType);
            }
        });

        // Drop zone events
        const dropZone = this.container.querySelector('.form-drop-zone');
        if (dropZone) {
            dropZone.addEventListener('dragover', (e) => {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'copy';
                dropZone.classList.add('drag-over');
            });

            dropZone.addEventListener('dragleave', () => {
                dropZone.classList.remove('drag-over');
            });

            dropZone.addEventListener('drop', (e) => {
                e.preventDefault();
                dropZone.classList.remove('drag-over');
                
                const fieldType = e.dataTransfer.getData('fieldType');
                if (fieldType) {
                    this.addField(fieldType);
                }
            });
        }
    }

    addField(type) {
        if (this.fields.length >= this.maxFields) {
            alert(`Maximum ${this.maxFields} fields allowed`);
            return;
        }

        const field = {
            id: this.fieldIdCounter++,
            type: type,
            label: '',
            required: true,
            options: type === 'dropdown' || type === 'radio' ? 'Option 1, Option 2, Option 3' : null
        };

        this.fields.push(field);
        this.renderDropZone();
        this.notifyChange();
    }

    removeField(index) {
        this.fields.splice(index, 1);
        this.renderDropZone();
        this.notifyChange();
    }

    updateFieldLabel(index, label) {
        this.fields[index].label = label;
        this.notifyChange();
    }

    updateFieldRequired(index, required) {
        this.fields[index].required = required;
        this.notifyChange();
    }

    updateFieldOptions(index, options) {
        this.fields[index].options = options;
        this.notifyChange();
    }

    getFields() {
        return this.fields;
    }

    setFields(fields) {
        this.fields = fields || [];
        this.renderDropZone();
    }

    notifyChange() {
        // Dispatch custom event for Blazor integration
        const event = new CustomEvent('formBuilderChange', { 
            detail: { fields: this.fields }
        });
        this.container.dispatchEvent(event);
    }
}

// Global instance
let formBuilder;

// Auto-initialize when DOM is ready
if (typeof document !== 'undefined') {
    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', autoInitFormBuilder);
    } else {
        // DOM already loaded
        setTimeout(autoInitFormBuilder, 100);
    }
}

function autoInitFormBuilder() {
    console.log('Auto-initializing form builder...');
    const container = document.getElementById('joinFormBuilder');
    if (container && !formBuilder) {
        console.log('Container found, creating form builder');
        formBuilder = new FormBuilder('joinFormBuilder');
    } else if (!container) {
        console.log('Container not found, will retry...');
        // Retry after a short delay (for Blazor rendering)
        setTimeout(autoInitFormBuilder, 500);
    }
}

// Initialize when DOM is ready
window.initFormBuilder = function(containerId) {
    try {
        console.log('initFormBuilder called with:', containerId);
        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            return null;
        }
        if (!formBuilder) {
            formBuilder = new FormBuilder(containerId);
        }
        console.log('Form builder instance created:', formBuilder);
        return formBuilder;
    } catch (error) {
        console.error('Error initializing form builder:', error);
        return null;
    }
};

window.getFormBuilderFields = function() {
    try {
        return formBuilder ? formBuilder.getFields() : [];
    } catch (error) {
        console.error('Error getting form fields:', error);
        return [];
    }
};

window.setFormBuilderFields = function(fields) {
    try {
        if (formBuilder) {
            formBuilder.setFields(fields);
        }
    } catch (error) {
        console.error('Error setting form fields:', error);
    }
};
