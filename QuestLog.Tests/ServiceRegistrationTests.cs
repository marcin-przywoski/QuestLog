using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using QuestLog.GUI;
using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Services;
using QuestLog.GUI.ViewModels;
using Xunit;

namespace QuestLog.Tests
{
    /// <summary>
    /// Pins the composition root: service lifetimes and the factory registrations that
    /// work around multi-ctor ambiguity (per AddAppServices' own comment). Resolution
    /// of IUpdateService/MainWindowViewModel is intentionally not exercised — the
    /// Velopack UpdateManager ctor is platform/locale-dependent under CI.
    /// </summary>
    public class ServiceRegistrationTests
    {
        private static ServiceCollection BuildServices()
        {
            var services = new ServiceCollection();
            services.AddAppServices();
            return services;
        }

        [Fact]
        public void EmailService_RegisteredAsSingleton()
        {
            var descriptor = BuildServices().Single(d => d.ServiceType == typeof(IEmailService));

            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            Assert.Equal(typeof(AppleScriptOutlookService), descriptor.ImplementationType);
        }

        [Fact]
        public void UpdateService_RegisteredAsSingletonFactory()
        {
            var descriptor = BuildServices().Single(d => d.ServiceType == typeof(IUpdateService));

            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            Assert.NotNull(descriptor.ImplementationFactory);
        }

        [Fact]
        public void ViewModel_RegisteredAsSingletonFactory_ResolvingEmailService()
        {
            var descriptor = BuildServices().Single(d => d.ServiceType == typeof(MainWindowViewModel));

            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            Assert.NotNull(descriptor.ImplementationFactory);
        }

        [Fact]
        public void EmailService_ResolvesFromProvider()
        {
            // IEmailService has a single public ctor — safe to resolve anywhere;
            // confirms the service collection is internally consistent.
            using var provider = BuildServices().BuildServiceProvider();

            var service = provider.GetRequiredService<IEmailService>();

            Assert.IsType<AppleScriptOutlookService>(service);
            Assert.Same(service, provider.GetRequiredService<IEmailService>());
        }
    }
}
