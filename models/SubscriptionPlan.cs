using System.Text.RegularExpressions;


public class SubscriptionPlan
{
    private static int nextId = 1;
    private string name = null!;
    private decimal price;
    private string description = null!;

    public int Id { get; private set; }

    public string Name
    {
        get
        {
            return name;
        }
        set
        {
            validateName(value);
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
            validatePrice(value);
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
            validateDescription(value);
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
    internal static void validateName(string value)
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
    }
    internal static void validatePrice(decimal value)
    {
        if (value < 100 || value > 1000)
        {
            throw new ArgumentOutOfRangeException("Plan Price muet be between 100 and 1000");
        }
    }
    internal static void validateDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Description Can't be empty");
        }
    }
}
