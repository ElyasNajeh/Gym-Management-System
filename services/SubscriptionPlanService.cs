public class SubscriptionPlanService
{
    private readonly List<SubscriptionPlan> plans = new();

    public void addPlan(SubscriptionPlan plan)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }
        if (plans.Any(p => p.Name.Equals(plan.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                   "A plan with this name already exists");
        }
        plans.Add(plan);
    }
    public List<SubscriptionPlan> getAllPlans()
    {
        return plans.ToList();
    }
    public SubscriptionPlan? getPlanById(int id)
    {
        return plans.FirstOrDefault(p => p.Id == id);
    }
    public bool updatePlan(int id, string name, decimal price, string description)
    {
        SubscriptionPlan? plan = getPlanById(id);
        if (plan == null)
        {
            return false;
        }
        if (plans.Any(p => p.Id != id && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
    "A plan with this name already exists");
        }

        SubscriptionPlan.validateName(name);
        SubscriptionPlan.validatePrice(price);
        SubscriptionPlan.validateDescription(description);

        plan.Name = name;
        plan.Price = price;
        plan.Description = description;
        return true;
    }
    public bool deletePlan(int id)
    {
        SubscriptionPlan? plan = getPlanById(id);
        if (plan == null)
        {
            return false;
        }
        return plans.Remove(plan);
    }
}


