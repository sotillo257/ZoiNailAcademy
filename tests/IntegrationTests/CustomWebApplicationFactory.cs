using IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using MediatR;
using ZoiNailAcademy.Application.Common.Interfaces;
using ZoiNailAcademy.Application.Ediciones;
using ZoiNailAcademy.Application.ListasEspera;
using ZoiNailAcademy.Application.Reservas;
using ZoiNailAcademy.Host.Jobs;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using ZoiNailAcademy.Infrastructure.Data;

namespace IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@seed.test";
    public const string AdminPassword = "admin-seed-password";
    public const string SuperAdminEmail = "superadmin@seed.test";
    public const string SuperAdminPassword = "superadmin-seed-password";

    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    /// <summary>
    /// Clock of the shared host. It starts at the real "now" because JWT lifetime validation uses
    /// the real clock; tests that move time forward create their own host with their own clock.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public const string StripeWebhookSecret = "whsec_test_integration_secret";
    public const string PagoTitular = "Zoi Nail Academy";
    public const string PagoIban = "ES91 2100 0418 4502 0005 1332";
    public const string ClientaReplyTo = "zoi@academia.test";
    public const string AvisosClienta = "avisos@academia.test";

    /// <summary>
    /// A separate host on the same database with its own clock, for tests that move time forward (the
    /// shared clock must stay near the real one: JWT validation uses the real clock).
    /// </summary>
    public WebApplicationFactory<Program> CreateHost(FakeTimeProvider clock, bool conJob = false) =>
        WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(clock);
            if (conJob)
            {
                services.AddHostedService<ExpirarReservasJob>();
            }
        }));

    private static void RemoveBackgroundJobs(IServiceCollection services)
    {
        var job = services.Where(d => d.ServiceType == typeof(IHostedService)
            && (d.ImplementationType == typeof(ExpirarReservasJob) || d.ImplementationType == typeof(EnviarEmailsJob))).ToList();
        foreach (var descriptor in job)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>Stripe without network, shared by the tests (sessions are keyed by id).</summary>
    public FakeStripeGateway StripeGateway { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgreSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgreSqlContainer.GetConnectionString());
        builder.UseSetting("PasswordHasher:WorkFactor", "4");
        builder.UseSetting("Auth:CookieSecure", "true");
        builder.UseSetting("Admin:SeedEmail", AdminEmail);
        builder.UseSetting("Admin:SeedPassword", AdminPassword);
        builder.UseSetting("Admin:SeedNombre", "Clienta");
        builder.UseSetting("SuperAdmin:SeedEmail", SuperAdminEmail);
        builder.UseSetting("SuperAdmin:SeedPassword", SuperAdminPassword);
        builder.UseSetting("SuperAdmin:SeedNombre", "Desarrollador");
        builder.UseSetting("App:BaseUrl", "https://web.test");
        builder.UseSetting("Stripe:WebhookSecret", StripeWebhookSecret);
        builder.UseSetting("Pago:Titular", PagoTitular);
        builder.UseSetting("Pago:Iban", PagoIban);
        builder.UseSetting("Pago:BizumTelefono", "600111222");
        builder.UseSetting("Pago:WhatsAppTelefono", "34600111222");
        builder.UseSetting("Email:ReplyTo", ClientaReplyTo);
        builder.UseSetting("Email:AvisosClienta", AvisosClienta);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.AddTransient<IStartupFilter, ProtectedTestEndpointStartupFilter>();
            services.AddControllers().AddApplicationPart(typeof(TestAdminController).Assembly);

            services.AddSingleton(StripeGateway);
            services.AddSingleton<IStripeGateway>(StripeGateway);
            services.AddTransient<INotificationHandler<EdicionPublicadaEvent>, RecordingHandler<EdicionPublicadaEvent>>();
            services.AddTransient<INotificationHandler<ReservaPagadaEvent>, RecordingHandler<ReservaPagadaEvent>>();
            services.AddTransient<INotificationHandler<ReservaExpiradaEvent>, RecordingHandler<ReservaExpiradaEvent>>();
            services.AddTransient<INotificationHandler<PagoTrasCaducidadSinPlazaEvent>, RecordingHandler<PagoTrasCaducidadSinPlazaEvent>>();
            services.AddTransient<INotificationHandler<ReservaManualCreadaEvent>, RecordingHandler<ReservaManualCreadaEvent>>();
            services.AddTransient<INotificationHandler<ReservaCanceladaEvent>, RecordingHandler<ReservaCanceladaEvent>>();
            services.AddTransient<INotificationHandler<ListaEsperaAltaEvent>, RecordingHandler<ListaEsperaAltaEvent>>();

            // The expiry job is started explicitly by the tests that need it (CreateHost(conJob: true)), and the
            // outbox is sent by the email tests through the command, so neither runs behind unrelated tests.
            RemoveBackgroundJobs(services);
        });
    }
}
