// Quadrant Item Scoring — bubble chart dashboard
'use strict';

const quadrantBubbleCharts = new Map(); // canvas ID → Chart instance

/**
 * Render (or update) a bubble chart for the Quadrant dashboard.
 *
 * @param {string} canvasId   - ID of the <canvas> element
 * @param {string} xAxisLabel - Label for the X axis
 * @param {string} yAxisLabel - Label for the Y axis
 * @param {Array}  items      - Array of { label, avgX, avgY, count, isCurrent }
 * @param {boolean} uniformSize - If true all bubbles have the same radius
 * @param {number} xMin       - Axis minimum (lowest score value)
 * @param {number} xMax       - Axis maximum (highest score value)
 */
window.renderQuadrantBubbleChart = function (canvasId, xAxisLabel, yAxisLabel, items, uniformSize, xMin, xMax) {
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
        ctx.fillStyle = '#6c757d';
        ctx.font = '16px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('No responses yet', canvas.width / 2, canvas.height / 2);
        return;
    }

    const maxCount = Math.max(...items.map(d => d.count), 1);
    const MIN_R = 8;
    const MAX_R = 30;

    const dataPoints = items.map(item => ({
        x: item.avgX,
        y: item.avgY,
        r: uniformSize ? 15 : Math.max(MIN_R, Math.round((item.count / maxCount) * MAX_R)),
        label: item.label,
        count: item.count,
        isCurrent: !!item.isCurrent
    }));

    const backgroundColors = dataPoints.map(d =>
        d.isCurrent ? 'rgba(13,110,253,0.85)' : 'rgba(108,117,125,0.55)'
    );
    const borderColors = dataPoints.map(d =>
        d.isCurrent ? '#0a58ca' : '#6c757d'
    );

    const min = (xMin !== null && xMin !== undefined) ? xMin : 0;
    const max = (xMax !== null && xMax !== undefined) ? xMax : 10;
    const padding = (max - min) * 0.15 || 1;

    const chart = new Chart(ctx, {
        type: 'bubble',
        data: {
            datasets: [{
                label: 'Items',
                data: dataPoints,
                backgroundColor: backgroundColors,
                borderColor: borderColors,
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: { display: true, text: xAxisLabel, font: { weight: 'bold' } },
                    min: min - padding,
                    max: max + padding,
                    grid: { color: 'rgba(0,0,0,0.06)' }
                },
                y: {
                    title: { display: true, text: yAxisLabel, font: { weight: 'bold' } },
                    min: min - padding,
                    max: max + padding,
                    grid: { color: 'rgba(0,0,0,0.06)' }
                }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            const d = context.raw;
                            return [
                                d.label,
                                `${xAxisLabel}: ${d.x.toFixed(1)}  ${yAxisLabel}: ${d.y.toFixed(1)}`,
                                `Responses: ${d.count}`
                            ];
                        }
                    }
                }
            },
            animation: { duration: 400 }
        },
        plugins: [{
            id: 'bubbleLabels',
            afterDraw(chart) {
                const ctx = chart.ctx;
                chart.data.datasets.forEach((dataset, di) => {
                    dataset.data.forEach((d, pi) => {
                        const meta = chart.getDatasetMeta(di);
                        const elem = meta.data[pi];
                        if (!elem) return;
                        const { x, y } = elem.getCenterPoint ? elem.getCenterPoint() : elem;
                        ctx.save();
                        ctx.font = 'bold 10px sans-serif';
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'middle';
                        ctx.fillStyle = d.isCurrent ? '#fff' : '#333';
                        // Truncate long labels to keep bubbles clean
                        const short = d.label.length > 12 ? d.label.slice(0, 11) + '…' : d.label;
                        ctx.fillText(short, x, y);
                        ctx.restore();
                    });
                });
            }
        }]
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
