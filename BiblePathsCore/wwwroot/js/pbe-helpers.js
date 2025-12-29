// helper to scroll to a verse element by id
function bpScrollToVerse(id) {
    try {
        const el = document.getElementById(id);
        if (!el) return;
        // Smooth scroll to element, offset slightly for fixed headers
        const yOffset = -80; // adjust if you have a fixed header
        const y = el.getBoundingClientRect().top + window.pageYOffset + yOffset;
        window.scrollTo({ top: y, behavior: 'smooth' });
        // Optionally highlight the element briefly
        el.classList.add('bp-highlight');
        setTimeout(() => el.classList.remove('bp-highlight'), 2000);
    } catch (e) {
        console.error(e);
    }
}

// expose function for Blazor JS interop
window.bpScrollToVerse = bpScrollToVerse;
