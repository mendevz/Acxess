document.addEventListener('alpine:init', () => {
    Alpine.data('sellingPlanForm', (model, tiers) => {
        return {
            form: model,
            initialState: null,
            tiersMap: {},
            units: { 1: "Días", 2: "Meses", 3: "Años" },
            init() {
                if (tiers && tiers.length > 0) {
                    this.tiersMap = tiers.reduce((acc, tier) => {
                        acc[tier.IdAccessTier] = tier.Name;
                        return acc;
                    }, {});
                }
                this.form.AccessTiersIds = this.form.AccessTiersIds || [];
                this.initialState = JSON.stringify(this.form);
            },
            get formattedTiers() {
                if (!this.form.AccessTiersIds || this.form.AccessTiersIds.length === 0) return "";
                return this.form.AccessTiersIds
                    .map(id => this.tiersMap[id])
                    .filter(Boolean)
                    .join(", ");
            },
            get durationLabel() {
                return this.units[this.form.DurationUnit] || 'Unidad';
            },
            get isDirty(){
                return JSON.stringify(this.form) !== this.initialState;
            }
        }
    })
    Alpine.data('sellingPlanApp', (initialData) => {
        return {
            selectedId: null,
            loadedId: null,
            isLoading: false,
            mapDayUnit(value){
                const dict = {1: 'Días', 2: 'Meses', 3: 'Años'}
                return dict[value] || 'Desconocido';
            }
        }
    })
})