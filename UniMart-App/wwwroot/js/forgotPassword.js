document.addEventListener("DOMContentLoaded", function() {
  // Dark Mode Toggle Logic
  const darkToggle = document.querySelector('.dark-toggle');
  if (darkToggle) {
    const darkModeIcon = darkToggle.querySelector('i');
    
    // Check for saved user preference
    if (localStorage.getItem('darkMode') === 'enabled') {
      document.body.classList.add('dark-mode');
      darkModeIcon.classList.replace('bi-moon', 'bi-sun');
    }

    darkToggle.addEventListener('click', function() {
      // Toggle dark mode class on body
      document.body.classList.toggle('dark-mode');
      
      // Toggle between moon and sun icons
      darkModeIcon.classList.toggle('bi-moon');
      darkModeIcon.classList.toggle('bi-sun');
      
      // Save user preference
      if (document.body.classList.contains('dark-mode')) {
        localStorage.setItem('darkMode', 'enabled');
      } else {
        localStorage.setItem('darkMode', 'disabled');
      }
    });
  }

  // Smooth page transition
  document.body.classList.add('page-transition');

  // Form submission handler
  document.getElementById('forgot-password-form').addEventListener('submit', function(event) {
    event.preventDefault(); // Prevent form submission

    // Show the popup
    document.getElementById('popup').style.display = 'block';

    // Hide the popup after 3 seconds and redirect to another page
    setTimeout(function() {
      document.getElementById('popup').style.display = 'none';
      // Redirect to reset password page
      window.location.href = "resetPassword.html";
    }, 3000);
  });

  // Close Popup
  window.closePopup = function() {
    document.getElementById('popup').style.display = 'none';
  }

  const form = document.getElementById('forgot-password-form');
  const fullName = document.querySelector('input[type="text"]');
  const email = document.querySelector('input[type="email"]');

  form.addEventListener('submit', function(e) {
    e.preventDefault();
    clearErrors();
    
    if (validateForm()) {
      this.submit();
    }
  });

  function validateForm() {
    let isValid = true;

    // Full Name validation
    if (!fullName.value.trim()) {
      showError(fullName, 'Full name is required');
      isValid = false;
    } else if (!/^[a-zA-Z\s]{2,}$/.test(fullName.value.trim())) {
      showError(fullName, 'Please enter a valid name (letters and spaces only)');
      isValid = false;
    }

    // Email validation
    if (!email.value.trim()) {
      showError(email, 'Email is required');
      isValid = false;
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim())) {
      showError(email, 'Please enter a valid email address');
      isValid = false;
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
});
