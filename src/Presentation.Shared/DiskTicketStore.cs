using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace RassAppControleFin.Presentation;

public sealed class DiskTicketStore(string directory, IDataProtectionProvider protection) : ITicketStore
{
    private readonly IDataProtector _protector = protection.CreateProtector("RassAppControleFin.ServerTickets.v1");
    public const string KeyClaim = "app-ticket";
    public const string SessionClaim = "app-session";
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        ((ClaimsIdentity)ticket.Principal.Identity!).AddClaim(new(KeyClaim, key));
        await RenewAsync(key, ticket);
        return key;
    }
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(directory);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, _protector.Protect(TicketSerializer.Default.Serialize(ticket)));
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var path = PathFor(key);
        if (!File.Exists(path)) return null;
        try
        {
            var ticket = TicketSerializer.Default.Deserialize(_protector.Unprotect(await File.ReadAllBytesAsync(path)));
            if (ticket?.Properties.ExpiresUtc <= DateTimeOffset.UtcNow) { await RemoveAsync(key); return null; }
            return ticket;
        }
        catch (Exception e) when (e is CryptographicException or IOException) { return null; }
    }
    public Task RemoveAsync(string key)
    { File.Delete(PathFor(key)); return Task.CompletedTask; }
    private string PathFor(string key)
    {
        if (key.Length != 64 || !key.All(Uri.IsHexDigit)) throw new ArgumentException("Invalid ticket key.");
        return Path.Combine(directory, key + ".ticket");
    }
}
