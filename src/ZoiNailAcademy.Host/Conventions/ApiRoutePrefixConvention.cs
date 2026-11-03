using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ZoiNailAcademy.Host.Conventions;

/// <summary>
/// Prefixes every controller attribute route with <c>api/</c>, so no controller has to declare it
/// (and none can forget it). Absolute action routes (starting with <c>/</c>) are left untouched.
/// </summary>
public sealed class ApiRoutePrefixConvention : IApplicationModelConvention
{
    public const string Prefix = "api";

    private readonly AttributeRouteModel _prefix = new(new Microsoft.AspNetCore.Mvc.RouteAttribute(Prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var controllerRoutes = controller.Selectors.Where(s => s.AttributeRouteModel is not null).ToList();
            if (controllerRoutes.Count > 0)
            {
                foreach (var selector in controllerRoutes)
                {
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel);
                }

                continue;
            }

            foreach (var selector in controller.Actions.SelectMany(a => a.Selectors).Where(s => s.AttributeRouteModel is not null))
            {
                selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel);
            }
        }
    }
}
