// Password Toggle Logic
document.addEventListener('DOMContentLoaded', function() {
    const toggleButton = document.getElementById('togglePassword');
    const passwordInput = document.getElementById('password');

    if (toggleButton && passwordInput) {
        toggleButton.addEventListener("click", function(e) {
            e.preventDefault(); // Prevent form submission
            const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
            passwordInput.setAttribute("type", type);
            
            const icon = toggleButton.querySelector("i");
            if (icon) {
                icon.classList.toggle("bi-eye");
                icon.classList.toggle("bi-eye-slash");
            }
        });
    }

    // Form validation and submission
    const form = document.getElementById('loginForm');
    const email = document.getElementById('Email');
    const rememberMe = document.getElementById('RememberMe');

    if (form) {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            clearErrors();
            
            if (validateForm()) {
                // Handle remember me
                if (rememberMe && rememberMe.checked) {
                    localStorage.setItem('rememberedEmail', email ? email.value : '');
                } else {
                    localStorage.removeItem('rememberedEmail');
                }
                
                // Submit the form
                this.submit();
            }
        });
    }

    function validateForm() {
        let isValid = true;

        if (!email) {
            console.error('Email input element not found');
            isValid = false;
        } else {
            // Email validation
            if (!email.value.trim()) {
                showError(email, 'Email is required');
                isValid = false;
            } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim())) {
                showError(email, 'Please enter a valid email address');
                isValid = false;
            }
        }

        if (!passwordInput) {
            console.error('Password input element not found');
            isValid = false;
        } else {
            // Password validation
            if (!passwordInput.value) {
                showError(passwordInput, 'Password is required');
                isValid = false;
            }
        }

        return isValid;
    }

    function showError(element, message) {
        const errorDiv = document.createElement('div');
        errorDiv.className = 'error-message';
        errorDiv.textContent = message;
        errorDiv.style.color = 'red';
        errorDiv.style.fontSize = '0.8rem';
        errorDiv.style.marginTop = '0.25rem';
        
        // Remove any existing error message
        const existingError = element.parentElement.querySelector('.error-message');
        if (existingError) {
            existingError.remove();
        }
        
        // Add new error message
        element.parentElement.appendChild(errorDiv);
    }

    function clearErrors() {
        const errorElements = document.querySelectorAll('.error-message');
        errorElements.forEach(element => element.remove());
    }

    // Real-time validation feedback for email
    const emailInput = document.getElementById('Email');
    if (emailInput) {
        emailInput.addEventListener('input', function () {
            if (emailInput.validity.valid) {
                emailInput.classList.remove('is-invalid');
            } else {
                emailInput.classList.add('is-invalid');
            }
        });
    }

    // Real-time validation feedback for password
    if (passwordInput) {
        passwordInput.addEventListener('input', function () {
            if (passwordInput.validity.valid) {
                passwordInput.classList.remove('is-invalid');
            } else {
                passwordInput.classList.add('is-invalid');
            }
        });
    }
});
