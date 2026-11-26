using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Starter;
using Starter.WebApi;

#pragma warning disable CA1812
var builder = WebApplication.CreateBuilder(args);
#pragma warning restore CA1812

if (!builder.Environment.IsDevelopment())
{
    // Fail at start-up instead of on the first request. Add every secret the API cannot run without.
    string[] requiredSecrets = ["ConnectionString"];
    var missingSecrets = requiredSecrets.Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key])).ToArray();
    if (missingSecrets.Length > 0)
    {
        throw new InvalidOperationException($"Missing required secrets: {string.Join(", ", missingSecrets)}. Configure them through environment variables or a secret provider.");
    }
}

builder.Services.AddDbContext<StarterContext>(options => options.UseNpgsql(StarterContextFactory.Configure(builder.Configuration["ConnectionString"])));
builder.Services.AddStarterLogic();
builder.Services.AddStarterForwardedHeaders(builder.Configuration);
builder.Services.AddHsts(static options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Starter", Version = "v1" });
    // string? stays nullable and string does not, so the generated client (Starter.PWA/src/logic/api.ts) matches C#.
    c.SupportNonNullableReferenceTypes();
});

var app = builder.Build();
var appVersion = AppVersion.Resolve(app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(static c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Starter v1"));
}
else
{
    // Outside Development the API applies its own migrations on start-up (docs/deployment.md).
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StarterContext>();
    await db.Database.MigrateAsync();
}

// First, so everything after it sees the real client address and scheme behind the reverse proxy.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAppVersionHeader(appVersion);

var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
app.UseCors(policy =>
{
    if (app.Environment.IsDevelopment())
    {
        policy.SetIsOriginAllowed(static _ => true);
    }
    else
    {
        policy.WithOrigins(allowedOrigins);
    }

    policy.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders(AppVersion.HeaderName);
});

// The deploy workflows wait until /api/health reports the version they just published.
app.MapGet("/health", () => Results.Ok(new { status = "ok", version = appVersion })).AllowAnonymous().ExcludeFromDescription();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", version = appVersion })).AllowAnonymous().ExcludeFromDescription();
app.MapControllers();
await app.RunAsync();
