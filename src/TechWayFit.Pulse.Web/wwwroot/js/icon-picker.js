// Icon Picker Component for Session Groups
// Provides Font Awesome icon selection with visual picker and color selector

class IconPicker {
    constructor(inputId, options = {}) {
        this.inputId = inputId;
        this.input = document.getElementById(inputId);
        this.displayId = options.displayId || `${inputId}-display`;
        this.triggerButtonId = options.triggerButtonId || null; // Separate trigger button
        this.colorInputId = options.colorInputId || null;
        this.pickerContainerId = options.pickerContainerId || `${inputId}-picker`;
        this.defaultIcon = options.defaultIcon || 'folder';
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
        
        // Icon list combining Font Awesome and colorful Noto Emoji SVGs - organized by category
        this.icons = [
            // === Colorful Noto Emoji SVGs ===
            // Emotions & Expressions
            { name: 'grinning', svg: '/images/icons/grinning.svg' },
            { name: 'grinning-big', svg: '/images/icons/grinning-big.svg' },
            { name: 'smile', svg: '/images/icons/smile.svg' },
            { name: 'smiling', svg: '/images/icons/smiling.svg' },
            { name: 'thinking', svg: '/images/icons/thinking.svg' },
            { name: 'sad', svg: '/images/icons/sad.svg' },
            { name: 'confused', svg: '/images/icons/confused.svg' },
            { name: 'heart', svg: '/images/icons/heart.svg' },
            
            // Gestures & Hands
            { name: 'thumbs-up', svg: '/images/icons/thumbs-up.svg' },
            { name: 'thumbs-down', svg: '/images/icons/thumbs-down.svg' },
            { name: 'ok-hand', svg: '/images/icons/ok-hand.svg' },
            { name: 'waving-hand', svg: '/images/icons/waving-hand.svg' },
            { name: 'raised-hand', svg: '/images/icons/raised-hand.svg' },
            { name: 'handshake', svg: '/images/icons/handshake.svg' },
            
            // People & Groups
            { name: 'bust', svg: '/images/icons/bust.svg' },
            { name: 'busts', svg: '/images/icons/busts.svg' },
            { name: 'people', svg: '/images/icons/people.svg' },
            { name: 'family', svg: '/images/icons/family.svg' },
            
            // Achievement & Success
            { name: 'trophy', svg: '/images/icons/trophy.svg' },
            { name: 'medal', svg: '/images/icons/medal.svg' },
            { name: 'medal-sports', svg: '/images/icons/medal-sports.svg' },
            { name: 'star', svg: '/images/icons/star.svg' },
            { name: 'party', svg: '/images/icons/party.svg' },
            { name: 'confetti', svg: '/images/icons/confetti.svg' },
            { name: 'hundred', svg: '/images/icons/hundred.svg' },
            
            // Ideas & Innovation
            { name: 'lightbulb', svg: '/images/icons/lightbulb.svg' },
            { name: 'fire', svg: '/images/icons/fire.svg' },
            { name: 'rocket', svg: '/images/icons/rocket.svg' },
            { name: 'robot', svg: '/images/icons/robot.svg' },
            { name: 'target', svg: '/images/icons/target.svg' },
            { name: 'lightning', svg: '/images/icons/lightning.svg' },
            { name: 'bolt', svg: '/images/icons/bolt.svg' },
            { name: 'bomb', svg: '/images/icons/bomb.svg' },
            { name: 'collision', svg: '/images/icons/collision.svg' },
            
            // Learning & Education
            { name: 'books', svg: '/images/icons/books.svg' },
            { name: 'book-open', svg: '/images/icons/book-open.svg' },
            { name: 'pen', svg: '/images/icons/pen.svg' },
            { name: 'pencil', svg: '/images/icons/pencil.svg' },
            
            // Charts & Data
            { name: 'chart', svg: '/images/icons/chart.svg' },
            { name: 'chart-increasing', svg: '/images/icons/chart-increasing.svg' },
            { name: 'chart-decreasing', svg: '/images/icons/chart-decreasing.svg' },
            { name: 'bar-chart', svg: '/images/icons/bar-chart.svg' },
            
            // Documents & Files
            { name: 'clipboard', svg: '/images/icons/clipboard.svg' },
            { name: 'memo', svg: '/images/icons/memo.svg' },
            { name: 'document', svg: '/images/icons/document.svg' },
            { name: 'bookmark-tabs', svg: '/images/icons/bookmark-tabs.svg' },
            { name: 'folder', svg: '/images/icons/folder.svg' },
            { name: 'folder-open', svg: '/images/icons/folder-open.svg' },
            
            // Time & Calendar
            { name: 'calendar', svg: '/images/icons/calendar.svg' },
            { name: 'calendar-tear-off', svg: '/images/icons/calendar-tear-off.svg' },
            { name: 'alarm', svg: '/images/icons/alarm.svg' },
            { name: 'stopwatch', svg: '/images/icons/stopwatch.svg' },
            { name: 'hourglass', svg: '/images/icons/hourglass.svg' },
            
            // Communication
            { name: 'speech-balloon', svg: '/images/icons/speech-balloon.svg' },
            { name: 'thought-balloon', svg: '/images/icons/thought-balloon.svg' },
            { name: 'chat', svg: '/images/icons/chat.svg' },
            
            // Technology & Objects
            { name: 'laptop', svg: '/images/icons/laptop.svg' },
            { name: 'mobile', svg: '/images/icons/mobile.svg' },
            { name: 'email', svg: '/images/icons/email.svg' },
            { name: 'trash', svg: '/images/icons/trash.svg' },
            { name: 'save', svg: '/images/icons/save.svg' },
            { name: 'wrench', svg: '/images/icons/wrench.svg' },
            { name: 'settings', svg: '/images/icons/settings.svg' },
            
            // Security
            { name: 'locked', svg: '/images/icons/locked.svg' },
            { name: 'unlocked', svg: '/images/icons/unlocked.svg' },
            { name: 'key', svg: '/images/icons/key.svg' },
            
            // Location & Places
            { name: 'pin', svg: '/images/icons/pin.svg' },
            { name: 'pushpin', svg: '/images/icons/pushpin.svg' },
            { name: 'location', svg: '/images/icons/location.svg' },
            { name: 'globe', svg: '/images/icons/globe.svg' },
            { name: 'house', svg: '/images/icons/house.svg' },
            { name: 'office', svg: '/images/icons/office.svg' },
            { name: 'school', svg: '/images/icons/school.svg' },
            
            // Symbols & Indicators
            { name: 'check-mark', svg: '/images/icons/check-mark.svg' },
            { name: 'cross-mark', svg: '/images/icons/cross-mark.svg' },
            { name: 'plus', svg: '/images/icons/plus.svg' },
            { name: 'minus', svg: '/images/icons/minus.svg' },
            { name: 'warning', svg: '/images/icons/warning.svg' },
            { name: 'info', svg: '/images/icons/info.svg' },
            { name: 'question', svg: '/images/icons/question.svg' },
            { name: 'exclamation', svg: '/images/icons/exclamation.svg' },
            
            // Arrows
            { name: 'right-arrow', svg: '/images/icons/right-arrow.svg' },
            { name: 'left-arrow', svg: '/images/icons/left-arrow.svg' },
            { name: 'up-arrow', svg: '/images/icons/up-arrow.svg' },
            { name: 'down-arrow', svg: '/images/icons/down-arrow.svg' },
            
            // Misc
            { name: 'search', svg: '/images/icons/search.svg' },
            { name: 'zzz', svg: '/images/icons/zzz.svg' },
            
            // === Font Awesome Icons (for backwards compatibility) ===
            { name: 'fa-book', class: 'fas fa-book' },
            { name: 'fa-clipboard-list', class: 'fas fa-clipboard-list' },
            { name: 'fa-chart-bar', class: 'fas fa-chart-bar' },
            { name: 'fa-chart-line', class: 'fas fa-chart-line' },
            { name: 'fa-chart-pie', class: 'fas fa-chart-pie' },
            { name: 'fa-graduation-cap', class: 'fas fa-graduation-cap' },
            { name: 'fa-bullseye', class: 'fas fa-bullseye' },
            { name: 'fa-award', class: 'fas fa-award' },
            { name: 'fa-sparkles', class: 'fas fa-sparkles' },
            { name: 'fa-gem', class: 'fas fa-gem' },
            { name: 'fa-crown', class: 'fas fa-crown' },
            { name: 'fa-wand-magic-sparkles', class: 'fas fa-wand-magic-sparkles' },
            { name: 'fa-palette', class: 'fas fa-palette' },
            { name: 'fa-masks-theater', class: 'fas fa-masks-theater' },
            { name: 'fa-film', class: 'fas fa-film' },
            { name: 'fa-thumbtack', class: 'fas fa-thumbtack' },
            { name: 'fa-location-dot', class: 'fas fa-location-dot' },
            { name: 'fa-paperclip', class: 'fas fa-paperclip' },
            { name: 'fa-bookmark', class: 'fas fa-bookmark' },
            { name: 'fa-tag', class: 'fas fa-tag' },
            { name: 'fa-calendar-days', class: 'fas fa-calendar-days' },
            { name: 'fa-comment', class: 'fas fa-comment' },
            { name: 'fa-comments', class: 'fas fa-comments' },
            { name: 'fa-users', class: 'fas fa-users' },
            { name: 'fa-briefcase', class: 'fas fa-briefcase' },
            { name: 'fa-microphone', class: 'fas fa-microphone' },
            { name: 'fa-bullhorn', class: 'fas fa-bullhorn' },
            { name: 'fa-bell', class: 'fas fa-bell' },
            { name: 'fa-check', class: 'fas fa-check' },
            { name: 'fa-circle-check', class: 'fas fa-circle-check' }
        ];
        
        this.init();
    }
    
    init() {
        console.log('IconPicker init:', this.displayId, this.inputId);
        const display = document.getElementById(this.displayId);
        console.log('Display element found:', display);
        
        // If input has a value in ics-* format, extract the icon name
        if (this.input && this.input.value && this.input.value.startsWith('ics-')) {
            this.selectedIcon = this.input.value.replace('ics-', '');
        }
        
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
        
        // Create grid of icons (SVG + Font Awesome)
        const iconGrid = document.createElement('div');
        iconGrid.style.cssText = 'display: grid; grid-template-columns: repeat(12, 1fr); gap: 0.25rem; max-height: 240px; overflow-y: auto; padding: 0.5rem; background: #f8fafc; border-radius: 0.5rem; margin-bottom: ' + (this.mode === 'icon-only' ? '0' : '1rem') + '; border: 1px solid #e2e8f0;';
        
        this.icons.forEach(icon => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'icon-picker-item';
            
            // Render SVG or Font Awesome icon
            if (icon.svg) {
                btn.innerHTML = `<img src="${icon.svg}" alt="${icon.name}" style="width: 20px; height: 20px; object-fit: contain;" />`;
            } else if (icon.class) {
                btn.innerHTML = `<i class="${icon.class}"></i>`;
            }
            
            btn.dataset.icon = icon.name;
            btn.style.cssText = 'width: 28px; height: 28px; border: 2px solid transparent; border-radius: 0.375rem; background: white; cursor: pointer; font-size: 1.1rem; display: flex; align-items: center; justify-content: center; transition: all 0.15s ease;';
            
            // Highlight if this is the selected icon
            if (icon.name === this.selectedIcon) {
                btn.style.borderColor = '#3b82f6';
                btn.style.background = '#eff6ff';
                btn.style.transform = 'scale(1.1)';
            }
            
            btn.addEventListener('mouseenter', () => {
                if (icon.name !== this.selectedIcon) {
                    btn.style.background = '#f1f5f9';
                    btn.style.borderColor = '#cbd5e0';
                    btn.style.transform = 'scale(1.1)';
                }
            });
            
            btn.addEventListener('mouseleave', () => {
                if (icon.name !== this.selectedIcon) {
                    btn.style.background = 'white';
                    btn.style.borderColor = 'transparent';
                    btn.style.transform = 'scale(1)';
                }
            });
            
            btn.addEventListener('click', () => {
                this.selectIcon(icon.name);
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
    
    selectIcon(iconName) {
        console.log('selectIcon called with:', iconName);
        console.log('this.inputId:', this.inputId);
        console.log('this.input:', this.input);
        
        this.selectedIcon = iconName;
        
        if (this.input) {
            // Store as CSS class format (ics-iconname)
            const cssClass = `ics-${iconName}`;
            this.input.value = cssClass;
            console.log('Icon input value set to:', this.input.value);
            // Trigger change event
            this.input.dispatchEvent(new Event('change'));
        } else {
            console.error('Icon input element not found!');
        }
        
        this.updateDisplay(iconName);
        
        // Update selected state in grid
        const picker = document.getElementById(this.pickerContainerId);
        if (picker) {
            picker.querySelectorAll('.icon-picker-item').forEach(item => {
                if (item.dataset.icon === iconName) {
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
    
    updateDisplay(iconName) {
        const display = document.getElementById(this.displayId);
        if (display) {
            // Find the icon from the icons array
            const icon = this.icons.find(i => i.name === iconName);
            if (icon) {
                if (icon.svg) {
                    // Render SVG icon
                    display.innerHTML = `<img src="${icon.svg}" alt="${icon.name}" style="width: 32px; height: 32px; object-fit: contain;" />`;
                } else if (icon.class) {
                    // Render Font Awesome icon
                    display.innerHTML = `<i class="${icon.class}"></i>`;
                }
            } else {
                // Fallback to default folder icon if not found
                display.innerHTML = '<i class="fas fa-folder"></i>';
            }
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
