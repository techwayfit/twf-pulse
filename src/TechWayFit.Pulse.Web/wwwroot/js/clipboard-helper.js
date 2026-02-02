// Simply select all text in an input field
window.selectAllText = function (inputId) {
    console.log('Attempting to select text from input:', inputId);
    try {
        const input = document.getElementById(inputId);
        console.log('Found input:', input);
        if (input) {
            input.focus();
            input.select();
            input.setSelectionRange(0, input.value.length);
            console.log('Text selected');
        } else {
            console.error('Input element not found with ID:', inputId);
        }
    } catch (err) {
        console.error('Select text error:', err);
    }
};
