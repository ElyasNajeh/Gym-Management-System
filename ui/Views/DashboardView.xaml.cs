using System.Windows.Controls;

namespace GymManagementSystem.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(
        MemberService memberService,
        SubscriptionPlanService planService,
        SubscriptionService subscriptionService,
        PaymentService paymentService)
    {
        InitializeComponent();

        List<Payment> payments = paymentService.getAllPayments();

        MembersCount.Text = memberService.getAllMembers().Count.ToString();
        PlansCount.Text = planService.getAllPlans().Count.ToString();
        SubscriptionsCount.Text = subscriptionService
            .getAllSubscriptions()
            .Count(subscription => subscription.EndDate > DateTime.Today)
            .ToString();
        PaymentCount.Text = payments.Count.ToString();
        RevenueTotal.Text = payments.Sum(payment => payment.Amount).ToString("C");
    }
}
