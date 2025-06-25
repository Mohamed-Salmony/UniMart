  // Validate Password Match and Strength
  document.getElementById('reset-password-form').addEventListener('submit', function(event) {
    event.preventDefault(); // Prevent form submission

    // Get password values
    const newPassword = document.getElementById('new-password').value;
    const confirmPassword = document.getElementById('confirm-password').value;

    // Error flag
    let errorFlag = false;

    // Clear previous error messages
    document.getElementById('new-password-error').textContent = "";
    document.getElementById('confirm-password-error').textContent = "";

    // Validate password length and letter presence
    if (newPassword.length < 8) {
      document.getElementById('new-password-error').textContent = "Password must be at least 8 characters long.";
      errorFlag = true;
    } else if (!/[a-zA-Z]/.test(newPassword)) {
      document.getElementById('new-password-error').textContent = "Password must contain at least one letter.";
      errorFlag = true;
    }

    // Validate password match
    if (newPassword !== confirmPassword) {
      document.getElementById('confirm-password-error').textContent = "Passwords do not match!";
      errorFlag = true;
    }

    if (errorFlag) return; // Stop form submission if there's an error

    // Show the popup
    document.getElementById('popup').style.display = 'block';

    // Hide the popup after 3 seconds and redirect to another page
    setTimeout(function() {
      document.getElementById('popup').style.display = 'none';
      // Redirect to another page (for example, a success page)
      window.location.href = "Login.html"; // replace with your desired URL
    }, 3000);
  });

  // Close Popup
  function closePopup() {
    document.getElementById('popup').style.display = 'none';
  }