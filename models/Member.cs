using System.Text.RegularExpressions;

public class Member
{
    private static int nextId = 1;
    private string name;
    private int age;
    private double weight;
    private string phoneNumber;

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
                throw new ArgumentException("Name Can't be empty");
            }
            value = value.Trim();
            if (!Regex.IsMatch(value, @"^[\p{L}]+(?: [\p{L}]+)*$"))
            {
                throw new ArgumentException("Name must contain only letters");
            }
            name = value;
        }

    }
    public int Age
    {
        get
        {
            return age;
        }
        set
        {
            if (value < 18 || value > 65)
            {
                throw new ArgumentOutOfRangeException("Age must be between 18 and 65");
            }
            age = value;
        }
    }
    public double Weight
    {
        get
        {
            return weight;
        }
        set
        {
            if (value < 40 || value > 150)
            {
                throw new ArgumentOutOfRangeException("Invalid Weight");
            }
            weight = value;
        }
    }
    public string PhoneNumber
    {
        get
        {
            return phoneNumber;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"^05[69][0-9]{7}$"))
            {
                throw new ArgumentException("Invalid phone number");
            }

            phoneNumber = value;
        }
    }
    public DateTime JoinDate { get; private set; } = DateTime.Today;

    public Member(string name, int age, double weight, string phoneNumber)
    {
        Name = name;
        Age = age;
        Weight = weight;
        PhoneNumber = phoneNumber;

        Id = nextId++;
    }

}