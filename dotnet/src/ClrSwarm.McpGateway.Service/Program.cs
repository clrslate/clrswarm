using Azure.Identity;
using ClrSwarm.McpGateway.Management.Deployment;
using ClrSwarm.McpGateway.Management.Service;
using ClrSwarm.McpGateway.Management.Store;
using ClrSwarm.McpGateway.Service.Routing;
using ClrSwarm.McpGateway.Service.Session;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using ClrSwarm.McpGateway.Management.Deployment;
using ModelContextProtocol.AspNetCore.Authentication;
using Scalar.AspNetCore;
using System.Security.Claims;
using ClrSwarm.McpGateway.Management.Extensions; // persistence extension
using ClrSwarm.McpGateway.Service.Extensions; // cache extension

var builder = WebApplication.CreateBuilder(args);
var credential = new DefaultAzureCredential();

builder.Services.AddLogging();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IKubernetesClientFactory, LocalKubernetesClientFactory>();
builder.Services.AddSingleton<IAdapterSessionStore, DistributedMemorySessionStore>();
builder.Services.AddSingleton<IServiceNodeInfoProvider, AdapterKubernetesNodeInfoProvider>();
builder.Services.AddSingleton<ISessionRoutingHandler, AdapterSessionRoutingHandler>();

// Auth only when not development
if (!builder.Environment.IsDevelopment())
{
    var azureAdConfig = builder.Configuration.GetSection("AzureAd");
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            Resource = new Uri(builder.Configuration.GetValue<string>("PublicOrigin")!),
            AuthorizationServers = { new Uri($"https://login.microsoftonline.com/{azureAdConfig["TenantId"]}/v2.0") },
            ScopesSupported = [$"api://{azureAdConfig["ClientId"]}/.default"]
        };
    })
    .AddMicrosoftIdentityWebApi(azureAdConfig);
}

// Persistence & Cache
builder.Services.AddAdapterResourcePersistence(builder.Configuration, credential);
builder.Services.AddDistributedCache(builder.Configuration, credential);

builder.Services.AddSingleton<IKubeClientWrapper>(c =>
{
    var kubeClientFactory = c.GetRequiredService<IKubernetesClientFactory>();
    return new KubeClient(kubeClientFactory, "adapter");
});
builder.Services.AddSingleton<IAdapterDeploymentManager>(c =>
{
    var config = builder.Configuration.GetSection("ContainerRegistrySettings");
    return new KubernetesAdapterDeploymentManager(c.GetRequiredService<IKubeClientWrapper>(), c.GetRequiredService<ILogger<KubernetesAdapterDeploymentManager>>());
});
builder.Services.AddSingleton<IAdapterManagementService, AdapterManagementService>();
builder.Services.AddSingleton<IAdapterRichResultProvider, AdapterRichResultProvider>();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var devIdentity = new ClaimsIdentity("Development");
        devIdentity.AddClaim(new Claim(ClaimTypes.Name, "dev"));
        context.User = new ClaimsPrincipal(devIdentity);
        await next();
    });
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.MapOpenApi();
app.MapScalarApiReference(options => {
    options.DocumentDownloadType = DocumentDownloadType.Both;
    options.DynamicBaseServerUrl = true;
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
