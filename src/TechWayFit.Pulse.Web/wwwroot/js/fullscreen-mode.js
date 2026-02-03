/**
 * Full-Screen Mode Utility
 * Enables full-screen presentation mode for workshop sessions
 */

window.fullscreenMode = {
  /**
   * Toggle full-screen mode
   */
  toggle: async function () {
    if (!document.fullscreenElement) {
      await this.enter();
    } else {
      await this.exit();
    }
  },

  /**
   * Enter full-screen mode
   */
  enter: async function () {
    try {
      const element = document.documentElement;

      if (element.requestFullscreen) {
        await element.requestFullscreen();
      } else if (element.webkitRequestFullscreen) {
        // Safari
        await element.webkitRequestFullscreen();
      } else if (element.msRequestFullscreen) {
        // IE11
        await element.msRequestFullscreen();
      }

      // Add CSS class for full-screen specific styling
      document.body.classList.add("fullscreen-active");

      // Dispatch custom event
      window.dispatchEvent(
        new CustomEvent("fullscreenchange", { detail: { active: true } }),
      );

      return true;
    } catch (error) {
      console.error("Failed to enter full-screen:", error);
      return false;
    }
  },

  /**
   * Exit full-screen mode
   */
  exit: async function () {
    try {
      if (document.exitFullscreen) {
        await document.exitFullscreen();
      } else if (document.webkitExitFullscreen) {
        // Safari
        await document.webkitExitFullscreen();
      } else if (document.msExitFullscreen) {
        // IE11
        await document.msExitFullscreen();
      }

      // Remove CSS class
      document.body.classList.remove("fullscreen-active");

      // Dispatch custom event
      window.dispatchEvent(
        new CustomEvent("fullscreenchange", { detail: { active: false } }),
      );

      return true;
    } catch (error) {
      console.error("Failed to exit full-screen:", error);
      return false;
    }
  },

  /**
   * Check if full-screen is supported
   */
  isSupported: function () {
    return !!(
      document.fullscreenEnabled ||
      document.webkitFullscreenEnabled ||
      document.msFullscreenEnabled
    );
  },

  /**
   * Check if currently in full-screen
   */
  isActive: function () {
    return !!(
      document.fullscreenElement ||
      document.webkitFullscreenElement ||
      document.msFullscreenElement
    );
  },
};

// Listen for fullscreen change events
document.addEventListener("fullscreenchange", handleFullscreenChange);
document.addEventListener("webkitfullscreenchange", handleFullscreenChange);
document.addEventListener("msfullscreenchange", handleFullscreenChange);

function handleFullscreenChange() {
  if (fullscreenMode.isActive()) {
    document.body.classList.add("fullscreen-active");
  } else {
    document.body.classList.remove("fullscreen-active");
  }
}

// Keyboard shortcut: F11 or Ctrl/Cmd + F
document.addEventListener("keydown", function (e) {
  // F11 key
  if (e.key === "F11") {
    e.preventDefault();
    fullscreenMode.toggle();
  }

  // Ctrl/Cmd + F
  if ((e.ctrlKey || e.metaKey) && e.key === "f") {
    // Only trigger if on a session page (check for data attribute)
    if (document.body.dataset.fullscreenEnabled === "true") {
      e.preventDefault();
      fullscreenMode.toggle();
    }
  }
});

// ESC key to exit (already handled by browser, but we can add custom behavior)
document.addEventListener("keydown", function (e) {
  if (e.key === "Escape" && fullscreenMode.isActive()) {
    // Optional: Add custom logic before exit
    console.log("Exiting full-screen mode");
  }
});
