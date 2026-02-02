using Aims.Wpf.Services;
using System.Net.Http;
using System.Windows;

namespace Aims.Wpf;

public partial class MainWindow
{
    private readonly ITokenStore _tokenStore = new TokenStore();
    private ApiClient _api = null!;
    private AuthService _auth = null!;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _tokenStore.LoadAsync();

        _api = new ApiClient(_tokenStore);
        _auth = new AuthService(_api, _tokenStore);

        AppendLog($"BaseUrl: {_api.Http.BaseAddress}");
        UpdateAuthUi();
    }

    private void UpdateAuthUi()
    {
        AuthStatusText.Text = _tokenStore.HasToken
            ? "Authenticated (token loaded)"
            : "Not authenticated";
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;

        try
        {
            var email = EmailBox.Text;
            var password = PasswordBox.Password;
            var remember = RememberMeCheckBox.IsChecked == true;

            var result = await _auth.LoginAsync(email, password, remember);

            AuthStatusText.Text = $"Login OK (role: {result.role})";
            AppendLog("Login OK");
        }
        catch (Exception ex)
        {
            AuthStatusText.Text = "Login failed";
            AppendLog("Login failed: " + ex.Message);
        }
        finally
        {
            LoginButton.IsEnabled = true;
            UpdateAuthUi();
        }
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        LogoutButton.IsEnabled = false;

        try
        {
            await _auth.LogoutAsync();
            AuthStatusText.Text = "Logged out";
            AppendLog("Logout OK");
        }
        catch (Exception ex)
        {
            AppendLog("Logout failed: " + ex.Message);
        }
        finally
        {
            LogoutButton.IsEnabled = true;
            UpdateAuthUi();
        }
    }

    private async void PingButton_Click(object sender, RoutedEventArgs e)
    {
        PingButton.IsEnabled = false;
        ResultText.Text = "Calling...";

        try
        {
            var resp = await _api.Http.GetAsync("api/ping");
            var body = await resp.Content.ReadAsStringAsync();

            ResultText.Text = $"{(int)resp.StatusCode} {resp.StatusCode}";
            AppendLog($"GET /api/ping -> {ResultText.Text}");
            AppendLog(body);
        }
        catch (HttpRequestException ex)
        {
            ResultText.Text = "HTTP error";
            AppendLog("HTTP error: " + ex.Message);
        }
        catch (Exception ex)
        {
            ResultText.Text = "Error";
            AppendLog("Error: " + ex);
        }
        finally
        {
            PingButton.IsEnabled = true;
        }
    }

    private void AppendLog(string message)
    {
        LogText.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogText.ScrollToEnd();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        RegisterButton.IsEnabled = false;

        try
        {
            var email = RegEmailBox.Text.Trim();
            var password = RegPasswordBox.Password;
            var orgIdText = RegOrgIdBox.Text.Trim();

            if (!Guid.TryParse(orgIdText, out var orgId))
                throw new InvalidOperationException("Invalid OrgId (must be GUID)");

            var role = RegRoleCombo.SelectedIndex; // 0/1/2

            var result = await _auth.RegisterAsync(email, password, orgId, role);

            AppendLog($"REGISTER OK -> {result.id} {result.email} role={result.role}");
            ResultText.Text = "Register OK";
        }
        catch (Exception ex)
        {
            AppendLog("Register failed: " + ex.Message);
            ResultText.Text = "Register failed";
        }
        finally
        {
            RegisterButton.IsEnabled = true;
        }
    }

}
