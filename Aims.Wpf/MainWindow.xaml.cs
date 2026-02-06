using Aims.Wpf.Services;
using System.Net.Http;
using System.Windows;

namespace Aims.Wpf;

public partial class MainWindow : Window
{
    private readonly ITokenStore _tokenStore = new TokenStore();
    private readonly AppSettingsStore _settingsStore = new AppSettingsStore();

    private AppSettingsStore.LocalSettings _settings = new();

    private ApiClient _api = null!;
    private AuthService _auth = null!;

    private string? _currentRole;
    private string? _currentEmail;

    private bool IsAdmin =>
        string.Equals(_currentRole, "InternalAdmin", StringComparison.OrdinalIgnoreCase);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;

        RememberMeCheckBox.Checked += RememberMeCheckBox_Checked;
        RememberMeCheckBox.Unchecked += RememberMeCheckBox_Unchecked;

        AutoLoginCheckBox.Checked += AutoLoginCheckBox_Checked;
        AutoLoginCheckBox.Unchecked += AutoLoginCheckBox_Unchecked;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 1) local settings load
        _settings = await _settingsStore.LoadAsync();

        // RememberMe(Email 기억) 체크 상태 복원 (항상 활성)
        RememberMeCheckBox.IsChecked = _settings.RememberEmail;

        // RememberEmail이면 아이디 자동 입력
        if (_settings.RememberEmail && !string.IsNullOrWhiteSpace(_settings.LastEmail))
        {
            EmailBox.Text = _settings.LastEmail;
        }

        // AutoLogin은 토큰과 무관하게 "사용자 선호"로 체크 상태 유지 (항상 활성)
        AutoLoginCheckBox.IsChecked = _settings.AutoLogin;
        AutoLoginCheckBox.IsEnabled = true;

        // 2) token load
        await _tokenStore.LoadAsync();

        _api = new ApiClient(_tokenStore);
        _auth = new AuthService(_api, _tokenStore);

        AppendLog($"BaseUrl: {_api.Http.BaseAddress}");

        // AutoLogin이 체크되어 있고 token이 있을 때만 자동으로 Vehicle로 진입
        if (_settings.AutoLogin && _tokenStore.HasToken)
        {
            await TryRestoreSessionAsync();

            if (_tokenStore.HasToken && !string.IsNullOrWhiteSpace(_currentRole))
            {
                OpenVehicleStatusWindow();
                return;
            }
        }

        // 로그인 화면에서는 Admin 버튼 기본 숨김 (로그인 성공/세션복원 때만 표시)
        AdminShortcutsGroup.Visibility = Visibility.Collapsed;
    }

    private async Task TryRestoreSessionAsync()
    {
        try
        {
            _api.ApplyAuthHeader();

            var me = await _auth.MeAsync();
            _currentRole = me.role;
            _currentEmail = me.email;

            AppendLog($"Session restored: {_currentEmail} role={_currentRole}");
            SetAdminShortcutVisibility();
        }
        catch (Exception ex)
        {
            // 토큰이 만료/무효면 정리 (AutoLogin 체크 상태는 건드리지 않음)
            AppendLog("Auto login failed -> clearing token: " + ex.Message);
            await _auth.LogoutAsync();
            _currentRole = null;
            _currentEmail = null;
            AdminShortcutsGroup.Visibility = Visibility.Collapsed;
        }
    }

    private void SetAdminShortcutVisibility()
    {
        AdminShortcutsGroup.Visibility = (_tokenStore.HasToken && IsAdmin)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;
        ResultText.Text = "";

        try
        {
            var email = EmailBox.Text.Trim();
            var password = PasswordBox.Password;

            // Remember me 체크 시 token 저장 (기존 요구: Remember me = 다음에 바로 쓰기 위해 토큰 저장)
            var rememberToken = 
                (RememberMeCheckBox.IsChecked == true) || (AutoLoginCheckBox.IsChecked == true);

            var result = await _auth.LoginAsync(email, password, rememberToken);


            _currentRole = result.role;
            _currentEmail = email;

            AppendLog($"Login OK: {_currentEmail} role={_currentRole}");

            // RememberEmail이 켜져 있으면 LastEmail 저장
            if (_settings.RememberEmail)
            {
                _settings.LastEmail = email;
                await _settingsStore.SaveAsync(_settings);
            }

            SetAdminShortcutVisibility();

            // 로그인 성공하면 Vehicle로 이동
            OpenVehicleStatusWindow();
        }
        catch (Exception ex)
        {
            _currentRole = null;
            _currentEmail = null;

            AppendLog("Login failed: " + ex.Message);
            ResultText.Text = "Login failed";
            AdminShortcutsGroup.Visibility = Visibility.Collapsed;
        }
        finally
        {
            LoginButton.IsEnabled = true;
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

    private void OpenRegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdmin) return;

        var win = new RegisterWindow(_api, _tokenStore);
        win.Owner = this;
        win.ShowDialog();
    }

    private void OpenVehicleStatusWindow()
    {
        var win = new VehicleStatusWindow(_api, _tokenStore, _currentEmail, _currentRole);
        win.Owner = this;

        Hide();
        win.Closed += (_, __) =>
        {
            if (win.ClosedByLogout)
            {
                // Logout이면 로그인 창으로 복귀
                Show();
                AdminShortcutsGroup.Visibility = Visibility.Collapsed;
                PasswordBox.Password = "";

                // AutoLogin 체크/활성 상태는 "사용자 선호"이므로 건드리지 않음
                // (token은 Logout으로 삭제되었으니 다음 앱 시작 때 AutoLogin이 체크돼 있어도 자동진입은 안 됨)
                _currentRole = null;
                _currentEmail = null;
            }
            else
            {
                // Close/X = 앱 종료
                Application.Current.Shutdown();
            }
        };

        win.Show();
    }

    // Remember me 체크: "Email 기억" (아이디만)
    private async void RememberMeCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        _settings.RememberEmail = true;

        var email = EmailBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(email))
            _settings.LastEmail = email;

        await _settingsStore.SaveAsync(_settings);
    }

    private async void RememberMeCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        _settings.RememberEmail = false;
        _settings.LastEmail = null;

        await _settingsStore.SaveAsync(_settings);
    }

    // AutoLogin은 토큰과 무관하게 사용자 선호로 저장만 한다
    private async void AutoLoginCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        _settings.AutoLogin = true;
        await _settingsStore.SaveAsync(_settings);
    }

    private async void AutoLoginCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        _settings.AutoLogin = false;
        await _settingsStore.SaveAsync(_settings);
    }

    private void AppendLog(string message)
    {
        LogText.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogText.ScrollToEnd();
    }

    private void EmailBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {

    }
}
