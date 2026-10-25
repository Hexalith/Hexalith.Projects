#if DEBUG && SUPPORTED_PROJECT_READ_SOURCE
using Hexalith.EventStore.ServiceDefaults.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddEventStoreWorkloadBindingAuthority(builder.Configuration);
WebApplication app = builder.Build();
app.MapEventStoreWorkloadBindingAuthority();
app.MapGet("/alive", () => Results.Ok());
await app.RunAsync().ConfigureAwait(false);
#else
throw new InvalidOperationException("The fixture authority requires local Debug source references.");
#endif
