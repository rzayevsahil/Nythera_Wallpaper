const wallpaperImage = document.getElementById('wallpaper-image');
const parallaxLayer = document.getElementById('parallax-layer');

// State
let currentSettings = {
    imagePath: '',
    blur: 0,
    brightness: 100,
    contrast: 100,
    layoutMode: 'Fill',
    enableKenBurns: false,
    enableParallax: false
};

// Expose API to C# WebView2
window.NytheraAPI = {
    setWallpaper: function(settingsJson) {
        try {
            const settings = JSON.parse(settingsJson);
            currentSettings = { ...currentSettings, ...settings };
            
            applySettings();
        } catch(e) {
            console.error("Error parsing settings:", e);
        }
    },
    
    updateFilter: function(blur, brightness, contrast) {
        currentSettings.blur = blur;
        currentSettings.brightness = brightness;
        currentSettings.contrast = contrast;
        updateFilters();
    },
    
    updateEffects: function(enableKenBurns, enableParallax) {
        currentSettings.enableKenBurns = enableKenBurns;
        currentSettings.enableParallax = enableParallax;
        applyEffects();
    },
    
    updateMousePosition: function(x, y, screenWidth, screenHeight) {
        if(currentSettings.enableParallax) {
            // Calculate offset based on normalized mouse coords (-1 to 1)
            const normX = (x / screenWidth) * 2 - 1;
            const normY = (y / screenHeight) * 2 - 1;
            
            // Move layer in opposite direction for depth effect
            const moveX = normX * -15; // Max 15px movement
            const moveY = normY * -15;
            
            parallaxLayer.style.transform = `translate(${moveX}px, ${moveY}px)`;
        }
    }
};

function applySettings() {
    // 1. Set Image
    if(currentSettings.imagePath) {
        // Convert local path to file URI if needed, or C# will send valid URL
        const imgSrc = currentSettings.imagePath.replace(/\\/g, '/');
        wallpaperImage.style.backgroundImage = `url('${imgSrc}')`;
    }
    
    // 2. Set Layout
    wallpaperImage.className = ''; // Reset
    const layoutClass = `layout-${currentSettings.layoutMode.toLowerCase()}`;
    wallpaperImage.classList.add(layoutClass);
    
    // 3. Set Filters
    updateFilters();
    
    // 4. Set Effects
    applyEffects();
}

function updateFilters() {
    const blur = currentSettings.blur || 0;
    const brightness = (currentSettings.brightness || 100) / 100;
    const contrast = (currentSettings.contrast || 100) / 100;
    
    wallpaperImage.style.filter = `blur(${blur}px) brightness(${brightness}) contrast(${contrast})`;
}

function applyEffects() {
    if(currentSettings.enableKenBurns) {
        wallpaperImage.classList.add('ken-burns-active');
    } else {
        wallpaperImage.classList.remove('ken-burns-active');
    }
    
    if(!currentSettings.enableParallax) {
        parallaxLayer.style.transform = 'translate(0px, 0px)';
    }
}

// Fallback auto-parallax (fake movement) if mouse tracking isn't sent from C#
let time = 0;
function autoParallax() {
    if(currentSettings.enableParallax && !window.hasReceivedMouseUpdate) {
        time += 0.01;
        const moveX = Math.sin(time) * 10;
        const moveY = Math.cos(time * 0.8) * 10;
        parallaxLayer.style.transform = `translate(${moveX}px, ${moveY}px)`;
    }
    requestAnimationFrame(autoParallax);
}

// Start auto parallax just in case
requestAnimationFrame(autoParallax);
