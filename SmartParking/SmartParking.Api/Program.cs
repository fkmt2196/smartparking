using SmartParking.Application.UseCases;
using SmartParking.Domain.Interfaces;
using SmartParking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký Clean Architecture Services (DI)
builder.Services.AddScoped<IParkingRepository, MockParkingRepository>();
builder.Services.AddScoped<GetSlotStatusUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
