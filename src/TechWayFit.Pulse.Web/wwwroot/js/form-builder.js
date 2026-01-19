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
        this.setupMobileAddButton();
        console.log('Form builder initialized successfully');
    }

    setupMobileAddButton() {
        const addBtn = document.getElementById('mobileAddFieldBtn');
        const select = document.getElementById('mobileFieldTypeSelect');
        
        if (addBtn && select) {
            addBtn.addEventListener('click', () => {
                const fieldType = select.value;
                if (fieldType) {
                    this.addField(fieldType);
                    select.value = ''; // Reset dropdown
                } else {
                    alert('Please select a field type');
                }
            });
        }
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
            const fieldsHtml = this.fields.map((field, index) => {
                const dropTargetTop = `<div class="drop-target" data-insert-before="${index}"></div>`;
                const fieldHtml = this.renderField(field, index);
                return dropTargetTop + fieldHtml;
            }).join('');
            
            // Add final drop target at the end
            const dropTargetBottom = `<div class="drop-target" data-insert-before="${this.fields.length}"></div>`;
            
            dropZone.innerHTML = `
                <div class="form-fields-list">
                    ${fieldsHtml}
                    ${dropTargetBottom}
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

        const fieldDisplayName = field.label && field.label.trim() !== '' 
            ? field.label 
            : `Field ${index + 1}`;

        const isExpanded = field.expanded !== false;
        const bodyClass = isExpanded ? '' : 'collapse';
        const chevronIcon = isExpanded ? '▼' : '▶';

        return `
            <div class="card form-field-item" data-field-id="${field.id}" data-field-index="${index}" draggable="true">
                <div class="card-header field-header d-flex align-items-center gap-2 py-2">
                    <button class="chevron-toggle" onclick="event.stopPropagation(); formBuilder.toggleField(${index})" title="Expand/Collapse">
                        <span class="chevron-icon">${chevronIcon}</span>
                    </button>
                    <div class="field-drag-handle text-muted" onclick="event.stopPropagation()" title="Drag to reorder">⋮⋮</div>
                    <div class="flex-grow-1 d-flex align-items-center gap-2" onclick="formBuilder.toggleField(${index})">
                        <strong class="small field-title-display">${fieldDisplayName}</strong>
                        <span class="badge bg-secondary">${this.getFieldTypeLabel(field.type)}</span>
                    </div>
                    <span class="badge bg-danger remove-badge" onclick="event.stopPropagation(); formBuilder.removeField(${index})" title="Remove field">
                        Remove
                    </span>
                </div>
                <div class="card-body field-body ${bodyClass}">
                    <div class="row g-3">
                        <div class="col-md-8">
                            <label class="form-label">Field Label</label>
                            <input class="form-control field-label-input" type="text" value="${field.label || ''}" 
                                   onchange="formBuilder.updateFieldLabel(${index}, this.value)" 
                                   oninput="formBuilder.updateFieldTitlePreview(${index}, this.value)"
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
        let draggedFieldIndex = null;
        let touchStartY = 0;
        let touchStartX = 0;
        let draggedElement = null;
        let isDragging = false;
        let clone = null;
        let touchTarget = null;

        // Touch event handlers for mobile
        this.container.addEventListener('touchstart', (e) => {
            // Match HTML structure: .field-type for available types, .form-field-item for added fields
            const fieldType = e.target.closest('.field-type');
            const fieldItem = e.target.closest('.form-field-item, .added-field');
            
            if (fieldType || fieldItem) {
                touchStartY = e.touches[0].clientY;
                touchStartX = e.touches[0].clientX;
                draggedElement = fieldType || fieldItem;
                touchTarget = e.target;
                
                if (fieldItem) {
                    draggedFieldIndex = parseInt(fieldItem.dataset.fieldIndex);
                }
            }
        }, { passive: true });

        this.container.addEventListener('touchmove', (e) => {
            if (!draggedElement) return;
            
            const touch = e.touches[0];
            const moveY = Math.abs(touch.clientY - touchStartY);
            const moveX = Math.abs(touch.clientX - touchStartX);
            
            // Start dragging if moved more than 10px
            if (!isDragging && (moveY > 10 || moveX > 10)) {
                isDragging = true;
                e.preventDefault();
                
                // Create visual clone
                clone = draggedElement.cloneNode(true);
                clone.style.position = 'fixed';
                clone.style.zIndex = '10000';
                clone.style.opacity = '0.8';
                clone.style.pointerEvents = 'none';
                clone.style.width = draggedElement.offsetWidth + 'px';
                document.body.appendChild(clone);
                
                if (draggedElement.classList.contains('form-field-item')) {
                    draggedElement.style.opacity = '0.4';
                }
            }
            
            if (isDragging && clone) {
                e.preventDefault();
                clone.style.left = touch.clientX - (clone.offsetWidth / 2) + 'px';
                clone.style.top = touch.clientY - (clone.offsetHeight / 2) + 'px';
                
                // Highlight drop targets
                const dropTargets = this.container.querySelectorAll('.drop-target');
                dropTargets.forEach(target => {
                    const rect = target.getBoundingClientRect();
                    if (touch.clientY >= rect.top && touch.clientY <= rect.bottom &&
                        touch.clientX >= rect.left && touch.clientX <= rect.right) {
                        target.classList.add('drop-target-active');
                    } else {
                        target.classList.remove('drop-target-active');
                    }
                });
            }
        }, { passive: false });

        this.container.addEventListener('touchend', (e) => {
            if (isDragging && clone) {
                const touch = e.changedTouches[0];
                const dropTarget = this.container.querySelectorAll('.drop-target');
                let dropped = false;
                
                dropTarget.forEach(target => {
                    const rect = target.getBoundingClientRect();
                    if (touch.clientY >= rect.top && touch.clientY <= rect.bottom &&
                        touch.clientX >= rect.left && touch.clientX <= rect.right) {
                        const insertBefore = parseInt(target.dataset.insertBefore);
                        
                        // Match HTML: field-type has data-type attribute
                        if (draggedElement.classList.contains('field-type') && draggedElement.dataset.type) {
                            const fieldType = draggedElement.dataset.type;
                            this.addFieldAtPosition(fieldType, insertBefore);
                        } else if (draggedFieldIndex !== null) {
                            this.reorderFields(draggedFieldIndex, insertBefore);
                        }
                        dropped = true;
                    }
                    target.classList.remove('drop-target-active');
                });
                
                // If not dropped on a target but in drop zone, add at end
                if (!dropped && draggedElement.classList.contains('field-type') && draggedElement.dataset.type) {
                    const dropZone = this.container.querySelector('.form-drop-zone');
                    if (dropZone) {
                        const rect = dropZone.getBoundingClientRect();
                        if (touch.clientY >= rect.top && touch.clientY <= rect.bottom &&
                            touch.clientX >= rect.left && touch.clientX <= rect.right) {
                            const fieldType = draggedElement.dataset.type;
                            this.addField(fieldType);
                        }
                    }
                }
                
                clone.remove();
                clone = null;
            }
            
            if (draggedElement && draggedElement.classList.contains('form-field-item')) {
                draggedElement.style.opacity = '';
            }
            
            draggedElement = null;
            isDragging = false;
            draggedFieldIndex = null;
        });

        // Use event delegation on container for all drag events
        this.container.addEventListener('dragstart', (e) => {
            if (e.target.classList.contains('field-type-item')) {
                e.dataTransfer.effectAllowed = 'copy';
                e.dataTransfer.setData('fieldType', e.target.dataset.fieldType);
                console.log('Dragging field type:', e.target.dataset.fieldType);
            } else if (e.target.closest('.form-field-item')) {
                // Use closest to handle dragging from child elements
                const fieldItem = e.target.closest('.form-field-item');
                draggedFieldIndex = parseInt(fieldItem.dataset.fieldIndex);
                console.log('Dragging field at index:', draggedFieldIndex);
                e.dataTransfer.effectAllowed = 'move';
                e.dataTransfer.setData('text/html', fieldItem.innerHTML);
                fieldItem.style.opacity = '0.4';
            }
        });

        this.container.addEventListener('dragend', (e) => {
            const fieldItem = e.target.closest('.form-field-item');
            if (fieldItem) {
                fieldItem.style.opacity = '';
                console.log('Drag ended, resetting index');
                draggedFieldIndex = null;
                // Remove all highlights
                this.container.querySelectorAll('.drop-target').forEach(el => {
                    el.classList.remove('drop-target-active');
                });
            }
        });

        // Handle dragover on drop targets
        this.container.addEventListener('dragover', (e) => {
            if (e.target.classList.contains('drop-target')) {
                e.preventDefault();
                e.stopPropagation();
                
                // Remove all highlights
                this.container.querySelectorAll('.drop-target').forEach(el => {
                    el.classList.remove('drop-target-active');
                });
                
                // Highlight current target
                e.target.classList.add('drop-target-active');
                console.log('Hovering over drop target:', e.target.dataset.insertBefore);
            }
        });

        // Handle drop on drop targets
        this.container.addEventListener('drop', (e) => {
            console.log('Drop event fired on:', e.target);
            
            if (e.target.classList.contains('drop-target')) {
                e.preventDefault();
                e.stopPropagation();
                e.target.classList.remove('drop-target-active');
                
                const insertBefore = parseInt(e.target.dataset.insertBefore);
                const fieldType = e.dataTransfer.getData('fieldType');
                
                console.log('Drop event:', { insertBefore, fieldType, draggedFieldIndex });
                
                if (fieldType) {
                    // Adding new field at specific position
                    this.addFieldAtPosition(fieldType, insertBefore);
                } else if (draggedFieldIndex !== null) {
                    // Reordering existing field
                    console.log('Calling reorderFields with:', draggedFieldIndex, insertBefore);
                    this.reorderFields(draggedFieldIndex, insertBefore);
                }
                
                draggedFieldIndex = null;
            }
        });

        // Also handle drop zone for backward compatibility
        const dropZone = this.container.querySelector('.form-drop-zone');
        if (dropZone) {
            dropZone.addEventListener('dragover', (e) => {
                e.preventDefault();
            });

            dropZone.addEventListener('drop', (e) => {
                // Only handle if not dropped on a drop-target
                if (!e.target.classList.contains('drop-target')) {
                    e.preventDefault();
                    
                    const fieldType = e.dataTransfer.getData('fieldType');
                    if (fieldType) {
                        this.addField(fieldType);
                    }
                }
            });
        }
    }

    addFieldAtPosition(type, position) {
        if (this.fields.length >= this.maxFields) {
            alert(`Maximum ${this.maxFields} fields allowed`);
            return;
        }

        const field = {
            id: this.fieldIdCounter++,
            type: type,
            label: '',
            required: true,
            expanded: true,
            options: type === 'dropdown' || type === 'radio' ? 'Option 1, Option 2, Option 3' : null
        };

        this.fields.splice(position, 0, field);
        this.renderDropZone();
        this.notifyChange();
    }

    addField(type) {
        // Add at the end
        this.addFieldAtPosition(type, this.fields.length);
    }

    removeField(index) {
        this.fields.splice(index, 1);
        this.renderDropZone();
        this.notifyChange();
    }

    toggleField(index) {
        this.fields[index].expanded = !this.fields[index].expanded;
        this.renderDropZone();
    }

    updateFieldLabel(index, label) {
        this.fields[index].label = label;
        this.notifyChange();
    }

    updateFieldTitlePreview(index, label) {
        // Update the field title display in real-time as user types
        const fieldItems = this.container.querySelectorAll('.form-field-item');
        if (fieldItems[index]) {
            const titleDisplay = fieldItems[index].querySelector('.field-title-display');
            if (titleDisplay) {
                titleDisplay.textContent = label && label.trim() !== '' ? label : `Field ${index + 1}`;
            }
        }
    }

    updateFieldRequired(index, required) {
        this.fields[index].required = required;
        this.notifyChange();
    }

    updateFieldOptions(index, options) {
        this.fields[index].options = options;
        this.notifyChange();
    }

    reorderFields(fromIndex, toIndex) {
        console.log('Reordering:', { fromIndex, toIndex, fieldsLength: this.fields.length });
        
        // Don't reorder if dropping in the same position
        if (fromIndex === toIndex || fromIndex === toIndex - 1) {
            console.log('Same position, skipping reorder');
            this.renderDropZone();
            return;
        }
        
        // Extract the field being moved
        const [movedField] = this.fields.splice(fromIndex, 1);
        console.log('Moved field:', movedField);
        
        // Adjust target index if moving down
        const adjustedToIndex = fromIndex < toIndex ? toIndex - 1 : toIndex;
        console.log('Adjusted toIndex:', adjustedToIndex);
        
        // Insert at new position
        this.fields.splice(adjustedToIndex, 0, movedField);
        console.log('New field order:', this.fields.map(f => f.label || f.type));
        
        this.renderDropZone();
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

// Initialize when called from Blazor
window.initFormBuilder = function(containerId) {
    try {
        console.log('initFormBuilder called with:', containerId);
        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            return null;
        }
        
        // Always create fresh instance to avoid stale references
        formBuilder = new FormBuilder(containerId);
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
