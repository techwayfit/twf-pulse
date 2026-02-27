// Quadrant Item Scoring — bubble chart dashboard
'use strict';

const quadrantBubbleCharts = new Map(); // canvas ID → Chart instance

// Per-item colour palette (cycles if more than 8 items)
const BUBBLE_PALETTE = [
    { bg: 'rgba(59,130,246,0.72)',  border: 'rgba(59,130,246,0.95)'  },  // blue
    { bg: 'rgba(139,92,246,0.72)', border: 'rgba(139,92,246,0.95)'  },  // purple
    { bg: 'rgba(239,68,68,0.68)',  border: 'rgba(239,68,68,0.90)'   },  // red
    { bg: 'rgba(16,185,129,0.68)', border: 'rgba(16,185,129,0.90)'  },  // green
    { bg: 'rgba(245,158,11,0.72)', border: 'rgba(245,158,11,0.95)'  },  // amber
    { bg: 'rgba(236,72,153,0.68)', border: 'rgba(236,72,153,0.90)'  },  // pink
    { bg: 'rgba(14,165,233,0.72)', border: 'rgba(14,165,233,0.95)'  },  // sky
    { bg: 'rgba(251,146,60,0.72)', border: 'rgba(251,146,60,0.95)'  },  // orange
];

// Quadrant background zones + labels plugin
const quadrantZonesPlugin = {
    id: 'quadrantZones',
    beforeDraw(chart) {
        const { ctx, chartArea: { left, top, right, bottom }, scales } = chart;
        const midX = scales.x.getPixelForValue((scales.x.min + scales.x.max) / 2);
        const midY = scales.y.getPixelForValue((scales.y.min + scales.y.max) / 2);
        const labels = chart.options.quadrantLabels || {};

        const zones = [
            { x: left,  y: top,   w: midX - left,  h: midY - top,    bg: 'rgba(59,130,246,0.06)',  label: (labels.q1 || 'Quick Wins').toUpperCase(),      lx: left + 10,  ly: top + 14  },
            { x: midX,  y: top,   w: right - midX,  h: midY - top,    bg: 'rgba(239,68,68,0.06)',   label: (labels.q2 || 'Major Projects').toUpperCase(),  lx: midX + 10,  ly: top + 14  },
            { x: left,  y: midY,  w: midX - left,  h: bottom - midY,  bg: 'rgba(16,185,129,0.06)',  label: (labels.q3 || 'Fill-Ins').toUpperCase(),        lx: left + 10,  ly: midY + 14 },
            { x: midX,  y: midY,  w: right - midX,  h: bottom - midY, bg: 'rgba(139,92,246,0.06)',  label: (labels.q4 || 'Thankless Tasks').toUpperCase(), lx: midX + 10,  ly: midY + 14 },
        ];

        zones.forEach(z => {
            ctx.save();
            ctx.fillStyle = z.bg;
            ctx.fillRect(z.x, z.y, z.w, z.h);
            ctx.fillStyle = 'rgba(100,116,139,0.45)';
            ctx.font = '700 9px "DM Sans", sans-serif';
            ctx.letterSpacing = '0.06em';
            ctx.fillText(z.label, z.lx, z.ly);
            ctx.restore();
        });

        // Dashed midlines
        ctx.save();
        ctx.setLineDash([4, 5]);
        ctx.strokeStyle = 'rgba(0,0,0,0.10)';
        ctx.lineWidth = 1;
        ctx.beginPath(); ctx.moveTo(midX, top);   ctx.lineTo(midX, bottom); ctx.stroke();
        ctx.beginPath(); ctx.moveTo(left, midY);  ctx.lineTo(right, midY);  ctx.stroke();
        ctx.restore();
    }
};

// Bubble label plugin
const bubbleLabelsPlugin = {
    id: 'bubbleLabels',
    afterDraw(chart) {
        const { ctx } = chart;
        chart.data.datasets.forEach((dataset, di) => {
            const meta = chart.getDatasetMeta(di);
            dataset.data.forEach((d, pi) => {
                const elem = meta.data[pi];
                if (!elem) return;
                const { x, y } = elem.getCenterPoint ? elem.getCenterPoint() : elem;
                const short = d.label && d.label.length > 12 ? d.label.slice(0, 11) + '…' : (d.label || '');
                ctx.save();
                ctx.font = '600 10px "DM Sans", sans-serif';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = 'rgba(255,255,255,0.92)';
                ctx.fillText(short, x, y);
                ctx.restore();
            });
        });
    }
};

/**
 * Render (or update) a bubble chart for the Quadrant dashboard.
 *
 * @param {string}  canvasId    - ID of the <canvas> element
 * @param {string}  xAxisLabel  - Label for the X axis
 * @param {string}  yAxisLabel  - Label for the Y axis
 * @param {Array}   items       - Array of { label, avgX, avgY, count, isCurrent }
 * @param {boolean} uniformSize - If true all bubbles have the same radius
 * @param {number}  xMin        - Axis minimum (lowest score value)
 * @param {number}  xMax        - Axis maximum (highest score value)
 * @param {string}  q1Label     - Top-left quadrant label (low X, high Y)
 * @param {string}  q2Label     - Top-right quadrant label (high X, high Y)
 * @param {string}  q3Label     - Bottom-left quadrant label (low X, low Y)
 * @param {string}  q4Label     - Bottom-right quadrant label (high X, low Y)
 */
window.renderQuadrantBubbleChart = function (canvasId, xAxisLabel, yAxisLabel, items, uniformSize, xMin, xMax, q1Label, q2Label, q3Label, q4Label) {
    if (!canvasId) { console.error('renderQuadrantBubbleChart: canvas ID is required'); return; }
    const canvas = document.getElementById(canvasId);
    if (!canvas) { console.warn('renderQuadrantBubbleChart: canvas not found:', canvasId); return; }
    const ctx = canvas.getContext('2d');
    if (!ctx) { console.error('renderQuadrantBubbleChart: failed to get 2D context'); return; }

    // Destroy existing chart
    if (quadrantBubbleCharts.has(canvasId)) {
        quadrantBubbleCharts.get(canvasId).destroy();
        quadrantBubbleCharts.delete(canvasId);
    }

    if (!items || items.length === 0) {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        return;
    }

    const maxCount = Math.max(...items.map(d => d.count), 1);
    const MIN_R = 14;
    const MAX_R = 36;
    const mid = ((xMin ?? 0) + (xMax ?? 10)) / 2;
    const padding = ((xMax ?? 10) - (xMin ?? 0)) * 0.12 || 1;

    // One dataset per item so each gets its own colour
    const datasets = items.map((item, idx) => {
        const colour = BUBBLE_PALETTE[idx % BUBBLE_PALETTE.length];
        return {
            label: item.label,
            data: [{
                x: item.avgX,
                y: item.avgY,
                r: uniformSize ? 18 : Math.max(MIN_R, Math.round((item.count / maxCount) * MAX_R)),
                label: item.label,
                count: item.count,
                isCurrent: !!item.isCurrent,
            }],
            backgroundColor: item.isCurrent ? colour.border : colour.bg,
            borderColor:     colour.border,
            borderWidth:     item.isCurrent ? 2.5 : 1.5,
        };
    });

    const chart = new Chart(ctx, {
        type: 'bubble',
        data: { datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 500, easing: 'easeOutBack' },
            quadrantLabels: { q1: q1Label, q2: q2Label, q3: q3Label, q4: q4Label },
            layout: { padding: { top: 6, right: 14, bottom: 4, left: 4 } },
            scales: {
                x: {
                    min: (xMin ?? 0) - padding,
                    max: (xMax ?? 10) + padding,
                    title: {
                        display: true,
                        text: xAxisLabel,
                        color: '#64748b',
                        font: { family: '"DM Sans", sans-serif', size: 11, weight: '600' },
                        padding: { top: 6 },
                    },
                    ticks: {
                        color: '#94a3b8',
                        font: { family: '"Space Mono", monospace', size: 10 },
                        stepSize: 1,
                    },
                    grid: { color: 'rgba(0,0,0,0.05)' },
                    border: { color: 'rgba(0,0,0,0.10)', width: 1 },
                },
                y: {
                    min: (xMin ?? 0) - padding,
                    max: (xMax ?? 10) + padding,
                    title: {
                        display: true,
                        text: yAxisLabel,
                        color: '#64748b',
                        font: { family: '"DM Sans", sans-serif', size: 11, weight: '600' },
                        padding: { bottom: 6 },
                    },
                    ticks: {
                        color: '#94a3b8',
                        font: { family: '"Space Mono", monospace', size: 10 },
                        stepSize: 1,
                    },
                    grid: { color: 'rgba(0,0,0,0.05)' },
                    border: { color: 'rgba(0,0,0,0.10)', width: 1 },
                }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: 'rgba(15,23,42,0.92)',
                    borderColor: 'rgba(255,255,255,0.08)',
                    borderWidth: 1,
                    titleColor: '#f8fafc',
                    bodyColor: 'rgba(248,250,252,0.65)',
                    titleFont: { family: '"DM Sans", sans-serif', size: 12, weight: '700' },
                    bodyFont: { family: '"Space Mono", monospace', size: 10 },
                    padding: 10,
                    cornerRadius: 9,
                    displayColors: true,
                    boxWidth: 9, boxHeight: 9,
                    callbacks: {
                        title(items) { return items[0].dataset.label; },
                        label(item) {
                            const d = item.raw;
                            return [`${xAxisLabel}: ${d.x.toFixed(1)}   ${yAxisLabel}: ${d.y.toFixed(1)}`, `Responses: ${d.count}`];
                        }
                    }
                }
            }
        },
        plugins: [quadrantZonesPlugin, bubbleLabelsPlugin]
    });

    quadrantBubbleCharts.set(canvasId, chart);
};

/**
 * Destroy a bubble chart instance (call on component dispose).
 * @param {string} canvasId
 */
window.destroyQuadrantBubbleChart = function (canvasId) {
    if (quadrantBubbleCharts.has(canvasId)) {
        quadrantBubbleCharts.get(canvasId).destroy();
        quadrantBubbleCharts.delete(canvasId);
    }
};
