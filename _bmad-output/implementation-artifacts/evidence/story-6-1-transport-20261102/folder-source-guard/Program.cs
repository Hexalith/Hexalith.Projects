using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Hexalith.Folders.Server;
var builder=WebApplication.CreateBuilder(new WebApplicationOptions{EnvironmentName="Development"});
builder.AddFoldersDevelopmentOwnerMetadata();
if(builder.Services.Any(x=>x.ServiceType.Name=="INativeSourceMetadataReader"))throw new InvalidOperationException("Disabled private path registered authority");
builder.Configuration["Folders:EventStore:OwnerMetadataReads:Enabled"]="true";
try{builder.AddFoldersDevelopmentOwnerMetadata();throw new InvalidOperationException("Missing SDK-source private flag was accepted");}
catch(InvalidOperationException ex)when(ex.Message=="Private owner metadata requires a local Debug source build."){}
using var file=File.OpenRead(typeof(Program).Assembly.Location);using var pe=new PEReader(file);var metadata=pe.GetMetadataReader();
if(metadata.TypeReferences.Any(x=>metadata.GetString(metadata.GetTypeReference(x).Name)=="BoundOwnerNativeSourceMetadataReader"))throw new InvalidOperationException("Unpublished source-only API leaked into mixed Debug output");
Console.WriteLine("Mixed Debug conditional compilation: default inert; explicit private selection refused; unpublished adapter type reference absent.");
