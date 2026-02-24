document.addEventListener('alpine:init', () => {
    Alpine.data('digitalExpedientApp', (initialId) => ({
        selectedMemberId: initialId,
        selectMember(id) {
            this.selectedMemberId = id;
        },
        statusFilter: 'active',
        setFilter(filter) {
            this.statusFilter = filter;
            this.$nextTick(() => {
                document.body.dispatchEvent(new Event('reloadMembersList'));
            });
        }
    }))
});