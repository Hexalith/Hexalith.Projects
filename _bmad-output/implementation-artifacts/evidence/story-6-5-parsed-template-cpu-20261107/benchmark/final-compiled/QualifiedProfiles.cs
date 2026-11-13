using System.Reflection;using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;using Hexalith.EventStore.ServiceDefaults.Authentication;using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Builder;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.Controllers;using Microsoft.AspNetCore.Routing;using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;using Microsoft.Extensions.Hosting;using Microsoft.Extensions.Logging;using NSubstitute;
internal static class QualifiedProfiles
{
    internal static async Task<GrantCorpus[]> CreateAsync()
    {
        var result=new List<GrantCorpus>();
        foreach(var assembly in new[]{typeof(Hexalith.EventStore.Controllers.AdminStorageCommandController).Assembly,typeof(Hexalith.EventStore.Admin.Server.Controllers.AdminTenantsController).Assembly})
        {
            var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=[],EnvironmentName="Development",ContentRootPath=AppContext.BaseDirectory});
            builder.Logging.ClearProviders();builder.Host.UseDefaultServiceProvider(o=>o.ValidateOnBuild=false);
            builder.Services.AddEventStoreHttpHostSecurity();
            builder.Services.AddAuthorization(o=>
            {
                o.AddPolicy("AdminReadOnly",new AuthorizationPolicyBuilder(EventStoreHttpHostSecurityExtensions.CreateHumanPolicy()).RequireClaim("eventstore:admin-role","Admin","Operator","ReadOnly").Build());
                o.AddPolicy("AdminOperator",new AuthorizationPolicyBuilder(EventStoreHttpHostSecurityExtensions.CreateHumanPolicy()).RequireClaim("eventstore:admin-role","Admin","Operator").Build());
                o.AddPolicy("AdminFull",new AuthorizationPolicyBuilder(EventStoreHttpHostSecurityExtensions.CreateHumanPolicy()).RequireClaim("eventstore:admin-role","Admin").Build());
            });
            var operations=assembly.GetTypes().Where(t=>typeof(ControllerBase).IsAssignableFrom(t)).SelectMany(t=>t.GetCustomAttributes<AuthorizeAttribute>().Concat(t.GetMethods().SelectMany(m=>m.GetCustomAttributes<AuthorizeAttribute>())))
                .Select(a=>a.Policy).Where(p=>p?.StartsWith("DaprInternal:",StringComparison.Ordinal)==true).Select(p=>p!["DaprInternal:".Length..]).Distinct().ToArray();
            builder.Services.AddAuthentication().AddEventStoreWorkloadScheme("DaprInternal","Authentication:Workload",(o,_)=>{o.Audience="metadata-only";o.AllowedCallers=["metadata-only"];});
            builder.Services.AddEventStoreWorkloadPolicies("DaprInternal",operations);
            foreach(var filter in assembly.GetTypes().Where(t=>typeof(ControllerBase).IsAssignableFrom(t)).SelectMany(t=>t.GetCustomAttributes<ServiceFilterAttribute>().Concat(t.GetMethods().SelectMany(m=>m.GetCustomAttributes<ServiceFilterAttribute>()))).Select(a=>a.ServiceType).Distinct())builder.Services.AddScoped(filter);
            var authority=Substitute.For<IRouteCatalogAuthority>();builder.Services.AddSingleton(authority);builder.Services.AddScoped<GeneratedRouteCatalogFilter>();
            builder.Services.AddControllers(o=>o.Filters.AddService<GeneratedRouteCatalogFilter>()).AddApplicationPart(assembly).ConfigureApplicationPartManager(m=>m.FeatureProviders.Add(new ProfileControllerFeatureProvider(assembly)));
            await using var app=builder.Build(); // Metadata only: never Start/Run; no controller/authority method is invoked.
            app.MapControllers();
            var pass=new TechnicalHttpObservationPass(app.Services);var accepted=new List<RouteCatalogTechnicalHttpEndpoint>();var refused=new List<object>();
            foreach(var endpoint in ((IEndpointRouteBuilder)app).DataSources.SelectMany(s=>s.Endpoints).OfType<RouteEndpoint>())
            {
                var action=endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();if(action is null||!TechnicalHttpActionObservation.IsMarked(action))continue;
                var description=TechnicalHttpActionObservation.Describe(endpoint,app.Services,pass);
                if(description is not null)accepted.Add(description);
                else
                {
                    string? failure=null;try{TechnicalHttpActionObservation.Describe(endpoint,app.Services,pass,required:true);}catch(Exception e){failure=e.GetType().FullName+":"+e.Message;}
                    refused.Add(new{pattern=endpoint.RoutePattern.RawText,controller=action.ControllerTypeInfo.FullName,action=action.MethodInfo.Name,failure});
                }
            }
            pass.Complete();if(authority.ReceivedCalls().Any())throw new Exception("Metadata qualification invoked authority");
            var endpoints=accepted.OrderBy(e=>e.Pattern,StringComparer.Ordinal).ThenBy(e=>string.Join(',',e.Methods),StringComparer.Ordinal).ToArray();
            if(endpoints.Length==0)throw new Exception("No actual Describe-qualified endpoints: "+assembly.GetName().Name+" "+JsonSerializer.Serialize(refused));
            var raw=endpoints.Select(e=>new RouteCatalogDeclaredInvocation("generated-static-profile-caller","/"+Concrete(e.Pattern),e.Methods.ToArray())).ToList();
            // Group identical concrete paths/verbs as a real configuration would, without duplicate static route rows.
            var invocations=raw.GroupBy(i=>i.Method,StringComparer.Ordinal).Select(g=>new RouteCatalogDeclaredInvocation(g.First().CallerAppId,g.Key,g.SelectMany(i=>i.HttpMethods).Distinct(StringComparer.Ordinal).ToArray())).ToArray();
            result.Add(new(assembly.GetName().Name!,endpoints,invocations,JsonSerializer.Serialize(invocations),refused.ToArray()));
        }
        return result.ToArray();
    }
    private static string Concrete(string template)
    {
        var p=RoutePatternFactory.Parse(template.TrimStart('/'));
        return string.Join('/',p.PathSegments.Select(s=>string.Concat(s.Parts.Select(part=>part switch{RoutePatternLiteralPart literal=>literal.Content,RoutePatternSeparatorPart separator=>separator.Content,RoutePatternParameterPart parameter=>Value(parameter),_=>throw new Exception("Unknown parsed part")}))));
    }
    private static string Value(RoutePatternParameterPart p)
    {
        if(p.Default is not null)return p.Default.ToString()!;
        string policy=string.Join(':',p.ParameterPolicies.Select(c=>c.Content));
        if(policy.Contains("guid",StringComparison.Ordinal))return"00000000-0000-0000-0000-000000000001";
        if(policy.Contains("bool",StringComparison.Ordinal))return"true";
        if(policy.Contains("datetime",StringComparison.Ordinal))return"2026-11-07";
        if(policy.Contains("long",StringComparison.Ordinal)||policy.Contains("int",StringComparison.Ordinal)||policy.Contains("range",StringComparison.Ordinal)||policy.Contains("min(",StringComparison.Ordinal)||policy.Contains("max(",StringComparison.Ordinal))return"7";
        if(policy.Contains("decimal",StringComparison.Ordinal)||policy.Contains("double",StringComparison.Ordinal)||policy.Contains("float",StringComparison.Ordinal))return"1.5";
        return p.Name.Contains("tenant",StringComparison.OrdinalIgnoreCase)?"tenant-a":p.Name.Contains("domain",StringComparison.OrdinalIgnoreCase)?"orders":"owned";
    }
}
