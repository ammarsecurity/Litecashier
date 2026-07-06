using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using POS.Db;
using POS.Hubs;
using POS.Models;
using POS.Models.Requests;
using POS.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DbConfig>(options =>
        options
            .UseMySql(builder.Configuration.GetConnectionString("WebApiDatabase"), 
                new MySqlServerVersion(new Version(8, 0, 21)))
);

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("NebulaPayment");
builder.Services.AddScoped<INebulaPaymentService, NebulaPaymentService>();
builder.Services.AddScoped<IOrderCheckoutService, OrderCheckoutService>();
builder.Services.AddScoped<IItemImportService, ItemImportService>();
builder.Services.AddScoped<ICommercialCatalogClearService, CommercialCatalogClearService>();
builder.Services.AddScoped<ICreditAccountService, CreditAccountService>();
builder.Services.AddSingleton<ICardPaymentProcessingService, CardPaymentProcessingService>();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddSingleton(new MapperConfiguration(cfg =>
{
    cfg.CreateMap<UserRequest, User>()
        .ForMember(dest => dest.Password, opt => opt.Ignore())
        .ReverseMap();
    cfg.CreateMap<TagRequset, Tag>().ReverseMap();
    cfg.CreateMap<ItemRequest, Item>().ReverseMap();
    cfg.CreateMap<CustomerOrderRequest, CustomerOrder>().ReverseMap();
    cfg.CreateMap<CustomerOrderItemRequest, CustomerOrderItem>().ReverseMap();
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

    jwtBearerOptions.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

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
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "MyAPI", Version = "v1" });
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
                      .SetIsOriginAllowed(_ => true)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials());
});

builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSignalR();

var app = builder.Build();

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
            migrateLogger.LogError(ex, "Failed to apply EF migrations on startup.");
            throw;
        }

        var seedOnStartup = app.Configuration.GetValue("DatabaseSettings:SeedOnStartup", false);
        if (seedOnStartup)
        {
            try
            {
                var seedDemo = app.Configuration.GetValue("DatabaseSettings:SeedDemoAccounts", true);
                if (seedDemo)
                {
                    var summary = POS.Db.SeedData.SeedDemoEnvironment(db);
                    migrateLogger.LogInformation("Database seed completed: {Message}", summary.ToMessage());
                }
                else
                {
                    var commercialUserId = app.Configuration.GetValue("DatabaseSettings:CommercialUserId", 0);
                    if (commercialUserId <= 0)
                    {
                        migrateLogger.LogWarning("DatabaseSettings:SeedOnStartup is enabled but CommercialUserId is not set.");
                    }
                    else
                    {
                        var summary = POS.Db.SeedData.SeedDatabase(db, commercialUserId);
                        migrateLogger.LogInformation("Database seed completed: {Message}", summary.ToMessage());
                    }
                }
            }
            catch (Exception ex)
            {
                migrateLogger.LogWarning(ex, "Database seed on startup failed (app will continue).");
            }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "EppReservations Project API"); });
}

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "EppReservations Project API"); });
app.UseStaticFiles();
app.UseAuthentication();
app.UseCors("CorsPolicy");
app.UseAuthorization();
app.MapControllers();
app.MapHub<OrderHub>("/orderHub");

app.Run();
