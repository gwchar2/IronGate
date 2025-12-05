using IronGate.Api.Features.Auth.PasswordHasher;
using IronGate.Api.Features.Auth.TotpValidator;
using IronGate.Api.Features.Auth.AuthService;
using IronGate.Core.Database;
using IronGate.Core.Database.Seeder;
using Microsoft.EntityFrameworkCore;
using IronGate.Api.Features.Config.ConfigService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

/* Create Migrations and update database automatically */
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    /* Pepper configuration */
    var pepper = builder.Configuration["Security:PasswordPepper"];
    if (string.IsNullOrWhiteSpace(pepper))
        throw new InvalidOperationException("Security:PasswordPepper is not configured.");
        
    /* Initiate Seeder */
    await DbSeeder.SeedAsync(db, pepper);
    builder.Services.AddSingleton<IPasswordHasher>(_ => new PasswordHasher(pepper));
    builder.Services.AddScoped<ITotpValidator, TotpValidator>();

    builder.Services.AddScoped<IAuthService>(sp =>
    {
        var dbCtx = sp.GetRequiredService<AppDbContext>();
        var configService = sp.GetRequiredService<IConfigService>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var totp = sp.GetRequiredService<ITotpValidator>();

        return new AuthService(dbCtx, configService, hasher, totp, pepper);
    });
}

/* Configure HTTP Request Pipeline (localhost:8080/swagger) */
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

/* Simple health endpoint that also checks DB connectivity */
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) => {
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "ok" })
        : Results.Problem("Database connection failed");
})
.WithName("Health");

app.Run();
