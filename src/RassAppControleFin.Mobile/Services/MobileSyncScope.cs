using RassAppControleFin.Synchronization;

namespace RassAppControleFin.Mobile.Services;

public sealed class MobileSyncScope : ISyncScope
{
    public Guid? TenantId { get; private set; }
    public string? UserId { get; private set; }
    public Guid? DeviceId { get; private set; }
    public bool IsReady => TenantId is not null && !string.IsNullOrWhiteSpace(UserId) && DeviceId is not null;

    public async Task SetAsync(Guid tenantId, string userId)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("Sync scope required.");
        var stored = await SecureStorage.Default.GetAsync("sync-device-id");
        if (!Guid.TryParse(stored, out var deviceId))
        {
            deviceId = Guid.NewGuid();
            await SecureStorage.Default.SetAsync("sync-device-id", deviceId.ToString());
        }
        TenantId = tenantId;
        UserId = userId;
        DeviceId = deviceId;
    }

    public void Clear() { TenantId = null; UserId = null; DeviceId = null; }
}
