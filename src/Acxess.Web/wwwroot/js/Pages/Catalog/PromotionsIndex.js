document.addEventListener("alpine:init", ()=> {
    Alpine.data('promotionsForm', (model) => (
        {
            form: model,
            initialState: null,
            init() {
                const fixDate = (dateStr) => {
                    if (!dateStr) return '';

                    if (dateStr.includes('-')) return dateStr.split('T')[0];

                    const match = dateStr.match(/(\d{1,2})\/(\d{1,2})\/(\d{4})/);

                    if (match) {
                        const dia = match[1].padStart(2, '0');
                        const mes = match[2].padStart(2, '0');
                        const anio = match[3];
                        return `${anio}-${mes}-${dia}`; 
                    }

                    return dateStr;
                };

                this.form.AvailableFrom = fixDate(this.form.AvailableFrom);
                this.form.AvailableTo = fixDate(this.form.AvailableTo);
                
                
                this.initialState = JSON.stringify(this.form);

                this.$watch('form.RequiresCoupon', (value) => {
                    if(value) {
                        this.form.AutoApply = false;
                    }
                });
            },
            get isDirty(){
                return JSON.stringify(this.form) !== this.initialState;
            },
            get discountSymbol() {
                return this.form.DiscountType === 1 ? '%' : '$';
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