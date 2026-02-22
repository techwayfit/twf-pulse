/* =============================================
   PULSE BACKOFFICE — SHARED JS UTILITIES
   ============================================= */

// Auto-highlight active nav + sidebar based on current page filename
(function() {
  const page = window.location.pathname.split('/').pop() || 'dashboard.html';
  const map = {
    'dashboard.html':       'dashboard',
    'users.html':           'users',
    'user-detail.html':     'users',
    'sessions.html':        'sessions',
    'session-detail.html':  'sessions',
    'activity-detail.html': 'sessions',
    'audit.html':           'audit',
    'operators.html':       'operators',
    'report.html':          'sessions',
  };
  const active = map[page] || 'dashboard';
  const navEl = document.getElementById('nav-' + active);
  if (navEl) navEl.classList.add('active');
  const sbEl = document.getElementById('sb-' + active);
  if (sbEl) sbEl.classList.add('active');
})();

// Toast
let _toastTimer;
function showToast(msg, type = 'success') {
  const t = document.getElementById('toast');
  if (!t) return;
  t.textContent = (type === 'success' ? '✓  ' : '✕  ') + msg;
  t.className = 'toast show ' + type;
  clearTimeout(_toastTimer);
  _toastTimer = setTimeout(() => t.classList.remove('show'), 3000);
}

// Modal open/close
function openModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.add('open');
}
function closeModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.remove('open');
}
// Click-outside closes modal
document.addEventListener('click', function(e) {
  document.querySelectorAll('.modal-overlay.open').forEach(function(overlay) {
    if (e.target === overlay) overlay.classList.remove('open');
  });
});
