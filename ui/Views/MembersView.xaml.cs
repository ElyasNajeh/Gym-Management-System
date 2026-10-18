using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymManagementSystem.UI.Dialogs;

namespace GymManagementSystem.UI.Views;

public partial class MembersView : UserControl
{
    private readonly MemberService memberService;
    private readonly SubscriptionService subscriptionService;
    private readonly Action<string, bool> showStatus;

    public MembersView(
        MemberService memberService,
        SubscriptionService subscriptionService,
        Action<string, bool> showStatus)
    {
        InitializeComponent();
        this.memberService = memberService;
        this.subscriptionService = subscriptionService;
        this.showStatus = showStatus;
        RefreshMembers();
    }

    private void AddMember_Click(object sender, RoutedEventArgs e)
    {
        MemberDialog dialog = new() { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            Member member = new(
                dialog.MemberName,
                dialog.MemberAge,
                dialog.MemberWeight,
                dialog.MemberPhoneNumber);

            memberService.addMember(member);
            RefreshMembers();
            showStatus($"{member.Name} was added successfully", false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ShowError(ex.Message);
        }
    }

    private void EditMember_Click(object sender, RoutedEventArgs e) => EditSelectedMember();

    private void EditSelectedMember()
    {
        if (MembersGrid.SelectedItem is not Member member)
        {
            return;
        }

        MemberDialog dialog = new(member) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            bool updated = memberService.updateMember(
                member.Id,
                dialog.MemberName,
                dialog.MemberAge,
                dialog.MemberWeight,
                dialog.MemberPhoneNumber);

            if (!updated)
            {
                ShowError("Member no longer exists.");
                return;
            }

            RefreshMembers();
            showStatus($"{dialog.MemberName} was updated successfully", false);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ShowError(ex.Message);
        }
    }

    private void DeleteMember_Click(object sender, RoutedEventArgs e)
    {
        if (MembersGrid.SelectedItem is not Member member)
        {
            return;
        }

        if (subscriptionService.getSubscriptionsByMemberId(member.Id).Count > 0)
        {
            ShowError("This member cannot be deleted because subscription records reference them.");
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            $"Delete {member.Name}? This action cannot be undone.",
            "Confirm deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (memberService.deleteMember(member.Id))
        {
            RefreshMembers();
            showStatus($"{member.Name} was deleted", false);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
        RefreshMembers();
    }

    private void RefreshMembers()
    {
        if (MembersGrid == null)
        {
            return;
        }

        string query = SearchTextBox?.Text.Trim() ?? string.Empty;
        List<Member> members = memberService.getAllMembers()
            .Where(member =>
                string.IsNullOrEmpty(query) ||
                member.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                member.PhoneNumber.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                member.Id.ToString().Equals(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        MembersGrid.ItemsSource = members;
        EmptyState.Visibility = members.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = string.IsNullOrEmpty(query) ? "No members yet" : "No matching members";
        EmptyMessage.Text = string.IsNullOrEmpty(query)
            ? "Add your first member to get started."
            : "Try a different name, phone number, or ID.";
    }

    private void MembersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = MembersGrid.SelectedItem != null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private void MembersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (MembersGrid.SelectedItem != null)
        {
            EditSelectedMember();
        }
    }

    private void ShowError(string message)
    {
        showStatus(message, true);
        MessageBox.Show(message, "Unable to complete action", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
