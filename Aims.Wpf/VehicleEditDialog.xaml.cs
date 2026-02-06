using Aims.Wpf.Services;
using System.Windows;

namespace Aims.Wpf;

public partial class VehicleEditDialog : Window
{
    private readonly ApiClient _api;
    private readonly VehicleService _vehicleService;
    private readonly VehicleService.VehicleDto? _existingVehicle;
    private readonly bool _isEditMode;

    public VehicleService.VehicleDto? SavedVehicle { get; private set; }

    // Create mode constructor
    public VehicleEditDialog(ApiClient api)
        : this(api, null)
    {
    }

    // Edit mode constructor
    public VehicleEditDialog(ApiClient api, VehicleService.VehicleDto? existingVehicle)
    {
        InitializeComponent();

        _api = api;
        _vehicleService = new VehicleService(api);
        _existingVehicle = existingVehicle;
        _isEditMode = existingVehicle is not null;

        SetupForMode();
    }

    private void SetupForMode()
    {
        if (_isEditMode && _existingVehicle is not null)
        {
            HeaderText.Text = "Edit Vehicle";
            Title = "Edit Vehicle";

            // Populate fields
            VehicleCodeBox.Text = _existingVehicle.vehicleCode;
            VehicleCodeBox.IsEnabled = false; // VehicleCode is immutable
            DisplayNameBox.Text = _existingVehicle.displayName;
            VinBox.Text = _existingVehicle.vin ?? "";
            PlateNumberBox.Text = _existingVehicle.plateNumber ?? "";
            ManufacturerBox.Text = _existingVehicle.manufacturer ?? "";
            ModelBox.Text = _existingVehicle.model ?? "";
            ModelYearBox.Text = _existingVehicle.modelYear?.ToString() ?? "";

            // Show IsActive checkbox in edit mode
            IsActiveLabel.Visibility = Visibility.Visible;
            IsActiveCheckBox.Visibility = Visibility.Visible;
            IsActiveCheckBox.IsChecked = _existingVehicle.isActive;
        }
        else
        {
            HeaderText.Text = "Create Vehicle";
            Title = "Create Vehicle";
        }
    }

    private void ShowError(string message)
    {
        StatusText.Text = message;
        StatusBorder.Visibility = string.IsNullOrEmpty(message)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        ShowError("");

        try
        {
            // Validate required fields
            var vehicleCode = VehicleCodeBox.Text.Trim();
            var displayName = DisplayNameBox.Text.Trim();

            if (!_isEditMode && string.IsNullOrWhiteSpace(vehicleCode))
            {
                ShowError("Vehicle Code is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                ShowError("Display Name is required.");
                return;
            }

            // Parse optional ModelYear
            int? modelYear = null;
            var modelYearText = ModelYearBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(modelYearText))
            {
                if (!int.TryParse(modelYearText, out var parsedYear))
                {
                    ShowError("Model Year must be a valid number.");
                    return;
                }
                modelYear = parsedYear;
            }

            // Get optional fields (empty string -> null)
            string? vin = string.IsNullOrWhiteSpace(VinBox.Text)
                          ? null : VinBox.Text.Trim();
            string? plateNumber = string.IsNullOrWhiteSpace(PlateNumberBox.Text)
                                  ? null : PlateNumberBox.Text.Trim();
            string? manufacturer = string.IsNullOrWhiteSpace(ManufacturerBox.Text)
                                   ? null : ManufacturerBox.Text.Trim();
            string? model = string.IsNullOrWhiteSpace(ModelBox.Text)
                            ? null : ModelBox.Text.Trim();

            // Ensure auth header is applied before API call
            _api.ApplyAuthHeader();

            if (_isEditMode && _existingVehicle is not null)
            {
                // UPDATE
                var updateReq = new VehicleService.UpdateVehicleRequest(
                    displayName,
                    vin,
                    plateNumber,
                    manufacturer,
                    model,
                    modelYear,
                    IsActiveCheckBox.IsChecked
                );

                SavedVehicle = await _vehicleService.UpdateAsync(_existingVehicle.id, updateReq);
            }
            else
            {
                // CREATE
                var createReq = new VehicleService.CreateVehicleRequest(
                    vehicleCode,
                    displayName,
                    vin,
                    plateNumber,
                    manufacturer,
                    model,
                    modelYear
                );

                SavedVehicle = await _vehicleService.CreateAsync(createReq);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
