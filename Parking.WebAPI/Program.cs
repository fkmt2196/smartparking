using Parking.Application.Common.Interfaces;
using Parking.Application.Services;
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
