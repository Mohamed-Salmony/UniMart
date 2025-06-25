document.addEventListener("DOMContentLoaded", function() {
    // Get form elements
    const signUpForm = document.getElementById("signUpForm");
    if (!signUpForm) return; // Exit if form not found
    
    const fullNameField = document.getElementById("fullName");
    const emailField = document.getElementById("email");
    const passwordField = document.getElementById("password");
    const confirmPasswordField = document.getElementById("confirmPassword");
    const roleCustomer = document.getElementById("roleCustomer");
    const roleMerchant = document.getElementById("roleMerchant");

    const fullNameError = document.getElementById("fullNameError");
    const emailError = document.getElementById("emailError");
    const passwordError = document.getElementById("passwordError");
    const confirmPasswordError = document.getElementById("confirmPasswordError");

    // Toggle password visibility
    function setupPasswordToggle(toggleId, passwordInputId) {
        const toggleButton = document.getElementById(toggleId);
        const passwordInput = document.getElementById(passwordInputId);
        
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
    }

    // Setup both password toggles
    setupPasswordToggle("togglePassword", "password");
    setupPasswordToggle("toggleConfirmPassword", "confirmPassword");

    // Client-side validation before form submission
    signUpForm.addEventListener("submit", function(event) {
        // Clear previous errors
        clearErrors();

        // Get values
        const fullName = fullNameField ? fullNameField.value.trim() : "";
        const email = emailField ? emailField.value.trim() : "";
        const password = passwordField ? passwordField.value : "";
        const confirmPassword = confirmPasswordField ? confirmPasswordField.value : "";
        const role = roleCustomer && roleCustomer.checked ? "User" : 
                    roleMerchant && roleMerchant.checked ? "Merchant" : "";

        let isValid = true;

        // Validate full name
        if (fullNameField && fullName === "") {
            showError(fullNameError, "Full name is required.");
            isValid = false;
        }

        // Validate email
        if (emailField && email === "") {
            showError(emailError, "Email is required.");
            isValid = false;
        } else if (emailField && !validateEmail(email)) {
            showError(emailError, "Please enter a valid email.");
            isValid = false;
        }

        // Validate password
        if (passwordField && password === "") {
            showError(passwordError, "Password is required.");
            isValid = false;
        } else if (passwordField && !validatePassword(password)) {
            showError(passwordError, "Password must be at least 8 characters, including at least one letter, one number, and one special character.");
            isValid = false;
        }

        // Validate confirm password
        if (confirmPasswordField && confirmPassword === "") {
            showError(confirmPasswordError, "Confirm password is required.");
            isValid = false;
        } else if (confirmPasswordField && password !== confirmPassword) {
            showError(confirmPasswordError, "Passwords do not match.");
            isValid = false;
        }

        // Validate role
        if (!role) {
            event.preventDefault();
            const roleError = document.querySelector('span[asp-validation-for="Role"]');
            if (roleError) {
                roleError.textContent = "Please select a role";
            }
            return;
        }

        // If not valid, prevent form submission
        if (!isValid) {
            event.preventDefault();
        }
    });

    // Helper functions
    function showError(errorElement, message) {
        if (errorElement) {
            errorElement.textContent = message;
        }
    }

    function clearErrors() {
        if (fullNameError) fullNameError.textContent = "";
        if (emailError) emailError.textContent = "";
        if (passwordError) passwordError.textContent = "";
        if (confirmPasswordError) confirmPasswordError.textContent = "";
        const roleError = document.querySelector('span[asp-validation-for="Role"]');
        if (roleError) roleError.textContent = "";
    }

    function validateEmail(email) {
        const regex = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6}$/;
        return regex.test(email);
    }

    function validatePassword(password) {
        // Minimum 8 characters, at least one letter, one number, and one special character
        const regex = /^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$/;
        return regex.test(password);
    }

}); 