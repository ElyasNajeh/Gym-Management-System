namespace GymManagementSystem.UI.ViewModels;

public sealed class SubscriptionRow
{
    public required Subscription Subscription { get; init; }
    public int Id => Subscription.Id;
    public int MemberId => Subscription.MemberId;
    public int PlanId => Subscription.PlanId;
    public required string MemberName { get; init; }
    public required string PlanName { get; init; }
    public decimal PlanPrice { get; init; }
    public DateTime StartDate => Subscription.StartDate;
    public DateTime EndDate => Subscription.EndDate;
    public string Status => EndDate > DateTime.Today ? "Active" : "Expired";
    public string DisplayName => $"#{Id}  ·  {MemberName}  ·  {PlanName}";
}

public sealed class PaymentRow
{
    public required Payment Payment { get; init; }
    public int Id => Payment.Id;
    public int SubscriptionId => Payment.SubscriptionId;
    public decimal Amount => Payment.Amount;
    public string PaymentMethod => Payment.PaymentMethod;
    public DateTime PaymentDate => Payment.PaymentDate;
    public required string MemberName { get; init; }
    public required string PlanName { get; init; }
}

public sealed class MemberFilterOption
{
    public int? MemberId { get; init; }
    public required string Label { get; init; }
}

public sealed class SubscriptionFilterOption
{
    public int? SubscriptionId { get; init; }
    public required string Label { get; init; }
}

public sealed class PaymentMethodOption
{
    public required string Name { get; init; }
    public required Func<IPaymentMethod> Create { get; init; }
}
