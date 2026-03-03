using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Acxess.Web.Utils.Validators;

public class EnsureMinimumElementsAttribute(int minElements) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IList list)
        {
            return ValidationResult.Success; 
        }

        return list.Count < minElements 
            ? new ValidationResult(ErrorMessage ?? $"Debes seleccionar al menos {minElements} opción.")
            : ValidationResult.Success;
    }

}