using System.Text.RegularExpressions;


public class SubscriptionPlan
{
    private static int nextId = 1;
    private string name;
    private decimal price;
    private string description;

    public int Id { get; private set; }

    public string Name
    {
        get
        {
            return name;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Plan name Can't be empty");
            }
            value = value.Trim();
            if (!Regex.IsMatch(value, @"^[\p{L}]+(?: [\p{L}]+)*$"))
            {
                throw new ArgumentException("Name must contain only letters");
            }
            name = value;
        }
    }
    public decimal Price
    {
        get
        {
            return price;
        }
        set
        {
            if (value < 100 || value > 1000)
            {
                throw new ArgumentOutOfRangeException("Plan Price muet be between 100 and 1000");
            }
            price = value;
        }
    }
    public string Description
    {
        get
        {
            return description;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Description Can't be empty");
            }
            description = value.Trim();
        }
    }
    public SubscriptionPlan(string name, decimal price, string description)
    {
        Name = name;
        Price = price;
        Description = description;

        Id = nextId++;
    }
}