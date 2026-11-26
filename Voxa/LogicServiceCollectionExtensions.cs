using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Services;
using Voxa.Logic;
using Voxa.Services;

namespace Voxa;

public static class LogicServiceCollectionExtensions
{
    /// <summary>Registers every logic class and service. Add new ones here so Program.cs stays short.</summary>
    public static IServiceCollection AddVoxaLogic(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.Configure<CodeOptions>(configuration.GetSection(CodeOptions.Section));
        services.Configure<MailOptions>(configuration.GetSection(MailOptions.Section));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.Section));
        services.Configure<ImportOptions>(configuration.GetSection(ImportOptions.Section));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.Section));

        services.AddSingleton<ICodeService, CodeService>();
        services.AddSingleton<IAttemptLimiter, AttemptLimiter>();
        services.AddScoped<IClientIdentity, ClientIdentity>();
        services.AddScoped<ClubSettingsStore>();

        var mail = configuration.GetSection(MailOptions.Section).Get<MailOptions>() ?? new MailOptions();
        if (mail.IsMailgun)
        {
            services.AddHttpClient<IMailSender, MailgunMailSender>();
            services.AddSingleton<IOutbox?>(_ => null);
        }
        else
        {
            services.AddSingleton<OutboxMailSender>();
            services.AddSingleton<IMailSender>(static s => s.GetRequiredService<OutboxMailSender>());
            services.AddSingleton<IOutbox?>(static s => s.GetRequiredService<OutboxMailSender>());
        }

        services.AddScoped<IClubLogic, ClubLogic>();
        services.AddScoped<IMeetingLogic, MeetingLogic>();
        services.AddScoped<IAgendaLogic, AgendaLogic>();
        services.AddScoped<IResultLogic, ResultLogic>();
        services.AddScoped<IVoterLogic, VoterLogic>();
        services.AddScoped<IBallotLogic, BallotLogic>();
        services.AddScoped<IAdminAuthLogic, AdminAuthLogic>();
        services.AddScoped<ITrialLogic, TrialLogic>();
        return services;
    }
}
