/**
 * FilterState - Unified filter persistence manager
 * Stores filter values in URL query parameters and session storage
 * Allows filters to persist through page navigation and reloads
 */
const FilterState = {
    // Use pathname as key to isolate filters per page
    pageKey: window.location.pathname,

    /**
     * Save filter values to both URL and session storage
     * @param {Object} filters - Key-value pairs of filter names and values
     */
    save(filters) {
        // Save to session storage (for navigation to other pages and back)
        sessionStorage.setItem(`filters_${this.pageKey}`, JSON.stringify(filters));

        // Update URL query parameters (for refresh/sharing/bookmarking)
        // Preserve existing non-filter params (like 'id')
        const params = new URLSearchParams(window.location.search);

        // Remove old filter params first
        this.filterKeys.forEach(key => params.delete(key));

        // Add new filter params
        Object.entries(filters).forEach(([key, value]) => {
            if (value !== null && value !== undefined && value !== '') {
                params.set(key, value);
            }
        });

        const newUrl = params.toString()
            ? `${window.location.pathname}?${params.toString()}`
            : window.location.pathname;

        history.replaceState(null, '', newUrl);
    },

    // Known filter parameter names (excludes page-specific params like 'id')
    filterKeys: ['teacher', 'subject', 'class', 'room', 'hideNonMatching'],

    /**
     * Restore filter values from URL or session storage
     * URL parameters take precedence over session storage
     * @returns {Object} - Key-value pairs of restored filter values
     */
    restore() {
        // First try URL params (takes precedence - allows sharing/bookmarking)
        const urlParams = new URLSearchParams(window.location.search);

        // Only consider URL params if they contain actual filter keys
        // This prevents page params like ?id=123 from being treated as filters
        const urlFilters = {};
        let hasFilterParams = false;

        this.filterKeys.forEach(key => {
            if (urlParams.has(key)) {
                urlFilters[key] = urlParams.get(key);
                hasFilterParams = true;
            }
        });

        if (hasFilterParams) {
            // Save to session storage for consistency
            sessionStorage.setItem(`filters_${this.pageKey}`, JSON.stringify(urlFilters));
            return urlFilters;
        }

        // Fall back to session storage (for returning from other pages)
        const saved = sessionStorage.getItem(`filters_${this.pageKey}`);
        if (saved) {
            try {
                const filters = JSON.parse(saved);
                // Update URL to reflect restored state (merge with existing params)
                this.save(filters);
                return filters;
            } catch (e) {
                console.warn('Failed to parse saved filters:', e);
                return {};
            }
        }

        return {};
    },

    /**
     * Clear all saved filters for current page
     */
    clear() {
        sessionStorage.removeItem(`filters_${this.pageKey}`);

        // Remove filter params from URL but preserve others (like 'id')
        const params = new URLSearchParams(window.location.search);
        this.filterKeys.forEach(key => params.delete(key));

        const newUrl = params.toString()
            ? `${window.location.pathname}?${params.toString()}`
            : window.location.pathname;

        history.replaceState(null, '', newUrl);
    },

    /**
     * Get a single filter value
     * @param {string} key - Filter name
     * @returns {string|null} - Filter value or null if not set
     */
    get(key) {
        const filters = this.restore();
        return filters[key] || null;
    },

    /**
     * Set a single filter value (merges with existing filters)
     * @param {string} key - Filter name
     * @param {string} value - Filter value
     */
    set(key, value) {
        const filters = this.restore();
        if (value !== null && value !== undefined && value !== '') {
            filters[key] = value;
        } else {
            delete filters[key];
        }
        this.save(filters);
    },

    /**
     * Build a URL with current filters appended as query parameters
     * Useful for "Back" links from CRUD pages
     * @param {string} basePath - The base URL path
     * @returns {string} - URL with filter parameters
     */
    buildReturnUrl(basePath) {
        const saved = sessionStorage.getItem(`filters_${basePath}`);
        if (saved) {
            try {
                const filters = JSON.parse(saved);
                const params = new URLSearchParams();
                Object.entries(filters).forEach(([key, value]) => {
                    if (value !== null && value !== undefined && value !== '') {
                        params.set(key, value);
                    }
                });
                return params.toString() ? `${basePath}?${params.toString()}` : basePath;
            } catch (e) {
                return basePath;
            }
        }
        return basePath;
    },

    /**
     * Store the current page URL with filters for return navigation
     * Call this before navigating away from a filtered page
     */
    storeReturnUrl() {
        const currentUrl = window.location.pathname + window.location.search;
        sessionStorage.setItem('filterReturnUrl', currentUrl);
    },

    /**
     * Get the stored return URL (for back navigation)
     * @returns {string|null} - The stored return URL or null
     */
    getReturnUrl() {
        return sessionStorage.getItem('filterReturnUrl');
    }
};

// Make available globally
window.FilterState = FilterState;
