using Microsoft.AspNetCore.Identity;

namespace MACHTEN.Infrastructure.Identity;

/// <summary>
/// Application user. Extend with profile fields as needed — keeping a derived
/// type from the start avoids a painful migration later.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>;
