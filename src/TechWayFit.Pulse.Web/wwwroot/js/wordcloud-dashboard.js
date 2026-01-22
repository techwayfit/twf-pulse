// WordCloud Dashboard Visualization Functions

// Render word cloud using Chart.js word cloud plugin
window.renderWordCloud = function (canvasId, words) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Canvas not found:', canvasId);
    return;
    }

    const ctx = canvas.getContext('2d');

    if (!words || words.length === 0) {
        ctx.fillStyle = '#6c757d';
        ctx.font = '16px Arial';
 ctx.textAlign = 'center';
  ctx.fillText('No words to display yet', canvas.width / 2, canvas.height / 2);
        return;
    }

    // Destroy existing chart if it exists
    if (window.wordCloudInstances && window.wordCloudInstances[canvasId]) {
 window.wordCloudInstances[canvasId].destroy();
    }

    // Initialize charts storage
    if (!window.wordCloudInstances) {
        window.wordCloudInstances = {};
    }

  console.log('Rendering word cloud with data:', words);

 // Color palette for word cloud
    const colors = [
        '#0d6efd', // Blue
        '#6610f2', // Purple
      '#6f42c1', // Indigo
        '#d63384', // Pink
        '#dc3545', // Red
        '#fd7e14', // Orange
        '#ffc107', // Yellow
    '#198754', // Green
        '#20c997', // Teal
   '#0dcaf0'  // Cyan
    ];

    // Check if we're in presenter mode (larger fonts)
    const isPresenterMode = document.querySelector('.presenter-mode-page') !== null;
const sizeMultiplier = isPresenterMode ? 30 : 10; // 3x larger in presenter mode
    const fontSizeRange = isPresenterMode ? [24, 80] : [12, 48]; // Larger font range

    // Create word cloud chart with custom colors and larger fonts
    window.wordCloudInstances[canvasId] = new Chart(ctx, {
        type: 'wordCloud',
        data: {
     labels: words.map(w => w.text || w.Text),
   datasets: [{
       label: '',
     data: words.map(w => (w.count || w.Count) * sizeMultiplier),
    color: colors,
   backgroundColor: (context) => {
     return colors[context.dataIndex % colors.length];
       }
 }]
        },
        options: {
    title: {
      display: false
},
        plugins: {
        legend: {
    display: false
          },
     tooltip: {
         enabled: true,
        callbacks: {
                   label: function(context) {
     return context.label + ': ' + Math.round(context.parsed.y / sizeMultiplier);
    }
              }
       }
         },
       // Word cloud specific options
            elements: {
      word: {
           fontFamily: 'Arial, sans-serif',
           fontWeight: 'bold',
     padding: isPresenterMode ? 4 : 2,
            minRotation: 0,
        maxRotation: 0, // Keep words horizontal for better readability
          rotate: 0
   }
          }
        }
    });
};

// Render word cloud as bar chart using Chart.js
window.renderWordCloudChart = function (chartId, data) {
    const canvas = document.getElementById(chartId);
    if (!canvas) {
        console.error('Chart canvas not found:', chartId);
        return;
    }

    const ctx = canvas.getContext('2d');

    // Destroy existing chart if it exists
    if (window.wordCloudCharts && window.wordCloudCharts[chartId]) {
        window.wordCloudCharts[chartId].destroy();
    }

    // Initialize charts storage
    if (!window.wordCloudCharts) {
        window.wordCloudCharts = {};
    }

    // Create new chart - C# sends PascalCase properties (Label, Count)
    window.wordCloudCharts[chartId] = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.map(item => item.Label || item.label),
            datasets: [{
                label: 'Word Count',
                data: data.map(item => item.Count || item.count),
                backgroundColor: [
                    'rgba(13, 110, 253, 0.8)',
                    'rgba(102, 16, 242, 0.8)',
                    'rgba(111, 66, 193, 0.8)',
                    'rgba(214, 51, 132, 0.8)',
                    'rgba(220, 53, 69, 0.8)',
                    'rgba(253, 126, 20, 0.8)',
                    'rgba(255, 193, 7, 0.8)',
                    'rgba(25, 135, 84, 0.8)',
                    'rgba(32, 201, 151, 0.8)',
                    'rgba(13, 202, 240, 0.8)'
                ],
                borderColor: [
                    'rgba(13, 110, 253, 1)',
                    'rgba(102, 16, 242, 1)',
                    'rgba(111, 66, 193, 1)',
                    'rgba(214, 51, 132, 1)',
                    'rgba(220, 53, 69, 1)',
                    'rgba(253, 126, 20, 1)',
                    'rgba(255, 193, 7, 1)',
                    'rgba(25, 135, 84, 1)',
                    'rgba(32, 201, 151, 1)',
                    'rgba(13, 202, 240, 1)'
                ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: 'y',
            plugins: {
                legend: {
                    display: false
                },
                title: {
                    display: true,
                    text: 'Top 10 Words',
                    font: {
                        size: 16,
                        weight: 'bold'
                    }
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1
                    },
                    title: {
                        display: true,
                        text: 'Count'
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: 'Words'
                    }
                }
            }
        }
    });
};
