using IronGate.Api.Features.Auth.AuthService;
using IronGate.Api.Features.Auth.Filters;
using IronGate.Api.Features.Auth.PasswordHasher;
using IronGate.Api.Features.Captcha.CaptchaService;
using IronGate.Api.Features.Config.ConfigService;
using IronGate.Api.Features.Lockout;
using IronGate.Api.Features.Rate_Limiting;
using IronGate.Core.Database;
using IronGate.Core.Database.Seeder;
using IronGate.Core.Security.TotpValidator;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

/* Pepper configuration */
var pepper = builder.Configuration["Security:PasswordPepper"];
if (string.IsNullOrWhiteSpace(pepper))
    throw new InvalidOperationException("Security:PasswordPepper is not configured.");

/* Registering Endpoints & Swagger */
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


/* Registering Services */
builder.Services.AddScoped<LockoutActionFilter>();
builder.Services.AddScoped<RateLimitActionFilter>();
builder.Services.AddSingleton<IRateLimiter, RateLimiter>();
builder.Services.AddSingleton<IPasswordHasher>(_ => new PasswordHasher(pepper));
builder.Services.AddScoped<ITotpValidator, TotpValidator>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var dbCtx = sp.GetRequiredService<AppDbContext>();
    var configService = sp.GetRequiredService<IConfigService>();
    var hasher = sp.GetRequiredService<IPasswordHasher>();
    var totp = sp.GetRequiredService<ITotpValidator>();
    var captcha = sp.GetRequiredService<ICaptchaService>();

    return new AuthService(dbCtx, configService, hasher, totp, captcha, httpContextAccessor, pepper);
});

/* Build the App (API + DB) */
var app = builder.Build();

/* Create Migrations and update database automatically */
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
        
    /* Initiate Seeder */
    await DbSeeder.SeedAsync(db, pepper);

}

/* Configure HTTP Request Pipeline (localhost:8080/swagger) */
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
