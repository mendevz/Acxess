using System.ComponentModel.DataAnnotations;

namespace Acxess.Web.Pages.Membership.AddRenewMember;

public class ProcessOrderRequest : IValidatableObject
{

    public const string NEW_MEMBER = "new";
    public const string RENEW_MEMBER = "renew";
    public const string VISIT_MEMBER = "visit";
    
    [Required]
    public string Mode { get; set; } = NEW_MEMBER;
    public AddRenewMemberInput MemberData { get; set; } = new();
    public int? PlanId { get; set; }

    public List<int> AddOnIds { get; set; } = [];
    public List<AddRenewMemberInput> AdditionalBeneficiaries { get; set; } = [];
    public string PaymentMethod { get; set; } = "cash";
    public decimal AmountPaid { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {

        if (PaymentMethod == "cash")
        {
            if (AmountPaid <= 0)
                yield return new ValidationResult("El monto pagado debe ser mayor a 0.");
        }
        
        if (Mode == VISIT_MEMBER)
        {
            if (AddOnIds.Count == 0)
                yield return new ValidationResult("Debes seleccionar al menos un pase de visita o complemento.", new[] { nameof(AddOnIds) });
        }

        
        switch (Mode)
        {
            case NEW_MEMBER:
            {
                if (string.IsNullOrWhiteSpace(MemberData.FirstName))
                    yield return new ValidationResult("El nombre del titular es obligatorio.", new[] { nameof(MemberData.FirstName) });
            
                if (string.IsNullOrWhiteSpace(MemberData.LastName))
                    yield return new ValidationResult("Los apellidos del titular son obligatorios.", new[] { nameof(MemberData.LastName) });
                break;
            }
            case RENEW_MEMBER:
            {
                if (MemberData.Id <= 0)
                    yield return new ValidationResult("Debes seleccionar un miembro existente para renovar.", new[] { nameof(MemberData.Id) });
                break;
            }
        }

        for (var i = 0; i < AdditionalBeneficiaries.Count; i++)
        {
            var ben = AdditionalBeneficiaries[i];
            if (ben.Id != 0) continue;
            if (string.IsNullOrWhiteSpace(ben.FirstName))
                yield return new ValidationResult($"El nombre del beneficiario #{i + 1} es obligatorio.", 
                    new[] { $"AdditionalBeneficiaries[{i}].FirstName" });
        }
    }
}