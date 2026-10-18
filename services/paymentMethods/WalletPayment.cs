public class WalletPayment : IPaymentMethod
{
    public void Pay(decimal amount)
    {
        Console.WriteLine($"Wallet payment of {amount} completed successfully.");
    }
}