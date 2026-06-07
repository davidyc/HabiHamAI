using System.Text;
using HabiHamAIAPI.Authorization;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using HabiHamAIAPI.Services;
using HabiHamAIAPI.Services.Ai;
using HabiHamAIAPI.Services.Mcp;
using HabiHamAIAPI.Services.Telegram;
using ModelContextProtocol.AspNetCore;
using Telegram.Bot;

LoadDotEnv();
var builder = WebApplication.CreateBuilder(args);

var telegramBotToken = (Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
    ?? builder.Configuration["Telegram:BotToken"]
    ?? string.Empty).Trim();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<KernestalOptions>(builder.Configuration.GetSection("Kernestal"));
builder.Services.Configure<TrainerMcpOptions>(builder.Configuration.GetSection("TrainerMcp"));
builder.Services.Configure<TelegramBotOptions>(builder.Configuration.GetSection("Telegram"));
builder.Services.PostConfigure<TelegramBotOptions>(options =>
{
    var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
    if (!string.IsNullOrEmpty(token))
    {
        options.BotToken = token;
    }

    var baseUrl = Environment.GetEnvironmentVariable("TELEGRAM_PUBLIC_BASE_URL");
    if (!string.IsNullOrEmpty(baseUrl))
    {
        options.PublicBaseUrl = baseUrl;
    }

    var botUsername = Environment.GetEnvironmentVariable("TELEGRAM_BOT_USERNAME");
    if (!string.IsNullOrEmpty(botUsername))
    {
        options.BotUsername = botUsername.TrimStart('@');
    }
});
builder.Services.PostConfigure<KernestalOptions>(options =>
{
    options.BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? options.BaseUrl;
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? options.ApiKey;
    options.Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? options.Model;
});
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddHttpClient<IKernestalAiService, KernestalAiService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TrainerToolExecutionContext>();
builder.Services.AddScoped<TrainerDataQueryService>();
builder.Services.AddScoped<ITrainerToolInvoker, TrainerToolInvoker>();
builder.Services.AddScoped<ITrainerAgentService, TrainerAgentService>();
var trainerMcpEnabled = builder.Configuration.GetValue("TrainerMcp:Enabled", true);
if (trainerMcpEnabled)
{
    builder.Services.AddMcpServer()
        .WithHttpTransport(transport => transport.Stateless = true)
        .WithTools<TrainerMcpTools>();
}

builder.Services.AddScoped<IAiUserService, AiUserService>();
builder.Services.AddScoped<IAdminAiAssistantsService, AdminAiAssistantsService>();
builder.Services.AddScoped<IAdminAiAssistantExtraFieldsService, AdminAiAssistantExtraFieldsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IUserWeightRecordingService>(static sp => (IUserWeightRecordingService)sp.GetRequiredService<IUsersService>());
builder.Services.AddScoped<ITelegramUserLinkService, TelegramUserLinkService>();
builder.Services.AddScoped<IAdminUsersService, AdminUsersService>();
builder.Services.AddScoped<IAdminDialogsService, AdminDialogsService>();
builder.Services.AddScoped<IAdminCategoriesService, AdminCategoriesService>();
builder.Services.AddScoped<IAdminRolesService, AdminRolesService>();
builder.Services.AddScoped<IAppPermissionService, AppPermissionService>();
builder.Services.AddScoped<IWorkoutsService, WorkoutsService>();
builder.Services.AddScoped<IBikeActivitiesService, BikeActivitiesService>();
builder.Services.AddScoped<IInvestmentsService, InvestmentsService>();
builder.Services.AddSingleton<IPingService, PingService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 30 * 1024 * 1024;
});
if (!string.IsNullOrEmpty(telegramBotToken))
{
    builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(telegramBotToken));
    builder.Services.AddSingleton<TelegramChatStateStore>();
    builder.Services.AddScoped<ITelegramUpdateHandler, TelegramUpdateHandler>();
    builder.Services.AddHostedService<TelegramWebhookRegistrationHostedService>();
}
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
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

builder.Services.AddAuthorization(options =>
{
    foreach (var code in AppPermissionCatalog.AllCodes)
    {
        options.AddPolicy(PermissionPolicyNames.For(code), policy =>
            policy.RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(code)));
    }
});
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
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
    await dbContext.Database.MigrateAsync();
    await EnsureSystemRolesAsync(dbContext);
    var permissionService = scope.ServiceProvider.GetRequiredService<IAppPermissionService>();
    await permissionService.EnsureCatalogAsync();
    await permissionService.EnsureDefaultRolePermissionsAsync();
    await DatabaseBootstrap.EnsureTrainerAssistantAsync(dbContext);
    await DatabaseBootstrap.EnsureDefaultUserCategoriesAsync(dbContext);

    var adminSection = builder.Configuration.GetSection("AdminBootstrap");
    var adminUsername = (Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_USERNAME")
        ?? adminSection["Username"]
        ?? "admin").Trim().ToLowerInvariant();
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_PASSWORD")
        ?? adminSection["Password"]
        ?? "admin123";

    var adminUser = await dbContext.Users
        .Include(x => x.RoleAssignments)
        .FirstOrDefaultAsync(x => x.Username == adminUsername);
    if (adminUser is null)
    {
        var createdAdmin = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = adminUsername,
            CreatedAtUtc = DateTime.UtcNow
        };
        AppUserRoleHelper.SetRoles(createdAdmin, ["Admin"]);
        createdAdmin.PasswordHash = passwordHasher.HashPassword(createdAdmin, adminPassword);
        dbContext.Users.Add(createdAdmin);
        await dbContext.SaveChangesAsync();
    }
    else if (!AppUserRoleHelper.HasRole(adminUser, "Admin"))
    {
        AppUserRoleHelper.EnsureRole(adminUser, "Admin");
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

var trainerMcpHttpEnabled = app.Configuration.GetValue("TrainerMcp:HttpEndpointEnabled", true);
var trainerMcpPath = app.Configuration.GetValue("TrainerMcp:HttpPath", "/api/mcp/trainer") ?? "/api/mcp/trainer";
if (trainerMcpEnabled && trainerMcpHttpEnabled)
{
    app.MapMcp(trainerMcpPath).RequireAuthorization();
}

app.Run();

static async Task EnsureSystemRolesAsync(AppDbContext dbContext)
{
    var systemRoles = new (string Name, string Label, string? Description, int SortOrder)[]
    {
        ("Admin", "Администратор", "Полный доступ к админ-панели", 1),
        ("User", "Пользователь", "Базовый доступ к приложению", 2),
        ("AiUser", "AI-пользователь", "Доступ к разделу ИИ-помощника", 3),
    };

    var now = DateTime.UtcNow;
    foreach (var (name, label, description, sortOrder) in systemRoles)
    {
        var existing = await dbContext.AppRoles.FirstOrDefaultAsync(x => x.Name == name);
        if (existing is null)
        {
            dbContext.AppRoles.Add(new AppRole
            {
                Id = Guid.NewGuid(),
                Name = name,
                Label = label,
                Description = description,
                IsSystem = true,
                IsActive = true,
                SortOrder = sortOrder,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }
        else
        {
            existing.Label = label;
            existing.Description = description;
            existing.IsSystem = true;
            existing.IsActive = true;
            existing.SortOrder = sortOrder;
            existing.UpdatedAtUtc = now;
        }
    }

    await dbContext.SaveChangesAsync();
}

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
