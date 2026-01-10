// Test script to debug form builder loading
console.log('TEST SCRIPT LOADED - form builder test');

// Test 1: Check if window object is available
console.log('Window object:', typeof window);

// Test 2: Try to find the container immediately
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOMContentLoaded fired');
    const container = document.getElementById('joinFormBuilder');
    console.log('Container found in DOMContentLoaded:', container);
});

// Test 3: Simple init function
window.testFormBuilder = function() {
    console.log('testFormBuilder called');
    const container = document.getElementById('joinFormBuilder');
    console.log('Container:', container);
    
    if (container) {
        const fieldTypes = container.querySelector('.field-types');
        const dropZone = container.querySelector('.form-drop-zone');
        console.log('Field types:', fieldTypes);
        console.log('Drop zone:', dropZone);
        
        if (fieldTypes) {
            fieldTypes.innerHTML = '<div style="padding:20px; background:lightgreen;">✅ JavaScript is working! Field types div found.</div>';
        }
        
        if (dropZone) {
            dropZone.innerHTML = '<div style="padding:20px; background:lightblue;">✅ JavaScript is working! Drop zone div found.</div>';
        }
        
        return { container, fieldTypes, dropZone };
    } else {
        console.error('❌ Container not found!');
        return null;
    }
};

console.log('TEST: testFormBuilder function registered');
