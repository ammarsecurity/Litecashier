using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using POS.Db;
using POS.Models;
using POS.Models.Requests;
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

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddSingleton(new MapperConfiguration(cfg =>
{
    cfg.CreateMap<UserRequest, User>().ReverseMap();
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
               builder => builder.AllowAnyOrigin()
                      .AllowAnyMethod()
                             .AllowAnyHeader());
});




var app = builder.Build();


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

app.Run();