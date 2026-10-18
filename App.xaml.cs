using System.Windows;
using System.Windows.Threading;

namespace GymManagementSystem;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += HandleUnhandledException;

        MemberService memberService = new();
        SubscriptionPlanService planService = new();
        SubscriptionService subscriptionService = new(memberService, planService);
        PaymentService paymentService = new(subscriptionService, planService);

        MainWindow = new MainWindow(
            memberService,
            planService,
            subscriptionService,
            paymentService);

        MainWindow.Show();
    }

    private static void HandleUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred.\n\n{e.Exception.Message}",
            "GymFlow",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
