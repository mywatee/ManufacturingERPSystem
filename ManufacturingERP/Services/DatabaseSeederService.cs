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
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role { RoleName = "Admin" },
                    new Role { RoleName = "Quản lý sản xuất" },
                    new Role { RoleName = "Nhân viên vận hành" },
                    new Role { RoleName = "Quản lý kho" },
                    new Role { RoleName = "QC" },
                    new Role { RoleName = "Kế toán" }
                };
                context.Roles.AddRange(roles);
                await context.SaveChangesAsync();
            }

            // 2. Seed Admin User & Employees
            if (!await context.Users.AnyAsync())
            {
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                
                // Create Admin Employee
                var adminEmp = new Employee 
                { 
                    EmployeeCode = "EMP001", 
                    FullName = "Huy Hoàng (Administrator)", 
                    Email = "admin@manufacturing-erp.com",
                    Phone = "0987654321",
                    Department = "Hành chính",
                    Position = "Administrator",
                    Status = "Active",
                    JoinDate = DateTime.Now.AddYears(-1)
                };
                context.Employees.Add(adminEmp);
                await context.SaveChangesAsync();

                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = _passwordHasher.HashPassword("123456"),
                    EmployeeId = adminEmp.EmployeeId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                
                if (adminRole != null) adminUser.Roles.Add(adminRole);
                context.Users.Add(adminUser);

                // Seed some worker employees
                var opRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Nhân viên vận hành");
                var workerData = new[]
                {
                    new { Code = "EMP002", Name = "Nguyễn Văn A", Dept = "Sản xuất", Pos = "Vận hành máy", Sal = 7500000m },
                    new { Code = "EMP003", Name = "Trần Thị B", Dept = "Kiểm soát chất lượng", Pos = "QC Inspector", Sal = 8200000m },
                    new { Code = "EMP004", Name = "Lê Văn C", Dept = "Kho bãi", Pos = "Thủ kho", Sal = 7800000m }
                };

                foreach(var data in workerData)
                {
                    var emp = new Employee
                    {
                        EmployeeCode = data.Code,
                        FullName = data.Name,
                        Department = data.Dept,
                        Position = data.Pos,
                        BasicSalary = data.Sal,
                        JoinDate = DateTime.Now.AddMonths(-6),
                        Status = "Active"
                    };
                    context.Employees.Add(emp);
                    await context.SaveChangesAsync();

                    var user = new User
                    {
                        Username = data.Name.Split(' ').Last().ToLower() + emp.EmployeeId,
                        PasswordHash = _passwordHasher.HashPassword("123456"),
                        EmployeeId = emp.EmployeeId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    if (opRole != null) user.Roles.Add(opRole);
                    context.Users.Add(user);

                    // Seed attendance
                    context.Attendances.Add(new Attendance
                    {
                        EmployeeId = emp.EmployeeId,
                        Date = DateTime.Today,
                        CheckIn = new TimeSpan(7, 30, 0),
                        Status = "Present"
                    });
                }
                
                await context.SaveChangesAsync();
            }

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

            // 2.2 Seed default Role Permissions (RBAC)
            if (!await context.RolePermissions.AnyAsync())
            {
                var roles = await context.Roles.ToListAsync();
                foreach (var role in roles)
                {
                    var isAdmin = role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
                    foreach (var (moduleKey, _) in SystemModules.All)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.RoleId,
                            ModuleKey = moduleKey,
                            CanView = true,
                            CanAdd = isAdmin,
                            CanEdit = isAdmin,
                            CanDelete = isAdmin
                        });
                    }
                }
                await context.SaveChangesAsync();
            }

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
        }
    }
}


