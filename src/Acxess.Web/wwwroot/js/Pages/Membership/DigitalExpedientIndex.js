document.addEventListener('alpine:init', () => {
    Alpine.data('digitalExpedientApp', (initialId) => ({
        selectedMemberId: initialId,
        selectMember(id) {
            this.selectedMemberId = id;
        },
        statusFilter: new URLSearchParams(window.location.search).get('StatusFilter') || 'all',
        setFilter(filter) {
            this.statusFilter = filter;
            this.$nextTick(() => {
                document.body.dispatchEvent(new Event('reloadMembersList'));
            });
        }
    }))
});