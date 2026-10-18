public class SubscriptionService
{
    private readonly List<Subscription> subscriptions = new();

    private readonly MemberService memberService;
    private readonly SubscriptionPlanService planService;

    public SubscriptionService(
      MemberService memberService,
      SubscriptionPlanService planService)
    {
        this.memberService = memberService;
        this.planService = planService;
    }


    public void addSubscription(Subscription subscription)
    {
        if (subscription == null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }
        if (memberService.getMemberById(subscription.MemberId) == null)
        {
            throw new InvalidOperationException(
                "Member does not exist");
        }

        if (planService.getPlanById(subscription.PlanId) == null)
        {
            throw new InvalidOperationException(
                "Subscription plan does not exist");
        }

        if (subscriptions.Any(s =>
            s.MemberId == subscription.MemberId &&
            s.EndDate > DateTime.Today))
        {
            throw new InvalidOperationException(
                "Member already has an active subscription");
        }

        subscriptions.Add(subscription);
    }
    public List<Subscription> getAllSubscriptions()
    {
        return subscriptions.ToList();
    }
    public Subscription? getSubscriptionById(int id)
    {
        return subscriptions.FirstOrDefault(p => p.Id == id);
    }
    public List<Subscription> getSubscriptionsByMemberId(int memberId)
    {
        return subscriptions
            .Where(s => s.MemberId == memberId)
            .ToList();
    }
}