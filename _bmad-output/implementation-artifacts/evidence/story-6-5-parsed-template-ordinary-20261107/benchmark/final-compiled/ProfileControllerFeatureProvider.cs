using System.Reflection;using Hexalith.EventStore.Contracts.Routing;using Microsoft.AspNetCore.Mvc.ApplicationParts;using Microsoft.AspNetCore.Mvc.Controllers;
internal sealed class ProfileControllerFeatureProvider(Assembly owner) : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts,ControllerFeature feature)
    {
        foreach(var controller in feature.Controllers.Where(c=>c.Assembly!=owner||!c.IsDefined(typeof(EventStoreTechnicalHttpControllerAttribute),false)&&!c.GetMethods().Any(m=>m.IsDefined(typeof(EventStoreTechnicalHttpControllerAttribute),false))).ToArray())feature.Controllers.Remove(controller);
    }
}
