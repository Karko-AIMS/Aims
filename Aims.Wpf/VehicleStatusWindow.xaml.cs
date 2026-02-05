using Aims.Wpf.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Aims.Wpf;

public partial class VehicleStatusWindow : Window
{
    private readonly ApiClient _api;
    private readonly ITokenStore _tokenStore;
    private readonly AuthService _auth;
    private readonly VehicleService _vehicleService;

    private readonly string? _role;

    private List<VehicleService.VehicleDto> _vehicles = [];

    public bool ClosedByLogout { get; private set; } = false;

    private bool IsAdmin =>
        string.Equals(_role, "InternalAdmin", StringComparison.OrdinalIgnoreCase);

    private bool IsOperator =>
        string.Equals(_role, "Operator", StringComparison.OrdinalIgnoreCase);

    private bool IsOperatorOrViewer =>
        string.Equals(_role, "Operator", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(_role, "Viewer", StringComparison.OrdinalIgnoreCase);

    public VehicleStatusWindow(ApiClient api, ITokenStore tokenStore, string? email, string? role)
    {
        InitializeComponent();

        _api = api;
        _tokenStore = tokenStore;
        _auth = new AuthService(_api, _tokenStore);
        _vehicleService = new VehicleService(_api);

        _role = role;

        var who = string.IsNullOrWhiteSpace(email) ? "unknown" : email;
        var r = string.IsNullOrWhiteSpace(role) ? "unknown" : role;

        WhoText.Text = $"Logged in as: {who} (role: {r})";

        // Admin이면 Register 패널 표시, Vehicle 패널 숨김
        AdminRegisterGroup.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;

        if (IsAdmin)
        {
            // InternalAdmin은 org가 없어서 Vehicle 접근 불가
            VehicleGroup.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Operator/Viewer는 Vehicle 패널 표시
            SetupVehicleUI();
        }

        // Admin이 아니면 Register 버튼 비활성
        if (!IsAdmin)
        {
            RegisterButton.IsEnabled = false;
        }

        // Window 로드 시 Vehicle 목록 로드
        Loaded += async (_, _) =>
        {
            if (IsOperatorOrViewer)
            {
                await LoadVehiclesAsync();
            }
        };
    }

    private void SetupVehicleUI()
    {
        // Only Operators can Create/Edit/Delete/Restore
        if (!IsOperator)
        {
            CreateButton.IsEnabled = false;
            CreateButton.Visibility = Visibility.Collapsed;

            EditButton.IsEnabled = false;
            EditButton.Visibility = Visibility.Collapsed;

            DeleteButton.IsEnabled = false;
            DeleteButton.Visibility = Visibility.Collapsed;

            RestoreButton.IsEnabled = false;
            RestoreButton.Visibility = Visibility.Collapsed;
        }
    }

    // ========== Vehicle CRUD ==========

    private async Task LoadVehiclesAsync()
    {
        RefreshButton.IsEnabled = false;
        VehicleStatusText.Text = "Loading...";

        // Ensure auth header is applied
        _api.ApplyAuthHeader();

        try
        {
            var q = SearchBox.Text.Trim();
            bool? isActive = StatusFilterCombo.SelectedIndex switch
            {
                1 => true,   // Active
                2 => false,  // Inactive
                _ => null    // All
            };

            var result = await _vehicleService.ListAsync(
                string.IsNullOrWhiteSpace(q) ? null : q,
                isActive,
                skip: 0,
                take: 500
            );

            _vehicles = result.items.ToList();

            VehicleGrid.ItemsSource = _vehicles;
            CountText.Text = $"Showing {_vehicles.Count} of {result.total}";
            VehicleStatusText.Text = "";

            UpdateVehicleButtonStates();
        }
        catch (Exception ex)
        {
            VehicleStatusText.Text = $"Load failed: {ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void UpdateVehicleButtonStates()
    {
        if (!IsOperator) return;

        var selected = VehicleGrid.SelectedItem as VehicleService.VehicleDto;
        var hasSelection = selected is not null;

        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection && selected!.isActive;
        RestoreButton.IsEnabled = hasSelection && !selected!.isActive;

        // Show Restore button only when viewing inactive items
        RestoreButton.Visibility = StatusFilterCombo.SelectedIndex == 2
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void VehicleGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVehicleButtonStates();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadVehiclesAsync();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = LoadVehiclesAsync();
        }
    }

    private async void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && IsOperatorOrViewer)
        {
            await LoadVehiclesAsync();
        }
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsOperator) return;

        var dialog = new VehicleEditDialog(_api);
        dialog.Owner = this;

        if (dialog.ShowDialog() == true && dialog.SavedVehicle is not null)
        {
            VehicleStatusText.Text = $"Created: {dialog.SavedVehicle.vehicleCode}";
            await LoadVehiclesAsync();
        }
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsOperator) return;

        var selected = VehicleGrid.SelectedItem as VehicleService.VehicleDto;
        if (selected is null) return;

        var dialog = new VehicleEditDialog(_api, selected);
        dialog.Owner = this;

        if (dialog.ShowDialog() == true && dialog.SavedVehicle is not null)
        {
            VehicleStatusText.Text = $"Updated: {dialog.SavedVehicle.vehicleCode}";
            await LoadVehiclesAsync();
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsOperator) return;

        var selected = VehicleGrid.SelectedItem as VehicleService.VehicleDto;
        if (selected is null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete vehicle '{selected.vehicleCode}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result != MessageBoxResult.Yes) return;

        DeleteButton.IsEnabled = false;
        VehicleStatusText.Text = "Deleting...";

        try
        {
            _api.ApplyAuthHeader();
            await _vehicleService.DeleteAsync(selected.id);
            VehicleStatusText.Text = $"Deleted: {selected.vehicleCode}";
            await LoadVehiclesAsync();
        }
        catch (Exception ex)
        {
            VehicleStatusText.Text = $"Delete failed: {ex.Message}";
        }
        finally
        {
            DeleteButton.IsEnabled = true;
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsOperator) return;

        var selected = VehicleGrid.SelectedItem as VehicleService.VehicleDto;
        if (selected is null) return;

        RestoreButton.IsEnabled = false;
        VehicleStatusText.Text = "Restoring...";

        try
        {
            _api.ApplyAuthHeader();
            await _vehicleService.RestoreAsync(selected.id);
            VehicleStatusText.Text = $"Restored: {selected.vehicleCode}";
            await LoadVehiclesAsync();
        }
        catch (Exception ex)
        {
            VehicleStatusText.Text = $"Restore failed: {ex.Message}";
        }
        finally
        {
            RestoreButton.IsEnabled = true;
        }
    }

    // ========== Admin Register ==========

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

            var regResult = await _auth.RegisterAsync(email, password, orgId, role);

            RegisterStatusText.Text = $"REGISTER OK -> {regResult.email} role={regResult.role} id={regResult.id}";
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

    // ========== Logout / Close ==========

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        LogoutButton.IsEnabled = false;
        VehicleStatusText.Text = "Logging out...";

        try
        {
            await _auth.LogoutAsync();
            ClosedByLogout = true;
            Close();
        }
        catch (Exception ex)
        {
            VehicleStatusText.Text = "Logout failed: " + ex.Message;
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
