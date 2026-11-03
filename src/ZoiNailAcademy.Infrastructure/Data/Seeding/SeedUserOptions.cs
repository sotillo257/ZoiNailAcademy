namespace ZoiNailAcademy.Infrastructure.Data.Seeding;

/// <summary>Seed settings of one backoffice user (sections <c>Admin</c> and <c>SuperAdmin</c>).</summary>
public class SeedUserOptions
{
    public string? SeedEmail { get; set; }
    public string? SeedPassword { get; set; }
    public string? SeedNombre { get; set; }
}

public class AdminSeedOptions : SeedUserOptions
{
    public const string SectionName = "Admin";
}

public class SuperAdminSeedOptions : SeedUserOptions
{
    public const string SectionName = "SuperAdmin";

    /// <summary>
    /// When true, the Admin password is reset to <c>Admin:SeedPassword</c> at startup. This is the
    /// v1 "forgot my password" mechanism; set it back to false after use.
    /// </summary>
    public bool ResetAdminPassword { get; set; }
}
