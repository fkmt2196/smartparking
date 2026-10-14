using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Parking.Persistence.Contexts;

public class ParkingDbContextFactory : IDesignTimeDbContextFactory<ParkingDbContext>
{
    public ParkingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Parking");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Placeholder để EF build model khi tạo Migration, không phải thông tin đăng nhập.
            // Khi update DB bằng dotnet ef, đặt biến môi trường ConnectionStrings__Parking (DbContextFactory đọc biến môi trường, không đọc User Secrets).
            connectionString = "Host=127.0.0.1;Port=5432;Database=smartparking_dev;Username=smartparking";
        }

        var optionsBuilder = new DbContextOptionsBuilder<ParkingDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new ParkingDbContext(optionsBuilder.Options);
    }
}
