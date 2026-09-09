// debugRegistration.js
// This file contains the debugRegistration function for Razor Pages registration debug

function toPascalCase(obj) {
    const result = {};
    for (const key in obj) {
        if (Object.hasOwnProperty.call(obj, key)) {
            const pascalKey = key.replace(/(^|_)([a-z])/g, function(_, __, l) { return l.toUpperCase(); });
            result[pascalKey] = obj[key];
        }
    }
    return result;
}

window.debugRegistration = function() {
    console.log('?? DEBUG: Starting comprehensive client-side registration test...');
    const form = document.getElementById('registerForm');
    const formData = new FormData(form);
    const dateInput = document.getElementById('DateOfBirth');
    const rawDateValue = dateInput.value;
    const testDataRaw = {
        firstName: formData.get('FirstName') || 'Test',
        lastName: formData.get('LastName') || 'User',
        email: formData.get('Email') || ('test' + Math.random().toString(36).substring(7) + '@example.com'),
        phoneNumber: formData.get('PhoneNumber') || '0901234567',
        dateOfBirth: rawDateValue || '1990-01-01',
        password: formData.get('Password') || 'Test123!',
        confirmPassword: formData.get('ConfirmPassword') || 'Test123!',
        acceptTerms: true
    };
    const testData = toPascalCase(testDataRaw);
    console.log('?? DEBUG: Sending test data:', testData);
    console.log('?? DEBUG: Date format being sent:', testData.DateOfBirth, 'Type:', typeof testData.DateOfBirth);
    Promise.all([
        fetch('/Account/DebugRegistrationDetailed', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify(testData)
        }).then(response => response.json()),
        fetch('/Account/DebugModel', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(testData)
        }).then(response => response.json())
    ])
    .then(([debugResult, modelResult]) => {
        console.log('?? DEBUG: Debug registration result:', debugResult);
        console.log('?? DEBUG: Model binding result:', modelResult);
        const analysis = {
            debugEndpoint: {
                success: debugResult && !debugResult.exception,
                step: debugResult?.step || 'unknown',
                dateOfBirth: debugResult?.inputData?.DateOfBirth,
                dateValidation: debugResult?.inputData?.IsValidYear
            },
            modelBinding: {
                success: modelResult && modelResult.IsValid,
                errors: modelResult?.Errors || [],
                modelDateOfBirth: modelResult?.Model?.DateOfBirth
            }
        };
        console.log('?? DEBUG: Analysis:', analysis);
        if (debugResult?.exception) {
            console.error('?? DEBUG: Exception occurred:', debugResult.exception);
            alert('Debug Error: ' + debugResult.exception.Message + '\n\nCheck console for detailed information.');
        } else {
            alert('Debug completed successfully!\n\n' +
                  'Debug Step: ' + (debugResult?.step || 'unknown') + '\n' +
                  'Model Valid: ' + (modelResult?.IsValid || false) + '\n' +
                  'Date Value: ' + testData.DateOfBirth + '\n\n' +
                  'Check browser console for detailed results.');
        }
    })
    .catch(error => {
        console.error('?? DEBUG: Network error:', error);
        alert('Debug network error: ' + error.message);
    });
};

// Add debug button (only in development)
if (window.location.hostname === 'localhost') {
    const debugBtn = document.createElement('button');
    debugBtn.textContent = '?? Debug Registration';
    debugBtn.type = 'button';
    debugBtn.className = 'btn btn-secondary';
    debugBtn.style.marginTop = '10px';
    debugBtn.onclick = window.debugRegistration;
    const form = document.getElementById('registerForm');
    if (form) form.appendChild(debugBtn);
}
