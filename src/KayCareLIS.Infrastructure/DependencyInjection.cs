using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using KayCareLIS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using Azure.Storage.Blobs;

namespace KayCareLIS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // EF Core
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3)));

        // Tenant context — scoped so it's per-request and mutated by middleware
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        // HTTP accessor for current user
        services.AddHttpContextAccessor();

        // Auth + identity services
        services.AddScoped<ITokenService,        TokenService>();
        services.AddScoped<ICurrentUserService,  CurrentUserService>();
        services.AddScoped<IAuthService,         AuthService>();

        // Admin services
        services.AddScoped<ITenantService,          TenantService>();
        services.AddScoped<IUserManagementService,  UserManagementService>();
        services.AddScoped<IFacilitySettingsService, FacilitySettingsService>();

        // Domain services
        services.AddScoped<IPatientService,      PatientService>();
        services.AddScoped<IAppointmentService,  AppointmentService>();
        services.AddScoped<ILabOrderService,     LabOrderService>();
        services.AddScoped<ILabResultService,    LabResultService>();
        services.AddScoped<IBillingService,      BillingService>();
        services.AddScoped<IDocumentService,     DocumentService>();
        services.AddScoped<IIcdCodeService,      IcdCodeService>();
        services.AddScoped<IAuditService,        AuditService>();

        services.AddScoped<IRadiologyOrderService,  RadiologyOrderService>();

        // PDF services
        services.AddScoped<ILabReportService,       LabReportService>();
        services.AddScoped<IBillingPdfService,      BillingPdfService>();
        services.AddScoped<IRadiologyReportService, RadiologyReportService>();

        // Azure Blob Storage
        var blobConn = config.GetConnectionString("BlobStorage")
            ?? config["AzureStorage:ConnectionString"]
            ?? "UseDevelopmentStorage=true";
        services.AddSingleton(_ => new BlobServiceClient(blobConn));
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        // MLLP listener (HL7 instrument integration) — disabled by default, enable via config
        if (config.GetValue<bool>("Mllp:Enabled"))
        {
            var port = config.GetValue<int>("Mllp:Port", 2575);
            services.AddHostedService(sp =>
                new MllpListenerService(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MllpListenerService>>(),
                    port));
        }

        return services;
    }
}
