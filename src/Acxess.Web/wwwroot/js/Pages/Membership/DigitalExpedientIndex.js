document.addEventListener('alpine:init', () => {
    Alpine.data('digitalExpedientApp', () => ({
        search: '',
        selectedMemberId: null,
        getStatusBadgeClass(status) {
            if (status === 'Activo') return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 border-green-200 dark:border-green-800';
            if (status === 'Vencido') return 'bg-gray-100 dark:bg-slate-700 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-slate-600';
            if (status === 'Eliminado') return 'bg-red-500 text-white border-red-600';
            return '';
        }
    }))
});