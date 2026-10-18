using System.Windows;
using System.Windows.Media;
using GymManagementSystem.Importing;
using GymManagementSystem.UI.Views;

namespace GymManagementSystem;

public partial class MainWindow : Window
{
    private readonly MemberService memberService;
    private readonly SubscriptionPlanService planService;
    private readonly SubscriptionService subscriptionService;
    private readonly PaymentService paymentService;
    private readonly DataImportService importService;

    public MainWindow(
        MemberService memberService,
        SubscriptionPlanService planService,
        SubscriptionService subscriptionService,
        PaymentService paymentService)
    {
        InitializeComponent();

        this.memberService = memberService;
        this.planService = planService;
        this.subscriptionService = subscriptionService;
        this.paymentService = paymentService;
        importService = new DataImportService(memberService, planService);

        ShowDashboard();
    }

    private void DashboardNav_Click(object sender, RoutedEventArgs e) => ShowDashboard();

    private void MembersNav_Click(object sender, RoutedEventArgs e)
    {
        ShowMembers();
    }

    private void PlansNav_Click(object sender, RoutedEventArgs e)
    {
        ShowPlans();
    }

    private void ImportNav_Click(object sender, RoutedEventArgs e)
    {
        PageContent.Content = new ImportView(importService, HandleImportCompleted, ShowStatus);
        ShowStatus("Choose a member or subscription plan file to import", false);
    }

    private void SubscriptionsNav_Click(object sender, RoutedEventArgs e)
    {
        PageContent.Content = new SubscriptionsView(
            subscriptionService,
            memberService,
            planService,
            ShowStatus);
        ShowStatus("Subscriptions loaded", false);
    }

    private void PaymentsNav_Click(object sender, RoutedEventArgs e)
    {
        PageContent.Content = new PaymentsView(
            paymentService,
            subscriptionService,
            memberService,
            planService,
            ShowStatus);
        ShowStatus("Payments loaded", false);
    }

    private void ShowDashboard()
    {
        PageContent.Content = new DashboardView(
            memberService,
            planService,
            subscriptionService,
            paymentService);
        ShowStatus("Ready", false);
    }

    private void ShowMembers()
    {
        PageContent.Content = new MembersView(memberService, subscriptionService, ShowStatus);
        ShowStatus("Members loaded", false);
    }

    private void ShowPlans()
    {
        PageContent.Content = new PlansView(planService, subscriptionService, ShowStatus);
        ShowStatus("Subscription plans loaded", false);
    }

    private void HandleImportCompleted(ImportDataType dataType, int importedCount)
    {
        if (dataType == ImportDataType.Members)
        {
            MembersNav.IsChecked = true;
            ShowMembers();
            ShowStatus($"{importedCount} imported member record(s) are now available", false);
        }
        else
        {
            PlansNav.IsChecked = true;
            ShowPlans();
            ShowStatus($"{importedCount} imported plan record(s) are now available", false);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusDot.Fill = (Brush)FindResource(isError ? "DangerBrush" : "SuccessBrush");
    }
}
