/**
 * FilterState - Unified filter persistence manager
 * Stores filter values in URL query parameters and session storage
 * Allows filters to persist through page navigation and reloads
 */
const FilterState = {
    pageKey: window.location.pathname,

    save(filters) {
        sessionStorage.setItem(`filters_${this.pageKey}`, JSON.stringify(filters));

        const params = new URLSearchParams(window.location.search);

        this.filterKeys.forEach(key => params.delete(key));

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

    filterKeys: ['status', 'type', 'department', 'period', 'search'],

    restore() {
        const urlParams = new URLSearchParams(window.location.search);
        const urlFilters = {};
        let hasFilterParams = false;

        this.filterKeys.forEach(key => {
            if (urlParams.has(key)) {
                urlFilters[key] = urlParams.get(key);
                hasFilterParams = true;
            }
        });

        if (hasFilterParams) {
            sessionStorage.setItem(`filters_${this.pageKey}`, JSON.stringify(urlFilters));
            return urlFilters;
        }

        const saved = sessionStorage.getItem(`filters_${this.pageKey}`);
        if (saved) {
            try {
                const filters = JSON.parse(saved);
                this.save(filters);
                return filters;
            } catch (e) {
                console.warn('Failed to parse saved filters:', e);
                return {};
            }
        }

        return {};
    },

    clear() {
        sessionStorage.removeItem(`filters_${this.pageKey}`);

        const params = new URLSearchParams(window.location.search);
        this.filterKeys.forEach(key => params.delete(key));

        const newUrl = params.toString()
            ? `${window.location.pathname}?${params.toString()}`
            : window.location.pathname;

        history.replaceState(null, '', newUrl);
    },

    get(key) {
        const filters = this.restore();
        return filters[key] || null;
    },

    set(key, value) {
        const filters = this.restore();
        if (value !== null && value !== undefined && value !== '') {
            filters[key] = value;
        } else {
            delete filters[key];
        }
        this.save(filters);
    },

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

    storeReturnUrl() {
        const currentUrl = window.location.pathname + window.location.search;
        sessionStorage.setItem('filterReturnUrl', currentUrl);
    },

    getReturnUrl() {
        return sessionStorage.getItem('filterReturnUrl');
    }
};

window.FilterState = FilterState;
