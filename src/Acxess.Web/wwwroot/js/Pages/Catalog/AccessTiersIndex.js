document.addEventListener('alpine:init', () => {
    Alpine.data('accessTiersForm', (model) => {
        return {
            form: model,
            initialState: null,
            init() {
                this.initialState = JSON.stringify(this.form);
            },
            get isDirty(){
                return JSON.stringify(this.form) !== this.initialState;
            }
        }
    })
    Alpine.data('accessTiersApp', () => {
        return {
            selectedId: null,
            loadedId: null,
            isLoading: false
        }
    })
})