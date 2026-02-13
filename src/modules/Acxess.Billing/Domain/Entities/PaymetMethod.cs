using Acxess.Shared.Abstractions;

namespace Acxess.Billing.Domain.Entities;

public class PaymentMethod : IMayHaveTenant
{
    public int IdPaymentMethod { get; private set; }
    public int? IdTenant { get; private set; }
    public string Method { get; private set; } = string.Empty;

    private PaymentMethod() { }
    private PaymentMethod(string method, int? idTenant = null)
    {
        IdTenant = idTenant;
        Method = method;
    }

    public static PaymentMethod Create(string method, int? idTenant = null) => new(method, idTenant);
}
