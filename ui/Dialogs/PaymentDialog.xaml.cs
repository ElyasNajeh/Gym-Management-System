using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.UI.ViewModels;

namespace GymManagementSystem.UI.Dialogs;

public partial class PaymentDialog : Window
{
    public SubscriptionRow? SelectedSubscription => SubscriptionComboBox.SelectedItem as SubscriptionRow;
    public IPaymentMethod? SelectedPaymentMethod { get; private set; }

    public PaymentDialog(List<SubscriptionRow> subscriptions)
    {
        InitializeComponent();
        SubscriptionComboBox.ItemsSource = subscriptions;
        MethodComboBox.ItemsSource = new List<PaymentMethodOption>
        {
            new() { Name = "Cash", Create = () => new CashPayment() },
            new() { Name = "Card", Create = () => new CardPayment() },
            new() { Name = "Bank transfer", Create = () => new BankTransferPayment() },
            new() { Name = "Wallet", Create = () => new WalletPayment() }
        };

        if (subscriptions.Count > 0)
        {
            SubscriptionComboBox.SelectedIndex = 0;
        }
        MethodComboBox.SelectedIndex = 0;
    }

    private void SubscriptionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AmountText.Text = SelectedSubscription?.PlanPrice.ToString("C") ?? "—";
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSubscription == null)
        {
            ShowError("Select a subscription before recording payment.");
            return;
        }

        if (MethodComboBox.SelectedItem is not PaymentMethodOption method)
        {
            ShowError("Select a payment method before continuing.");
            return;
        }

        SelectedPaymentMethod = method.Create();
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }
}
