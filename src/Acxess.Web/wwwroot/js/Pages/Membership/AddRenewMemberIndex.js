document.addEventListener('alpine:init', () => {
    Alpine.data('addRenewMemberApp', (inscAddon, preselectedMember) => ({
        mode: 'new', 
        selectedMember: null, 
        customer: {
            Id: 0,
            FirstName: '',
            LastName: '',
            Phone: '',
            Email: ''
        },
        inscAddon: inscAddon ,
        selectedPlan: null,
        beneficiaries: [],
        selectPlan(plan){
            this.selectedPlan = plan;
            this.beneficiaries = [];
            const extraSlots = plan.totalMembers  - 1;

            if (extraSlots > 0) {
                for (let i = 0; i < extraSlots; i++) {
                    this.beneficiaries.push(
                        {
                            Id: 0,
                            FirstName: '',
                            LastName: '',
                            Phone: '',
                            Email: ''
                        });
                }
            }
        },
        cartAddons: [], 
        discountAmount: 0,
        promoName: '',
        paymentMethod: 'cash',
        amountGiven: '',
        transferRef: '',
        init() {
            if (preselectedMember) {
                this.setMode('renew');
                this.selectMember(preselectedMember);
                setTimeout(() => document.getElementById('plans-items-list')?.scrollIntoView({behavior: 'smooth'}), 500);
            } else {
                this.setMode('new');
            }
        },
        setMode(newMode) {
            this.mode = newMode;
            this.resetCustomerForm();

            if (this.mode === 'new') {
                this.addSystemAddon(this.inscAddon);
                document.getElementById("first-name-input")?.focus();
            } else {
                this.removeSystemAddon(this.inscAddon.IdAddOn);
                document.getElementById("search-member-input")?.focus();
            }
        },
        selectMember(memberData) {
            this.selectedMember = memberData;
            this.customer.Id = memberData.Id;
            this.customer.FirstName = memberData.FirstName;
            this.customer.LastName = memberData.LastName;
            this.customer.Phone = memberData.Phone || '';
            this.customer.Email = memberData.Email || '';
            
            const listContainer = document.getElementById('members-list');
            if(listContainer) listContainer.innerHTML = '';
        },
        resetCustomerForm() {
            this.selectedMember = null;
            this.customer = {
                Id: null,
                FirstName: '',
                LastName: '',
                Phone: '',
                Email: '',
            };
        },
        addSystemAddon(addon) {
            const exists = this.cartAddons.find(a => a.IdAddOn == addon.IdAddOn);
            if (!exists) {
                this.cartAddons.push(addon);
            }
        },
        removeSystemAddon(addonId) {
            this.cartAddons = this.cartAddons.filter(a => a.IdAddOn != addonId);
        },
        resetAll() {
            this.setMode('new');
            this.selectedMember = null;
            this.selectedPlan = null;
            this.beneficiaries = [];
            this.customer = { Id: 0, FirstName: '', LastName: '', Phone: '', Email: '' };
            this.cartAddons = [];
            this.discountAmount = 0;
            this.paymentMethod = 'cash';
            this.amountGiven = '';
            this.promoName = '';
            this.transferRef = '';
        },
        get cannotSubmit() {
            const isFormInvalid = !this.selectedPlan || !this.customer.FirstName || this.customer.FirstName.trim() === '';
            const isMoneyInsufficient = this.paymentMethod === 'cash' && this.change < 0;
            return isFormInvalid || isMoneyInsufficient;
        },
        // COMPUTADOS
        get total() {
            const addonsTotal = this.cartAddons.reduce((sum, item) => sum + item.Price, 0);
            const pricePlan = this.selectedPlan?.price || 0.0;
            return (pricePlan + addonsTotal) - this.discountAmount;
        },

        get change() {
            const given = parseFloat(this.amountGiven) || 0;
            return given - this.total;
        },
        formatMoney(amount) {
            return new Intl.NumberFormat('es-MX', {
                style: 'currency',
                currency: 'MXN'
            }).format(amount);
        },
        get startDateDisplay() {
            if (this.mode === 'renew' && this.selectedMember?.IsSubscriptionActive) {
                const fechaVencimiento = new Date(this.selectedMember.LastExpirationDate);
                return fechaVencimiento;
            }
            return new Date();
        },
        get calculatedEndDate() {
            if (!this.selectedPlan) return null;

            let baseDate = new Date();

            if (this.mode === 'renew' && this.selectedMember?.IsSubscriptionActive) {
                baseDate = new Date(this.selectedMember.LastExpirationDate);
            }

            let finalDate = new Date(baseDate);

            const duration = this.selectedPlan.durationInValue || this.selectedPlan.DurationInValue || 0;
            const unit = this.selectedPlan.durationUnit || this.selectedPlan.DurationUnit; 

            if (unit === 1) { 
                finalDate.setDate(finalDate.getDate() + duration);
            } else if (unit === 2) { 
                finalDate.setMonth(finalDate.getMonth() + duration);
            } else if (unit === 3) { 
                finalDate.setFullYear(finalDate.getFullYear() + duration);
            }

            return finalDate;
        },
        formatDate(date) {
            if (!date) return '---';
            return date.toLocaleDateString('es-MX', {
                day: '2-digit',
                month: 'short',
                year: 'numeric'
            });
        }
    }))
});