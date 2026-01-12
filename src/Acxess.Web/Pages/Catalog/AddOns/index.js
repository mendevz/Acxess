document.addEventListener('alpine:init', () => {

    Alpine.data('addOnsApp', () => {

        return {
            selectedId: null, 
            isLoading: false
        }

    })


})