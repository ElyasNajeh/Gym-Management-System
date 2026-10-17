public class Subscription
{
    private static int nextId = 1;

    public int Id { get; private set; }
    public int MemberId { get; private set; }
    public int PlanId { get; private set; }

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public Subscription(int memberId, int planId)
    {
        if (memberId <= 0)
            throw new ArgumentException("Invalid Member ID");

        if (planId <= 0)
            throw new ArgumentException("Invalid Plan ID");

        MemberId = memberId;
        PlanId = planId;

        StartDate = DateTime.Today;
        EndDate = StartDate.AddYears(1);

        Id = nextId++;
    }
}