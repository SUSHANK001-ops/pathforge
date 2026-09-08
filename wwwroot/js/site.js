// PathForge Client Script
document.addEventListener('DOMContentLoaded', () => {
    // Reading progress strip update
    const progressBar = document.getElementById('readProgress');
    if (progressBar) {
        window.addEventListener('scroll', () => {
            const winScroll = document.documentElement.scrollTop || document.body.scrollTop;
            const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
            const scrolled = height > 0 ? (winScroll / height) * 100 : 0;
            progressBar.style.width = scrolled + '%';
        });
    }

    // Auto-dismiss alerts after timeout if desired
    const alerts = document.querySelectorAll('.pf-alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.5s ease';
            alert.style.opacity = '0.9';
        }, 4000);
    });
});
