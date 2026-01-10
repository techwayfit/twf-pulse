// QR Code generation for session join
window.generateQRCode = (canvasId, text) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
 console.error('Canvas not found:', canvasId);
  return;
    }

    // Simple QR code placeholder - in production, use a proper QR library
    const ctx = canvas.getContext('2d');
    const size = 200;
    
    // Clear canvas
    ctx.clearRect(0, 0, size, size);
    
// Draw background
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, size, size);
    
  // Draw border
    ctx.strokeStyle = '#1a2230';
  ctx.lineWidth = 2;
    ctx.strokeRect(5, 5, size - 10, size - 10);
    
    // Draw placeholder QR pattern
    const cellSize = 8;
    const margin = 20;
    const qrSize = size - (margin * 2);
    const cellsPerRow = Math.floor(qrSize / cellSize);
 
    ctx.fillStyle = '#1a2230';
    
    // Create a simple pattern based on the text
    const hash = simpleHash(text);
 for (let row = 0; row < cellsPerRow; row++) {
        for (let col = 0; col < cellsPerRow; col++) {
        const shouldFill = (hash + row * cellsPerRow + col) % 3 === 0;
        if (shouldFill) {
                ctx.fillRect(
             margin + col * cellSize,
     margin + row * cellSize,
    cellSize - 1,
    cellSize - 1
      );
        }
        }
    }
    
    // Draw corner markers
    drawCornerMarker(ctx, margin, margin, cellSize * 7);
    drawCornerMarker(ctx, size - margin - cellSize * 7, margin, cellSize * 7);
    drawCornerMarker(ctx, margin, size - margin - cellSize * 7, cellSize * 7);
    
    // Add text below QR code
    ctx.font = '12px Arial';
    ctx.fillStyle = '#516173';
    ctx.textAlign = 'center';
    const shortUrl = text.length > 30 ? text.substring(0, 27) + '...' : text;
    ctx.fillText(shortUrl, size / 2, size - 5);
};

function simpleHash(str) {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
        const char = str.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
    hash = hash & hash; // Convert to 32bit integer
    }
    return Math.abs(hash);
}

function drawCornerMarker(ctx, x, y, size) {
    // Outer square
    ctx.fillRect(x, y, size, size);
    
    // Inner white square
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(x + size * 0.15, y + size * 0.15, size * 0.7, size * 0.7);
    
    // Inner black square
    ctx.fillStyle = '#1a2230';
    ctx.fillRect(x + size * 0.3, y + size * 0.3, size * 0.4, size * 0.4);
}