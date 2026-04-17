using api.Application.Abstractions;
using api.Application.Services;
using api.Domain.Entities;
using api.Infrastructure.Auth;
using api.Infrastructure.Integrations;
using api.Infrastructure.Persistence;
using api.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClinicrestPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    public static IServiceCollection AddClinicrestRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IBacklogRepository, BacklogRepository>();
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        return services;
    }

    public static IServiceCollection AddClinicrestApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IGoogleDriveService, GoogleDriveService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IBacklogService, BacklogService>();
        services.AddScoped<IGlobalSearchService, GlobalSearchService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISlotService, SlotService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        return services;
    }
}
