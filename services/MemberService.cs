public class MemberService
{
    private readonly List<Member> members = new();

    public void addMember(Member member)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }
        if (members.Any(p => p.PhoneNumber.Equals(member.PhoneNumber)))
        {
            throw new InvalidOperationException(
                   "A Phone Number already exists");
        }
        members.Add(member);
    }
    public List<Member> getAllMembers()
    {
        return members.ToList();
    }
    public Member? getMemberById(int id)
    {
        return members.FirstOrDefault(p => p.Id == id);
    }
    public bool updateMember(int id, string name, int age, double weight, string phoneNumber)
    {
        Member? member = getMemberById(id);
        if (member == null)
        {
            return false;
        }
        if (members.Any(p => p.Id != id && p.PhoneNumber.Equals(phoneNumber)))
        {
            throw new InvalidOperationException(
    "A Member with this Phone already exists");
        }

        Member.validateName(name);
        Member.validateAge(age);
        Member.validateWeight(weight);
        Member.validatePhoneNumber(phoneNumber);

        member.Name = name;
        member.Age = age;
        member.Weight = weight;
        member.PhoneNumber = phoneNumber;
        return true;
    }
    public bool deleteMember(int id)
    {
        Member? member = getMemberById(id);
        if (member == null)
        {
            return false;
        }
        return members.Remove(member);
    }
}