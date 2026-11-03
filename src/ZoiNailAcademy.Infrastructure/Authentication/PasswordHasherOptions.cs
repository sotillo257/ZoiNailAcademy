namespace ZoiNailAcademy.Infrastructure.Authentication;

public class PasswordHasherOptions
{
    public const string SectionName = "PasswordHasher";

    public int WorkFactor { get; set; } = 11;
}
