using System.Text;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using HabiHamAIAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
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
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = jwtSettings["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
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
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE users
        ADD COLUMN IF NOT EXISTS role character varying(30) NOT NULL DEFAULT 'User';
        """);

    var adminSection = builder.Configuration.GetSection("AdminBootstrap");
    var adminUsername = (adminSection["Username"] ?? "admin").Trim().ToLowerInvariant();
    var adminPassword = adminSection["Password"] ?? "admin123";

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
