// ============ Auth helpers (UI + password policy) ============
// Expose a tiny API on window.Auth so views can call these helpers.

(function () {
    const banned = [
        'password', 'passw0rd', '123456', '123456789', '12345678', '111111',
        'qwerty', 'abc123', 'letmein', 'iloveyou', '000000', '123123', '654321'
    ];

    // Score password 0..4
    function score(pw) {
        if (!pw) return 0;
        let s = 0;
        if (pw.length >= 8) s++;
        if (/[a-z]/.test(pw) && /[A-Z]/.test(pw)) s++;
        if (/\d/.test(pw)) s++;
        if (/[^A-Za-z0-9]/.test(pw)) s++;
        // penalize common/repeated
        if (banned.includes(pw.toLowerCase())) s = Math.min(s, 1);
        if (/(.)\1{2,}/.test(pw)) s = Math.min(s, 2);
        return s; // 0..4
    }

    // Validate and return issues
    function validate(email, pw) {
        const issues = [];
        if (!pw || pw.length < 8) issues.push('Use at least 8 characters.');
        if (!/[a-z]/.test(pw)) issues.push('Add a lowercase letter.');
        if (!/[A-Z]/.test(pw)) issues.push('Add an uppercase letter.');
        if (!/\d/.test(pw)) issues.push('Add a number.');
        if (!/[^A-Za-z0-9]/.test(pw)) issues.push('Add a symbol (e.g., !@#$).');
        if (banned.includes(pw?.toLowerCase?.())) issues.push('Avoid common passwords (e.g., 123456, password).');
        if (/(.)\1{2,}/.test(pw)) issues.push('Avoid repeating the same character 3+ times.');
        if (email) {
            const local = String(email).split('@')[0] || '';
            if (local && pw && pw.toLowerCase().includes(local.toLowerCase()))
                issues.push('Don’t include your email/username in the password.');
        }
        return { ok: issues.length === 0, issues, score: score(pw) };
    }

    // Live meter hookup
    function attachStrengthMeter(emailInputId, pwInputId, meterBarId) {
        const emailEl = document.getElementById(emailInputId);
        const pwEl = document.getElementById(pwInputId);
        const bar = document.getElementById(meterBarId);
        if (!pwEl || !bar) return;

        function render() {
            const v = pwEl.value || '';
            const res = validate(emailEl?.value || '', v);
            const pct = [0, 25, 50, 75, 100][res.score] || 0;
            bar.style.width = pct + '%';
            bar.classList.remove('pw-weak', 'pw-fair', 'pw-good', 'pw-strong');
            bar.classList.add(
                res.score <= 1 ? 'pw-weak' :
                    res.score === 2 ? 'pw-fair' :
                        res.score === 3 ? 'pw-good' : 'pw-strong'
            );
        }
        pwEl.addEventListener('input', render);
        emailEl?.addEventListener('input', render);
        render();
    }

    // Simple show/hide toggle
    function wireShowHide(toggleBtnId, inputId) {
        const btn = document.getElementById(toggleBtnId);
        const input = document.getElementById(inputId);
        if (!btn || !input) return;
        btn.addEventListener('click', () => {
            const isPwd = input.type === 'password';
            input.type = isPwd ? 'text' : 'password';
            btn.textContent = isPwd ? 'Hide' : 'Show';
        });
    }

    // Error helpers
    function showError(containerId, msg) {
        const el = document.getElementById(containerId);
        if (!el) return;
        el.textContent = msg || 'Something went wrong.';
        el.style.display = 'block';
        // auto hide after 6s
        window.clearTimeout(el.__t);
        el.__t = setTimeout(() => { el.style.display = 'none'; }, 6000);
    }

    function clearError(containerId) {
        const el = document.getElementById(containerId);
        if (!el) return;
        el.textContent = '';
        el.style.display = 'none';
    }

    // expose
    window.Auth = {
        validatePassword: (email, pw) => validate(email, pw),
        attachStrengthMeter,
        wireShowHide,
        showError,
        clearError
    };
})();
