using System.Globalization;
using System.Windows;

namespace GymManagementSystem.UI.Dialogs;

public partial class MemberDialog : Window
{
    public string MemberName => NameTextBox.Text;
    public int MemberAge { get; private set; }
    public double MemberWeight { get; private set; }
    public string MemberPhoneNumber => PhoneTextBox.Text;

    public MemberDialog(Member? member = null)
    {
        InitializeComponent();

        if (member == null)
        {
            return;
        }

        Title = "Edit member";
        TitleText.Text = "Edit member";
        SaveButton.Content = "Save changes";
        SaveButton.Width = 130;

        NameTextBox.Text = member.Name;
        AgeTextBox.Text = member.Age.ToString(CultureInfo.CurrentCulture);
        WeightTextBox.Text = member.Weight.ToString(CultureInfo.CurrentCulture);
        PhoneTextBox.Text = member.PhoneNumber;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(AgeTextBox.Text, out int age))
        {
            ShowError("Age must be a whole number.");
            return;
        }

        if (!double.TryParse(WeightTextBox.Text, out double weight) || !double.IsFinite(weight))
        {
            ShowError("Weight must be a valid number.");
            return;
        }

        try
        {
            Member.validateName(NameTextBox.Text);
            Member.validateAge(age);
            Member.validateWeight(weight);
            Member.validatePhoneNumber(PhoneTextBox.Text);
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
            return;
        }

        MemberAge = age;
        MemberWeight = weight;
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }
}
