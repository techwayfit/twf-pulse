// QR Code generation for session join
window.generateQRCode = (canvasId, text) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
   console.error('Canvas not found:', canvasId);
   return;
    }

    try {
        // Use the qrcodegen library if available for a real QR code
        if (typeof qrcode !== 'undefined') {
            const qr = qrcode(0, 'M');
            qr.addData(text);
   qr.make();
         
            // Draw QR code on canvas
       const ctx = canvas.getContext('2d');
     const moduleCount = qr.getModuleCount();
      const cellSize = Math.floor(200 / moduleCount);
     const size = cellSize * moduleCount;
 
            // Clear and resize canvas
   canvas.width = size;
         canvas.height = size;
       ctx.clearRect(0, 0, size, size);
   
            // Draw white background
   ctx.fillStyle = '#ffffff';
     ctx.fillRect(0, 0, size, size);
            
    // Draw QR code modules
 ctx.fillStyle = '#000000';
            for (let row = 0; row < moduleCount; row++) {
         for (let col = 0; col < moduleCount; col++) {
   if (qr.isDark(row, col)) {
        ctx.fillRect(col * cellSize, row * cellSize, cellSize, cellSize);
   }
    }
         }
            return;
        }
    } catch (error) {
        console.error('Error generating QR code:', error);
    }
    
    // Fallback to placeholder pattern
    const ctx = canvas.getContext('2d');
    const size = 200;
    
    ctx.clearRect(0, 0, size, size);
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, size, size);
    
    ctx.strokeStyle = '#1a2230';
    ctx.lineWidth = 2;
    ctx.strokeRect(5, 5, size - 10, size - 10);
    
    const cellSize = 8;
    const margin = 20;
    const qrSize = size - (margin * 2);
    const cellsPerRow = Math.floor(qrSize / cellSize);
 
    ctx.fillStyle = '#1a2230';
    
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
    
  drawCornerMarker(ctx, margin, margin, cellSize * 7);
drawCornerMarker(ctx, size - margin - cellSize * 7, margin, cellSize * 7);
    drawCornerMarker(ctx, margin, size - margin - cellSize * 7, cellSize * 7);
};

function simpleHash(str) {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
        const char = str.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash & hash;
    }
    return Math.abs(hash);
}

function drawCornerMarker(ctx, x, y, size) {
    ctx.fillRect(x, y, size, size);
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(x + size * 0.15, y + size * 0.15, size * 0.7, size * 0.7);
    ctx.fillStyle = '#1a2230';
    ctx.fillRect(x + size * 0.3, y + size * 0.3, size * 0.4, size * 0.4);
}

// Auto-generate QR codes for canvases with data-qr-url attribute
function autoGenerateQRCodes() {
    const qrCanvases = document.querySelectorAll('canvas[data-qr-url]');
    
    if (qrCanvases.length === 0) {
        return;
    }
    
    qrCanvases.forEach(canvas => {
        const canvasId = canvas.id;
     const url = canvas.getAttribute('data-qr-url');
        
        if (canvasId && url) {
  generateQRCode(canvasId, url);
        }
    });
}

// Run on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', autoGenerateQRCodes);
} else {
    autoGenerateQRCodes();
}

// Run after Blazor finishes rendering
window.addEventListener('load', () => {
    setTimeout(autoGenerateQRCodes, 500);
});