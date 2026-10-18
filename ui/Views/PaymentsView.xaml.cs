using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.UI.Dialogs;
using GymManagementSystem.UI.ViewModels;

namespace GymManagementSystem.UI.Views;

public partial class PaymentsView : UserControl
{
    private readonly PaymentService paymentService;
    private readonly SubscriptionService subscriptionService;
    private readonly MemberService memberService;
    private readonly SubscriptionPlanService planService;
    private readonly Action<string, bool> showStatus;

    public PaymentsView(
        PaymentService paymentService,
        SubscriptionService subscriptionService,
        MemberService memberService,
        SubscriptionPlanService planService,
        Action<string, bool> showStatus)
    {
        InitializeComponent();
        this.paymentService = paymentService;
        this.subscriptionService = subscriptionService;
        this.memberService = memberService;
        this.planService = planService;
        this.showStatus = showStatus;
        RefreshSubscriptionFilter();
        RefreshPayments();
    }

    private void AddPayment_Click(object sender, RoutedEventArgs e)
    {
        HashSet<int> paidSubscriptionIds = paymentService.getAllPayments()
            .Select(payment => payment.SubscriptionId)
            .ToHashSet();

        List<SubscriptionRow> unpaidSubscriptions = subscriptionService.getAllSubscriptions()
            .Where(subscription => !paidSubscriptionIds.Contains(subscription.Id))
            .Select(CreateSubscriptionRow)
            .ToList();

        if (unpaidSubscriptions.Count == 0)
        {
            ShowError(subscriptionService.getAllSubscriptions().Count == 0
                ? "Create a subscription before recording a payment."
                : "All existing subscriptions have already been paid.");
            return;
        }

        PaymentDialog dialog = new(unpaidSubscriptions) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true ||
            dialog.SelectedSubscription == null ||
            dialog.SelectedPaymentMethod == null)
        {
            return;
        }

        try
        {
            paymentService.addPayment(
                dialog.SelectedSubscription.Id,
                dialog.SelectedPaymentMethod);

            RefreshSubscriptionFilter(dialog.SelectedSubscription.Id);
            RefreshPayments();
            showStatus($"Payment for subscription #{dialog.SelectedSubscription.Id} was recorded", false);
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
        RefreshPayments();
    }

    private void RefreshSubscriptionFilter(int? selectedSubscriptionId = null)
    {
        if (SubscriptionFilter == null)
        {
            return;
        }

        int? currentSelection = selectedSubscriptionId ??
            (SubscriptionFilter.SelectedItem as SubscriptionFilterOption)?.SubscriptionId;

        List<SubscriptionFilterOption> options = new()
        {
            new SubscriptionFilterOption { Label = "All subscriptions", SubscriptionId = null }
        };

        options.AddRange(subscriptionService.getAllSubscriptions()
            .OrderByDescending(subscription => subscription.Id)
            .Select(subscription =>
            {
                Member? member = memberService.getMemberById(subscription.MemberId);
                return new SubscriptionFilterOption
                {
                    SubscriptionId = subscription.Id,
                    Label = $"Subscription #{subscription.Id} · {member?.Name ?? "Unknown member"}"
                };
            }));

        SubscriptionFilter.ItemsSource = options;
        SubscriptionFilter.SelectedItem = options.FirstOrDefault(option => option.SubscriptionId == currentSelection) ?? options[0];
    }

    private void RefreshPayments()
    {
        if (PaymentsGrid == null)
        {
            return;
        }

        string query = SearchTextBox?.Text.Trim() ?? string.Empty;
        int? subscriptionId = (SubscriptionFilter?.SelectedItem as SubscriptionFilterOption)?.SubscriptionId;

        List<PaymentRow> rows = paymentService.getAllPayments()
            .Select(CreatePaymentRow)
            .Where(row =>
                (!subscriptionId.HasValue || row.SubscriptionId == subscriptionId.Value) &&
                (string.IsNullOrEmpty(query) ||
                 row.Id.ToString().Equals(query, StringComparison.OrdinalIgnoreCase) ||
                 row.SubscriptionId.ToString().Equals(query, StringComparison.OrdinalIgnoreCase) ||
                 row.MemberName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                 row.PaymentMethod.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(row => row.Id)
            .ToList();

        PaymentsGrid.ItemsSource = rows;
        EmptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        bool hasFilter = !string.IsNullOrEmpty(query) || subscriptionId.HasValue;
        EmptyTitle.Text = hasFilter ? "No matching payments" : "No payments yet";
        EmptyMessage.Text = hasFilter
            ? "Adjust the search or subscription filter and try again."
            : "Record payment for an existing subscription.";
    }

    private SubscriptionRow CreateSubscriptionRow(Subscription subscription)
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

    private PaymentRow CreatePaymentRow(Payment payment)
    {
        Subscription? subscription = subscriptionService.getSubscriptionById(payment.SubscriptionId);
        Member? member = subscription == null ? null : memberService.getMemberById(subscription.MemberId);
        SubscriptionPlan? plan = subscription == null ? null : planService.getPlanById(subscription.PlanId);

        return new PaymentRow
        {
            Payment = payment,
            MemberName = member?.Name ?? "Unknown member",
            PlanName = plan?.Name ?? "Unknown plan"
        };
    }

    private void ShowError(string message)
    {
        showStatus(message, true);
        MessageBox.Show(message, "Unable to complete action", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
