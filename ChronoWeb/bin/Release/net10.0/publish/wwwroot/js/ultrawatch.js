// CHRONO Ultra Watch – Zifferblatt fuer die Anmeldeseite
window.chronoUltraWatch = {
    animationId: null,

    start: function (canvasId) {
        const cv = document.getElementById(canvasId);
        if (!cv) return;

        const ctx = cv.getContext('2d');
        const W = 800, H = 880;
        const CX = 400, CY = 460;
        const R_BEZEL_OUT = 350, R_BEZEL_IN = 284, R_FACE = 280;

        // Hoehenangabe wechselt bei jedem Laden – Flughafenbezug
        const ALT = (Math.floor(Math.random() * 14 + 2) * 1000
                   + Math.floor(Math.random() * 9) * 100);
        const ALT_STR = ALT.toLocaleString('de-AT').replace(/\./g, ' ') + 'FT';

        let tick = 0;

        function rr(c, x, y, w, h, r) {
            c.beginPath();
            c.moveTo(x + r, y); c.lineTo(x + w - r, y);
            c.arcTo(x + w, y, x + w, y + r, r);
            c.lineTo(x + w, y + h - r);
            c.arcTo(x + w, y + h, x + w - r, y + h, r);
            c.lineTo(x + r, y + h);
            c.arcTo(x, y + h, x, y + h - r, r);
            c.lineTo(x, y + r); c.arcTo(x, y, x + r, y, r);
            c.closePath();
        }

        function drawRing(x, y, r, prog, col, track, w) {
            ctx.beginPath();
            ctx.arc(x, y, r, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2, false);
            ctx.strokeStyle = track; ctx.lineWidth = w; ctx.lineCap = 'round';
            ctx.stroke();
            ctx.beginPath();
            ctx.arc(x, y, r, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * prog, false);
            ctx.strokeStyle = col; ctx.lineWidth = w; ctx.lineCap = 'round';
            ctx.stroke();
        }

        function drawHand(ang, len, w, tail, col) {
            ctx.save();
            // dunkle Kontur
            ctx.beginPath();
            ctx.moveTo(CX - Math.cos(ang) * tail, CY - Math.sin(ang) * tail);
            ctx.lineTo(CX + Math.cos(ang) * len, CY + Math.sin(ang) * len);
            ctx.strokeStyle = 'rgba(0,0,0,0.8)';
            ctx.lineWidth = w + 4; ctx.lineCap = 'round';
            ctx.shadowColor = 'rgba(0,0,0,0.6)'; ctx.shadowBlur = 10;
            ctx.stroke();
            // heller Koerper
            ctx.shadowBlur = 0;
            ctx.beginPath();
            ctx.moveTo(CX - Math.cos(ang) * tail, CY - Math.sin(ang) * tail);
            ctx.lineTo(CX + Math.cos(ang) * len, CY + Math.sin(ang) * len);
            ctx.strokeStyle = col; ctx.lineWidth = w; ctx.lineCap = 'round';
            ctx.stroke();
            // blauer Leuchtstreifen
            ctx.beginPath();
            ctx.moveTo(CX + Math.cos(ang) * (len * 0.35), CY + Math.sin(ang) * (len * 0.35));
            ctx.lineTo(CX + Math.cos(ang) * (len - 15), CY + Math.sin(ang) * (len - 15));
            ctx.strokeStyle = 'rgba(30,136,255,0.7)';
            ctx.lineWidth = w * 0.4; ctx.lineCap = 'round';
            ctx.stroke();
            ctx.restore();
        }

        function curvedText(text, startAng, endAng, radius, color, font, reverse) {
            ctx.save();
            ctx.font = font + ',-apple-system,sans-serif';
            ctx.fillStyle = color;
            ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
            const total = endAng - startAng;
            for (let i = 0; i < text.length; i++) {
                const frac = text.length > 1 ? i / (text.length - 1) : 0.5;
                const ang = reverse ? endAng - frac * total : startAng + frac * total;
                ctx.save();
                ctx.translate(CX + Math.cos(ang) * radius, CY + Math.sin(ang) * radius);
                ctx.rotate(ang + (reverse ? -Math.PI / 2 : Math.PI / 2));
                ctx.fillText(text[i], 0, 0);
                ctx.restore();
            }
            ctx.restore();
        }

        function draw() {
            const now = new Date();
            const hr = now.getHours(), mn = now.getMinutes(),
                  sc = now.getSeconds(), ms = now.getMilliseconds();
            tick += 0.035;
            ctx.clearRect(0, 0, W, H);

            // ═══ ECKEN-KOMPLIKATIONEN ═══

            // oben links – Hoehe
            ctx.save();
            ctx.translate(28, 52);
            ctx.shadowColor = 'rgba(30,136,255,0.6)'; ctx.shadowBlur = 10;
            ctx.fillStyle = '#4DA3FF';
            ctx.font = '800 30px Inter,-apple-system,sans-serif';
            ctx.textAlign = 'left'; ctx.textBaseline = 'middle';
            ctx.fillText(ALT_STR, 0, 0);
            ctx.restore();

            // oben rechts – Wochentage und Datum
            const dayNow = now.getDay();
            const wd = ['MO', 'DI', 'MI', 'DO', 'FR'];
            let pills, todayIdx;
            if (dayNow >= 1 && dayNow <= 5) {
                const i = dayNow - 1;
                if (i === 0)      { pills = ['MO','DI','MI']; todayIdx = 0; }
                else if (i === 4) { pills = ['MI','DO','FR']; todayIdx = 2; }
                else              { pills = [wd[i-1], wd[i], wd[i+1]]; todayIdx = 1; }
            } else { pills = ['MO','DI','MI']; todayIdx = -1; }

            ctx.save();
            const pw = 52, ph = 34, pg = 5;
            const dateStr = String(now.getDate());
            ctx.font = '800 52px Inter,-apple-system,sans-serif';
            const dateW = ctx.measureText(dateStr).width;
            const pillsW = 3 * pw + 2 * pg;
            const startX = W - 28 - (pillsW + 16 + dateW);
            const py = 32;

            for (let i = 0; i < 3; i++) {
                const px = startX + i * (pw + pg);
                rr(ctx, px, py, pw, ph, 9);
                if (i === todayIdx) {
                    ctx.fillStyle = '#1E88FF'; ctx.fill();
                    ctx.save();
                    ctx.shadowColor = 'rgba(30,136,255,0.7)'; ctx.shadowBlur = 14;
                    rr(ctx, px, py, pw, ph, 9); ctx.fill();
                    ctx.restore();
                    ctx.fillStyle = '#fff';
                } else {
                    ctx.fillStyle = 'rgba(255,255,255,0.1)'; ctx.fill();
                    ctx.fillStyle = 'rgba(255,255,255,0.45)';
                }
                ctx.font = '700 16px Inter,-apple-system,sans-serif';
                ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
                ctx.fillText(pills[i], px + pw / 2, py + ph / 2 + 1);
            }

            ctx.fillStyle = '#F0F0F8';
            ctx.font = '800 52px Inter,-apple-system,sans-serif';
            ctx.textAlign = 'left'; ctx.textBaseline = 'middle';
            ctx.fillText(dateStr, startX + pillsW + 16, py + ph / 2);
            ctx.restore();

            // unten links – pulsierende Uhrzeit
            ctx.save();
            ctx.translate(28, H - 46);
            const glow = 0.7 + 0.3 * Math.sin(tick * 2);
            ctx.shadowColor = 'rgba(30,136,255,' + glow + ')';
            ctx.shadowBlur = 28 * glow;
            ctx.fillStyle = 'rgba(90,170,255,' + (0.88 + 0.12 * glow) + ')';
            ctx.font = '800 36px Inter,-apple-system,sans-serif';
            ctx.textAlign = 'left'; ctx.textBaseline = 'middle';
            ctx.fillText(String(hr).padStart(2,'0') + ':' + String(mn).padStart(2,'0'), 0, 0);
            ctx.restore();

            // unten rechts – Statushinweis
            ctx.save();
            ctx.translate(W - 28, H - 58);
            ctx.textAlign = 'right'; ctx.textBaseline = 'middle';
            ctx.shadowColor = 'rgba(30,136,255,0.5)'; ctx.shadowBlur = 8;
            ctx.fillStyle = '#F0F0F8';
            ctx.font = '800 22px Inter,-apple-system,sans-serif';
            ctx.fillText('READY FOR', 0, -13);
            ctx.fillStyle = '#1E88FF';
            ctx.fillText('TAKE OFF ✈', 0, 13);
            ctx.restore();

            // ═══ BLAUER LUENETTENRING ═══
            ctx.beginPath();
            ctx.arc(CX, CY, R_BEZEL_OUT, 0, Math.PI * 2);
            ctx.arc(CX, CY, R_BEZEL_IN, 0, Math.PI * 2, true);
            const ringGrad = ctx.createRadialGradient(CX, CY, R_BEZEL_IN, CX, CY, R_BEZEL_OUT);
            ringGrad.addColorStop(0, '#1560C0');
            ringGrad.addColorStop(0.5, '#1E88FF');
            ringGrad.addColorStop(1, '#1560C0');
            ctx.fillStyle = ringGrad;
            ctx.fill();

            // dunkle Teilstriche auf dem Ring
            for (let i = 0; i < 120; i++) {
                const deg = i * 3, ang = (deg / 360) * Math.PI * 2 - Math.PI / 2;
                const isM = deg % 30 === 0, isT = deg % 10 === 0, isF = deg % 5 === 0;
                const oR = R_BEZEL_OUT - 3;
                let iR, lw, col;
                if (isM)      { iR = R_BEZEL_OUT - 22; lw = 3.5; col = 'rgba(5,15,40,0.95)'; }
                else if (isT) { iR = R_BEZEL_OUT - 14; lw = 2.2; col = 'rgba(5,15,40,0.7)'; }
                else if (isF) { iR = R_BEZEL_OUT - 10; lw = 1.5; col = 'rgba(5,15,40,0.5)'; }
                else          { iR = R_BEZEL_OUT - 7;  lw = 1;   col = 'rgba(5,15,40,0.3)'; }
                ctx.beginPath();
                ctx.moveTo(CX + Math.cos(ang) * oR, CY + Math.sin(ang) * oR);
                ctx.lineTo(CX + Math.cos(ang) * iR, CY + Math.sin(ang) * iR);
                ctx.strokeStyle = col; ctx.lineWidth = lw; ctx.lineCap = 'round';
                ctx.stroke();
            }

            // Gradzahlen
            for (let d = 0; d < 360; d += 30) {
                const ang = (d / 360) * Math.PI * 2 - Math.PI / 2;
                const nR = (R_BEZEL_OUT + R_BEZEL_IN) / 2 + 6;
                ctx.save();
                ctx.translate(CX + Math.cos(ang) * nR, CY + Math.sin(ang) * nR);
                ctx.rotate(ang + Math.PI / 2);
                ctx.fillStyle = 'rgba(5,15,40,0.95)';
                ctx.font = '800 19px Inter,-apple-system,sans-serif';
                ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
                ctx.fillText(d === 0 ? 'N' : String(d), 0, 0);
                ctx.restore();
            }

            // Himmelsrichtungen
            [{t:'W',a:Math.PI},{t:'E',a:0},{t:'S',a:Math.PI/2}].forEach(function (c) {
                const cR = (R_BEZEL_OUT + R_BEZEL_IN) / 2 - 16;
                ctx.save();
                ctx.translate(CX + Math.cos(c.a) * cR, CY + Math.sin(c.a) * cR);
                ctx.rotate(c.a + Math.PI / 2);
                ctx.fillStyle = 'rgba(5,15,40,0.8)';
                ctx.font = '700 14px Inter,-apple-system,sans-serif';
                ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
                ctx.fillText(c.t, 0, 0);
                ctx.restore();
            });

            // Nordmarkierung
            ctx.save();
            const tAng = -Math.PI / 2;
            const tx = CX + Math.cos(tAng) * (R_BEZEL_OUT + 10);
            const ty = CY + Math.sin(tAng) * (R_BEZEL_OUT + 10);
            ctx.beginPath();
            ctx.moveTo(tx, ty + 3); ctx.lineTo(tx - 10, ty - 12); ctx.lineTo(tx + 10, ty - 12);
            ctx.closePath();
            ctx.fillStyle = '#4DA3FF';
            ctx.shadowColor = 'rgba(77,163,255,0.8)'; ctx.shadowBlur = 12;
            ctx.fill();
            ctx.restore();

            // ═══ INNERES ZIFFERBLATT ═══
            const fGrad = ctx.createRadialGradient(CX, CY - 40, 20, CX, CY, R_FACE);
            fGrad.addColorStop(0, '#20202C');
            fGrad.addColorStop(1, '#12121C');
            ctx.beginPath(); ctx.arc(CX, CY, R_FACE, 0, Math.PI * 2);
            ctx.fillStyle = fGrad; ctx.fill();

            curvedText("FLUGHAFEN INNSBRUCK · 47°15'N",
                Math.PI * 0.55, Math.PI * 1.45, R_FACE - 20,
                'rgba(140,180,255,0.75)', '700 15px Inter', false);
            curvedText("CHRONO SYSTEM · 11°20'E",
                -Math.PI * 0.45, Math.PI * 0.45, R_FACE - 20,
                'rgba(140,180,255,0.55)', '700 15px Inter', true);

            // Minutenstriche
            const R_T = R_FACE - 42;
            for (let i = 0; i < 60; i++) {
                const ang = (i / 60) * Math.PI * 2 - Math.PI / 2;
                const iH = i % 5 === 0;
                ctx.beginPath();
                ctx.moveTo(CX + Math.cos(ang) * R_T, CY + Math.sin(ang) * R_T);
                ctx.lineTo(CX + Math.cos(ang) * (R_T - (iH ? 18 : 9)),
                           CY + Math.sin(ang) * (R_T - (iH ? 18 : 9)));
                ctx.strokeStyle = iH ? 'rgba(255,255,255,0.55)' : 'rgba(255,255,255,0.18)';
                ctx.lineWidth = iH ? 2.8 : 1.2; ctx.lineCap = 'round';
                ctx.stroke();
            }

            // Schriftzug
            ctx.save();
            ctx.fillStyle = '#F0F0F8';
            ctx.font = '700 30px Inter,-apple-system,sans-serif';
            ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
            ctx.fillText('CHRONO', CX, CY - 82);
            ctx.restore();

            // Statuspunkte
            for (let i = 0; i < 4; i++) {
                ctx.beginPath();
                ctx.arc(CX - 30 + i * 20, CY - 50, 5, 0, Math.PI * 2);
                ctx.fillStyle = i < 3 ? '#1E88FF' : 'rgba(30,136,255,0.25)';
                ctx.fill();
            }

            // Nebenzifferblatt links – Kalenderwoche
            const sdX = CX - 82, sdY = CY + 26, sdR = 50;
            ctx.beginPath(); ctx.arc(sdX, sdY, sdR, 0, Math.PI * 2);
            ctx.fillStyle = 'rgba(10,10,20,0.7)'; ctx.fill();
            ctx.strokeStyle = 'rgba(30,136,255,0.3)'; ctx.lineWidth = 1.5; ctx.stroke();
            for (let i = 0; i < 12; i++) {
                const a = (i / 12) * Math.PI * 2;
                ctx.beginPath();
                ctx.moveTo(sdX + Math.cos(a) * (sdR - 3), sdY + Math.sin(a) * (sdR - 3));
                ctx.lineTo(sdX + Math.cos(a) * (sdR - 10), sdY + Math.sin(a) * (sdR - 10));
                ctx.strokeStyle = 'rgba(255,255,255,0.3)'; ctx.lineWidth = 1.5; ctx.stroke();
            }
            ctx.fillStyle = 'rgba(140,180,255,0.6)';
            ctx.font = '600 11px Inter,-apple-system,sans-serif';
            ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
            ctx.fillText('KW', sdX, sdY - 12);
            ctx.fillStyle = '#F0F0F8';
            ctx.font = '800 26px Inter,-apple-system,sans-serif';
            const kw = Math.ceil((((now - new Date(now.getFullYear(), 0, 1)) / 864e5)
                + new Date(now.getFullYear(), 0, 1).getDay() + 1) / 7);
            ctx.fillText(kw, sdX, sdY + 9);

            // Aktivitaetsringe rechts
            const arX = CX + 82, arY = CY + 26;
            drawRing(arX, arY, 46, 0.86, '#1E88FF', 'rgba(30,136,255,0.15)', 10);
            drawRing(arX, arY, 32, 0.72, '#4DA3FF', 'rgba(77,163,255,0.15)', 10);
            drawRing(arX, arY, 18, 0.94, '#E8F0FF', 'rgba(232,240,255,0.15)', 10);

            // Bogenanzeige unten
            const gaX = CX, gaY = CY + 108, gaR = 46;
            ctx.beginPath();
            ctx.arc(gaX, gaY, gaR, Math.PI * 0.75, Math.PI * 2.25, false);
            ctx.strokeStyle = 'rgba(255,255,255,0.1)'; ctx.lineWidth = 9;
            ctx.lineCap = 'round'; ctx.stroke();
            ctx.beginPath();
            ctx.arc(gaX, gaY, gaR, Math.PI * 0.75, Math.PI * 0.75 + (Math.PI * 1.5) * 0.65, false);
            const gGrad = ctx.createLinearGradient(gaX - gaR, gaY, gaX + gaR, gaY);
            gGrad.addColorStop(0, '#1E88FF'); gGrad.addColorStop(1, '#4DA3FF');
            ctx.strokeStyle = gGrad; ctx.lineWidth = 9; ctx.lineCap = 'round'; ctx.stroke();
            ctx.fillStyle = '#F0F0F8';
            ctx.font = '800 26px Inter,-apple-system,sans-serif';
            ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
            ctx.fillText('8°', gaX, gaY);

            curvedText('· EINSATZBEREIT ·', Math.PI * 0.70, Math.PI * 1.30,
                R_FACE - 42, 'rgba(240,240,248,0.9)', '800 16px Inter', false);

            // ═══ ZEIGER ═══
            const hAng = ((hr % 12 + mn / 60) / 12) * Math.PI * 2 - Math.PI / 2;
            const mAng = ((mn + sc / 60) / 60) * Math.PI * 2 - Math.PI / 2;
            const sAng = ((sc + ms / 1000) / 60) * Math.PI * 2 - Math.PI / 2;

            drawHand(hAng, 115, 14, 32, '#F0F0F4');
            drawHand(mAng, 185, 11, 38, '#F0F0F4');

            ctx.save();
            ctx.shadowColor = 'rgba(255,80,60,0.6)'; ctx.shadowBlur = 6;
            ctx.beginPath();
            ctx.moveTo(CX - Math.cos(sAng) * 45, CY - Math.sin(sAng) * 45);
            ctx.lineTo(CX + Math.cos(sAng) * 225, CY + Math.sin(sAng) * 225);
            ctx.strokeStyle = '#FF453A'; ctx.lineWidth = 2.8; ctx.lineCap = 'round';
            ctx.stroke();
            ctx.restore();

            // Mittelpunkt
            ctx.beginPath(); ctx.arc(CX, CY, 13, 0, Math.PI * 2);
            ctx.fillStyle = '#F0F0F4'; ctx.fill();
            ctx.beginPath(); ctx.arc(CX, CY, 8, 0, Math.PI * 2);
            ctx.fillStyle = '#FF453A'; ctx.fill();
            ctx.beginPath(); ctx.arc(CX, CY, 3.5, 0, Math.PI * 2);
            ctx.fillStyle = '#1A1A24'; ctx.fill();

            window.chronoUltraWatch.animationId = requestAnimationFrame(draw);
        }

        draw();
    },

    stop: function () {
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
            this.animationId = null;
        }
    }
};
