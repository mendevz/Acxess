document.addEventListener("alpine:init", ()=> {
    Alpine.data('promotionsForm', (model) => (
        {
            form: model,
            initialState: null,
            init() {
                this.initialState = JSON.stringify(this.form);
            },
            get isDirty(){
                return JSON.stringify(this.form) !== this.initialState;
            }
        }
    ))
    
    Alpine.data('promotionsApp', (model) => (
        {
            selectedId: null,
            loadedId: null,
            isLoading: false
        }
    ))
})