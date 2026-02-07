// Icon Picker Component for Session Groups
// Provides emoji selection with visual picker and color selector

class IconPicker {
    constructor(inputId, options = {}) {
        this.inputId = inputId;
        this.input = document.getElementById(inputId);
        this.displayId = options.displayId || `${inputId}-display`;
        this.triggerButtonId = options.triggerButtonId || null; // Separate trigger button
        this.colorInputId = options.colorInputId || null;
        this.pickerContainerId = options.pickerContainerId || `${inputId}-picker`;
        this.defaultIcon = options.defaultIcon || '📁';
        this.selectedIcon = this.input ? this.input.value || this.defaultIcon : this.defaultIcon;
        this.mode = options.mode || 'both'; // 'icon-only', 'color-only', or 'both'
        this.onColorChange = options.onColorChange || null;
        
        console.log('IconPicker constructor called for:', inputId);
        console.log('Options received:', options);
        console.log('Mode:', this.mode);
        
        // Predefined color palette - expanded for workshops
        this.colors = [
            { name: 'Blue', value: '#3b82f6', bg: '#eff6ff' },
            { name: 'Sky Blue', value: '#0ea5e9', bg: '#e0f2fe' },
            { name: 'Cyan', value: '#06b6d4', bg: '#ecfeff' },
            { name: 'Green', value: '#10b981', bg: '#ecfdf5' },
            { name: 'Emerald', value: '#059669', bg: '#d1fae5' },
            { name: 'Lime', value: '#84cc16', bg: '#f7fee7' },
            { name: 'Yellow', value: '#eab308', bg: '#fefce8' },
            { name: 'Orange', value: '#f59e0b', bg: '#fffbeb' },
            { name: 'Amber', value: '#f97316', bg: '#fff7ed' },
            { name: 'Red', value: '#ef4444', bg: '#fef2f2' },
            { name: 'Rose', value: '#f43f5e', bg: '#fff1f2' },
            { name: 'Pink', value: '#ec4899', bg: '#fdf2f8' },
            { name: 'Purple', value: '#8b5cf6', bg: '#f5f3ff' },
            { name: 'Violet', value: '#a855f7', bg: '#faf5ff' },
            { name: 'Indigo', value: '#6366f1', bg: '#eef2ff' },
            { name: 'Teal', value: '#14b8a6', bg: '#f0fdfa' },
            { name: 'Slate', value: '#64748b', bg: '#f8fafc' },
            { name: 'Gray', value: '#6b7280', bg: '#f9fafb' }
        ];
        
        // Curated emoji list for workshops - organized by category
        this.emojis = [
            // Learning & Education
            '📚', '📖', '📝', '✏️', '📋', '📊', '📈', '📉', '🎓', '🎯',
            // Achievement & Goals
            '🏆', '🥇', '🥈', '🥉', '🏅', '⭐', '✨', '💎', '🎖️', '👑',
            // Ideas & Innovation
            '💡', '🔥', '⚡', '🚀', '🌟', '💫', '🎨', '🎭', '🎪', '🎬',
            // Organization & Planning
            '📁', '📂', '📌', '📍', '🗂️', '📎', '🔖', '🏷️', '📅', '🗓️',
            // Communication & Collaboration
            '💬', '🗣️', '👥', '🤝', '💼', '🎤', '📢', '📣', '🔔', '✅'
        ];
        
        this.init();
    }
    
    init() {
        console.log('IconPicker init:', this.displayId, this.inputId);
        const display = document.getElementById(this.displayId);
        console.log('Display element found:', display);
        
        this.createPicker();
        this.attachEventListeners();
        this.updateDisplay(this.selectedIcon);
    }
    
    createPicker() {
        const display = document.getElementById(this.displayId);
        const triggerButton = this.triggerButtonId ? document.getElementById(this.triggerButtonId) : null;
        
        if (!display && !triggerButton) {
            console.error('IconPicker: Neither display nor trigger button element found');
            return;
        }
        
        console.log('Creating picker for:', this.displayId, 'Mode:', this.mode);
        
        // Create picker container
        const picker = document.createElement('div');
        picker.id = this.pickerContainerId;
        picker.className = 'icon-picker-dropdown';
        picker.style.cssText = 'position: absolute; z-index: 1060; background: white; border: 1px solid #e3e8ef; border-radius: 0.75rem; padding: 1rem; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.1), 0 4px 6px -2px rgba(0,0,0,0.05); display: none; margin-top: 0.5rem; max-height: 80vh; overflow-y: auto;';
        
        // Add icon picker section if mode includes icons
        if (this.mode === 'icon-only' || this.mode === 'both') {
            this.createIconSection(picker);
        }
        
        // Add separator if both modes
        if (this.mode === 'both') {
            const separator = document.createElement('hr');
            separator.style.cssText = 'margin: 1rem 0; border: 0; border-top: 1px solid #e3e8ef;';
            picker.appendChild(separator);
        }
        
        // Add color picker section if mode includes colors
        if (this.mode === 'color-only' || this.mode === 'both') {
            this.createColorSection(picker);
        }
        
        const parentElement = triggerButton ? triggerButton.parentElement : display.parentElement;
        parentElement.style.position = 'relative';
        parentElement.appendChild(picker);
    }
    
    createIconSection(picker) {
        // Section title
        const iconTitle = document.createElement('div');
        iconTitle.textContent = 'Choose Icon';
        iconTitle.style.cssText = 'font-weight: 600; font-size: 0.875rem; color: #1e293b; margin-bottom: 0.75rem;';
        picker.appendChild(iconTitle);
        
        // Create grid of emojis
        const iconGrid = document.createElement('div');
        iconGrid.style.cssText = 'display: grid; grid-template-columns: repeat(10, 1fr); gap: 0.25rem; max-height: 180px; overflow-y: auto; padding: 0.5rem; background: #f8fafc; border-radius: 0.5rem; margin-bottom: ' + (this.mode === 'icon-only' ? '0' : '1rem') + '; border: 1px solid #e2e8f0;';
        
        this.emojis.forEach(emoji => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'icon-picker-item';
            btn.textContent = emoji;
            btn.dataset.icon = emoji;
            btn.style.cssText = 'width: 28px; height: 28px; border: 2px solid transparent; border-radius: 0.375rem; background: white; cursor: pointer; font-size: 1.1rem; display: flex; align-items: center; justify-content: center; transition: all 0.15s ease;';
            
            // Highlight if this is the selected icon
            if (emoji === this.selectedIcon) {
                btn.style.borderColor = '#3b82f6';
                btn.style.background = '#eff6ff';
                btn.style.transform = 'scale(1.1)';
            }
            
            btn.addEventListener('mouseenter', () => {
                if (emoji !== this.selectedIcon) {
                    btn.style.background = '#f1f5f9';
                    btn.style.borderColor = '#cbd5e0';
                    btn.style.transform = 'scale(1.1)';
                }
            });
            
            btn.addEventListener('mouseleave', () => {
                if (emoji !== this.selectedIcon) {
                    btn.style.background = 'white';
                    btn.style.borderColor = 'transparent';
                    btn.style.transform = 'scale(1)';
                }
            });
            
            btn.addEventListener('click', () => {
                this.selectIcon(emoji);
            });
            
            iconGrid.appendChild(btn);
        });
        
        picker.appendChild(iconGrid);
    }
    
    createColorSection(picker) {
        const colorTitle = document.createElement('div');
        colorTitle.textContent = this.mode === 'color-only' ? 'Choose Color' : 'Badge Color (Optional)';
        colorTitle.style.cssText = 'font-weight: 600; font-size: 0.875rem; color: #1e293b; margin-bottom: 0.75rem; padding-top: 0.25rem;';
        picker.appendChild(colorTitle);
        
        const colorGrid = document.createElement('div');
        colorGrid.style.cssText = 'display: grid; grid-template-columns: repeat(6, 1fr); gap: 0.5rem; margin-bottom: 0.5rem;';
        
        this.colors.forEach(color => {
            const colorBtn = document.createElement('button');
            colorBtn.type = 'button';
            colorBtn.className = 'color-picker-item';
            colorBtn.title = color.name; // Tooltip on hover
            colorBtn.style.cssText = 'width: 36px; height: 36px; border: 2px solid #e3e8ef; border-radius: 0.5rem; background: ' + color.value + '; cursor: pointer; transition: all 0.15s ease; position: relative;';
            
            colorBtn.addEventListener('mouseenter', () => {
                colorBtn.style.borderColor = color.value;
                colorBtn.style.transform = 'scale(1.15)';
                colorBtn.style.boxShadow = '0 4px 6px -1px rgba(0,0,0,0.2)';
            });
            
            colorBtn.addEventListener('mouseleave', () => {
                colorBtn.style.borderColor = '#e3e8ef';
                colorBtn.style.transform = 'scale(1)';
                colorBtn.style.boxShadow = 'none';
            });
            
            colorBtn.addEventListener('click', () => {
                this.selectColor(color.value);
            });
            
            colorGrid.appendChild(colorBtn);
        });
        
        picker.appendChild(colorGrid);
        
        // Clear color button
        const clearColorBtn = document.createElement('button');
        clearColorBtn.type = 'button';
        clearColorBtn.textContent = '✕ Clear Color';
        clearColorBtn.style.cssText = 'width: 100%; padding: 0.5rem; border: 1px solid #e3e8ef; border-radius: 0.375rem; background: white; color: #64748b; font-size: 0.8rem; cursor: pointer; transition: all 0.15s ease; margin-top: 0.5rem;';
        
        clearColorBtn.addEventListener('mouseenter', () => {
            clearColorBtn.style.background = '#f8fafc';
            clearColorBtn.style.borderColor = '#cbd5e0';
        });
        
        clearColorBtn.addEventListener('mouseleave', () => {
            clearColorBtn.style.background = 'white';
            clearColorBtn.style.borderColor = '#e3e8ef';
        });
        
        clearColorBtn.addEventListener('click', () => {
            this.selectColor(null);
        });
        
        picker.appendChild(clearColorBtn);
    }
    
    attachEventListeners() {
        const display = document.getElementById(this.displayId);
        const triggerButton = this.triggerButtonId ? document.getElementById(this.triggerButtonId) : null;
        const clickTarget = triggerButton || display;
        
        if (!clickTarget) {
            console.error('IconPicker: Cannot attach listeners, no clickable element found');
            return;
        }
        
        // Prevent duplicate event listeners
        if (clickTarget.dataset.iconPickerInitialized === 'true') {
            console.log('Event listener already attached to:', clickTarget.id);
            return;
        }
        clickTarget.dataset.iconPickerInitialized = 'true';
        
        console.log('Attaching event listeners to:', clickTarget.id);
        
        // Toggle picker on click
        clickTarget.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            console.log('Trigger clicked!');
            this.togglePicker();
        });
        
        // Close picker when clicking outside
        document.addEventListener('click', (e) => {
            const picker = document.getElementById(this.pickerContainerId);
            if (picker && !picker.contains(e.target) && e.target !== clickTarget) {
                this.hidePicker();
            }
        });
    }
    
    togglePicker() {
        let picker = document.getElementById(this.pickerContainerId);
        console.log('Toggle picker:', this.pickerContainerId, picker);
        if (!picker) {
            console.warn('Picker element not found, recreating:', this.pickerContainerId);
            // Try to recreate the picker
            this.createPicker();
            picker = document.getElementById(this.pickerContainerId);
            if (!picker) {
                console.error('Failed to create picker element');
                return;
            }
        }
        
        if (picker.style.display === 'none' || picker.style.display === '') {
            console.log('Showing picker');
            picker.style.display = 'block';
        } else {
            console.log('Hiding picker');
            picker.style.display = 'none';
        }
    }
    
    hidePicker() {
        const picker = document.getElementById(this.pickerContainerId);
        if (picker) {
            picker.style.display = 'none';
        }
    }
    
    selectIcon(emoji) {
        console.log('selectIcon called with:', emoji);
        console.log('this.inputId:', this.inputId);
        console.log('this.input:', this.input);
        
        this.selectedIcon = emoji;
        
        if (this.input) {
            this.input.value = emoji;
            console.log('Icon input value set to:', this.input.value);
            // Trigger change event
            this.input.dispatchEvent(new Event('change'));
        } else {
            console.error('Icon input element not found!');
        }
        
        this.updateDisplay(emoji);
        
        // Update selected state in grid
        const picker = document.getElementById(this.pickerContainerId);
        if (picker) {
            picker.querySelectorAll('.icon-picker-item').forEach(item => {
                if (item.dataset.icon === emoji) {
                    item.style.borderColor = '#3b82f6';
                    item.style.background = '#eff6ff';
                    item.style.transform = 'scale(1.1)';
                } else {
                    item.style.borderColor = 'transparent';
                    item.style.background = 'white';
                    item.style.transform = 'scale(1)';
                }
            });
        }
        
        // Close the picker after selection
        this.hidePicker();
    }
    
    selectColor(color) {
        console.log('selectColor called with:', color);
        console.log('this.inputId:', this.inputId);
        console.log('this.input:', this.input);
        console.log('this.onColorChange:', this.onColorChange);
        
        // Call the callback if provided
        if (this.onColorChange) {
            console.log('Calling onColorChange callback');
            this.onColorChange(color);
        }
        
        // Also update input if colorInputId is provided (for backwards compatibility)
        if (this.colorInputId) {
            const colorInput = document.getElementById(this.colorInputId);
            console.log('colorInputId element:', colorInput);
            if (colorInput) {
                colorInput.value = color || '';
                console.log('Color input via colorInputId set to:', colorInput.value);
                colorInput.dispatchEvent(new Event('change'));
            }
        }
        
        // Update the input field for color-only mode
        if (this.mode === 'color-only' && this.input) {
            this.input.value = color || '';
            console.log('Color input via this.input set to:', this.input.value);
            this.input.dispatchEvent(new Event('change'));
        }
        
        // Close the picker after selection
        this.hidePicker();
    }
    
    updateDisplay(emoji) {
        const display = document.getElementById(this.displayId);
        if (display) {
            display.textContent = emoji;
        }
    }
}

// Initialize icon pickers when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Auto-initialize any elements with data-icon-picker attribute
    document.querySelectorAll('[data-icon-picker]').forEach(input => {
        new IconPicker(input.id);
    });
});
