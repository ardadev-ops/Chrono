// ── CSV Download ──────────────────────────────────────────
window.downloadCsv = function (content, filename) {
    const BOM = '﻿';
    const blob = new Blob([BOM + content], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.showModal = (id) => {
    document.getElementById(id).style.display = 'flex';
}
window.hideModal = (id) => {
    document.getElementById(id).style.display = 'none';
}

// ── CHRONO Clock ──────────────────────────────────────────
function startClock() {
    updateClock();
    setInterval(updateClock, 1000);
}

function updateClock() {
    const now = new Date();

    const timeEl = document.getElementById('digital-time');
    if (timeEl) {
        timeEl.textContent = now.toLocaleTimeString('de-AT', {
            hour: '2-digit', minute: '2-digit', second: '2-digit'
        });
    }

    const s = now.getSeconds();
    const m = now.getMinutes() + s / 60;
    const h = (now.getHours() % 12) + m / 60;

    setHandAngle('second-hand', s * 6, 22, 108);
    setHandAngle('minute-hand', m * 6, 28, 100);
    setHandAngle('hour-hand',   h * 30, 45, 100);
}

function setHandAngle(id, angle, tipY, tailY) {
    const el = document.getElementById(id);
    if (!el) return;
    const rad = angle * Math.PI / 180;
    const cx = 100, cy = 100;
    const len2 = cy - tipY;
    const len1 = tailY - cy;
    el.setAttribute('x1', (cx - len1 * Math.sin(rad)).toFixed(1));
    el.setAttribute('y1', (cy + len1 * Math.cos(rad)).toFixed(1));
    el.setAttribute('x2', (cx + len2 * Math.sin(rad)).toFixed(1));
    el.setAttribute('y2', (cy - len2 * Math.cos(rad)).toFixed(1));
}
