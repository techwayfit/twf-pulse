// QR Code Scanner for Participant Join
// Uses html5-qrcode library

let html5QrcodeScanner = null;

window.initQRScanner = async function (dotNetHelper) {
    const qrReaderElement = document.getElementById('qr-reader');
    
    if (!qrReaderElement) {
        console.error('QR reader element not found');
        return;
    }

    // Check if library is loaded
    if (typeof Html5Qrcode === 'undefined') {
        console.log('Loading html5-qrcode library...');
        await loadQRCodeLibrary();
    }

    try {
        html5QrcodeScanner = new Html5QrcodeScanner(
            "qr-reader",
            { 
                fps: 10,
                qrbox: { width: 250, height: 250 },
                aspectRatio: 1.0
            },
            /* verbose= */ false
        );

        html5QrcodeScanner.render(
            (decodedText, decodedResult) => {
                // Success callback - QR code detected
                console.log(`QR Code detected: ${decodedText}`);
                
                // Stop scanner
                stopQRScanner();
                
                // Send result back to Blazor
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnQRCodeScanned', decodedText);
                }
            },
            (errorMessage) => {
                // Error callback (can be noisy, so we don't log every frame)
                // Most "errors" are just "No QR code found in frame"
            }
        );
    } catch (err) {
        console.error('Error starting QR scanner:', err);
    }
};

window.stopQRScanner = function () {
    if (html5QrcodeScanner) {
        try {
            html5QrcodeScanner.clear();
            html5QrcodeScanner = null;
        } catch (err) {
            console.error('Error stopping QR scanner:', err);
        }
    }
};

async function loadQRCodeLibrary() {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js';
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}
