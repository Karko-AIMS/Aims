using System.Net.Http.Json;

namespace Aims.Wpf.Services;

public sealed class VehicleService
{
    private readonly ApiClient _api;

    public VehicleService(ApiClient api)
    {
        _api = api;
    }

    // ---------- DTOs ----------

    public sealed record VehicleDto(
        Guid id,
        Guid orgId,
        string vehicleCode,
        string displayName,
        string? vin,
        string? plateNumber,
        string? manufacturer,
        string? model,
        int? modelYear,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc
    );

    public sealed record CreateVehicleRequest(
        string VehicleCode,
        string DisplayName,
        string? Vin,
        string? PlateNumber,
        string? Manufacturer,
        string? Model,
        int? ModelYear
    );

    public sealed record UpdateVehicleRequest(
        string DisplayName,
        string? Vin,
        string? PlateNumber,
        string? Manufacturer,
        string? Model,
        int? ModelYear,
        bool? IsActive
    );

    public sealed record PagedResult<T>(
        IReadOnlyList<T> items,
        int total
    );

    // ---------- LIST ----------

    public async Task<PagedResult<VehicleDto>> ListAsync(
        string? q = null,
        bool? isActive = null,
        int skip = 0,
        int take = 50)
    {
        var query = $"api/vehicles?skip={skip}&take={take}";

        if (!string.IsNullOrWhiteSpace(q))
            query += $"&q={Uri.EscapeDataString(q)}";

        if (isActive.HasValue)
            query += $"&isActive={isActive.Value.ToString().ToLowerInvariant()}";

        var resp = await _api.Http.GetAsync(query);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"List vehicles failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        return await resp.Content.ReadFromJsonAsync<PagedResult<VehicleDto>>()
               ?? new PagedResult<VehicleDto>([], 0);
    }

    // ---------- GET BY ID ----------

    public async Task<VehicleDto?> GetByIdAsync(Guid id)
    {
        var resp = await _api.Http.GetAsync($"api/vehicles/{id}");

        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Get vehicle failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        return await resp.Content.ReadFromJsonAsync<VehicleDto>();
    }

    // ---------- CREATE ----------

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        var resp = await _api.Http.PostAsJsonAsync("api/vehicles", request);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Create vehicle failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        return await resp.Content.ReadFromJsonAsync<VehicleDto>()
               ?? throw new InvalidOperationException("Create response is empty");
    }

    // ---------- UPDATE ----------

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        var resp = await _api.Http.PutAsJsonAsync($"api/vehicles/{id}", request);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Update vehicle failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        return await resp.Content.ReadFromJsonAsync<VehicleDto>()
               ?? throw new InvalidOperationException("Update response is empty");
    }

    // ---------- DELETE (soft) ----------

    public async Task DeleteAsync(Guid id)
    {
        var resp = await _api.Http.DeleteAsync($"api/vehicles/{id}");

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Delete vehicle failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }
    }

    // ---------- RESTORE ----------

    public async Task RestoreAsync(Guid id)
    {
        var resp = await _api.Http.PostAsync($"api/vehicles/{id}/restore", null);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Restore vehicle failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }
    }
}
