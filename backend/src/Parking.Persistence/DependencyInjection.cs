using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parking.Persistence.Contexts;

namespace Parking.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddParkingDatabase(
        this IServiceCollection services,
        string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddDbContext<ParkingDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
