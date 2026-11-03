namespace ZoiNailAcademy.Application.Authentication.ChangePassword;

/// <summary>Body of <c>POST /api/auth/cambiar-password</c>.</summary>
public sealed record ChangePasswordRequest(string PasswordActual, string PasswordNueva);
