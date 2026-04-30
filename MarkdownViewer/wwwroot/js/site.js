// highlight.js init
document.addEventListener('DOMContentLoaded', () => {
    if (typeof hljs !== 'undefined') {
        document.querySelectorAll('pre code').forEach(el => hljs.highlightElement(el));
    }

    // TOC active-link tracking via IntersectionObserver
    const headings = document.querySelectorAll('#markdown-content h1,h2,h3,h4');
    const tocLinks = document.querySelectorAll('.toc-link');
    if (headings.length && tocLinks.length) {
        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const id = entry.target.getAttribute('id');
                    tocLinks.forEach(a => {
                        a.classList.toggle('active', a.dataset.anchor === id);
                    });
                }
            });
        }, { rootMargin: '-10% 0px -80% 0px' });
        headings.forEach(h => observer.observe(h));
    }
});
