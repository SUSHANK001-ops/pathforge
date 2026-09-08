// PathForge Constellation Interactive Animation
document.addEventListener('DOMContentLoaded', () => {
    const canvas = document.querySelector('.pf-constellation-canvas');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    let width = canvas.width = canvas.parentElement.clientWidth;
    let height = canvas.height = canvas.parentElement.clientHeight;

    const stars = [];
    const starCount = Math.floor((width * height) / 10000);

    for (let i = 0; i < starCount; i++) {
        stars.push({
            x: Math.random() * width,
            y: Math.random() * height,
            radius: Math.random() * 1.5 + 0.5,
            vx: (Math.random() - 0.5) * 0.3,
            vy: (Math.random() - 0.5) * 0.3
        });
    }

    function draw() {
        ctx.clearRect(0, 0, width, height);

        // Draw connections
        ctx.strokeStyle = 'rgba(216, 0, 15, 0.15)';
        ctx.lineWidth = 1;
        for (let i = 0; i < stars.length; i++) {
            for (let j = i + 1; j < stars.length; j++) {
                const dx = stars[i].x - stars[j].x;
                const dy = stars[i].y - stars[j].y;
                const dist = Math.sqrt(dx * dx + dy * dy);
                if (dist < 100) {
                    ctx.beginPath();
                    ctx.moveTo(stars[i].x, stars[i].y);
                    ctx.lineTo(stars[j].x, stars[j].y);
                    ctx.stroke();
                }
            }
        }

        // Draw stars
        ctx.fillStyle = '#F5F2EF';
        for (let i = 0; i < stars.length; i++) {
            const s = stars[i];
            ctx.beginPath();
            ctx.arc(s.x, s.y, s.radius, 0, Math.PI * 2);
            ctx.fill();

            s.x += s.vx;
            s.y += s.vy;

            if (s.x < 0 || s.x > width) s.vx = -s.vx;
            if (s.y < 0 || s.y > height) s.vy = -s.vy;
        }

        requestAnimationFrame(draw);
    }

    window.addEventListener('resize', () => {
        width = canvas.width = canvas.parentElement.clientWidth;
        height = canvas.height = canvas.parentElement.clientHeight;
    });

    draw();
});
