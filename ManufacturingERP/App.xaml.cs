using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.ViewModels;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Core;

namespace ManufacturingERP
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }
        public IServiceProvider Services => AppHost!.Services;

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Database Context Factory
                    services.AddDbContextFactory<ManufacturingContext>(options =>
                        options.UseSqlServer("Server=.\\SQLEXPRESS;Database=ManufacturingERP;Trusted_Connection=True;TrustServerCertificate=True;"),
                        ServiceLifetime.Singleton);

                    // Services
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IAuthService, AuthService>();
                    services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
                    services.AddSingleton<IDatabaseSeeder, DatabaseSeederService>();
                    services.AddSingleton<IProductionService, ProductionService>();
                    services.AddSingleton<IMasterDataService, MasterDataService>();
                    services.AddSingleton<INotificationService, NotificationService>();
                    services.AddSingleton<IActivityService, ActivityService>();
                    services.AddSingleton<IFileService, FileService>();
                    services.AddSingleton<IUserManagementService, UserManagementService>();
                    services.AddSingleton<IPermissionService, PermissionService>();
                    services.AddSingleton<IPartnerService, PartnerService>();
                    services.AddSingleton<IAuditLogService, AuditLogService>();
                    services.AddSingleton<IAccessControlService, AccessControlService>();
                    services.AddSingleton<ISessionMonitorService, SessionMonitorService>();
                    services.AddSingleton<IWarehouseService, WarehouseService>();
                    services.AddSingleton<IQualityControlService, QualityControlService>();
                    services.AddSingleton<IHRService, HRService>();
                    services.AddSingleton<IFinanceService, FinanceService>();

                    // Password Hashing
                    services.AddSingleton<IPasswordHasher, BCryptHasher>();
                    services.AddSingleton<IPasswordHasher, Argon2Hasher>();
                    services.AddSingleton<IPasswordHasher, Sha256Hasher>();
                    services.AddSingleton<PasswordHasherFactory>();

                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<DashboardViewModel>();
                    services.AddSingleton<AdminViewModel>();
                    services.AddSingleton<MasterDataViewModel>();
                    services.AddSingleton<LoginViewModel>();
                    services.AddSingleton<ProductionViewModel>();
                    services.AddSingleton<QualityControlViewModel>();
                    services.AddSingleton<WarehouseViewModel>();
                    services.AddSingleton<HRViewModel>();
                    services.AddSingleton<FinanceViewModel>();
                    services.AddTransient<CreateInvoiceViewModel>();
                    services.AddTransient<CreateWorkOrderViewModel>();
                    services.AddTransient<CreateMaterialViewModel>();
                    services.AddTransient<CreateWarehouseViewModel>();
                    services.AddTransient<ImportExportViewModel>();
                    services.AddTransient<ActivityDetailViewModel>();
                    services.AddTransient<ActivitiesViewModel>();
                    services.AddTransient<ActivityImportExportViewModel>();
                    services.AddTransient<WorkOrderDetailViewModel>();
                    services.AddTransient<CreateFinancialTransactionViewModel>();
                    services.AddTransient<CreateTransactionViewModel>();
                    services.AddTransient<MasterDataImportExportViewModel>();
                    services.AddTransient<WarehouseReportViewModel>();
                    services.AddTransient<CreateEmployeeViewModel>();
                    services.AddTransient<EditEmployeeViewModel>();
                    services.AddTransient<AttendanceConsoleViewModel>();
                    services.AddTransient<ScheduleImportExportViewModel>();

                    
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
            // Set global culture to Vietnamese for DatePicker and other controls
            var culture = new System.Globalization.CultureInfo("vi-VN");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN")));

            await AppHost!.StartAsync();

            // Run database seeder
            var seeder = AppHost.Services.GetRequiredService<IDatabaseSeeder>();
            await seeder.SeedAsync();

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
