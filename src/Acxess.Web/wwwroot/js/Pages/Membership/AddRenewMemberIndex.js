document.addEventListener('alpine:init', () => {
    Alpine.data('addRenewMemberApp', (inscAddon, preselectedMember) => ({
        mode: 'new', 
        selectedMember: null,
        omitInscription: false,
        customer: {
            Id: 0,
            FirstName: '',
            LastName: '',
            Phone: '',
            Email: '',
            PhotoUrl: '',     
            PhotoBase64: ''  
        },
        inscAddon: inscAddon ,
        visitAddon: null,
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

            const urlParams = new URLSearchParams(window.location.search);
            const modeParam = urlParams.get('mode');
            
            if (preselectedMember) {
                this.setMode('renew');
                this.selectMember(preselectedMember);
                setTimeout(() => document.getElementById('plans-items-list')?.scrollIntoView({behavior: 'smooth'}), 500);
            } else if (modeParam === 'renew') {
                this.setMode('renew');
            }
            else if (modeParam === 'visit') {
                this.setMode('visit');
            } else {
                this.setMode('new');
            }
        },
        registerVisitAddon(addon) {
            this.visitAddon = addon;
            if (this.mode === 'visit') {
                this.addSystemAddon(this.visitAddon);
            }
        },
        setMode(newMode) {
            this.mode = newMode;
            this.resetCustomerForm();

            if (this.mode === 'new') {
                this.omitInscription = false;
                this.addSystemAddon(this.inscAddon);
                document.getElementById("first-name-input")?.focus();
            } else if (this.mode === 'renew') {
                this.removeSystemAddon(this.inscAddon.IdAddOn);
                // document.getElementById("search-member-input")?.focus();
            } else {
                // MODO VISITA
                this.removeSystemAddon(this.inscAddon.IdAddOn);
                if (this.visitAddon) this.addSystemAddon(this.visitAddon);
                document.getElementById("first-name-input")?.focus();
            }
        },
        selectMember(memberData) {
            this.selectedMember = memberData;
            this.customer.Id = memberData.Id;

            this.selectedPlan = null; // Resetear plan para obligar a nueva selección
            this.beneficiaries = [];  // Limpiar beneficiarios previos
            
            this.customer.FirstName = memberData.FirstName;
            this.customer.LastName = memberData.LastName;
            this.customer.Phone = memberData.Phone || '';
            this.customer.Email = memberData.Email || '';

            this.customer.PhotoUrl = memberData.PhotoUrl || '';
            this.customer.PhotoBase64 = '';
            
            const listContainer = document.getElementById('members-list');
            if(listContainer) listContainer.innerHTML = '';

            document.body.dispatchEvent(new CustomEvent('loadRenewalContext', {
                detail: { id: memberData.Id } // Asegúrate de que coincida con la propiedad de tu JSON
            }));
        },
        addSuggestion(member) {
            // Busca el primer renglón que no tenga ID y esté vacío
            const slotIndex = this.beneficiaries.findIndex(b => b.Id === 0 && b.FirstName.trim() === '');

            if (slotIndex !== -1) {
                this.beneficiaries[slotIndex].Id = member.Id;
                this.beneficiaries[slotIndex].FirstName = member.FirstName;
                this.beneficiaries[slotIndex].LastName = member.LastName;
                this.beneficiaries[slotIndex].Phone = member.Phone || '';
                this.beneficiaries[slotIndex].Email = member.Email || '';
            } else {
                alert('Ya no hay espacios vacíos para agregar a este compañero.');
            }
        },
        isSuggestionUsed(id) {
            return this.beneficiaries.some(b => b.Id === id);
        },
        // 3. Selecciona a un usuario desde el buscador de HTMX
        selectBeneficiaryFromSearch(index, member) {
            this.beneficiaries[index].Id = member.Id;
            this.beneficiaries[index].FirstName = member.FirstName;
            this.beneficiaries[index].LastName = member.LastName;
            this.beneficiaries[index].Phone = member.Phone || '';
            this.beneficiaries[index].Email = member.Email || '';

            const dropdown = document.getElementById('search-results-' + index);
            if (dropdown) dropdown.innerHTML = '';
        },
        clearBeneficiary(index) {
            this.beneficiaries[index].Id = 0;
            this.beneficiaries[index].FirstName = '';
            this.beneficiaries[index].LastName = '';
            this.beneficiaries[index].Phone = '';
            this.beneficiaries[index].Email = '';
        },
        resetCustomerForm() {
            this.selectedMember = null;
            this.customer = {
                Id: null,
                FirstName: '',
                LastName: '',
                Phone: '',
                Email: '',
                PhotoUrl: '',    
                PhotoBase64: ''  
            };
            this.selectedPlan = null; // Resetear plan para obligar a nueva selección
            this.beneficiaries = [];  // Limpiar beneficiarios previos
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
        toggleInscription() {
            if (this.omitInscription) {
                this.removeSystemAddon(this.inscAddon.IdAddOn);
            } else {
                this.addSystemAddon(this.inscAddon);
            }
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
            if (this.mode === 'visit') {
                return this.cartAddons.length === 0 || (this.paymentMethod === 'cash' && this.change < 0);
            }

            // Si no es visita, requiere plan y nombre.
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
            const unit = this.selectedPlan.durationSubscriptionUnit || this.selectedPlan.durationSubscriptionUnit; 

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