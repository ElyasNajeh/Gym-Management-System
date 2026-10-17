public class Payment
{
    private static int nextId = 1;

    public int Id { get; private set; }
    public int SubscriptionId { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; }
    public DateTime PaymentDate { get; private set; }

    public Payment(int subscriptionId, decimal amount, string paymentMethod)
    {
        if (subscriptionId <= 0)
        {
            throw new ArgumentException("Invalid Subscripton ID");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero"
            );
        }
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            throw new ArgumentException("Invalid Payment Method");
        }

        SubscriptionId = subscriptionId;
        Amount = amount;
        PaymentMethod = paymentMethod.Trim();

        PaymentDate = DateTime.Today;

        Id = nextId++;
    }
}