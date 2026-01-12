// Create Session Page - Tab Switching
window.createPageTabs = {
    init: function() {
        // Set basic tab as active on load
        this.switchTab('basic');
    },
    
    switchTab: function(tabName) {
        // Hide all content
        const basicContent = document.getElementById('basicContent');
        const aiContent = document.getElementById('aiContent');
        
        if (basicContent) basicContent.style.display = 'none';
        if (aiContent) aiContent.style.display = 'none';
        
        // Remove active class from all tabs
        const basicTab = document.getElementById('basicTab');
        const aiTab = document.getElementById('aiTab');
        
        if (basicTab) basicTab.classList.remove('active');
        if (aiTab) aiTab.classList.remove('active');
        
        // Show selected content and activate tab
        if (tabName === 'basic') {
            if (basicContent) basicContent.style.display = 'block';
            if (basicTab) basicTab.classList.add('active');
        } else {
            if (aiContent) aiContent.style.display = 'block';
            if (aiTab) aiTab.classList.add('active');
        }
    }
};

// Auto-initialize on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => window.createPageTabs.init());
} else {
    window.createPageTabs.init();
}

// Also initialize after a short delay for Blazor pre-rendering
setTimeout(() => window.createPageTabs.init(), 100);

// Global function for onclick handlers
function switchTab(tabName) {
    window.createPageTabs.switchTab(tabName);
}
