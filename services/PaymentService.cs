
public class PaymentService
{
    private readonly List<Payment> payments = new();

    private readonly SubscriptionService subscriptionService;
    private readonly SubscriptionPlanService planService;

    public PaymentService(
        SubscriptionService subscriptionService,
        SubscriptionPlanService planService)
    {
        this.subscriptionService = subscriptionService;
        this.planService = planService;
    }

    public void addPayment(int subscriptionId, IPaymentMethod paymentMethod)
    {
        if (paymentMethod == null)
        {
            throw new ArgumentNullException(nameof(paymentMethod));
        }

        Subscription? subscription =
            subscriptionService.getSubscriptionById(subscriptionId);

        if (subscription == null)
        {
            throw new InvalidOperationException(
                "Subscription does not exist");
        }

        if (payments.Any(p => p.SubscriptionId == subscriptionId))
        {
            throw new InvalidOperationException(
                "This subscription has already been paid");
        }

        SubscriptionPlan? plan =
            planService.getPlanById(subscription.PlanId);

        if (plan == null)
        {
            throw new InvalidOperationException(
                "Subscription plan does not exist");
        }

        paymentMethod.Pay(plan.Price);

        Payment payment = new Payment(
            subscriptionId,
            plan.Price,
            paymentMethod.GetType().Name
        );

        payments.Add(payment);
    }

    public List<Payment> getAllPayments()
    {
        return payments.ToList();
    }

    public Payment? getPaymentById(int id)
    {
        return payments.FirstOrDefault(p => p.Id == id);
    }

    public List<Payment> getPaymentsBySubscriptionId(int subscriptionId)
    {
        return payments
            .Where(p => p.SubscriptionId == subscriptionId)
            .ToList();
    }
}
