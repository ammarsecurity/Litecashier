using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestaurantPOS.Db;
using Microsoft.AspNetCore.SignalR;
using RestaurantPOS.Hubs;
using RestaurantPOS.Logging;
using RestaurantPOS.Middleware;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Requests.Restaurant;
using RestaurantPOS.Models.Restaurant;
using RestaurantPOS.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

const long maxBackupUploadBytes = 1024L * 1024L * 1024L; // 1 GB

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxBackupUploadBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxBackupUploadBytes;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Provider Configuration
var databaseProvider = builder.Configuration["DatabaseSettings:Provider"]?.ToLower() ?? "sqlserver";
var connectionString = builder.Configuration.GetConnectionString("WebApiDatabase") 
    ?? throw new InvalidOperationException("Connection string 'WebApiDatabase' not found.");

builder.Services.AddDbContext<DbConfig>(options =>
{
    switch (databaseProvider)
    {
        case "mysql":
            // Auto-detect MySQL Server Version (will connect to detect version)
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            options.UseMySql(
                connectionString,
                serverVersion,
                mySqlOptions => mySqlOptions
                    .EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)
            );
            break;
        case "sqlserver":
        default:
            options.UseSqlServer(connectionString);
            break;
    }
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("NebulaPayment");
builder.Services.AddScoped<INebulaPaymentService, NebulaPaymentService>();
builder.Services.AddScoped<IOrderCheckoutService, OrderCheckoutService>();
builder.Services.AddScoped<ICommercialTenantDeleteService, CommercialTenantDeleteService>();
builder.Services.AddScoped<ISystemBackupService, SystemBackupService>();
builder.Services.AddScoped<ICreditAccountService, CreditAccountService>();
builder.Services.AddScoped<IReservationTableSyncService, ReservationTableSyncService>();
builder.Services.AddScoped<IReservationExpiryService, ReservationExpiryService>();
builder.Services.AddHostedService<ReservationExpiryBackgroundService>();
builder.Services.Configure<RestaurantPOS.Configuration.ReservationSettingsOptions>(
    builder.Configuration.GetSection(RestaurantPOS.Configuration.ReservationSettingsOptions.SectionName));
builder.Services.AddScoped<ITableOrderSyncService, TableOrderSyncService>();
builder.Services.AddSingleton<ICardPaymentProcessingService, CardPaymentProcessingService>();
builder.Services.Configure<RestaurantPOS.Configuration.SyncSettingsOptions>(
    builder.Configuration.GetSection(RestaurantPOS.Configuration.SyncSettingsOptions.SectionName));
builder.Services.AddScoped<RestaurantPOS.Services.Sync.IDatabaseSyncService, RestaurantPOS.Services.Sync.DatabaseSyncService>();
builder.Services.AddScoped<RestaurantPOS.Services.Sync.IFileSyncService, RestaurantPOS.Services.Sync.FileSyncService>();
builder.Services.AddHostedService<RestaurantPOS.Services.Sync.DatabaseSyncBackgroundService>();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddSingleton(new MapperConfiguration(cfg =>
{
    // UserRequest to User mapping - ignore Password as it's handled manually
    cfg.CreateMap<UserRequest, User>()
        .ForMember(dest => dest.Password, opt => opt.Ignore())
        .ReverseMap();
    cfg.CreateMap<TagRequset, Tag>().ReverseMap();
    cfg.CreateMap<ItemRequest, Item>().ReverseMap();
    cfg.CreateMap<CustomerOrderRequest, CustomerOrder>().ReverseMap();
    cfg.CreateMap<CustomerOrderItemRequest, CustomerOrderItem>().ReverseMap();
    
    // Restaurant Mappings
    cfg.CreateMap<TableRequest, Table>().ReverseMap();
    cfg.CreateMap<ReservationRequest, Reservation>().ReverseMap();

}).CreateMapper());

var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] 
    ?? throw new InvalidOperationException("JWT Secret Key is not configured");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "Issuer";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "Audience";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtBearerOptions =>
{
    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
    
    // Configure JWT for SignalR
    jwtBearerOptions.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            // If the request is for the SignalR hub
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/orderHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Restaurant POS API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
               builder => builder
                      .SetIsOriginAllowed(_ => true) // Allow any origin for development
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials());
});

// Add SignalR
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSignalR();

builder.Services.Configure<ErrorLogSettings>(builder.Configuration.GetSection("ErrorLogging"));
builder.Services.AddSingleton<IWwwrootErrorLogService, WwwrootErrorLogService>();




var app = builder.Build();

// تطبيق ترحيلات EF تلقائياً (مثل TableChipSizePx) عند التشغيل؛ عطّل عبر DatabaseSettings:ApplyMigrationsOnStartup = false
var applyMigrations = app.Configuration.GetValue("DatabaseSettings:ApplyMigrationsOnStartup", true);
if (applyMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DbConfig>();
        var migrateLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            migrateLogger.LogError(ex, "تعذر تطبيق ترحيلات قاعدة البيانات (EF Migrate). تحقق من سلسلة الاتصال والصلاحيات.");
            throw;
        }
    }
}

// Seed database on startup
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    try
//    {
//        var context = services.GetRequiredService<DbConfig>();
//        RestaurantPOS.Db.SeedData.SeedDatabase(context);
//    }
//    catch (Exception ex)
//    {
//        var logger = services.GetRequiredService<ILogger<Program>>();
//        logger.LogError(ex, "An error occurred while seeding the database.");
//    }
//}


if (app.Environment.IsDevelopment())
{
    //  app.UseSwaggerAuthorized();
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant POS API"); });
}
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "EppReservations Project API"); });

// Log HTTP 404/500 (and unhandled exceptions) to wwwroot/logs
app.UseMiddleware<HttpErrorLoggingMiddleware>();

// Block public HTTP access to log files under wwwroot/logs
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/logs", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

// Serve static files from wwwroot
app.UseStaticFiles();

// Enable routing
app.UseRouting();

// CORS must be before UseAuthentication
app.UseCors("CorsPolicy");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.MapControllers();

// Map SignalR Hub
app.MapHub<OrderHub>("/orderHub");

// Middleware to handle SPA routes before fallback
// This ensures routes like /menu/2 and /public-queue/2 work correctly
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    
    // If it's not an API route and not a file, rewrite to index.html
    if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/orderHub", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/static", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/Images", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/logs", StringComparison.OrdinalIgnoreCase) &&
        !System.IO.Path.HasExtension(path))
    {
        context.Request.Path = "/index.html";
    }
    
    await next();
});

// SPA fallback - serve index.html for all non-API routes
// This must be LAST, after all other routes
app.MapFallbackToFile("/index.html");

app.Run();