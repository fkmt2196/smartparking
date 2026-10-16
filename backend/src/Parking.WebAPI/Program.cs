using Parking.Application.Common.Interfaces;
using Parking.Application.Services;
using Parking.Persistence;
using Parking.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register Clean Architecture Dependency Injection (DI)
builder.Services.AddScoped<IParkingSlotRepository, ParkingSlotRepository>();
builder.Services.AddScoped<ParkingSlotService>();

var parkingLotsDataPath = ResolveParkingLotsDataPath(builder);
builder.Services.AddScoped<IParkingLotRepository>(_ => new ParkingLotRepository(parkingLotsDataPath));
builder.Services.AddScoped<ParkingLotService>();
builder.Services.AddScoped<IoTService>();

var parkingConnectionString = builder.Configuration.GetConnectionString("Parking");
builder.Services.AddParkingDatabase(parkingConnectionString);

// CORS: chỉ mở origin trong cấu hình. Chưa xác nhận origin frontend của Song.
// Không dùng wildcard (*). Cập nhật Cors:AllowedOrigins khi có origin chính thức.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
allowedOrigins = allowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedFrontendOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Parking")))
{
    app.Logger.LogInformation(
        "Chưa cấu hình ConnectionStrings:Parking. API JSON/mock vẫn chạy. Đặt User Secrets hoặc biến môi trường ConnectionStrings__Parking khi cần PostgreSQL. Xem docs/database/README.md.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger UI endpoint phục vụ kiểm thử API trực quan
    app.MapGet("/swagger", () => Results.Content("""
        <!DOCTYPE html>
        <html lang="vi">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>Smart Parking API - Swagger UI</title>
            <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
            <style>
                body { margin: 0; background: #fafafa; font-family: sans-serif; }
                .topbar { display: none; }
            </style>
        </head>
        <body>
            <div id="swagger-ui"></div>
            <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
            <script>
                window.onload = () => {
                    window.ui = SwaggerUIBundle({
                        url: '/openapi/v1.json',
                        dom_id: '#swagger-ui',
                        deepLinking: true,
                        validatorUrl: null,
                        presets: [
                            SwaggerUIBundle.presets.apis,
                            SwaggerUIBundle.SwaggerUIStandalonePreset
                        ]
                    });
                };
            </script>
        </body>
        </html>
        """, "text/html"));
}

app.UseHttpsRedirection();

app.UseCors("AllowedFrontendOrigins");

app.UseAuthorization();

app.MapControllers();

app.Run();

static string ResolveParkingLotsDataPath(WebApplicationBuilder builder)
{
    var relativePath = builder.Configuration["ParkingLots:DataFile"] ?? "Data/parking-lots.json";

    var fromContentRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, relativePath));
    if (File.Exists(fromContentRoot))
    {
        return fromContentRoot;
    }

    var fromOutputDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
    if (File.Exists(fromOutputDirectory))
    {
        return fromOutputDirectory;
    }

    return fromContentRoot;
}
