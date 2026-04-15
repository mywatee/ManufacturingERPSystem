using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Database Context
                    services.AddDbContext<ManufacturingContext>(options =>
                        options.UseSqlServer("Server=.\\SQLEXPRESS;Database=ManufacturingERP;Trusted_Connection=True;TrustServerCertificate=True;"));

                    // Services
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<IAuthService, AuthService>();

                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<DashboardViewModel>();
                    services.AddSingleton<AdminViewModel>();
                    services.AddSingleton<LoginViewModel>();
                    
                    // Windows
                    services.AddSingleton<MainWindow>(s => new MainWindow()
                    {
                        DataContext = s.GetRequiredService<MainViewModel>()
                    });
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (AppHost)
            {
                await AppHost!.StopAsync();
            }
            base.OnExit(e);
        }
    }
}
