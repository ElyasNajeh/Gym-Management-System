using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.UI.Dialogs;
using GymManagementSystem.UI.ViewModels;

namespace GymManagementSystem.UI.Views;

public partial class SubscriptionsView : UserControl
{
    private readonly SubscriptionService subscriptionService;
    private readonly MemberService memberService;
    private readonly SubscriptionPlanService planService;
    private readonly Action<string, bool> showStatus;

    public SubscriptionsView(
        SubscriptionService subscriptionService,
        MemberService memberService,
        SubscriptionPlanService planService,
        Action<string, bool> showStatus)
    {
        InitializeComponent();
        this.subscriptionService = subscriptionService;
        this.memberService = memberService;
        this.planService = planService;
        this.showStatus = showStatus;
        RefreshMemberFilter();
        RefreshSubscriptions();
    }

    private void AddSubscription_Click(object sender, RoutedEventArgs e)
    {
        List<Member> members = memberService.getAllMembers();
        List<SubscriptionPlan> plans = planService.getAllPlans();

        if (members.Count == 0 || plans.Count == 0)
        {
            string message = members.Count == 0
                ? "Add at least one member before creating a subscription."
                : "Add at least one subscription plan before creating a subscription.";
            ShowError(message);
            return;
        }

        SubscriptionDialog dialog = new(members, plans) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true || dialog.SelectedMember == null || dialog.SelectedPlan == null)
        {
            return;
        }

        try
        {
            Subscription subscription = new(dialog.SelectedMember.Id, dialog.SelectedPlan.Id);
            subscriptionService.addSubscription(subscription);
            RefreshMemberFilter(dialog.SelectedMember.Id);
            RefreshSubscriptions();
            showStatus($"Subscription #{subscription.Id} was created successfully", false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ShowError(ex.Message);
        }
    }

    private void FilterChanged(object sender, EventArgs e)
    {
        if (SearchTextBox != null)
        {
            SearchHint.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        RefreshSubscriptions();
    }

    private void RefreshMemberFilter(int? selectedMemberId = null)
    {
        if (MemberFilter == null)
        {
            return;
        }

        int? currentSelection = selectedMemberId ?? (MemberFilter.SelectedItem as MemberFilterOption)?.MemberId;
        List<MemberFilterOption> options = new()
        {
            new MemberFilterOption { Label = "All members", MemberId = null }
        };
        options.AddRange(memberService.getAllMembers()
            .OrderBy(member => member.Name)
            .Select(member => new MemberFilterOption { Label = member.Name, MemberId = member.Id }));

        MemberFilter.ItemsSource = options;
        MemberFilter.SelectedItem = options.FirstOrDefault(option => option.MemberId == currentSelection) ?? options[0];
    }

    private void RefreshSubscriptions()
    {
        if (SubscriptionsGrid == null)
        {
            return;
        }

        string query = SearchTextBox?.Text.Trim() ?? string.Empty;
        int? memberId = (MemberFilter?.SelectedItem as MemberFilterOption)?.MemberId;

        List<SubscriptionRow> rows = subscriptionService.getAllSubscriptions()
            .Select(CreateRow)
            .Where(row =>
                (!memberId.HasValue || row.MemberId == memberId.Value) &&
                (string.IsNullOrEmpty(query) ||
                 row.Id.ToString().Equals(query, StringComparison.OrdinalIgnoreCase) ||
                 row.MemberName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                 row.PlanName.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(row => row.Id)
            .ToList();

        SubscriptionsGrid.ItemsSource = rows;
        EmptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        bool hasFilter = !string.IsNullOrEmpty(query) || memberId.HasValue;
        EmptyTitle.Text = hasFilter ? "No matching subscriptions" : "No subscriptions yet";
        EmptyMessage.Text = hasFilter
            ? "Adjust the search or member filter and try again."
            : "Create a subscription after adding a member and plan.";
    }

    private SubscriptionRow CreateRow(Subscription subscription)
    {
        Member? member = memberService.getMemberById(subscription.MemberId);
        SubscriptionPlan? plan = planService.getPlanById(subscription.PlanId);

        return new SubscriptionRow
        {
            Subscription = subscription,
            MemberName = member?.Name ?? "Unknown member",
            PlanName = plan?.Name ?? "Unknown plan",
            PlanPrice = plan?.Price ?? 0
        };
    }

    private void ShowError(string message)
    {
        showStatus(message, true);
        MessageBox.Show(message, "Unable to complete action", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
