class DarkModeService {
    constructor() {
        this.isDark = localStorage.getItem('darkMode') === 'enabled';
        this.init();
    }

    init() {
        // Apply dark mode immediately if enabled
        if (this.isDark) {
            document.body.classList.add('dark-mode');
            this.updateUI(true);
        }
        this.setupToggle();
    }

    setupToggle() {
        const toggleButton = document.querySelector('#darkModeToggle');
        if (toggleButton) {
            // Remove any existing listeners first
            const newToggle = toggleButton.cloneNode(true);
            toggleButton.parentNode.replaceChild(newToggle, toggleButton);
            
            // Add new click listener
            newToggle.addEventListener('click', () => {
                this.toggle();
                // Play a subtle click sound
                this.playToggleSound();
            });

            // Set initial state
            this.updateUI(this.isDark);
        }
    }

    toggle() {
        this.isDark = !this.isDark;
        document.body.classList.toggle('dark-mode');
        localStorage.setItem('darkMode', this.isDark ? 'enabled' : 'disabled');
        this.updateUI(this.isDark);
    }

    updateUI(isDark) {
        const toggle = document.querySelector('#darkModeToggle');
        if (toggle) {
            // Remove all text nodes (including whitespace) so only the icon remains
            Array.from(toggle.childNodes).forEach(node => {
                if (node.nodeType === Node.TEXT_NODE) {
                    toggle.removeChild(node);
                }
            });
            const icon = toggle.querySelector('i');
            if (icon) {
                icon.className = isDark ? 'bi bi-sun' : 'bi bi-moon';
            }
        }

        // Update other dark mode specific elements
        const inputs = document.querySelectorAll('input, select, textarea');
        inputs.forEach(input => {
            input.style.transition = 'background-color var(--transition-speed) ease, color var(--transition-speed) ease';
        });
    }

    playToggleSound() {
        // Create and play a subtle toggle sound
        const audio = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1EOS8tNkRXY3R5eHRrYFdMQjozMjIxNkBPW2hueHt4cWpkWEhANS0oJSYpLzc8QkpWZ3J5eG5hUEIzJRkVGSEtOEVOW2x7gIF6bWJTQjYqICIoMTk/S1lldHh1b2hdUkQ4LyspLTM7QEVPYXBydmxgUj8sHhYVGiMtNT5NX3Z/eWxYPygYDg0TGiUyQ1VheH18dGhXRTYlGBUaJC48R1JfcHx8cV5KNiQWERIYICs2QlVnfYN+cFo/KBcPDxQbIC48TGJ3gX9xXEIuHhEOEhYdJzE9UGmChoJwVjsnFQ4NFBcdJTNDXHaBgHhkSC8eDw0QFBohKjhJYnyIg3NbQCkXDQwPEhYdJjhNaICHfGhMNCEQCgsNEBUcJTZLaoOJfm1RNyQSDw4QExYcJDRJaICIfnBUOSYUDg0PEhYcIzJHZn+JgXRZPikWDg0PExccIjFEYnyIfXFXPCcVDg0PFBccITBCXnmGfXJYPigWDw4QFRgiMEFdeISAcllAKRcPDhAWGyIwQFx3g39yWkAqGBAQERYbIjBAXHeEgHNbQSsZEBARFhwjMUFdeISAc1tBKxkREhMYHSUyQ199hoJzW0ErGhISExgdJTNEYH+GgnNbQSsaEhMUGR4mM0VhgIaCc1tBKxoTExQZHiYzRWGAhoJzW0ErGhMTFBkeJjRGYoCHg3RcQiwbFBQVGh8nNUZigYeDdF1DLRwVFhcbICg2R2OBiIN0XUQtHRYXGBwhKTdIZIKJhHVeRDAgGBgZHSIqOElmhImEdV5EMiMbGxseIys6SmeCioZ2X0UzJB0dHiAlLTtLaIOLhndgRjQlHh8gJi89TGmEi4d3YUc1Jh8gISguPk1qhYyId2JINicgISMpL0BObIaNiHhiSDgnISIkKjBBT22HjYl4Y0k5KCMjJSsxQlBtiI6JeWRKOikjJCYsMkNRbomOinlkSzspJCUnLTNEUm+Jj4p6ZUs8KiUlJy40RVNwipCLemZMPSslJigvNUZUcYqQi3pmTD0rJicoLzVGVHGLkYx7Z00+LCcnKTA2R1VyipCMe2dNPiwn','audio/wav');
        audio.volume = 0.2;  // Reduce volume to 20%
        audio.play().catch(() => {
            // Ignore any autoplay restrictions
            console.log('Audio play prevented');
        });
    }
}



            // Patch for darkMode.js: restore label if removed
        document.addEventListener('DOMContentLoaded', function() {
            const toggle = document.getElementById('darkModeToggle');
            if (toggle && !toggle.querySelector('.toggle-label')) {
                const label = document.createElement('span');
                label.className = 'toggle-label';
                label.textContent = 'Switch to Dark Mode';
                toggle.appendChild(label);
            }
        });

// Initialize dark mode
document.addEventListener('DOMContentLoaded', () => {
    if (!window.darkModeService) {
        window.darkModeService = new DarkModeService();
    }
});
