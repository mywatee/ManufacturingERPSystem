using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManufacturingERP.Models;
using ManufacturingERP.Core;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP.Services
{
    public class DatabaseSeederService : IDatabaseSeeder
    {
        private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
        private readonly IPasswordHasher _passwordHasher;

        public DatabaseSeederService(IDbContextFactory<ManufacturingContext> contextFactory, PasswordHasherFactory hasherFactory)
        {
            _contextFactory = contextFactory;
            // Use BCrypt as default for seeding
            _passwordHasher = hasherFactory.GetHasherByName("bcrypt (Khuyến nghị)");
        }

        public async Task SeedAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Phase 1: SELECTIVE NUCLEAR WIPE (Clear all business data, keep Accounts/Roles)
            // DISABLED: To allow data persistence
            /*
            try
            {
                // Disable constraints, delete business data, re-enable constraints
                await context.Database.ExecuteSqlRawAsync(@"
                    -- Disable all constraints
                    EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

                    -- Delete business data (Order doesn't matter now)
                    DELETE FROM WorkOrderProgress;
                    DELETE FROM QualityControl;
                    DELETE FROM WorkOrderItems;
                    DELETE FROM WorkOrders;
                    DELETE FROM StockTransactions;
                    DELETE FROM Routings;
                    DELETE FROM BOM;
                    DELETE FROM Inventory;
                    DELETE FROM Materials;
                    DELETE FROM Warehouses;
                    DELETE FROM Notifications;
                    DELETE FROM AuditLogs;
                    DELETE FROM ActivityLogs;
                    DELETE FROM PasswordResetRequests;

                    -- Re-enable all constraints
                    EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';
                ");
            }
            catch { }
            */

            // Phase 2 & 3: Schema Migrations
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF COL_LENGTH('WorkOrderProgress', 'ProducedQty') IS NULL
                    BEGIN
                        ALTER TABLE WorkOrderProgress ADD ProducedQty INT NULL;
                        ALTER TABLE WorkOrderProgress ADD DefectQty INT NULL;
                        ALTER TABLE WorkOrderProgress ADD StageName NVARCHAR(MAX) NULL;
                        ALTER TABLE WorkOrderProgress ADD RecordedBy NVARCHAR(MAX) NULL;
                        ALTER TABLE WorkOrderProgress ADD Notes NVARCHAR(MAX) NULL;
                    END
                    
                    IF COL_LENGTH('Users', 'FailedLoginAttempts') IS NULL
                    BEGIN
                        ALTER TABLE Users ADD FailedLoginAttempts INT NOT NULL DEFAULT 0;
                        ALTER TABLE Users ADD LockoutEnd DATETIME2 NULL;
                    END

                    IF COL_LENGTH('Materials', 'Status') IS NULL
                    BEGIN
                        ALTER TABLE Materials ADD Status NVARCHAR(50) NULL;
                    END

                    IF COL_LENGTH('Materials', 'UnitPrice') IS NULL
                    BEGIN
                        ALTER TABLE Materials ADD UnitPrice DECIMAL(18, 2) NULL;
                    END

                    IF COL_LENGTH('Invoices', 'Reference') IS NULL
                    BEGIN
                        ALTER TABLE Invoices ADD Reference NVARCHAR(100) NULL;
                    END

                    IF OBJECT_ID('Partners', 'U') IS NULL
                    BEGIN
                        CREATE TABLE Partners (
                            PartnerId INT PRIMARY KEY IDENTITY(1,1),
                            PartnerCode NVARCHAR(50) NOT NULL,
                            PartnerName NVARCHAR(255) NOT NULL,
                            PartnerType NVARCHAR(50),
                            ContactPerson NVARCHAR(100),
                            Phone NVARCHAR(20),
                            Email NVARCHAR(100),
                            Address NVARCHAR(500),
                            TaxCode NVARCHAR(50),
                            Status NVARCHAR(50) DEFAULT 'Hoạt động',
                            Note NVARCHAR(MAX) NULL,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        );
                    END
                ");

                // HR Schema Updates
                await context.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID('Employees', 'U') IS NULL
                    BEGIN
                        CREATE TABLE Employees (
                            EmployeeId INT PRIMARY KEY IDENTITY(1,1),
                            EmployeeCode NVARCHAR(20) NOT NULL,
                            FullName NVARCHAR(100) NOT NULL,
                            Email NVARCHAR(100) NULL,
                            Phone NVARCHAR(20) NULL,
                            Department NVARCHAR(50) NULL,
                            Position NVARCHAR(50) NULL,
                            JoinDate DATETIME NULL,
                            BasicSalary DECIMAL(18, 2) NULL,
                            Status NVARCHAR(20) DEFAULT 'Active' NOT NULL,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        );
                    END

                    IF OBJECT_ID('Attendances', 'U') IS NULL
                    BEGIN
                        CREATE TABLE Attendances (
                            AttendanceId INT PRIMARY KEY IDENTITY(1,1),
                            EmployeeId INT NOT NULL,
                            Date DATE NOT NULL,
                            CheckIn TIME NULL,
                            CheckOut TIME NULL,
                            Status NVARCHAR(50) NULL,
                            Note NVARCHAR(500) NULL,
                            CONSTRAINT FK_Attendances_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId) ON DELETE CASCADE
                        );
                    END
                ");

                // Aggressive column addition for Users
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Users]') AND name = 'EmployeeId')
                    BEGIN
                        -- Just in case there's a column with different casing or similar name, we ensure exactly 'EmployeeId'
                        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Users]') AND name = 'EmployeeID')
                            EXEC sp_rename '[dbo].[Users].EmployeeID', 'EmployeeId', 'COLUMN';
                        ELSE
                            ALTER TABLE [dbo].[Users] ADD [EmployeeId] INT NULL;
                    END
                ");

                // Create Shifts and Schedules if missing
                await context.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID('Shifts', 'U') IS NULL
                    BEGIN
                        CREATE TABLE Shifts (
                            ShiftId INT PRIMARY KEY IDENTITY(1,1),
                            ShiftCode NVARCHAR(20) NULL,
                            ShiftName NVARCHAR(100) NOT NULL,
                            StartTime TIME NOT NULL,
                            EndTime TIME NOT NULL,
                            BreakStartTime TIME NULL,
                            BreakEndTime TIME NULL,
                            ColorHex NVARCHAR(7) NULL,
                            IsActive BIT NOT NULL DEFAULT 1,
                            Note NVARCHAR(MAX) NULL
                        );
                    END
                    ELSE
                    BEGIN
                        -- Add missing columns if table exists
                        IF COL_LENGTH('Shifts', 'ShiftCode') IS NULL ALTER TABLE Shifts ADD ShiftCode NVARCHAR(20) NULL;
                        IF COL_LENGTH('Shifts', 'BreakStartTime') IS NULL ALTER TABLE Shifts ADD BreakStartTime TIME NULL;
                        IF COL_LENGTH('Shifts', 'BreakEndTime') IS NULL ALTER TABLE Shifts ADD BreakEndTime TIME NULL;
                        IF COL_LENGTH('Shifts', 'ColorHex') IS NULL ALTER TABLE Shifts ADD ColorHex NVARCHAR(7) NULL;
                        IF COL_LENGTH('Shifts', 'IsActive') IS NULL ALTER TABLE Shifts ADD IsActive BIT NOT NULL DEFAULT 1;
                    END

                    IF OBJECT_ID('EmployeeSchedules', 'U') IS NULL
                    BEGIN
                        CREATE TABLE EmployeeSchedules (
                            ScheduleId INT PRIMARY KEY IDENTITY(1,1),
                            UserId INT NOT NULL,
                            ShiftId INT NOT NULL,
                            WorkDate DATE NOT NULL,
                            MachineCode NVARCHAR(50) NULL,
                            Note NVARCHAR(MAX) NULL,
                            CONSTRAINT FK_Schedules_Users FOREIGN KEY (UserId) REFERENCES [dbo].[Users](UserId) ON DELETE CASCADE,
                            CONSTRAINT FK_Schedules_Shifts FOREIGN KEY (ShiftId) REFERENCES Shifts(ShiftId) ON DELETE CASCADE
                        );
                    END

                ");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical Migration Error: {ex.Message}");
                throw;
            }

            // 1. Seed Roles
            await EnsureRolesAsync(context);

            // 2. Seed demo users (6 roles)
            await EnsureDemoUsersAsync(context);

            // 2.1 Seed default System Settings (if missing)
            async Task EnsureSettingAsync(string key, string value, string? description = null)
            {
                var exists = await context.SystemSettings.AnyAsync(s => s.SettingKey == key);
                if (exists) return;
                context.SystemSettings.Add(new SystemSetting
                {
                    SettingKey = key,
                    SettingValue = value,
                    Description = description,
                    LastUpdated = DateTime.Now
                });
            }

            await EnsureSettingAsync("MinPasswordLength", "8", "Độ dài mật khẩu tối thiểu");
            await EnsureSettingAsync("HashAlgorithm", "bcrypt (Khuyến nghị)", "Thuật toán băm mật khẩu mặc định");
            await EnsureSettingAsync("SessionTimeout", "30", "Thời gian hết phiên (phút)");
            await EnsureSettingAsync("MaxLoginAttempts", "5", "Số lần đăng nhập sai tối đa");
            await EnsureSettingAsync("IsComplexityRequired", "True", "Yêu cầu mật khẩu phức tạp");
            await EnsureSettingAsync("IsRotationRequired", "True", "Bắt buộc đổi mật khẩu định kỳ");
            await EnsureSettingAsync("Is2FARequired", "False", "Bật xác thực 2 yếu tố");
            await context.SaveChangesAsync();

            // 2.2 Seed / sync Role Permissions (RBAC)
            await EnsureRolePermissionsAsync(context);

            // 2.3 Seed Default Shifts
            if (!await context.Shifts.AnyAsync())
            {
                var shifts = new List<Shift>
                {
                    new Shift 
                    { 
                        ShiftCode = "HC",
                        ShiftName = "Hành chính", 
                        StartTime = new TimeOnly(8, 0, 0), 
                        EndTime = new TimeOnly(17, 30, 0),
                        BreakStartTime = new TimeOnly(12, 0, 0),
                        BreakEndTime = new TimeOnly(13, 0, 0),
                        ColorHex = "#4A90E2" // Blue
                    },
                    new Shift 
                    { 
                        ShiftCode = "C1",
                        ShiftName = "Ca sáng (C1)", 
                        StartTime = new TimeOnly(6, 0, 0), 
                        EndTime = new TimeOnly(14, 0, 0),
                        BreakStartTime = new TimeOnly(10, 0, 0),
                        BreakEndTime = new TimeOnly(10, 30, 0),
                        ColorHex = "#5C6BC0" // Indigo
                    },
                    new Shift 
                    { 
                        ShiftCode = "C2",
                        ShiftName = "Ca chiều (C2)", 
                        StartTime = new TimeOnly(14, 0, 0), 
                        EndTime = new TimeOnly(22, 0, 0),
                        BreakStartTime = new TimeOnly(18, 0, 0),
                        BreakEndTime = new TimeOnly(18, 30, 0),
                        ColorHex = "#26A69A" // Teal
                    },
                    new Shift 
                    { 
                        ShiftCode = "C3",
                        ShiftName = "Ca đêm (C3)", 
                        StartTime = new TimeOnly(22, 0, 0), 
                        EndTime = new TimeOnly(6, 0, 0),
                        BreakStartTime = new TimeOnly(2, 0, 0),
                        BreakEndTime = new TimeOnly(2, 30, 0),
                        ColorHex = "#7E57C2" // Deep Purple
                    }
                };

                context.Shifts.AddRange(shifts);
                await context.SaveChangesAsync();

                // Assign schedules for today
                var users = await context.Users.ToListAsync();
                var adminShift = shifts[0];
                var workerShift = shifts[1];

                foreach (var user in users)
                {
                    context.EmployeeSchedules.Add(new EmployeeSchedule
                    {
                        UserId = user.UserId,
                        ShiftId = user.Username == "admin" ? adminShift.ShiftId : workerShift.ShiftId,
                        WorkDate = DateOnly.FromDateTime(DateTime.Today),
                        MachineCode = "LINE-01"
                    });
                }
                await context.SaveChangesAsync();
            }

            // 2.4 Seed Partners (Suppliers and Customers)
            if (!await context.Partners.AnyAsync())
            {
                var partners = new List<Partner>
                {
                    new Partner { PartnerCode = "NCC001", PartnerName = "Công ty TNHH Thép Việt", PartnerType = "NCC", Status = "Hoạt động", Email = "contact@thepviet.vn", Phone = "0281234567" },
                    new Partner { PartnerCode = "NCC002", PartnerName = "Nhựa Bình Minh", PartnerType = "NCC", Status = "Hoạt động", Email = "info@binhminhplastic.com.vn" },
                    new Partner { PartnerCode = "KH001", PartnerName = "Tập đoàn Xây dựng Hòa Bình", PartnerType = "KH", Status = "Hoạt động", Phone = "0287654321" },
                    new Partner { PartnerCode = "KH002", PartnerName = "Coteccons", PartnerType = "KH", Status = "Hoạt động" }
                };
                context.Partners.AddRange(partners);
                await context.SaveChangesAsync();
            }

            // 2.5 Seed Invoices & Financial Transactions
            if (!await context.Invoices.AnyAsync())
            {
                var ncc1 = await context.Partners.FirstOrDefaultAsync(p => p.PartnerCode == "NCC001");
                var kh1 = await context.Partners.FirstOrDefaultAsync(p => p.PartnerCode == "KH001");

                if (ncc1 != null)
                {
                    // Accounts Payable
                    context.Invoices.Add(new Invoice
                    {
                        InvoiceCode = "INV-PUR-001",
                        PartnerId = ncc1.PartnerId,
                        Type = "AP",
                        IssueDate = DateTime.Now.AddDays(-15),
                        DueDate = DateTime.Now.AddDays(15),
                        TotalAmount = 150000000,
                        PaidAmount = 50000000,
                        Status = "Chờ thanh toán",
                        Note = "Tiền nguyên vật liệu tháng 4"
                    });
                }

                if (kh1 != null)
                {
                    // Accounts Receivable
                    context.Invoices.Add(new Invoice
                    {
                        InvoiceCode = "INV-SAL-001",
                        PartnerId = kh1.PartnerId,
                        Type = "AR",
                        IssueDate = DateTime.Now.AddDays(-30),
                        DueDate = DateTime.Now.AddDays(-1),
                        TotalAmount = 300000000,
                        PaidAmount = 0,
                        Status = "Quá hạn",
                        Note = "Thanh toán đợt 1 công trình A"
                    });
                }

                // Financial Transactions
                context.FinancialTransactions.AddRange(new List<FinancialTransaction>
                {
                    new FinancialTransaction { Date = DateTime.Now.AddDays(-2), Type = "Thu", Amount = 50000000, Category = "Thu tiền KH", Method = "Chuyển khoản", Description = "Tạm ứng hợp đồng 001" },
                    new FinancialTransaction { Date = DateTime.Now.AddDays(-5), Type = "Chi", Amount = 20000000, Category = "Thanh toán NCC", Method = "Tiền mặt", Description = "Mua văn phòng phẩm" },
                    new FinancialTransaction { Date = DateTime.Now.AddDays(-10), Type = "Chi", Amount = 15000000, Category = "Lương nhân viên", Method = "Chuyển khoản", Description = "Lương tháng 4" }
                });

                await context.SaveChangesAsync();
            }

            // Phase 3: Historical demo data for 10 months (Jul 2025 - May 2026)
            await SeedHistoricalDataAsync(context);
        }

        private async Task SeedHistoricalDataAsync(ManufacturingContext context)
        {
            if (await context.SystemSettings.AnyAsync(s => s.SettingKey == "HistoricalDataSeeded"))
                return;

            var rng = new Random(42);

            // ==================== MATERIALS ====================
            if (!await context.Materials.AnyAsync())
            {
                context.Materials.AddRange(new List<Material>
                {
                    new() { MaterialCode = "NVL001", MaterialName = "Thép tấm HRC 3mm", Unit = "tấn", Category = "Nguyên liệu thô", MinStock = 20, UnitPrice = 15000000m },
                    new() { MaterialCode = "NVL002", MaterialName = "Nhôm định hình 6061", Unit = "tấn", Category = "Nguyên liệu thô", MinStock = 10, UnitPrice = 45000000m },
                    new() { MaterialCode = "NVL003", MaterialName = "Đồng thau C3604", Unit = "tấn", Category = "Nguyên liệu thô", MinStock = 5, UnitPrice = 85000000m },
                    new() { MaterialCode = "NVL004", MaterialName = "Inox 304", Unit = "tấn", Category = "Nguyên liệu thô", MinStock = 8, UnitPrice = 62000000m },
                    new() { MaterialCode = "NVL005", MaterialName = "Nhựa ABS", Unit = "tấn", Category = "Nguyên liệu thô", MinStock = 8, UnitPrice = 35000000m },
                    new() { MaterialCode = "VT001", MaterialName = "Bulong M8", Unit = "cái", Category = "Vật tư phụ", MinStock = 5000, UnitPrice = 2000m },
                    new() { MaterialCode = "VT002", MaterialName = "Ốc vít M6", Unit = "cái", Category = "Vật tư phụ", MinStock = 10000, UnitPrice = 500m },
                    new() { MaterialCode = "VT003", MaterialName = "Vòng bi 6205", Unit = "cái", Category = "Vật tư phụ", MinStock = 500, UnitPrice = 35000m },
                    new() { MaterialCode = "VT004", MaterialName = "Gioăng cao su", Unit = "mét", Category = "Vật tư phụ", MinStock = 200, UnitPrice = 15000m },
                    new() { MaterialCode = "VT005", MaterialName = "Sơn tĩnh điện", Unit = "kg", Category = "Vật tư phụ", MinStock = 300, UnitPrice = 80000m },
                    new() { MaterialCode = "VT006", MaterialName = "Dầu bôi trơn", Unit = "lít", Category = "Vật tư tiêu hao", MinStock = 100, UnitPrice = 45000m },
                    new() { MaterialCode = "VT007", MaterialName = "Dung dịch làm mát", Unit = "lít", Category = "Vật tư tiêu hao", MinStock = 200, UnitPrice = 25000m },
                    new() { MaterialCode = "SP001", MaterialName = "Khung máy loại A", Unit = "cái", Category = "Thành phẩm", MinStock = 500, UnitPrice = 500000m },
                    new() { MaterialCode = "SP002", MaterialName = "Bệ đỡ máy B200", Unit = "cái", Category = "Thành phẩm", MinStock = 300, UnitPrice = 350000m },
                    new() { MaterialCode = "SP003", MaterialName = "Trục truyền động T40", Unit = "cái", Category = "Thành phẩm", MinStock = 200, UnitPrice = 750000m },
                    new() { MaterialCode = "SP004", MaterialName = "Vỏ bảo vệ V100", Unit = "cái", Category = "Thành phẩm", MinStock = 400, UnitPrice = 250000m },
                });
                await context.SaveChangesAsync();
            }

            var matByCode = await context.Materials.ToDictionaryAsync(m => m.MaterialCode);
            var sp001 = matByCode["SP001"]; var sp002 = matByCode["SP002"];
            var sp003 = matByCode["SP003"]; var sp004 = matByCode["SP004"];
            var nvl001 = matByCode["NVL001"]; var nvl002 = matByCode["NVL002"];
            var nvl003 = matByCode["NVL003"]; var nvl004 = matByCode["NVL004"];
            var nvl005 = matByCode["NVL005"];
            var vt001 = matByCode["VT001"]; var vt002 = matByCode["VT002"];
            var vt003 = matByCode["VT003"]; var vt004 = matByCode["VT004"];
            var vt005 = matByCode["VT005"]; var vt006 = matByCode["VT006"]; var vt007 = matByCode["VT007"];

            // ==================== WAREHOUSES ====================
            async Task EnsureWarehouseAsync(string code, string name, string loc, decimal cap, string type)
            {
                if (!await context.Warehouses.AnyAsync(w => w.Code == code))
                {
                    context.Warehouses.Add(new Warehouse
                    {
                        Code = code, WarehouseName = name, Location = loc,
                        Capacity = cap, CapacityUnit = "Tấn", Status = "Hoạt động",
                        WarehouseType = type, SafetyThreshold = 10
                    });
                }
            }

            await EnsureWarehouseAsync("KHO-NVL", "Kho Nguyên Vật Liệu", "Phân xưởng A", 500m, "Nguyên liệu");
            await EnsureWarehouseAsync("KHO-TP", "Kho Thành Phẩm", "Phân xưởng B", 1000m, "Thành phẩm");
            await EnsureWarehouseAsync("KHO-VT", "Kho Vật Tư Phụ", "Phân xưởng C", 300m, "Vật tư");
            await context.SaveChangesAsync();

            var khoNvl = await context.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-NVL");
            var khoTp = await context.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-TP");
            var khoVt = await context.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-VT");

            if (khoNvl == null || khoTp == null || khoVt == null) return;

            // ==================== INVENTORY ====================
            if (!await context.Inventories.AnyAsync())
            {
                context.Inventories.AddRange(new List<Inventory>
                {
                    new() { MaterialId = nvl001.MaterialId, WarehouseId = khoNvl.WarehouseId, CurrentStock = 25, WarehouseLocation = "Kệ A-01" },
                    new() { MaterialId = nvl002.MaterialId, WarehouseId = khoNvl.WarehouseId, CurrentStock = 8, WarehouseLocation = "Kệ A-02" },
                    new() { MaterialId = nvl003.MaterialId, WarehouseId = khoNvl.WarehouseId, CurrentStock = 3, WarehouseLocation = "Kệ A-03" },
                    new() { MaterialId = nvl004.MaterialId, WarehouseId = khoNvl.WarehouseId, CurrentStock = 6, WarehouseLocation = "Kệ A-04" },
                    new() { MaterialId = nvl005.MaterialId, WarehouseId = khoNvl.WarehouseId, CurrentStock = 12, WarehouseLocation = "Kệ A-05" },
                    new() { MaterialId = vt001.MaterialId, WarehouseId = khoVt.WarehouseId, CurrentStock = 8000, WarehouseLocation = "Kệ B-01" },
                    new() { MaterialId = vt002.MaterialId, WarehouseId = khoVt.WarehouseId, CurrentStock = 15000, WarehouseLocation = "Kệ B-02" },
                    new() { MaterialId = vt003.MaterialId, WarehouseId = khoVt.WarehouseId, CurrentStock = 300, WarehouseLocation = "Kệ B-03" },
                    new() { MaterialId = vt004.MaterialId, WarehouseId = khoVt.WarehouseId, CurrentStock = 150, WarehouseLocation = "Kệ B-04" },
                    new() { MaterialId = vt005.MaterialId, WarehouseId = khoVt.WarehouseId, CurrentStock = 500, WarehouseLocation = "Kệ B-05" },
                    new() { MaterialId = vt006.MaterialId, WarehouseId = khoVt.WarehouseId, CurrentStock = 80, WarehouseLocation = "Kệ B-06" },
                    new() { MaterialId = vt007.MaterialId, WarehouseId = khoVt.WarehouseId, CurrentStock = 250, WarehouseLocation = "Kệ B-07" },
                    new() { MaterialId = sp001.MaterialId, WarehouseId = khoTp.WarehouseId, CurrentStock = 3000, WarehouseLocation = "Kệ C-01" },
                    new() { MaterialId = sp002.MaterialId, WarehouseId = khoTp.WarehouseId, CurrentStock = 1500, WarehouseLocation = "Kệ C-02" },
                    new() { MaterialId = sp003.MaterialId, WarehouseId = khoTp.WarehouseId, CurrentStock = 4500, WarehouseLocation = "Kệ C-03" },
                    new() { MaterialId = sp004.MaterialId, WarehouseId = khoTp.WarehouseId, CurrentStock = 800, WarehouseLocation = "Kệ C-04" },
                });
                await context.SaveChangesAsync();
            }

            // ==================== BOM ====================
            if (!await context.Boms.AnyAsync())
            {
                context.Boms.AddRange(new List<Bom>
                {
                    // SP001 = 2kg thép + 4 bulong + 8 ốc vít + 0.1kg sơn
                    new() { ParentId = sp001.MaterialId, ChildId = nvl001.MaterialId, QuantityPerUnit = 2m },
                    new() { ParentId = sp001.MaterialId, ChildId = vt001.MaterialId, QuantityPerUnit = 4m },
                    new() { ParentId = sp001.MaterialId, ChildId = vt002.MaterialId, QuantityPerUnit = 8m },
                    new() { ParentId = sp001.MaterialId, ChildId = vt005.MaterialId, QuantityPerUnit = 0.1m },
                    // SP002 = 1.5kg nhôm + 2 vòng bi + 1m gioăng
                    new() { ParentId = sp002.MaterialId, ChildId = nvl002.MaterialId, QuantityPerUnit = 1.5m },
                    new() { ParentId = sp002.MaterialId, ChildId = vt003.MaterialId, QuantityPerUnit = 2m },
                    new() { ParentId = sp002.MaterialId, ChildId = vt004.MaterialId, QuantityPerUnit = 1m },
                    // SP003 = 3kg inox + 0.5kg đồng + 4 vòng bi
                    new() { ParentId = sp003.MaterialId, ChildId = nvl004.MaterialId, QuantityPerUnit = 3m },
                    new() { ParentId = sp003.MaterialId, ChildId = nvl003.MaterialId, QuantityPerUnit = 0.5m },
                    new() { ParentId = sp003.MaterialId, ChildId = vt003.MaterialId, QuantityPerUnit = 4m },
                    // SP004 = 2kg nhựa ABS + 8 bulong + 0.2kg sơn
                    new() { ParentId = sp004.MaterialId, ChildId = nvl005.MaterialId, QuantityPerUnit = 2m },
                    new() { ParentId = sp004.MaterialId, ChildId = vt001.MaterialId, QuantityPerUnit = 8m },
                    new() { ParentId = sp004.MaterialId, ChildId = vt005.MaterialId, QuantityPerUnit = 0.2m },
                });
                await context.SaveChangesAsync();
            }

            // ==================== ROUTINGS ====================
            if (!await context.Routings.AnyAsync())
            {
                void AddRouting(Material product, int step, string name, string center, int estTime, string output)
                    => context.Routings.Add(new Routing { ProductId = product.MaterialId, StepNumber = step, StepName = name, WorkCenter = center, EstimatedTime = estTime, OutputDescription = output });

                AddRouting(sp001, 1, "Cắt thép tấm", "Máy cắt CNC", 10, "Phôi thép kích thước chuẩn");
                AddRouting(sp001, 2, "Gia công phay", "Máy phay CNC", 30, "Khung máy thô");
                AddRouting(sp001, 3, "Hàn kết cấu", "Máy hàn MIG", 20, "Khung máy đã hàn");
                AddRouting(sp001, 4, "Sơn tĩnh điện", "Buồng sơn", 25, "Khung máy hoàn thiện");
                AddRouting(sp001, 5, "Kiểm tra chất lượng", "Bàn kiểm tra", 15, "Sản phẩm đạt QC");

                AddRouting(sp002, 1, "Cắt nhôm", "Máy cắt CNC", 8, "Phôi nhôm");
                AddRouting(sp002, 2, "Khoan & Tarô", "Máy khoan", 15, "Bệ đỡ thô");
                AddRouting(sp002, 3, "Lắp vòng bi & gioăng", "Bàn lắp ráp", 12, "Bệ đỡ hoàn chỉnh");
                AddRouting(sp002, 4, "Kiểm tra chất lượng", "Bàn kiểm tra", 10, "Sản phẩm đạt QC");

                AddRouting(sp003, 1, "Tiện thô", "Máy tiện CNC", 25, "Phôi trục thô");
                AddRouting(sp003, 2, "Tiện tinh", "Máy tiện CNC", 20, "Trục đã tiện tinh");
                AddRouting(sp003, 3, "Mài & đánh bóng", "Máy mài", 15, "Trục đã đánh bóng");
                AddRouting(sp003, 4, "Lắp vòng bi", "Bàn lắp ráp", 10, "Trục hoàn chỉnh");
                AddRouting(sp003, 5, "Kiểm tra chất lượng", "Bàn kiểm tra", 15, "Sản phẩm đạt QC");

                AddRouting(sp004, 1, "Ép nhựa", "Máy ép nhựa", 15, "Vỏ nhựa thô");
                AddRouting(sp004, 2, "Cắt ba via", "Bàn thủ công", 8, "Vỏ nhựa sạch bavia");
                AddRouting(sp004, 3, "Khoan lỗ", "Máy khoan", 10, "Vỏ đã khoan lỗ");
                AddRouting(sp004, 4, "Sơn tĩnh điện", "Buồng sơn", 20, "Vỏ hoàn thiện");
                AddRouting(sp004, 5, "Kiểm tra chất lượng", "Bàn kiểm tra", 10, "Sản phẩm đạt QC");

                await context.SaveChangesAsync();
            }

            // ==================== ADDITIONAL PARTNERS ====================
            if (!await context.Partners.AnyAsync(p => p.PartnerCode == "NCC003"))
            {
                context.Partners.AddRange(new List<Partner>
                {
                    new() { PartnerCode = "NCC003", PartnerName = "Công ty CP Vật tư Công nghiệp Hà Nội", PartnerType = "NCC", ContactPerson = "Nguyễn Văn M", Phone = "0241234567", Status = "Hoạt động" },
                    new() { PartnerCode = "NCC004", PartnerName = "Tập đoàn Hóa chất Việt Nam", PartnerType = "NCC", ContactPerson = "Lê Văn N", Phone = "0247654321", Status = "Hoạt động" },
                    new() { PartnerCode = "KH003", PartnerName = "Công ty CP Cơ khí Xây dựng Số 1", PartnerType = "KH", ContactPerson = "Trần Văn P", Phone = "0283456789", Status = "Hoạt động" },
                    new() { PartnerCode = "KH004", PartnerName = "Nhà máy Đóng tàu Bạch Đằng", PartnerType = "KH", ContactPerson = "Phạm Văn Q", Phone = "0319876543", Status = "Hoạt động" },
                    new() { PartnerCode = "KH005", PartnerName = "Công ty CP Sản xuất Ô tô Thành Công", PartnerType = "KH", Status = "Hoạt động" },
                });
                await context.SaveChangesAsync();
            }

            var employees = await context.Employees.ToListAsync();
            var users = await context.Users.Include(u => u.Roles).ToListAsync();
            var userDict = users.ToDictionary(u => u.Username, u => u);
            var empDict = employees.ToDictionary(e => e.EmployeeCode, e => e);
            var allPartners = await context.Partners.ToListAsync();
            var nccPartners = allPartners.Where(p => p.PartnerType == "NCC").ToList();
            var khPartners = allPartners.Where(p => p.PartnerType == "KH").ToList();

            // ==================== HISTORICAL ATTENDANCE (Aug 2025 - May 2026) ====================
            if (!await context.Attendances.AnyAsync(a => a.Date < new DateTime(2026, 1, 1)))
            {
                var startDate = new DateTime(2025, 8, 1);
                var endDate = new DateTime(2026, 5, 27);
                var workingDays = new List<DateTime>();
                for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                        workingDays.Add(d);

                var attendanceList = new List<Attendance>();
                var statusOptions = new[] { "Present", "Present", "Present", "Present", "Late", "Absent" };

                foreach (var emp in employees)
                {
                    foreach (var day in workingDays)
                    {
                        if (emp.EmployeeCode == "EMP001" && day.DayOfWeek == DayOfWeek.Monday && rng.Next(5) == 0)
                            continue;

                        var status = statusOptions[rng.Next(statusOptions.Length)];
                        if (status == "Absent" && rng.Next(3) == 0) continue;

                        TimeSpan? checkIn = status == "Absent" ? null
                            : new TimeSpan(6, 30 + (status == "Late" ? rng.Next(15, 45) : rng.Next(0, 15)), 0);
                        TimeSpan? checkOut = status == "Absent" ? null
                            : new TimeSpan(16, 30 + rng.Next(0, 60), 0);

                        attendanceList.Add(new Attendance
                        {
                            EmployeeId = emp.EmployeeId,
                            Date = day,
                            CheckIn = checkIn,
                            CheckOut = checkOut,
                            Status = status,
                            Note = status == "Late" ? "Đi trễ" : status == "Absent" ? "Nghỉ không phép" : null,
                        });
                    }
                }

                context.Attendances.AddRange(attendanceList);
                await context.SaveChangesAsync();
            }

            // ==================== HISTORICAL PAYROLL (Aug 2025 - May 2026) ====================
            if (!await context.Payrolls.AnyAsync())
            {
                var monthData = new List<(int Year, int Month)>();
                for (int y = 2025; y <= 2026; y++)
                    for (int m = 1; m <= 12; m++)
                        if (new DateTime(y, m, 1) >= new DateTime(2025, 8, 1) && new DateTime(y, m, 1) <= new DateTime(2026, 5, 1))
                            monthData.Add((y, m));

                var payrollList = new List<Payroll>();
                foreach (var (year, month) in monthData)
                {
                    foreach (var emp in employees.Where(e => e.BasicSalary.HasValue && e.BasicSalary > 0))
                    {
                        var prodQty = rng.Next(300, 1200);
                        var unitPrice = emp.PiecesRate ?? 15000;
                        var attendanceBonus = rng.Next(2) == 0 ? 500000 : 0;
                        var qualityBonus = rng.Next(3) == 0 ? 300000 : 0;
                        var total = (emp.BasicSalary ?? 0) + prodQty * unitPrice + attendanceBonus + qualityBonus;

                        payrollList.Add(new Payroll
                        {
                            EmployeeId = emp.EmployeeId,
                            Month = month,
                            Year = year,
                            ProductionQty = prodQty,
                            UnitPrice = unitPrice,
                            BasicSalary = emp.BasicSalary ?? 0,
                            AttendanceBonus = attendanceBonus,
                            QualityBonus = qualityBonus,
                            TotalSalary = total,
                            Status = month < 5 || year < 2026 ? "Đã thanh toán" : (month == 5 ? "Đã duyệt" : "Chưa duyệt"),
                            CreatedAt = new DateTime(year, month, Math.Min(25, DateTime.DaysInMonth(year, month))),
                        });
                    }
                }

                context.Payrolls.AddRange(payrollList);
                await context.SaveChangesAsync();
            }

            // ==================== HISTORICAL WORK ORDERS (Aug 2025 - May 2026) ====================
            if (!await context.WorkOrders.AnyAsync())
            {
                var products = new[] { sp001, sp002, sp003, sp004 };
                var productWeights = new[] { 5, 4, 2, 3 };
                var stageNames = new[] { "Cắt nguyên liệu", "Gia công", "Hàn/Lắp ráp", "Sơn/Hoàn thiện", "Kiểm tra chất lượng (QC)" };
                var machineCodes = new[] { "M-CNC-01", "M-CNC-02", "M-PHAY-01", "M-HAN-01", "M-EP-01", "M-KHOAN-01", "M-MAI-01" };
                var qcUsers = users.Where(u => u.Roles.Any(r => r.RoleName == "QC")).ToList();
                var adminUser = userDict["admin"];
                var plannerUser = userDict["planner"];
                var qlsxUser = userDict["qlsx"];
                var allWorkerUsers = users.Where(u => u.Roles.Any(r => r.RoleName == "Nhân viên vận hành" || r.RoleName == "Admin" || r.RoleName == "Quản lý sản xuất")).Except([adminUser]).ToList();
                var operators = users.Where(u => u.Roles.Any(r => r.RoleName == "Nhân viên vận hành")).ToList();

                for (int monthOffset = 0; monthOffset < 10; monthOffset++)
                {
                    var baseDate = new DateTime(2025, 8, 1).AddMonths(monthOffset);
                    var daysInMonth = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
                    var numWOs = rng.Next(3, 6);
                    var createdBy = monthOffset < 2 ? adminUser : (monthOffset < 6 ? qlsxUser : plannerUser);

                    for (int woIdx = 0; woIdx < numWOs; woIdx++)
                    {
                        var product = products[rng.Next(products.Length)];
                        var day = rng.Next(5, Math.Min(daysInMonth - 5, 25));
                        var startDate = new DateTime(baseDate.Year, baseDate.Month, day, 7, 0, 0);
                        var durationDays = rng.Next(2, 8);
                        var endDate = startDate.AddDays(durationDays);
                        var targetQty = product == sp001 ? rng.Next(200, 600) * 10
                            : product == sp002 ? rng.Next(150, 400) * 10
                            : product == sp003 ? rng.Next(80, 250) * 10
                            : rng.Next(300, 700) * 10;
                        var wocode = $"WO-{baseDate:yyyyMM}-{woIdx + 1:D3}";

                        // Phân bổ trạng thái theo tháng
                        string woStatus;
                        int completionPct;
                        if (monthOffset >= 9)
                        {
                            var st = rng.Next(5);
                            woStatus = st == 0 ? "Planned" : st == 1 ? "Paused" : st == 2 ? "Cancelled" : "Running";
                            completionPct = woStatus == "Running" ? rng.Next(5, 50) : (woStatus == "Planned" ? 0 : rng.Next(5, 30));
                        }
                        else if (monthOffset >= 7)
                        {
                            var st = rng.Next(4);
                            woStatus = st == 0 ? "Paused" : st == 1 ? "Cancelled" : "Running";
                            completionPct = woStatus == "Running" ? rng.Next(10, 70) : rng.Next(5, 40);
                        }
                        else if (monthOffset >= 4)
                        {
                            var st = rng.Next(10);
                            woStatus = st == 0 ? "Paused" : st < 4 ? "Completed" : "Running";
                            completionPct = woStatus == "Completed" ? 100 : (woStatus == "Paused" ? rng.Next(40, 80) : rng.Next(50, 95));
                        }
                        else
                        {
                            var st = rng.Next(10);
                            woStatus = st == 0 ? "Cancelled" : st < 7 ? "Completed" : "Running";
                            completionPct = woStatus == "Completed" ? 100 : (woStatus == "Cancelled" ? rng.Next(10, 60) : rng.Next(70, 95));
                        }
                        var isCompleted = woStatus == "Completed";

                        var wo = new WorkOrder
                        {
                            Wocode = wocode,
                            ProductId = product.MaterialId,
                            TargetQty = targetQty,
                            ActualQty = isCompleted ? targetQty : (int)(targetQty * completionPct / 100m),
                            Status = woStatus,
                            StartDate = startDate,
                            EndDate = isCompleted ? endDate : (monthOffset >= 8 ? endDate.AddMonths(1) : endDate),
                            CreatedBy = createdBy.UserId,
                            CreatedAt = startDate.AddDays(-2),
                            IsUrgent = rng.Next(10) == 0,
                        };
                        context.WorkOrders.Add(wo);
                        await context.SaveChangesAsync();

                        var numItems = rng.Next(1, 3);
                        for (int itemIdx = 0; itemIdx < numItems; itemIdx++)
                        {
                            var itemProduct = numItems > 1 ? products[rng.Next(products.Length)] : product;
                            var itemTargetQty = targetQty / numItems;
                            var itemActualQty = (wo.ActualQty ?? 0) / numItems;

                            var item = new WorkOrderItem
                            {
                                WorkOrderId = wo.Woid,
                                ProductId = itemProduct.MaterialId,
                                TargetQty = itemTargetQty,
                                ActualQty = itemActualQty,
                                Status = woStatus,
                                CreatedAt = wo.CreatedAt,
                            };
                            context.WorkOrderItems.Add(item);
                            await context.SaveChangesAsync();

                            int numStages = rng.Next(3, 6);
                            for (int step = 0; step < numStages; step++)
                            {
                                var worker = allWorkerUsers[rng.Next(allWorkerUsers.Count)];
                                var stage = stageNames[step % stageNames.Length];
                                var stageDone = isCompleted || (completionPct > 50 && step < 2);
                                var producedQty = stageDone ? itemActualQty : (int)(itemActualQty * rng.Next(30, 90) / 100m);
                                var defectQty = stageDone ? rng.Next(0, (int)(producedQty * 0.03m)) : 0;
                                var stepStartTime = startDate.AddDays(step);
                                var stepEndTime = stepStartTime.AddHours(rng.Next(2, 8));

                                context.WorkOrderProgresses.Add(new WorkOrderProgress
                                {
                                    Woid = wo.Woid,
                                    WorkOrderItemId = item.ItemId,
                                    StepNumber = step + 1,
                                    WorkerId = worker.UserId,
                                    Status = stageDone ? "Completed" : "InProgress",
                                    StartTime = stepStartTime,
                                    EndTime = stageDone ? stepEndTime : null,
                                    MachineId = machineCodes[rng.Next(machineCodes.Length)],
                                    ProducedQty = producedQty,
                                    DefectQty = defectQty,
                                    StageName = stage,
                                    RecordedBy = worker.Username,
                                    Notes = stageDone ? null : "Đang thực hiện",
                                });

                                // QC check for final stage
                                if (step == numStages - 1 && qcUsers.Count > 0)
                                {
                                    var qcUser = qcUsers[rng.Next(qcUsers.Count)];
                                    context.QualityControls.Add(new QualityControl
                                    {
                                        Woid = wo.Woid,
                                        WorkOrderItemId = item.ItemId,
                                        StepNumber = step + 1,
                                        PassedQty = producedQty - defectQty,
                                        FailedQty = defectQty,
                                        DefectReason = defectQty > 5 ? "Lỗi kích thước gia công" : null,
                                        InspectorId = qcUser.UserId,
                                        InspectionDate = stepEndTime,
                                    });
                                }
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }

            // ==================== HISTORICAL STOCK TRANSACTIONS ====================
            if (!await context.StockTransactions.AnyAsync())
            {
                var transList = new List<StockTransaction>();
                var thuKho = userDict["thukho"];
                var store1 = userDict["store1"];
                var stockKeepers = new[] { thuKho, store1 };

                for (int monthOffset = 0; monthOffset < 10; monthOffset++)
                {
                    var baseDate = new DateTime(2025, 8, 1).AddMonths(monthOffset);
                    var daysInMonth = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
                    var rawMaterials = matByCode.Values.Where(m => m.Category == "Nguyên liệu thô").ToList();
                    var supplies = matByCode.Values.Where(m => m.Category == "Vật tư phụ" || m.Category == "Vật tư tiêu hao").ToList();
                    var finishedProducts = matByCode.Values.Where(m => m.Category == "Thành phẩm").ToList();

                    // Import transactions (nhập kho)
                    for (int i = 0; i < rng.Next(3, 5); i++)
                    {
                        var rawMat = rawMaterials[rng.Next(rawMaterials.Count)];
                        var qty = rng.Next(5, 30);
                        var day = rng.Next(3, daysInMonth - 2);
                        var ncc = nccPartners[rng.Next(nccPartners.Count)];

                        transList.Add(new StockTransaction
                        {
                            MaterialId = rawMat.MaterialId,
                            WarehouseId = khoNvl.WarehouseId,
                            Type = "Nhập kho",
                            Quantity = qty,
                            TransDate = new DateTime(baseDate.Year, baseDate.Month, day, rng.Next(7, 17), 0, 0),
                            TransBy = stockKeepers[rng.Next(2)].UserId,
                            ReferenceCode = $"PO-{baseDate:yyyyMM}-{i + 1:D3}",
                            Notes = $"Nhập {rawMat.MaterialName} từ {ncc.PartnerName}",
                            PartnerId = ncc.PartnerId,
                        });
                    }

                    // Export transactions (xuất kho cho sản xuất)
                    for (int i = 0; i < rng.Next(3, 5); i++)
                    {
                        var rawMat = rawMaterials[rng.Next(rawMaterials.Count)];
                        var qty = rng.Next(3, 15);
                        var day = rng.Next(5, daysInMonth - 1);

                        transList.Add(new StockTransaction
                        {
                            MaterialId = rawMat.MaterialId,
                            WarehouseId = khoNvl.WarehouseId,
                            Type = "Xuất kho",
                            Quantity = qty,
                            TransDate = new DateTime(baseDate.Year, baseDate.Month, day, rng.Next(7, 17), 0, 0),
                            TransBy = stockKeepers[rng.Next(2)].UserId,
                            ReferenceCode = $"WO-{baseDate:yyyyMM}-{i + 1:D3}",
                            Notes = $"Xuất {rawMat.MaterialName} cho sản xuất",
                        });
                    }

                    // Import supplies
                    for (int i = 0; i < rng.Next(1, 3); i++)
                    {
                        var supply = supplies[rng.Next(supplies.Count)];
                        var qty = supply.Unit == "cái" ? rng.Next(1000, 5000) : rng.Next(50, 300);
                        var day = rng.Next(5, daysInMonth - 2);

                        transList.Add(new StockTransaction
                        {
                            MaterialId = supply.MaterialId,
                            WarehouseId = khoVt.WarehouseId,
                            Type = "Nhập kho",
                            Quantity = qty,
                            TransDate = new DateTime(baseDate.Year, baseDate.Month, day, rng.Next(7, 17), 0, 0),
                            TransBy = stockKeepers[rng.Next(2)].UserId,
                            ReferenceCode = $"PO-SUP-{baseDate:yyyyMM}-{i + 1:D3}",
                            Notes = $"Nhập {supply.MaterialName}",
                        });
                    }

                    // Export finished goods
                    if (monthOffset > 0)
                    {
                        var fp = finishedProducts[rng.Next(finishedProducts.Count)];
                        var qty = rng.Next(500, 3000);
                        var day = rng.Next(10, daysInMonth - 1);
                        var kh = khPartners[rng.Next(khPartners.Count)];

                        transList.Add(new StockTransaction
                        {
                            MaterialId = fp.MaterialId,
                            WarehouseId = khoTp.WarehouseId,
                            Type = "Xuất kho",
                            Quantity = qty,
                            TransDate = new DateTime(baseDate.Year, baseDate.Month, day, rng.Next(7, 17), 0, 0),
                            TransBy = stockKeepers[rng.Next(2)].UserId,
                            ReferenceCode = $"SO-{baseDate:yyyyMM}-{monthOffset:D3}",
                            Notes = $"Xuất bán {fp.MaterialName} cho {kh.PartnerName}",
                            PartnerId = kh.PartnerId,
                        });
                    }
                }

                context.StockTransactions.AddRange(transList);
                await context.SaveChangesAsync();
            }

            // ==================== HISTORICAL INVOICES & FINANCIAL TRANSACTIONS ====================
            if (!await context.FinancialTransactions.AnyAsync(ft => ft.Date < new DateTime(2026, 1, 1)))
            {
                var invList = new List<Invoice>();
                var ftList = new List<FinancialTransaction>();
                var invoiceItems = new List<InvoiceItem>();
                var fpMaterials = matByCode.Values.Where(m => m.Category == "Thành phẩm").ToList();
                var rngFt = new Random(123);

                for (int monthOffset = 0; monthOffset < 10; monthOffset++)
                {
                    var baseDate = new DateTime(2025, 8, 1).AddMonths(monthOffset);
                    var daysInMonth = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);

                    // Purchase invoices (AP)
                    for (int i = 0; i < rngFt.Next(1, 3); i++)
                    {
                        var ncc = nccPartners[rngFt.Next(nccPartners.Count)];
                        var totalAmt = rngFt.Next(5, 30) * 10000000m;
                        var paidAmt = rngFt.Next(2) == 0 ? totalAmt : totalAmt * rngFt.Next(3, 8) / 10m;
                        var day = rngFt.Next(5, daysInMonth - 5);
                        var issueDate = new DateTime(baseDate.Year, baseDate.Month, day);
                        var dueDate = issueDate.AddDays(30);

                        invList.Add(new Invoice
                        {
                            InvoiceCode = $"INV-PUR-{baseDate:yyyyMM}-{i + 1:D3}",
                            PartnerId = ncc.PartnerId,
                            Type = "AP",
                            IssueDate = issueDate,
                            DueDate = dueDate,
                            TotalAmount = totalAmt,
                            PaidAmount = paidAmt,
                            VatRate = 10,
                            VatAmount = totalAmt * 0.1m,
                            Status = paidAmt >= totalAmt ? "Đã thanh toán" : (paidAmt > 0 ? "Một phần" : "Chưa thanh toán"),
                            Note = $"Mua hàng từ {ncc.PartnerName} tháng {baseDate.Month}/{baseDate.Year}",
                        });
                    }

                    // Sales invoices (AR)
                    for (int i = 0; i < rngFt.Next(1, 3); i++)
                    {
                        var kh = khPartners[rngFt.Next(khPartners.Count)];
                        var totalAmt = rngFt.Next(10, 60) * 10000000m;
                        var paidAmt = monthOffset < 7 ? totalAmt : (rngFt.Next(2) == 0 ? totalAmt : totalAmt * rngFt.Next(2, 6) / 10m);
                        var day = rngFt.Next(10, daysInMonth - 3);
                        var issueDate = new DateTime(baseDate.Year, baseDate.Month, day);
                        var dueDate = issueDate.AddDays(45 + rngFt.Next(0, 15));

                        var inv = new Invoice
                        {
                            InvoiceCode = $"INV-SAL-{baseDate:yyyyMM}-{i + 1:D3}",
                            PartnerId = kh.PartnerId,
                            Type = "AR",
                            IssueDate = issueDate,
                            DueDate = dueDate,
                            TotalAmount = totalAmt,
                            PaidAmount = paidAmt,
                            VatRate = 10,
                            VatAmount = totalAmt * 0.1m,
                            Status = paidAmt >= totalAmt ? "Đã thanh toán" : (dueDate < DateTime.Now && paidAmt < totalAmt ? "Quá hạn" : "Chờ thanh toán"),
                            Note = $"Bán hàng cho {kh.PartnerName} tháng {baseDate.Month}/{baseDate.Year}",
                        };
                        invList.Add(inv);
                    }

                    // Financial Transactions (revenue & expenses)
                    var monthRevenue = rngFt.Next(200000000, 500000000);
                    var monthExpenses = rngFt.Next(150000000, 350000000);

                    ftList.Add(new FinancialTransaction
                    {
                        Date = new DateTime(baseDate.Year, baseDate.Month, 15),
                        Type = "Thu",
                        Amount = monthRevenue,
                        Category = "Bán hàng",
                        Method = "Chuyển khoản",
                        Description = $"Doanh thu tháng {baseDate.Month}/{baseDate.Year}",
                    });

                    ftList.Add(new FinancialTransaction
                    {
                        Date = new DateTime(baseDate.Year, baseDate.Month, 25),
                        Type = "Chi",
                        Amount = monthExpenses,
                        Category = "Lương nhân viên",
                        Method = "Chuyển khoản",
                        Description = $"Chi lương tháng {baseDate.Month}/{baseDate.Year}",
                        IsOverhead = true,
                    });

                    ftList.Add(new FinancialTransaction
                    {
                        Date = new DateTime(baseDate.Year, baseDate.Month, rngFt.Next(3, 12)),
                        Type = "Chi",
                        Amount = rngFt.Next(10000000, 50000000),
                        Category = "Thanh toán NCC",
                        Method = rngFt.Next(2) == 0 ? "Chuyển khoản" : "Tiền mặt",
                        Description = $"Thanh toán NCC tháng {baseDate.Month}/{baseDate.Year}",
                    });
                }

                context.Invoices.AddRange(invList);
                await context.SaveChangesAsync();

                // Add invoice items
                foreach (var inv in invList)
                {
                    var numItems = rngFt.Next(1, 4);
                    for (int i = 0; i < numItems; i++)
                    {
                        var fp = fpMaterials[rngFt.Next(fpMaterials.Count)];
                        var qty = inv.Type == "AP" ? rngFt.Next(1, 10) : rngFt.Next(50, 500);
                        var price = inv.Type == "AP" ? (fp.UnitPrice ?? 0m) * rngFt.Next(8, 12) / 10m : (fp.UnitPrice ?? 0m);

                        invoiceItems.Add(new InvoiceItem
                        {
                            InvoiceId = inv.InvoiceId,
                            ProductName = fp.MaterialName,
                            Quantity = qty,
                            UnitPrice = price,
                        });
                    }
                }

                context.InvoiceItems.AddRange(invoiceItems);
                context.FinancialTransactions.AddRange(ftList);
                await context.SaveChangesAsync();
            }

            // ==================== ACTIVITY LOGS ====================
            if (!await context.ActivityLogs.AnyAsync(a => a.Timestamp < new DateTime(2026, 1, 1)))
            {
                var activities = new List<ActivityLog>();
                var activityTypes = new[] { "Đăng nhập", "Đăng xuất", "Tạo mới", "Cập nhật", "Xóa", "Duyệt", "Xuất báo cáo" };
                var allUsernames = new[] { "admin", "qlsx", "operator1", "thukho", "qc", "ketoan", "operator2", "hradmin", "planner", "store1", "qc2" };
                var activityRng = new Random(789);

                for (int monthOffset = 0; monthOffset < 10; monthOffset++)
                {
                    var baseDate = new DateTime(2025, 8, 1).AddMonths(monthOffset);
                    var daysInMonth = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
                    var numActivities = activityRng.Next(15, 30);

                    for (int i = 0; i < numActivities; i++)
                    {
                        var day = activityRng.Next(1, daysInMonth + 1);
                        var hour = activityRng.Next(7, 18);
                        var type = activityTypes[activityRng.Next(activityTypes.Length)];
                        var username = allUsernames[activityRng.Next(allUsernames.Length)];

                        var content = type switch
                        {
                            "Đăng nhập" => $"Người dùng {username} đăng nhập hệ thống",
                            "Đăng xuất" => $"Người dùng {username} đăng xuất khỏi hệ thống",
                            "Tạo mới" => activityRng.Next(3) switch
                            {
                                0 => $"{username} tạo lệnh sản xuất mới",
                                1 => $"{username} tạo phiếu nhập/xuất kho",
                                _ => $"{username} thêm nguyên vật liệu mới",
                            },
                            "Cập nhật" => activityRng.Next(3) switch
                            {
                                0 => $"{username} cập nhật tiến độ sản xuất",
                                1 => $"{username} cập nhật tồn kho",
                                _ => $"{username} chỉnh sửa thông tin nhân viên",
                            },
                            "Xóa" => $"{username} xóa dữ liệu lỗi",
                            "Duyệt" => $"{username} duyệt bảng lương tháng {baseDate.Month}/{baseDate.Year}",
                            _ => $"{username} xuất báo cáo tháng {baseDate.Month}/{baseDate.Year}",
                        };

                        activities.Add(new ActivityLog
                        {
                            ActivityType = type,
                            Content = content,
                            PerformedBy = username,
                            Timestamp = new DateTime(baseDate.Year, baseDate.Month, day, hour, activityRng.Next(0, 60), 0),
                        });
                    }
                }

                context.ActivityLogs.AddRange(activities);
                await context.SaveChangesAsync();
            }

            // Mark historical seed as completed
            if (!await context.SystemSettings.AnyAsync(s => s.SettingKey == "HistoricalDataSeeded"))
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    SettingKey = "HistoricalDataSeeded",
                    SettingValue = DateTime.Now.ToString("yyyy-MM-dd"),
                    Description = "Dữ liệu mẫu lịch sử đã được seed (10 tháng)",
                    LastUpdated = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
        }

        private static readonly (string Username, string FullName, string Email, string Dept, string Position, string RoleName, string EmpCode, decimal Salary)[] DemoUsers =
        [
            ("admin", "Huy Hoàng (Administrator)", "admin@manufacturing-erp.com", "Hành chính", "Administrator", "Admin", "EMP001", 0m),
            ("qlsx", "Phạm Minh Đức", "qlsx@manufacturing-erp.com", "Sản xuất", "Quản lý sản xuất", "Quản lý sản xuất", "DEMO_QLSX", 15000000m),
            ("operator1", "Nguyễn Văn A", "operator1@manufacturing-erp.com", "Sản xuất", "Vận hành máy", "Nhân viên vận hành", "DEMO_OP1", 7500000m),
            ("thukho", "Lê Văn C", "thukho@manufacturing-erp.com", "Kho bãi", "Quản lý kho", "Quản lý kho", "DEMO_THUKHO", 12000000m),
            ("qc", "Trần Thị B", "qc@manufacturing-erp.com", "Kiểm soát chất lượng", "QC Inspector", "QC", "DEMO_QC", 8200000m),
            ("ketoan", "Hoàng Thị Dung", "ketoan@manufacturing-erp.com", "Kế toán", "Kế toán viên", "Kế toán", "DEMO_KETOAN", 10000000m),
            ("operator2", "Nguyễn Văn B", "operator2@manufacturing-erp.com", "Sản xuất", "Vận hành máy", "Nhân viên vận hành", "DEMO_OP2", 7500000m),
            ("operator3", "Trần Văn C", "operator3@manufacturing-erp.com", "Sản xuất", "Vận hành máy", "Nhân viên vận hành", "DEMO_OP3", 7500000m),
            ("operator4", "Lê Thị D", "operator4@manufacturing-erp.com", "Sản xuất", "Vận hành máy", "Nhân viên vận hành", "DEMO_OP4", 7500000m),
            ("operator5", "Phạm Văn E", "operator5@manufacturing-erp.com", "Sản xuất", "Vận hành máy", "Nhân viên vận hành", "DEMO_OP5", 7500000m),
            ("worker1", "Hoàng Văn F", "worker1@manufacturing-erp.com", "Sản xuất", "Công nhân", "Nhân viên vận hành", "DEMO_WK1", 6500000m),
            ("worker2", "Đặng Thị G", "worker2@manufacturing-erp.com", "Sản xuất", "Công nhân", "Nhân viên vận hành", "DEMO_WK2", 6500000m),
            ("store1", "Ngô Văn H", "store1@manufacturing-erp.com", "Kho bãi", "Thủ kho", "Quản lý kho", "DEMO_ST1", 9000000m),
            ("qc2", "Bùi Thị I", "qc2@manufacturing-erp.com", "Kiểm soát chất lượng", "QC Inspector", "QC", "DEMO_QC2", 8200000m),
            ("hradmin", "Vũ Văn K", "hr@manufacturing-erp.com", "Hành chính", "Chuyên viên nhân sự", "Admin", "DEMO_HR1", 11000000m),
            ("planner", "Đỗ Thị L", "planner@manufacturing-erp.com", "Sản xuất", "Kế hoạch sản xuất", "Quản lý sản xuất", "DEMO_PL1", 13000000m),
        ];

        private static readonly string[] RequiredRoles =
        [
            "Admin",
            "Quản lý sản xuất",
            "Nhân viên vận hành",
            "Quản lý kho",
            "QC",
            "Kế toán"
        ];

        private static async Task EnsureRolesAsync(ManufacturingContext context)
        {
            var existing = await context.Roles.Select(r => r.RoleName).ToListAsync();
            foreach (var roleName in RequiredRoles)
            {
                if (existing.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                context.Roles.Add(new Role { RoleName = roleName });
            }

            if (context.ChangeTracker.HasChanges())
                await context.SaveChangesAsync();
        }

        private async Task EnsureDemoUsersAsync(ManufacturingContext context)
        {
            var passwordHash = _passwordHasher.HashPassword(RolePermissionDefaults.DefaultPassword);

            foreach (var demo in DemoUsers)
            {
                try
                {
                    var role = await context.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == demo.RoleName);
                    if (role == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Seeder] Thiếu vai trò: {demo.RoleName}");
                        continue;
                    }

                    var existingUser = await FindDemoUserAsync(context, demo.Username);
                    if (existingUser != null)
                    {
                        await EnsureUserHasRoleAsync(context, existingUser, role);
                        continue;
                    }

                    if (IsUsernamePending(context, demo.Username))
                        continue;

                    var employee = await context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeCode == demo.EmpCode);

                    if (employee == null)
                    {
                        employee = new Employee
                        {
                            EmployeeCode = demo.EmpCode,
                            FullName = demo.FullName,
                            Email = demo.Email,
                            Department = demo.Dept,
                            Position = demo.Position,
                            BasicSalary = demo.Salary > 0 ? demo.Salary : null,
                            JoinDate = DateTime.Now.AddMonths(-6),
                            Status = "Active"
                        };
                        context.Employees.Add(employee);
                        await context.SaveChangesAsync();
                    }

                    if (await FindDemoUserAsync(context, demo.Username) != null
                        || IsUsernamePending(context, demo.Username))
                        continue;

                    var user = new User
                    {
                        Username = demo.Username,
                        PasswordHash = passwordHash,
                        EmployeeId = employee.EmployeeId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    user.Roles.Add(role);
                    context.Users.Add(user);

                    if (!await context.Attendances.AnyAsync(a =>
                            a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Today))
                    {
                        context.Attendances.Add(new Attendance
                        {
                            EmployeeId = employee.EmployeeId,
                            Date = DateTime.Today,
                            CheckIn = new TimeSpan(7, 30, 0),
                            Status = "Present"
                        });
                    }

                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"[Seeder] Đã tạo user demo: {demo.Username}");
                }
                catch (DbUpdateException ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Seeder] Lỗi tạo user {demo.Username}: {ex.InnerException?.Message ?? ex.Message}");
                    context.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Seeder] Lỗi tạo user {demo.Username}: {ex.Message}");
                    context.ChangeTracker.Clear();
                }
            }

            await EnsureDemoUserPasswordsAsync(context);
        }

        private static async Task<User?> FindDemoUserAsync(ManufacturingContext context, string username)
        {
            var normalized = username.ToLowerInvariant();
            return await context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);
        }

        private static bool IsUsernamePending(ManufacturingContext context, string username)
        {
            var normalized = username.ToLowerInvariant();
            return context.Users.Local.Any(u => u.Username.ToLowerInvariant() == normalized);
        }

        private static async Task EnsureUserHasRoleAsync(ManufacturingContext context, User user, Role role)
        {
            if (user.Roles.Any(r => r.RoleId == role.RoleId))
                return;

            var trackedRole = await context.Roles.FindAsync(role.RoleId);
            if (trackedRole == null)
                return;

            user.Roles.Add(trackedRole);
            await context.SaveChangesAsync();
        }

        private async Task EnsureDemoUserPasswordsAsync(ManufacturingContext context)
        {
            var demoUsernames = DemoUsers.Select(d => d.Username).ToList();
            var passwordHash = _passwordHasher.HashPassword(RolePermissionDefaults.DefaultPassword);
            var users = await context.Users
                .Where(u => demoUsernames.Contains(u.Username))
                .ToListAsync();

            foreach (var user in users)
            {
                if (!_passwordHasher.VerifyPassword(RolePermissionDefaults.DefaultPassword, user.PasswordHash))
                    user.PasswordHash = passwordHash;
            }

            if (users.Count > 0)
                await context.SaveChangesAsync();
        }

        private async Task EnsureRolePermissionsAsync(ManufacturingContext context)
        {
            var versionSetting = await context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "RolePermissionsVersion");
            var currentVersion = versionSetting?.SettingValue;
            var needsSync = !await context.RolePermissions.AnyAsync()
                || currentVersion != RolePermissionDefaults.PermissionsVersion;

            if (!needsSync)
                return;

            var roles = await context.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var defaults = RolePermissionDefaults.GetForRole(role.RoleName);
                var existing = await context.RolePermissions
                    .Where(p => p.RoleId == role.RoleId)
                    .ToListAsync();

                foreach (var (moduleKey, flags) in defaults)
                {
                    var row = existing.FirstOrDefault(p =>
                        p.ModuleKey.Equals(moduleKey, StringComparison.OrdinalIgnoreCase));

                    if (row == null)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.RoleId,
                            ModuleKey = moduleKey,
                            CanView = flags.CanView,
                            CanAdd = flags.CanAdd,
                            CanEdit = flags.CanEdit,
                            CanDelete = flags.CanDelete
                        });
                    }
                    else
                    {
                        row.CanView = flags.CanView;
                        row.CanAdd = flags.CanAdd;
                        row.CanEdit = flags.CanEdit;
                        row.CanDelete = flags.CanDelete;
                    }
                }
            }

            if (versionSetting == null)
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    SettingKey = "RolePermissionsVersion",
                    SettingValue = RolePermissionDefaults.PermissionsVersion,
                    Description = "Phiên bản ma trận phân quyền mặc định",
                    LastUpdated = DateTime.Now
                });
            }
            else
            {
                versionSetting.SettingValue = RolePermissionDefaults.PermissionsVersion;
                versionSetting.LastUpdated = DateTime.Now;
            }

            await context.SaveChangesAsync();
        }
    }
}


