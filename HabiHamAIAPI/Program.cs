using System.Text;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using HabiHamAIAPI.Services;

LoadDotEnv();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<TokenService>();
builder.Services.Configure<KernestalOptions>(builder.Configuration.GetSection("Kernestal"));
builder.Services.PostConfigure<KernestalOptions>(options =>
{
    options.BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? options.BaseUrl;
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? options.ApiKey;
    options.Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? options.Model;
});
builder.Services.AddHttpClient<KernestalAiService>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
    options.UseNpgsql(connectionString);
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllForTests", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? jwtSettings["Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? jwtSettings["Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? jwtSettings["Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HabiHamAIAPI", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Bearer token. Example: Bearer {your token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
    await dbContext.Database.EnsureCreatedAsync();

    var adminSection = builder.Configuration.GetSection("AdminBootstrap");
    var adminUsername = (Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_USERNAME")
        ?? adminSection["Username"]
        ?? "admin").Trim().ToLowerInvariant();
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_PASSWORD")
        ?? adminSection["Password"]
        ?? "admin123";

    var adminUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == adminUsername);
    if (adminUser is null)
    {
        var createdAdmin = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = adminUsername,
            Role = AppUserRole.Admin,
            CreatedAtUtc = DateTime.UtcNow
        };
        createdAdmin.PasswordHash = passwordHasher.HashPassword(createdAdmin, adminPassword);
        dbContext.Users.Add(createdAdmin);
        await dbContext.SaveChangesAsync();
    }
    else if (adminUser.Role != AppUserRole.Admin)
    {
        adminUser.Role = AppUserRole.Admin;
        await dbContext.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAllForTests");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void LoadDotEnv()
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (!File.Exists(envPath))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(envPath))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(key))
        {
            continue;
        }

        Environment.SetEnvironmentVariable(key, value);
    }
}
