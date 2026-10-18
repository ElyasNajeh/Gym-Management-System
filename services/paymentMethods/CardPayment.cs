public class CardPayment : IPaymentMethod
{
    public void Pay(decimal amount)
    {
        Console.WriteLine($"Card payment of {amount} completed successfully.");
    }
}