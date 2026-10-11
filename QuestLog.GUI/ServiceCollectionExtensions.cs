using Microsoft.Extensions.DependencyInjection;
using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Services;
using QuestLog.GUI.ViewModels;

namespace QuestLog.GUI
{
    /// <summary>
    /// Composition-root wiring per the Avalonia DI guidance:
    /// https://docs.avaloniaui.net/docs/app-development/dependency-injection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddSingleton<IEmailService, AppleScriptOutlookService>();

            // Factory registrations: both types expose multiple public ctors, which the
            // Microsoft DI container treats as ambiguous, and the VM's parameterless ctor
            // is reserved for the XAML loader (design-time fakes) - never for DI.
            services.AddSingleton<IUpdateService>(_ => new VelopackUpdateService());
            services.AddSingleton(sp => new MainWindowViewModel(
                sp.GetRequiredService<IEmailService>(),
                sp.GetRequiredService<IUpdateService>()));

            return services;
        }
    }
}
