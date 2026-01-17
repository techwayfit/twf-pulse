// Chart.js-based poll dashboard implementation
let pollCharts = new Map(); // Store chart instances by canvas ID

window.renderPollChart = function (canvasId, labels, data, colors) {
    console.log('renderPollChart called with:', { canvasId, labels, data, colors });
    
    if (!canvasId) {
        console.error('Canvas ID is null or empty');
        return;
    }
    
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Canvas element not found:', canvasId);
        return;
    }

    const ctx = canvas.getContext('2d');
    if (!ctx) {
        console.error('Could not get 2D context');
        return;
    }
    
    // If chart already exists, destroy it first
    if (pollCharts.has(canvasId)) {
        pollCharts.get(canvasId).destroy();
    }
    
    // Handle empty data case
    if (!data || data.length === 0 || data.every(d => d === 0)) {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#6c757d';
        ctx.font = '16px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('No responses yet', canvas.width / 2, canvas.height / 2);
        return;
    }
    
    // Create new Chart.js chart
    const chart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels || data.map((_, i) => `Option ${i + 1}`),
            datasets: [{
                data: data,
                backgroundColor: colors || [
                    '#0d6efd', '#198754', '#dc3545', '#ffc107', 
                    '#6f42c1', '#fd7e14', '#20c997', '#e91e63'
                ],
                borderColor: colors ? colors.map(c => c) : [
                    '#0d6efd', '#198754', '#dc3545', '#ffc107',
                    '#6f42c1', '#fd7e14', '#20c997', '#e91e63'
                ],
                borderWidth: 1,
                borderRadius: 4,
                borderSkipped: false
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                intersect: false,
                mode: 'index'
            },
            animation: {
                duration: 800,
                easing: 'easeOutCubic'
            },
            plugins: {
                title: {
                    display: true,
                    text: 'Poll Results',
                    font: {
                        size: 16,
                        weight: 'bold'
                    },
                    padding: 20
                },
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: 'white',
                    bodyColor: 'white',
                    borderColor: 'rgba(255, 255, 255, 0.3)',
                    borderWidth: 1,
                    callbacks: {
                        label: function(context) {
                            const value = context.parsed.y;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                            return `${value} responses (${percentage}%)`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        maxRotation: 45,
                        color: '#666',
                        font: {
                            size: 12
                        }
                    }
                },
                y: {
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1,
                        color: '#666',
                        font: {
                            size: 12
                        }
                    },
                    grid: {
                        color: '#e9ecef'
                    }
                }
            },
            layout: {
                padding: {
                    top: 20,
                    right: 20,
                    bottom: 60,
                    left: 20
                }
            }
        }
    });
    
    // Store the chart instance
    pollCharts.set(canvasId, chart);
    
    console.log('Chart.js chart created successfully');
};

// Poll Dashboard auto-refresh functionality
window.pollDashboard = {
    refreshInterval: null,
    
    startAutoRefresh: function(callback, intervalMs = 30000) {
        this.stopAutoRefresh();
        this.refreshInterval = setInterval(callback, intervalMs);
    },
    
    stopAutoRefresh: function() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }
};
