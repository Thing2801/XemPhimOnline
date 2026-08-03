/* MovieOnline Interactive JavaScript */

document.addEventListener('DOMContentLoaded', () => {
    initHeroSlider();
    initAllAutocompleteSearches();
    initMobileNav();
});

/* ==========================================
   Hero Banner Slider Logic
   ========================================== */
function initHeroSlider() {
    const slides = document.querySelectorAll('.hero-slide');
    const dots = document.querySelectorAll('.slider-dots .dot');
    const btnNext = document.getElementById('heroNextBtn');
    const btnPrev = document.getElementById('heroPrevBtn');

    if (!slides.length) return;

    let currentIndex = 0;
    let autoSlideInterval;

    function showSlide(index) {
        if (index < 0) index = slides.length - 1;
        if (index >= slides.length) index = 0;

        slides.forEach((slide, i) => {
            slide.classList.toggle('active', i === index);
        });

        dots.forEach((dot, i) => {
            dot.classList.toggle('active', i === index);
        });

        currentIndex = index;
    }

    function nextSlide() { showSlide(currentIndex + 1); }
    function prevSlide() { showSlide(currentIndex - 1); }

    function startAutoSlide() {
        stopAutoSlide();
        autoSlideInterval = setInterval(nextSlide, 3000);
    }

    function stopAutoSlide() {
        if (autoSlideInterval) clearInterval(autoSlideInterval);
    }

    if (btnNext) btnNext.addEventListener('click', () => { nextSlide(); startAutoSlide(); });
    if (btnPrev) btnPrev.addEventListener('click', () => { prevSlide(); startAutoSlide(); });

    dots.forEach((dot, i) => {
        dot.addEventListener('click', () => { showSlide(i); startAutoSlide(); });
    });

    // Slider luôn chạy liên tục, không dừng khi hover

    startAutoSlide();
}

/* ==========================================
   Universal Autocomplete Search
   Gắn vào mọi input có data-autocomplete-search
   ========================================== */
function initAllAutocompleteSearches() {
    // Header search (id-based, always present)
    const headerInput  = document.getElementById('headerSearchInput');
    const headerPopup  = document.getElementById('headerSearchPopup');
    if (headerInput && headerPopup) {
        new AutocompleteSearch(headerInput, headerPopup, { redirectOnEnter: true });
    }

    // Any other input tagged with data-autocomplete-search="popup-id"
    document.querySelectorAll('[data-autocomplete-search]').forEach(input => {
        const popupId = input.dataset.autocompleteSearch;
        const popup   = document.getElementById(popupId);
        if (popup) {
            new AutocompleteSearch(input, popup, { redirectOnEnter: false });
        }
    });
}

/**
 * AutocompleteSearch - reusable autocomplete for any search input + popup pair.
 * Options:
 *   apiUrl        {string}  - endpoint (default: /api/movies/quick-search?q=)
 *   minChars      {number}  - min chars before triggering (default: 2)
 *   debounceMs    {number}  - debounce delay in ms (default: 280)
 *   redirectOnEnter {bool}  - navigate to /phim?q=... on Enter (default: true)
 */
class AutocompleteSearch {
    constructor(inputEl, popupEl, options = {}) {
        this.input        = inputEl;
        this.popup        = popupEl;
        this.apiUrl       = options.apiUrl       ?? '/api/movies/quick-search?q=';
        this.minChars     = options.minChars      ?? 2;
        this.debounceMs   = options.debounceMs    ?? 280;
        this.redirect     = options.redirectOnEnter ?? true;

        this._timer       = null;
        this._items       = [];   // current rendered items
        this._focusedIdx  = -1;   // keyboard cursor

        this._bind();
    }

    _bind() {
        this.input.addEventListener('input',   () => this._onInput());
        this.input.addEventListener('keydown', (e) => this._onKeydown(e));
        this.input.addEventListener('focus',   () => {
            if (this.input.value.trim().length >= this.minChars) this._show();
        });

        // Close on outside click
        document.addEventListener('click', (e) => {
            if (!this.input.contains(e.target) && !this.popup.contains(e.target)) {
                this._hide();
            }
        });
    }

    _onInput() {
        const q = this.input.value.trim();
        clearTimeout(this._timer);
        this._focusedIdx = -1;

        if (q.length < this.minChars) {
            this._hide();
            return;
        }

        // Show loading instantly
        this._renderLoading(q);
        this._show();

        this._timer = setTimeout(() => this._fetch(q), this.debounceMs);
    }

    _onKeydown(e) {
        if (!this.popup.classList.contains('active')) return;

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this._moveFocus(1);
                break;
            case 'ArrowUp':
                e.preventDefault();
                this._moveFocus(-1);
                break;
            case 'Enter':
                e.preventDefault();
                if (this._focusedIdx >= 0 && this._items[this._focusedIdx]) {
                    window.location.href = this._items[this._focusedIdx].href;
                } else if (this.redirect && this.input.value.trim()) {
                    window.location.href = `/phim?q=${encodeURIComponent(this.input.value.trim())}`;
                }
                break;
            case 'Escape':
                this._hide();
                this.input.blur();
                break;
        }
    }

    _moveFocus(delta) {
        const links = this.popup.querySelectorAll('.search-result-item');
        if (!links.length) return;

        this._focusedIdx = Math.max(-1, Math.min(links.length - 1, this._focusedIdx + delta));

        links.forEach((el, i) => el.classList.toggle('keyboard-focused', i === this._focusedIdx));

        // Scroll into view
        if (this._focusedIdx >= 0) links[this._focusedIdx].scrollIntoView({ block: 'nearest' });
    }

    async _fetch(query) {
        try {
            const res  = await fetch(`${this.apiUrl}${encodeURIComponent(query)}`);
            const data = await res.json();

            if (data.success && data.data && data.data.length > 0) {
                this._items = data.data.map(m => ({
                    ...m,
                    href: `/phim/${m.slug || m.id}`
                }));
                this._renderResults(query);
            } else {
                this._renderEmpty(query);
            }
        } catch (err) {
            console.warn('[AutocompleteSearch] fetch error:', err);
            this._renderEmpty(query);
        }
    }

    _renderLoading(query) {
        this.popup.innerHTML = `
            <div class="search-popup-header">Đang tìm kiếm "${this._esc(query)}"…</div>
            <div class="search-loading">
                <div class="search-spinner"></div>
                <span>Đang tải kết quả...</span>
            </div>`;
    }

    _renderResults(query) {
        const items = this._items;
        let html = `<div class="search-popup-header">Gợi ý phim — "${this._esc(query)}"</div>`;

        items.forEach(m => {
            const firstGenre = m.genres ? m.genres.split(',')[0].trim() : '';
            html += `
            <a href="${m.href}" class="search-result-item">
                <img src="${this._esc(m.posterUrl)}" class="search-result-img"
                     alt="${this._esc(m.title)}"
                     onerror="this.src='/image/placeholder.jpg'" />
                <div class="search-result-info">
                    <div class="search-result-title">${this._highlight(m.title, query)}</div>
                    <div class="search-result-meta">${m.year} • ${m.originalTitle || m.title}</div>
                    <div class="search-result-badges">
                        <span class="search-badge search-badge-rating">⭐ ${Number(m.rating).toFixed(1)}</span>
                        ${firstGenre ? `<span class="search-badge search-badge-genre">${this._esc(firstGenre)}</span>` : ''}
                    </div>
                </div>
            </a>`;
        });

        // Footer "see all"
        html += `
            <div class="search-popup-footer" onclick="window.location.href='/phim?q=${encodeURIComponent(query)}'">
                <i class="fas fa-search" style="margin-right:5px;font-size:0.75rem"></i>
                Xem tất cả kết quả cho "<strong>${this._esc(query)}</strong>"
            </div>`;

        this.popup.innerHTML = html;
    }

    _renderEmpty(query) {
        this.popup.innerHTML = `
            <div class="search-popup-header">Tìm kiếm "${this._esc(query)}"</div>
            <div class="search-no-results">
                <i class="fas fa-film"></i>
                Không tìm thấy phim phù hợp.<br>
                <small>Hãy thử từ khóa khác hoặc tên gốc.</small>
            </div>
            <div class="search-popup-footer" onclick="window.location.href='/phim?q=${encodeURIComponent(query)}'">
                <i class="fas fa-search" style="margin-right:5px;font-size:0.75rem"></i>
                Tìm kiếm toàn bộ kho phim
            </div>`;
    }

    _show() { this.popup.classList.add('active'); }
    _hide() {
        this.popup.classList.remove('active');
        this._focusedIdx = -1;
        this.popup.querySelectorAll('.keyboard-focused').forEach(el => el.classList.remove('keyboard-focused'));
    }

    /** Escape HTML to prevent XSS */
    _esc(str) {
        if (!str) return '';
        return String(str).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    }

    /** Highlight matching query text in bold red */
    _highlight(text, query) {
        if (!query) return this._esc(text);
        const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        return this._esc(text).replace(
            new RegExp(`(${escaped})`, 'gi'),
            '<mark style="background:transparent;color:var(--primary);font-weight:700">$1</mark>'
        );
    }
}

/* ==========================================
   Mobile Nav Toggle
   ========================================== */
function initMobileNav() {
    const toggleBtn = document.getElementById('mobileNavToggle');
    const navMenu   = document.getElementById('mainNavMenu');

    if (toggleBtn && navMenu) {
        toggleBtn.addEventListener('click', () => {
            navMenu.classList.toggle('active');
        });
    }
}

/* ==========================================
   Movie Trailer Modal & Details Popup
   ========================================== */
function playTrailerModal(trailerUrl, movieTitle) {
    const modal     = document.getElementById('videoTrailerModal');
    const iframe    = document.getElementById('trailerIframe');
    const titleElem = document.getElementById('trailerMovieTitle');

    if (!modal || !iframe) return;

    let embedUrl = trailerUrl;
    if (embedUrl.includes('watch?v=')) {
        embedUrl = embedUrl.replace('watch?v=', 'embed/');
    }

    iframe.src = embedUrl.includes('autoplay') ? embedUrl : `${embedUrl}?autoplay=1`;
    if (titleElem) titleElem.innerText = movieTitle || 'Trailer Phim';

    modal.classList.add('active');
}

function closeTrailerModal() {
    const modal  = document.getElementById('videoTrailerModal');
    const iframe = document.getElementById('trailerIframe');

    if (modal)  modal.classList.remove('active');
    if (iframe) iframe.src = '';
}

// Carousel Scroll Helpers
function scrollCarousel(containerId, direction) {
    const container = document.getElementById(containerId);
    if (!container) return;
    const scrollAmount = direction === 'left' ? -350 : 350;
    container.scrollBy({ left: scrollAmount, behavior: 'smooth' });
}
