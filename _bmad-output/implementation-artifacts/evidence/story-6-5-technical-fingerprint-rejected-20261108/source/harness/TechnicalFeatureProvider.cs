using System.Reflection;
using Hexalith.EventStore.Contracts.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class TechnicalFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var selected = feature.Controllers.Where(t => (t.Assembly.GetName().Name is "Hexalith.EventStore.Gateway" or "Hexalith.EventStore.Admin.Server")
            && (t.Name == "QueriesController" || t.IsDefined(typeof(EventStoreTechnicalHttpControllerAttribute), false)
                || t.GetMethods().Any(m => m.IsDefined(typeof(EventStoreTechnicalHttpControllerAttribute), false)))).ToArray();
        foreach (var type in feature.Controllers.Where(t => !selected.Contains(t)).ToArray()) feature.Controllers.Remove(type);
    }
}
