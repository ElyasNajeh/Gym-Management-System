public class BankTransferPayment : IPaymentMethod
{
    public void Pay(decimal amount)
    {
        Console.WriteLine($"Transfer payment of {amount} completed successfully.");
    }
}