/**
 * Copy Session Functionality
 * 
 * Provides modal-based session copying functionality across the application.
 * 
 * Requirements:
 * - Bootstrap 5 modals
 * - Copy session API endpoint: POST /api/sessions/{code}/copy
 * - HTML buttons with class 'copy-session-btn' and data attributes:
 *   - data-session-code: The session code to copy
 *   - data-session-title: The session title for display
 * 
 * Usage:
 * 1. Include this script in your page
 * 2. Include the _CopySessionModals.cshtml partial view
 * 3. Call initializeCopyButtons() after DOM is ready
 * 
 * Example:
 * <button class="copy-session-btn" 
 *    data-session-code="ABC123" 
 *    data-session-title="My Session">
 *     Copy
 * </button>
 */

(function(window) {
    'use strict';
    
  // Store pending copy operation
    let pendingCopySession = null;
    
    /**
     * Initialize copy button event handlers
     */
    function initializeCopyButtons() {
        const copyButtons = document.querySelectorAll('.copy-session-btn');
        console.log('Found copy buttons:', copyButtons.length);
    
        if (copyButtons.length === 0) {
            console.warn('No copy buttons found. Make sure buttons have class "copy-session-btn"');
          return;
 }
 
        // Attach click handlers to all copy buttons
        copyButtons.forEach(btn => {
  btn.addEventListener('click', handleCopyButtonClick);
    });
     
      // Attach confirmation button handler (only once)
        const confirmBtn = document.getElementById('confirmCopyBtn');
   if (confirmBtn) {
 // Remove existing listener if any
confirmBtn.replaceWith(confirmBtn.cloneNode(true));
    document.getElementById('confirmCopyBtn').addEventListener('click', handleConfirmCopy);
    }
     
   // Attach "Stay Here" button handler to close modal and reload page
        const stayBtn = document.getElementById('stayHereBtn');
        if (stayBtn) {
 stayBtn.replaceWith(stayBtn.cloneNode(true));
         document.getElementById('stayHereBtn').addEventListener('click', function() {
             console.log('Stay Here clicked - closing modal and reloading page');
             const successModal = bootstrap.Modal.getInstance(document.getElementById('copySuccessModal'));
           if (successModal) {
    successModal.hide();
     }
             // Reload the page to show the newly copied session
 window.location.reload();
 });
 }
    }
    
    /**
   * Handle copy button click - show confirmation modal
  */
    function handleCopyButtonClick(e) {
e.preventDefault();
        e.stopPropagation();
   
        const sessionCode = this.getAttribute('data-session-code');
        const sessionTitle = this.getAttribute('data-session-title');
        
 console.log('Copy button clicked:', { sessionCode, sessionTitle });
   
  if (!sessionCode || !sessionTitle) {
            console.error('Missing required data attributes on copy button:', this);
        showCopyError('Invalid button configuration. Missing session code or title.');
       return;
        }
    
   // Store session data for confirmation
        pendingCopySession = { code: sessionCode, title: sessionTitle };
        
      // Show confirmation modal
        document.getElementById('copySessionTitleDisplay').textContent = sessionTitle;
      const confirmModal = new bootstrap.Modal(document.getElementById('copySessionModal'));
        confirmModal.show();
    }
    
    /**
     * Handle confirm copy - call API and show result
     */
    async function handleConfirmCopy() {
    if (!pendingCopySession) {
          console.error('No pending copy session');
   return;
  }
      
   const btn = this;
        const sessionCode = pendingCopySession.code;
        const sessionTitle = pendingCopySession.title;

   // Hide confirmation modal
      bootstrap.Modal.getInstance(document.getElementById('copySessionModal')).hide();
        
  // Show loading state
        const originalHtml = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Copying...';
      
try {
console.log('Calling API to copy session:', sessionCode);
     
      // Call copy API
    const response = await fetch(`/api/sessions/${sessionCode}/copy`, {
 method: 'POST',
 credentials: 'include', // Include cookies for authentication
    headers: {
    'Content-Type': 'application/json'
    }
   });
      
   console.log('API response status:', response.status);
   console.log('API response headers:', Object.fromEntries(response.headers.entries()));

  if (response.ok) {
           const result = await response.json();
     console.log('API response data:', result);
       
     // Extract new session code
    const newCode = result.data?.code || result.code;
            if (!newCode) {
    console.error('No session code in response:', result);
   showCopyError('No session code returned from server');
  return;
  }
   
            console.log('Copy successful, new code:', newCode);
       
       // Show success modal
  showCopySuccess(newCode);
 } else {
 // Handle error response
     const contentType = response.headers.get('content-type');
let errorMessage = 'Unknown error';
       
     if (contentType && contentType.includes('application/json')) {
         try {
           const error = await response.json();
  console.error('Copy failed (JSON):', error);
  errorMessage = error.errors?.[0]?.message || error.message || JSON.stringify(error);
         } catch (parseErr) {
          console.error('Failed to parse JSON error:', parseErr);
   errorMessage = `HTTP ${response.status}: Failed to parse error response`;
}
     } else {
           const errorText = await response.text();
  console.error('Copy failed (text):', errorText);
     errorMessage = errorText || `HTTP ${response.status}`;
         }
     
    showCopyError(errorMessage);
    }
   } catch (error) {
        console.error('Exception during copy:', error);
            showCopyError(error.message || 'Network error occurred');
   } finally {
      // Restore button state
btn.disabled = false;
     btn.innerHTML = originalHtml;
 pendingCopySession = null;
     }
    }
    
    /**
     * Show success modal with new session code
   */
    function showCopySuccess(newCode) {
        document.getElementById('newSessionCodeDisplay').textContent = newCode;
      const successModal = new bootstrap.Modal(document.getElementById('copySuccessModal'));
    successModal.show();
        
   // Setup "Edit New Session" button
   document.getElementById('goToEditBtn').onclick = function() {
   window.location.href = `/facilitator/edit-session/${newCode}`;
};
    }
    
    /**
     * Show error modal with error message
     */
  function showCopyError(message) {
      document.getElementById('copyErrorMessage').textContent = message;
 const errorModal = new bootstrap.Modal(document.getElementById('copyErrorModal'));
   errorModal.show();
    }
    
    // Export to global scope
    window.CopySession = {
        initialize: initializeCopyButtons,
        showError: showCopyError,
        showSuccess: showCopySuccess
    };
    
    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeCopyButtons);
    } else {
        // DOM already loaded, initialize immediately
        initializeCopyButtons();
    }
    
})(window);
