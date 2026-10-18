using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymManagementSystem.UI.Dialogs;

namespace GymManagementSystem.UI.Views;

public partial class PlansView : UserControl
{
    private readonly SubscriptionPlanService planService;
    private readonly SubscriptionService subscriptionService;
    private readonly Action<string, bool> showStatus;

    public PlansView(
        SubscriptionPlanService planService,
        SubscriptionService subscriptionService,
        Action<string, bool> showStatus)
    {
        InitializeComponent();
        this.planService = planService;
        this.subscriptionService = subscriptionService;
        this.showStatus = showStatus;
        RefreshPlans();
    }

    private void AddPlan_Click(object sender, RoutedEventArgs e)
    {
        PlanDialog dialog = new() { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            SubscriptionPlan plan = new(dialog.PlanName, dialog.PlanPrice, dialog.PlanDescription);
            planService.addPlan(plan);
            RefreshPlans();
            showStatus($"{plan.Name} was added successfully", false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ShowError(ex.Message);
        }
    }

    private void EditPlan_Click(object sender, RoutedEventArgs e) => EditSelectedPlan();

    private void EditSelectedPlan()
    {
        if (PlansGrid.SelectedItem is not SubscriptionPlan plan)
        {
            return;
        }

        PlanDialog dialog = new(plan) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            bool updated = planService.updatePlan(
                plan.Id,
                dialog.PlanName,
                dialog.PlanPrice,
                dialog.PlanDescription);

            if (!updated)
            {
                ShowError("Subscription plan no longer exists.");
                return;
            }

            RefreshPlans();
            showStatus($"{dialog.PlanName} was updated successfully", false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ShowError(ex.Message);
        }
    }

    private void DeletePlan_Click(object sender, RoutedEventArgs e)
    {
        if (PlansGrid.SelectedItem is not SubscriptionPlan plan)
        {
            return;
        }

        if (subscriptionService.getAllSubscriptions().Any(subscription => subscription.PlanId == plan.Id))
        {
            ShowError("This plan cannot be deleted because subscription records reference it.");
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            $"Delete the {plan.Name} plan? This action cannot be undone.",
            "Confirm deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (planService.deletePlan(plan.Id))
        {
            RefreshPlans();
            showStatus($"{plan.Name} was deleted", false);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = string.IsNullOrEmpty(SearchTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        RefreshPlans();
    }

    private void RefreshPlans()
    {
        if (PlansGrid == null)
        {
            return;
        }

        string query = SearchTextBox?.Text.Trim() ?? string.Empty;
        List<SubscriptionPlan> plans = planService.getAllPlans()
            .Where(plan =>
                string.IsNullOrEmpty(query) ||
                plan.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                plan.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                plan.Id.ToString().Equals(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        PlansGrid.ItemsSource = plans;
        EmptyState.Visibility = plans.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = string.IsNullOrEmpty(query) ? "No plans yet" : "No matching plans";
        EmptyMessage.Text = string.IsNullOrEmpty(query)
            ? "Create a subscription plan to continue."
            : "Try a different name, description, or ID.";
    }

    private void PlansGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = PlansGrid.SelectedItem != null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private void PlansGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PlansGrid.SelectedItem != null)
        {
            EditSelectedPlan();
        }
    }

    private void ShowError(string message)
    {
        showStatus(message, true);
        MessageBox.Show(message, "Unable to complete action", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
