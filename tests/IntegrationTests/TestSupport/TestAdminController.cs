using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoiNailAcademy.Host.Authorization;

namespace IntegrationTests.TestSupport;

/// <summary>
/// Test-only backoffice controller (added as an application part by the factory). F01 has no real
/// /api/admin endpoints yet; this one lets the fail-closed tests exercise both policies. The real
/// admin controllers added by later features are picked up by the same tests automatically.
/// </summary>
[ApiController]
[Route("admin/__test")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public class TestAdminController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { ok = true });

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
    public IActionResult Delete(Guid id) => NoContent();
}
