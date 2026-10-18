public class CashPayment : IPaymentMethod
{
    public void Pay(decimal amount)
    {
        Console.WriteLine($"Cash payment of {amount} completed successfully.");
    }
}