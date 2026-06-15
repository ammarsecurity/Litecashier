using PrintServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register services
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddScoped<PrintService>();

// ✅ حدد الـ URL هنا
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("=" + new string('=', 49));
Console.WriteLine("Restaurant POS Print Server (C#)");
Console.WriteLine("=" + new string('=', 49));
Console.WriteLine("Windows Print API Available: True");
Console.WriteLine("=" + new string('=', 49));
Console.WriteLine("Starting server on http://localhost:5000");
Console.WriteLine("Endpoints:");
Console.WriteLine("  GET  /health - Health check");
Console.WriteLine("  POST /print - Print receipt (JSON)");
Console.WriteLine("  POST /print/html - Print HTML content");
Console.WriteLine("  GET  /printers - List available printers");
Console.WriteLine("  GET  /config - Get server configuration");
Console.WriteLine("  PUT  /config - Update server configuration");
Console.WriteLine("  PUT  /config/printer - Set default printer");
Console.WriteLine("=" + new string('=', 49));

// ✅ بدون URL هنا
app.Run();