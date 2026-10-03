document.addEventListener('DOMContentLoaded', () => {
    // Elements
    const settingsBtn = document.getElementById('settings-btn');
    const modalOverlay = document.getElementById('settings-modal');
    const closeModal = document.getElementById('close-modal');
    const dockInner = document.getElementById('dock-inner');
    const rgbBorder = document.getElementById('rgb-border');
    
    // Toggles
    const toggleMidia = document.getElementById('toggle-midia');
    const toggleWapp = document.getElementById('toggle-wapp');
    const toggleRgb = document.getElementById('toggle-rgb');

    // Templates
    const tplMidia = document.getElementById('tpl-midia').content;
    const tplWapp = document.getElementById('tpl-wapp').content;

    // State
    let isPlaying = false;

    // Modal Logic
    settingsBtn.addEventListener('click', () => {
        modalOverlay.classList.add('active');
    });

    closeModal.addEventListener('click', () => {
        modalOverlay.classList.remove('active');
    });

    modalOverlay.addEventListener('click', (e) => {
        if(e.target === modalOverlay) {
            modalOverlay.classList.remove('active');
        }
    });

    // Toggle Media Widget
    toggleMidia.addEventListener('change', (e) => {
        if (e.target.checked) {
            // Insert before settings button
            const divider = document.createElement('div');
            divider.className = 'divider';
            divider.id = 'div-midia';
            dockInner.insertBefore(divider, settingsBtn);
            
            const node = document.importNode(tplMidia, true);
            dockInner.insertBefore(node, settingsBtn);
            
            // Attach event listener to new play button
            const playBtn = document.getElementById('play-pause-btn');
            playBtn.addEventListener('click', (ev) => {
                ev.stopPropagation(); // prevent bounce
                isPlaying = !isPlaying;
                playBtn.textContent = isPlaying ? '⏸' : '▶';
                
                // Trigger RGB if enabled
                if(toggleRgb.checked && isPlaying) {
                    rgbBorder.classList.add('active');
                } else {
                    rgbBorder.classList.remove('active');
                }
            });
        } else {
            const widget = document.getElementById('widget-midia');
            const divider = document.getElementById('div-midia');
            if(widget) widget.remove();
            if(divider) divider.remove();
            
            // Turn off RGB if music stops by removing widget
            isPlaying = false;
            rgbBorder.classList.remove('active');
        }
    });

    // Toggle WhatsApp Widget
    toggleWapp.addEventListener('change', (e) => {
        if (e.target.checked) {
            const divider = document.createElement('div');
            divider.className = 'divider';
            divider.id = 'div-wapp';
            dockInner.insertBefore(divider, settingsBtn);
            
            const node = document.importNode(tplWapp, true);
            dockInner.insertBefore(node, settingsBtn);
        } else {
            const widget = document.getElementById('widget-wapp');
            const divider = document.getElementById('div-wapp');
            if(widget) widget.remove();
            if(divider) divider.remove();
        }
    });

    // Toggle RGB Setting
    toggleRgb.addEventListener('change', (e) => {
        if (e.target.checked && isPlaying) {
            rgbBorder.classList.add('active');
        } else {
            rgbBorder.classList.remove('active');
        }
    });

    // Setup Initial bouncing animation for all dock items
    dockInner.addEventListener('click', (e) => {
        const item = e.target.closest('.dock-item');
        if (item && !e.target.closest('button')) {
            item.style.transform = 'scale(1.2) translateY(-20px)';
            setTimeout(() => {
                item.style.transform = '';
            }, 200);
        }
    });

    // Set initial states (both enabled for the demo)
    toggleMidia.checked = true;
    toggleMidia.dispatchEvent(new Event('change'));
    toggleWapp.checked = true;
    toggleWapp.dispatchEvent(new Event('change'));
    toggleRgb.checked = true;
});
