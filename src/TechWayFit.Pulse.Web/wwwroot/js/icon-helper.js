// Icon Helper - Renders CSS-based SVG icons
// Supports icon picker values (ics-rocket) and legacy SVG filenames (rocket.svg)

const IconHelper = {
    /**
     * Converts an icon value to HTML
     * @param {string} iconValue - The icon value (CSS class like 'ics-rocket' or SVG filename like 'rocket.svg')
     * @param {string} size - Optional size class (ic-xs, ic-sm, ic-md, ic-lg, ic-xl, ic-2xl). Defaults to 'ic-md'
     * @returns {string} HTML string with icon element
     */
    toFontAwesome(iconValue, size = 'ic-md') {
        if (!iconValue) {
            return `<i class="ics ics-folder ${size}"></i>`; // Default icon
        }

        // Check if it's already a CSS icon class (e.g., "ics-rocket")
        if (iconValue.startsWith('ics-')) {
            return `<i class="ics ${iconValue} ${size}"></i>`;
        }

        // Check if it's an SVG filename (e.g., "rocket.svg") - for backwards compatibility
        if (iconValue.endsWith('.svg')) {
            const iconName = iconValue.replace('.svg', '');
            return `<i class="ics ics-${iconName} ${size}"></i>`;
        }

        // Default fallback
        return `<i class="ics ics-folder ${size}"></i>`;
    },

    /**
     * Render icon in an element by ID
     * @param {string} elementId - The ID of the element to update
     * @param {string} iconValue - The icon value (CSS class or SVG filename)
     */
    renderIcon(elementId, iconValue) {
        const element = document.getElementById(elementId);
        if (element) {
            element.innerHTML = this.toFontAwesome(iconValue);
        }
    },

    /**
     * Render icons in all elements with a specific class
     * @param {string} className - The class name of elements to update
     * @param {function} getIconValue - Function to get icon value from element
     */
    renderIconsByClass(className, getIconValue) {
        const elements = document.querySelectorAll(`.${className}`);
        elements.forEach(element => {
            const iconValue = getIconValue(element);
            if (iconValue) {
                element.innerHTML = this.toFontAwesome(iconValue);
            }
        });
    }
};

// Make it globally available
window.IconHelper = IconHelper;
