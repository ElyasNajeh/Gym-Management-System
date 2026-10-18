using System.Windows;

namespace GymManagementSystem.UI.Dialogs;

public partial class SubscriptionDialog : Window
{
    public Member? SelectedMember => MemberComboBox.SelectedItem as Member;
    public SubscriptionPlan? SelectedPlan => PlanComboBox.SelectedItem as SubscriptionPlan;

    public SubscriptionDialog(List<Member> members, List<SubscriptionPlan> plans)
    {
        InitializeComponent();
        MemberComboBox.ItemsSource = members;
        PlanComboBox.ItemsSource = plans;

        if (members.Count > 0)
        {
            MemberComboBox.SelectedIndex = 0;
        }

        if (plans.Count > 0)
        {
            PlanComboBox.SelectedIndex = 0;
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMember == null)
        {
            ShowError("Select a member before creating the subscription.");
            return;
        }

        if (SelectedPlan == null)
        {
            ShowError("Select a subscription plan before continuing.");
            return;
        }

        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }
}
