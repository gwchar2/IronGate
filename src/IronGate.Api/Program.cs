
using IronGate.Api.Features.Auth.Dtos;
using IronGate.Api.Features.Auth.Filters;
using IronGate.Api.Features.Auth.AuthService;
using IronGate.Api.Features.Auth.PasswordHasher;
using IronGate.Api.Features.Captcha;
using IronGate.Api.Features.Lockout;
using IronGate.Api.Features.Rate_Limiting;
using IronGate.Api.Features.Config.ConfigService;
using IronGate.Api.Features.Captcha.CaptchaService;
using IronGate.Api.JsonlLogging;
using IronGate.Api.JsonlLogging.AttemptsService;
using IronGate.Api.JsonlLogging.ResourceService;
using IronGate.Core.Database;
using IronGate.Core.Database.Seeder;
using IronGate.Core.Security.TotpValidator;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

/* Default DB Connection String */
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connStr))
    connStr = "Server=db;Database=IronGateDb;User Id=sa;Password=Ir0nGate_Dev1!;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connStr));

/* Pepper configuration */
var pepper = builder.Configuration["Security:PasswordPepper"];
if (string.IsNullOrWhiteSpace(pepper))
    pepper = "_HERhDonaWE5u563cwXf4v9AuC8IT0oYL_MN_8FDdMI="; // Default pepper for testing purposes only

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


/* We register The JSONL Logging Services  and limit channel capacity to 50,000 entries (as the instructions of the assignment said */
builder.Services.Configure<JsonlLoggingOptions>(builder.Configuration.GetSection("JsonlLogging"));
var channel = Channel.CreateBounded<AuthAttemptDto>(new BoundedChannelOptions(capacity: 50_000) {
    SingleReader = true,
    SingleWriter = false,
    FullMode = BoundedChannelFullMode.DropWrite
});
builder.Services.AddSingleton(channel);
builder.Services.AddSingleton<IAttemptsJsonlSink, AttemptsJsonlSink>();

builder.Services.AddHostedService<AttemptsJsonlWriterService>();
builder.Services.AddHostedService<ResourcesJsonlService>();


/* Registering Endpoints & Swagger */
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


/* Registering Action Filters */
builder.Services.AddScoped<LockoutActionFilter>();
builder.Services.AddScoped<RateLimitActionFilter>();
builder.Services.AddScoped<CaptchaActionFilter>();

/* Registering other services */
builder.Services.AddSingleton<IRateLimiter, RateLimiter>();
builder.Services.AddSingleton<IPasswordHasher>(_ => new PasswordHasher(pepper));
builder.Services.AddScoped<ITotpValidator, TotpValidator>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddHttpContextAccessor();

/* Registering Auth Service */
builder.Services.AddScoped<IAuthService>(sp =>
{
    var dbCtx = sp.GetRequiredService<AppDbContext>();
    var configService = sp.GetRequiredService<IConfigService>();
    var hasher = sp.GetRequiredService<IPasswordHasher>();
    var totp = sp.GetRequiredService<ITotpValidator>();
    var jsonlSink = sp.GetRequiredService<IAttemptsJsonlSink>();

    return new AuthService(dbCtx, configService, hasher, totp, jsonlSink, pepper);
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
