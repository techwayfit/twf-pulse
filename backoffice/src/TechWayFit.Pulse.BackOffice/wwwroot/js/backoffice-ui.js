/* =============================================
   PULSE BACKOFFICE — SHARED JS UTILITIES
   ============================================= */

// Toast
let _boToastTimer;
function boShowToast(msg, type) {
  type = type || 'success';
  const t = document.getElementById('bo-toast');
  if (!t) return;
  t.textContent = (type === 'success' ? '✓  ' : '✕  ') + msg;
  t.className = 'bo-toast show ' + type;
  clearTimeout(_boToastTimer);
  _boToastTimer = setTimeout(function() { t.classList.remove('show'); }, 3500);
}

// Modal open/close
function boOpenModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.add('open');
}
function boCloseModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.remove('open');
}

// Click-outside closes modal
document.addEventListener('click', function(e) {
  document.querySelectorAll('.bo-modal-overlay.open').forEach(function(overlay) {
    if (e.target === overlay) overlay.classList.remove('open');
  });
});

// Expand/collapse activity rows
function boToggleExpand(rowId, arrId) {
  const row = document.getElementById(rowId);
  const arr = document.getElementById(arrId);
  if (!row) return;
  const isOpen = row.classList.contains('open');
  document.querySelectorAll('.bo-expand-row.open').forEach(function(r) { r.classList.remove('open'); });
  document.querySelectorAll('[data-expand-label]').forEach(function(a) { a.textContent = '▸ expand'; });
  if (!isOpen) {
    row.classList.add('open');
    if (arr) arr.textContent = '▾ collapse';
  }
}

// Tab switching
function boSwitchTab(id, el) {
  document.querySelectorAll('.bo-tab-pane').forEach(function(p) { p.style.display = 'none'; });
  const pane = document.getElementById(id);
  if (pane) pane.style.display = 'block';
  document.querySelectorAll('.bo-tab').forEach(function(t) { t.classList.remove('active'); });
  if (el) el.classList.add('active');
}

// Inline double-confirm for destructive buttons
function boConfirmAction(msg) {
  return confirm(msg || 'Are you sure? This action is audited and cannot be undone.');
}

// Auto-show TempData toasts (called from layout)
function boInitToasts(successMsg, errorMsg) {
  if (successMsg) boShowToast(successMsg, 'success');
  if (errorMsg)   boShowToast(errorMsg, 'error');
}
