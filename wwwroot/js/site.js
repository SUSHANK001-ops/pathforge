/* PathForge — site.js
   Client-side form validation and general UI behaviour */

document.addEventListener('DOMContentLoaded', function () {

  // ── 1. Real-time field validation ──────────────────────────
  // On every pf-input blur, check it is not empty
  document.querySelectorAll('.pf-input[required]').forEach(function (input) {
    input.addEventListener('blur', function () {
      validateField(input);
    });
    input.addEventListener('input', function () {
      clearError(input);
    });
  });

  // ── 2. Pre-submit validation ────────────────────────────────
  document.querySelectorAll('form').forEach(function (form) {
    form.addEventListener('submit', function (e) {
      let valid = true;

      form.querySelectorAll('.pf-input[required]').forEach(function (input) {
        if (!validateField(input)) valid = false;
      });

      // Password match check
      const pw  = form.querySelector('input[name="Password"]');
      const cpw = form.querySelector('input[name="ConfirmPassword"]');
      if (pw && cpw && pw.value !== cpw.value) {
        showError(cpw, 'Passwords do not match');
        valid = false;
      }

      // Email format
      const email = form.querySelector('input[type="email"]');
      if (email && email.value && !isValidEmail(email.value)) {
        showError(email, 'Enter a valid email address');
        valid = false;
      }

      if (!valid) e.preventDefault();
    });
  });

  // ── 3. Reading progress bar ─────────────────────────────────
  const strip = document.getElementById('readProgress');
  if (strip) {
    window.addEventListener('scroll', function () {
      const scrolled  = window.scrollY;
      const docHeight = document.documentElement.scrollHeight - window.innerHeight;
      const pct       = docHeight > 0 ? (scrolled / docHeight) * 100 : 0;
      strip.style.width = pct + '%';
    });
  }

  // ── Helpers ─────────────────────────────────────────────────
  function validateField(input) {
    if (!input.value.trim()) {
      const label = input.previousElementSibling
                    ? input.previousElementSibling.textContent.trim()
                    : 'This field';
      showError(input, label + ' is required');
      return false;
    }
    clearError(input);
    return true;
  }

  function showError(input, message) {
    clearError(input);
    input.style.borderColor = 'var(--red)';
    const err = document.createElement('span');
    err.className = 'pf-field-error js-error';
    err.textContent = message;
    input.insertAdjacentElement('afterend', err);
  }

  function clearError(input) {
    input.style.borderColor = '';
    const next = input.nextElementSibling;
    if (next && next.classList.contains('js-error')) next.remove();
  }

  function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

});
