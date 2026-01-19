/**
 * Create Session Form Handler
 * Handles form interactions for the session creation page including drag-and-drop rearranging
 */

class CreateSessionForm {
    constructor() {
      this.fieldCounter = 0;
  this.maxFields = 5;
 this.draggedElement = null;
        this.draggedIndex = null;
        
      // Initialize when DOM is ready
   if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', () => this.init());
    } else {
     this.init();
        }
    }

 init() {
        console.log('Initializing Create Session Form...');
   
        // Cache DOM elements
        this.form = document.getElementById('sessionCreateForm');
        this.dropZone = document.getElementById('formDropZone');
        this.submitBtn = document.getElementById('submitBtn');
        
if (!this.form || !this.dropZone || !this.submitBtn) {
            console.error('Required form elements not found');
            return;
    }
  
        // Initialize components
        this.initFormBuilder();
        this.initFormSubmission();

        // Expose methods globally for HTML onclick handlers
        window.removeFormField = (fieldId) => this.removeFormField(fieldId);
        window.updateFormFieldsInput = () => this.updateFormFieldsInput();
        window.updateFieldName = (fieldId, value) => this.updateFieldName(fieldId, value);
     
    console.log('Create Session Form initialized successfully');
    }

    /**
     * Initialize drag-and-drop form builder
     */
    initFormBuilder() {
   const fieldTypes = document.querySelectorAll('.field-type');
   
        // Make field types draggable
   fieldTypes.forEach(fieldType => {
      fieldType.addEventListener('dragstart', (e) => {
 e.dataTransfer.setData('text/plain', fieldType.dataset.type);
                e.dataTransfer.effectAllowed = 'copy';
    console.log('Dragging field type:', fieldType.dataset.type);
  });
        });

        // Setup mobile Add button
        this.setupMobileAddButton();

        // Setup drop zone for new fields
 this.dropZone.addEventListener('dragover', (e) => {
 e.preventDefault();
 if (!this.draggedElement) {
      this.dropZone.classList.add('drag-over');
  }
     });

        this.dropZone.addEventListener('dragleave', (e) => {
            if (!this.dropZone.contains(e.relatedTarget)) {
     this.dropZone.classList.remove('drag-over');
            }
        });

        this.dropZone.addEventListener('drop', (e) => {
 e.preventDefault();
            this.dropZone.classList.remove('drag-over');
    
          // Only handle new field drops here (not rearranging)
  if (!this.draggedElement) {
        if (this.fieldCounter >= this.maxFields) {
       this.showError(`Maximum ${this.maxFields} fields allowed`);
         return;
          }

           const fieldType = e.dataTransfer.getData('text/plain');
        if (fieldType) {
      this.addFormField(fieldType);
         }
  }
   });
    }

    /**
     * Add a new form field
     */
    addFormField(type) {
        this.fieldCounter++;
   const fieldId = `field_${this.fieldCounter}`;
        
    const fieldHtml = this.createFieldHTML(type, fieldId);
        
        // Hide placeholder if this is the first field
        const placeholder = this.dropZone.querySelector('.drop-zone-placeholder');
        if (placeholder && this.fieldCounter === 1) {
            placeholder.style.display = 'none';
        }
   
        // Add the field to the drop zone
     this.dropZone.insertAdjacentHTML('beforeend', fieldHtml);
        
        // Setup drag and drop for the new field
      this.setupFieldDragDrop(fieldId);
        
    // Update the hidden form field
      this.updateFormFieldsInput();
        
        console.log(`Added ${type} field with id: ${fieldId}`);
    }

    /**
     * Setup drag and drop for a specific field (for rearranging)
 */
    setupFieldDragDrop(fieldId) {
        const field = document.querySelector(`[data-id="${fieldId}"]`);
        if (!field) return;

        const dragHandle = field.querySelector('.drag-handle');
  if (!dragHandle) return;

     // Only allow dragging when drag handle is used
    dragHandle.addEventListener('mousedown', () => {
            field.setAttribute('draggable', 'true');
        });

      document.addEventListener('mouseup', () => {
            field.setAttribute('draggable', 'false');
        });

 field.addEventListener('dragstart', (e) => {
     if (e.target.getAttribute('draggable') !== 'true') {
       e.preventDefault();
     return;
         }
 
          this.draggedElement = field;
          this.draggedIndex = this.getFieldIndex(fieldId);
         field.classList.add('dragging');
  e.dataTransfer.effectAllowed = 'move';
      console.log('Started dragging field:', fieldId, 'at index:', this.draggedIndex);
    });

        field.addEventListener('dragend', () => {
 field.classList.remove('dragging');
         this.draggedElement = null;
            this.draggedIndex = null;
   this.clearDropTargets();
        });

  field.addEventListener('dragover', (e) => {
      if (this.draggedElement && this.draggedElement !== field) {
            e.preventDefault();
      this.highlightDropTarget(field);
       }
        });

        field.addEventListener('drop', (e) => {
       if (this.draggedElement && this.draggedElement !== field) {
         e.preventDefault();
        e.stopPropagation();
        
      const targetIndex = this.getFieldIndex(field.dataset.id);
             console.log('Dropping at index:', targetIndex);
  
    this.rearrangeFields(this.draggedIndex, targetIndex);
    this.clearDropTargets();
  }
        });
    }

    /**
   * Get the index of a field by its ID
  */
    getFieldIndex(fieldId) {
 const fields = Array.from(this.dropZone.querySelectorAll('.form-field'));
        return fields.findIndex(f => f.dataset.id === fieldId);
    }

    /**
   * Highlight drop target
     */
  highlightDropTarget(targetField) {
     this.clearDropTargets();
        targetField.classList.add('drop-target-active');
    }

/**
     * Clear all drop target highlights
 */
    clearDropTargets() {
    const fields = this.dropZone.querySelectorAll('.form-field');
   fields.forEach(f => {
        f.classList.remove('drop-target-active');
     });
  }

    /**
 * Rearrange fields
  */
    rearrangeFields(fromIndex, toIndex) {
        if (fromIndex === toIndex) return;

        const fields = Array.from(this.dropZone.querySelectorAll('.form-field'));
        const draggedField = fields[fromIndex];
        const targetField = fields[toIndex];

        if (fromIndex < toIndex) {
            targetField.parentNode.insertBefore(draggedField, targetField.nextSibling);
        } else {
            targetField.parentNode.insertBefore(draggedField, targetField);
        }

 this.updateFormFieldsInput();
        console.log(`Moved field from index ${fromIndex} to ${toIndex}`);
    }

    /**
     * Create HTML for a form field - with drag handle and auto-updating name
     */
    createFieldHTML(type, fieldId) {
        const typeIcons = {
         'text': '📝',
            'checkbox': '☑️',
      'dropdown': '📋',
          'textarea': '📄',
        'radio': '◉'
        };
        
        const optionsInput = (type === 'dropdown' || type === 'radio') ? `
       <div class="mb-2">
    <label class="form-label small text-muted">Options (comma-separated)</label>
         <input type="text" placeholder="e.g., Option 1, Option 2, Option 3" 
        class="form-control form-control-sm field-options" 
 onchange="updateFormFieldsInput()" />
     </div>
        ` : '';
        
   return `
            <div class="form-field" data-id="${fieldId}" data-type="${type}" draggable="false">
     <div class="field-header">
  <div class="drag-handle" title="Drag to reorder"></div>
       <div class="d-flex align-items-center gap-2 flex-grow-1">
 <span class="field-icon">${typeIcons[type] || '📄'}</span>
    <span class="field-type-label" data-field-name="${fieldId}">${this.getFieldTypeLabel(type)}</span>
</div>
   <button type="button" class="btn-remove-field" onclick="removeFormField('${fieldId}')" title="Remove field">
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
           <path d="M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z"/>
         </svg>
    </button>
       </div>
        <div class="field-config">
          <div class="row g-2 mb-2">
          <div class="col-8">
        <label class="form-label small fw-medium mb-1">Field label</label>
     <input type="text" placeholder="e.g., Team Name, Role, Department" 
     class="form-control form-control-sm field-label" 
  data-field-id="${fieldId}"
   oninput="updateFieldName('${fieldId}', this.value)"
     onchange="updateFormFieldsInput()" required />
     </div>
    <div class="col-4 d-flex align-items-end">
          <div class="form-check">
               <input type="checkbox" class="form-check-input field-required" 
      id="required_${fieldId}" onchange="updateFormFieldsInput()" />
  <label class="form-check-label small" for="required_${fieldId}">
   Required
 </label>
      </div>
       </div>
</div>
   ${optionsInput}
      </div>
        </div>
        `;
    }

    /**
     * Update field name in header as user types
     */
    updateFieldName(fieldId, value) {
     const nameLabel = document.querySelector(`[data-field-name="${fieldId}"]`);
if (nameLabel) {
            const fieldType = this.getFieldTypeLabel(document.querySelector(`[data-id="${fieldId}"]`).dataset.type);
            nameLabel.textContent = value.trim() || fieldType;
}
 }

    /**
     * Remove a form field
     */
    removeFormField(fieldId) {
        const field = document.querySelector(`[data-id="${fieldId}"]`);
   if (field) {
            field.remove();
    this.fieldCounter--;

   // Show placeholder if no fields remain
       if (this.fieldCounter === 0) {
         const placeholder = this.dropZone.querySelector('.drop-zone-placeholder');
          if (placeholder) {
          placeholder.style.display = 'block';
        }
            }
          
    this.updateFormFieldsInput();
            console.log(`Removed field: ${fieldId}`);
        }
    }

    /**
     * Update the hidden form field with current field configuration
     */
    updateFormFieldsInput() {
   const fields = [];
 const formFields = this.dropZone.querySelectorAll('.form-field');
        
   formFields.forEach((field, index) => {
  const label = field.querySelector('.field-label').value;
        const required = field.querySelector('.field-required').checked;
      let type = field.dataset.type;
   const optionsElement = field.querySelector('.field-options');
        const options = optionsElement ? optionsElement.value : '';
    
        // Map internal field types to API enum values
        const typeMapping = {
            'text': 'text',
 'checkbox': 'boolean',  // Map checkbox to boolean
     'dropdown': 'dropdown',
  'textarea': 'text',      // Map textarea to text
            'radio': 'dropdown'      // Map radio to dropdown with options
        };
        
        type = typeMapping[type] || type;
  
        if (label.trim()) {
        fields.push({
        id: `field_${index + 1}`,
        label: label.trim(),
            type: type,
      required: required,
        options: options.trim()
    });
 }
    });
        
    const hiddenInput = document.getElementById('joinFormFields');
  if (hiddenInput) {
      hiddenInput.value = JSON.stringify(fields);
        }

        console.log('Updated form fields:', fields);
    }

    /**
     * Get human-readable label for field type
     */
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

    /**
     * Initialize form submission handling
     */
    initFormSubmission() {
        const btnText = this.submitBtn.querySelector('.btn-text');
        const btnLoading = this.submitBtn.querySelector('.btn-loading');

        this.form.addEventListener('submit', async (e) => {
   e.preventDefault();

            try {
// Show loading state
  btnText.style.display = 'none';
            btnLoading.style.display = 'inline-flex';
           this.submitBtn.disabled = true;
          this.hideError();
      
                // Ensure form fields are up to date
        this.updateFormFieldsInput();
          
   // Build session data
              const formData = new FormData(this.form);
           const sessionData = this.buildSessionData(formData);
            
                console.log('Submitting session data:', sessionData);
         
       // Submit to API
      const response = await fetch('/api/sessions', {
     method: 'POST',
         headers: { 'Content-Type': 'application/json' },
     body: JSON.stringify(sessionData)
           });
            
    if (response.ok) {
        const result = await response.json();
      console.log('Session created successfully:', result);
        
        // Extract code from the wrapped API response
 const sessionCode = result.data?.code || result.code;
  
        if (!sessionCode) {
            console.error('No session code in response:', result);
            this.showError('Session created but no code returned');
            return;
        }
        
        window.location.href = `/facilitator/live?code=${sessionCode}`;
    } else {
        const error = await response.json();
        console.error('API error:', error);
        
  // Extract error message from wrapped response
        const errorMessage = error.errors?.[0]?.message || error.message || 'Failed to create session';
        this.showError(errorMessage);
    }
            } catch (error) {
    console.error('Submission error:', error);
           this.showError(`Failed to create session: ${error.message}`);
            } finally {
          // Reset loading state
  btnText.style.display = 'inline';
          btnLoading.style.display = 'none';
     this.submitBtn.disabled = false;
            }
        });
    }

    /**
     * Build session data object from form
     */
    buildSessionData(formData) {
      // Build context string from all AI context fields
        const contextParts = [];
        if (formData.get('currentProcess')) contextParts.push(`Current Process: ${formData.get('currentProcess')}`);
        if (formData.get('painPoints')) contextParts.push(`Pain Points: ${formData.get('painPoints')}`);
  if (formData.get('technicalContext')) contextParts.push(`Technical Context: ${formData.get('technicalContext')}`);
   if (formData.get('teamBackground')) contextParts.push(`Team Background: ${formData.get('teamBackground')}`);
        if (formData.get('aiGoals')) contextParts.push(`AI Goals: ${formData.get('aiGoals')}`);

  return {
      // Let server generate the code - it will be unique and in XXX-XXX-XXX format
       title: formData.get('sessionTitle'),
    goal: formData.get('sessionGoal') || null,
    context: contextParts.length > 0 ? contextParts.join(' | ') : null,
    groupId: formData.get('groupId') || null,
            settings: {
     maxContributionsPerParticipantPerSession: 999, // High limit to effectively disable session-level cap
  maxContributionsPerParticipantPerActivity: parseInt(formData.get('maxContributions') || '5'),
   strictCurrentActivityOnly: true,
        allowAnonymous: false,
            ttlMinutes: 360
      },
         joinFormSchema: {
      maxFields: 5,
       fields: JSON.parse(formData.get('joinFormFields') || '[]')
          }
    };
    }

    /**
     * Setup mobile Add button for adding fields
     */
    setupMobileAddButton() {
        const addBtn = document.getElementById('mobileAddFieldBtn');
        const select = document.getElementById('mobileFieldTypeSelect');
        
        if (addBtn && select) {
            console.log('Setting up mobile add button');
            addBtn.addEventListener('click', () => {
                const fieldType = select.value;
                if (fieldType) {
                    console.log('Adding field from mobile:', fieldType);
                    this.addFormField(fieldType);
                    select.value = ''; // Reset dropdown
                } else {
                    this.showError('Please select a field type');
                }
            });
        }
    }

    /**
     * Show error message to user
  */
    showError(message) {
        const errorDiv = document.getElementById('errorMessage');
     if (errorDiv) {
      errorDiv.textContent = message;
    errorDiv.style.display = 'block';
  errorDiv.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    }

    /**
     * Hide error message
     */
    hideError() {
      const errorDiv = document.getElementById('errorMessage');
        if (errorDiv) {
  errorDiv.style.display = 'none';
     }
 }
}


// Initialize the form when this script loads
new CreateSessionForm();