using Aims.Wpf.Services;
using System.Windows;

namespace Aims.Wpf;

public partial class VehicleStatusWindow : Window
{
    private readonly ApiClient _api;
    private readonly ITokenStore _tokenStore;
    private readonly AuthService _auth;

    private readonly string? _role;

    public bool ClosedByLogout { get; private set; } = false;

    private bool IsAdmin =>
        string.Equals(_role, "InternalAdmin", StringComparison.OrdinalIgnoreCase);

    public VehicleStatusWindow(ApiClient api, ITokenStore tokenStore, string? email, string? role)
    {
        InitializeComponent();

        _api = api;
        _tokenStore = tokenStore;
        _auth = new AuthService(_api, _tokenStore);

        _role = role;

        var who = string.IsNullOrWhiteSpace(email) ? "unknown" : email;
        var r = string.IsNullOrWhiteSpace(role) ? "unknown" : role;

        WhoText.Text = $"Logged in as: {who} (role: {r})";
        InfoText.Text = $"BaseUrl: {_api.Http.BaseAddress}";
        StatusText.Text = "";

        // Admin이면 Register 패널 표시
        AdminRegisterGroup.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;

        // Admin이 아니면 UI 상으로 접근 불가하지만, 방어적으로 비활성
        if (!IsAdmin)
        {
            RegisterButton.IsEnabled = false;
        }
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdmin) return;

        RegisterButton.IsEnabled = false;
        RegisterStatusText.Text = "";

        try
        {
            var email = RegEmailBox.Text.Trim();
            var password = RegPasswordBox.Password;
            var orgIdText = RegOrgIdBox.Text.Trim();

            if (!Guid.TryParse(orgIdText, out var orgId))
                throw new InvalidOperationException("Invalid OrgId (must be GUID)");

            var role = RegRoleCombo.SelectedIndex; // 0/1/2

            var result = await _auth.RegisterAsync(email, password, orgId, role);

            RegisterStatusText.Text = $"REGISTER OK -> {result.email} role={result.role} id={result.id}";
        }
        catch (Exception ex)
        {
            RegisterStatusText.Text = "Register failed: " + ex.Message;
        }
        finally
        {
            RegisterButton.IsEnabled = true;
        }
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        LogoutButton.IsEnabled = false;
        StatusText.Text = "Logging out...";

        try
        {
            await _auth.LogoutAsync();
            ClosedByLogout = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Logout failed: " + ex.Message;
        }
        finally
        {
            LogoutButton.IsEnabled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
