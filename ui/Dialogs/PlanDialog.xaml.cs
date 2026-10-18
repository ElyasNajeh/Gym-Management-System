using System.Globalization;
using System.Windows;

namespace GymManagementSystem.UI.Dialogs;

public partial class PlanDialog : Window
{
    public string PlanName => NameTextBox.Text;
    public decimal PlanPrice { get; private set; }
    public string PlanDescription => DescriptionTextBox.Text;

    public PlanDialog(SubscriptionPlan? plan = null)
    {
        InitializeComponent();

        if (plan == null)
        {
            return;
        }

        Title = "Edit subscription plan";
        TitleText.Text = "Edit subscription plan";
        SaveButton.Content = "Save changes";
        SaveButton.Width = 130;
        NameTextBox.Text = plan.Name;
        PriceTextBox.Text = plan.Price.ToString(CultureInfo.CurrentCulture);
        DescriptionTextBox.Text = plan.Description;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(PriceTextBox.Text, out decimal price))
        {
            ShowError("Price must be a valid number.");
            return;
        }

        try
        {
            SubscriptionPlan.validateName(NameTextBox.Text);
            SubscriptionPlan.validatePrice(price);
            SubscriptionPlan.validateDescription(DescriptionTextBox.Text);
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
            return;
        }

        PlanPrice = price;
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }
}
