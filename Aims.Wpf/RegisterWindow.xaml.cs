using Aims.Wpf.Services;
using System.Windows;

namespace Aims.Wpf;

public partial class RegisterWindow : Window
{
    private readonly AuthService _auth;

    public RegisterWindow(ApiClient api, ITokenStore tokenStore)
    {
        InitializeComponent();
        _auth = new AuthService(api, tokenStore);
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        RegisterButton.IsEnabled = false;
        StatusText.Text = "";

        try
        {
            var email = RegEmailBox.Text.Trim();
            var password = RegPasswordBox.Password;
            var orgIdText = RegOrgIdBox.Text.Trim();

            if (!Guid.TryParse(orgIdText, out var orgId))
                throw new InvalidOperationException("Invalid OrgId (must be GUID)");

            var role = RegRoleCombo.SelectedIndex; // 0/1/2

            var result = await _auth.RegisterAsync(email, password, orgId, role);

            StatusText.Text = $"REGISTER OK -> {result.email} role={result.role}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Register failed: " + ex.Message;
        }
        finally
        {
            RegisterButton.IsEnabled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
