using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP.Models;

public partial class ManufacturingContext : DbContext
{
    public ManufacturingContext()
    {
    }

    public ManufacturingContext(DbContextOptions<ManufacturingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Bom> Boms { get; set; }

    public virtual DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<QualityControl> QualityControls { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Routing> Routings { get; set; }

    public virtual DbSet<Shift> Shifts { get; set; }

    public virtual DbSet<StockTransaction> StockTransactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WorkOrder> WorkOrders { get; set; }

    public virtual DbSet<WorkOrderProgress> WorkOrderProgresses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=ManufacturingERP;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AuditLog__5E5499A8B49707C8");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.TableName).HasMaxLength(50);
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AuditLogs__UserI__68487DD7");
        });

        modelBuilder.Entity<Bom>(entity =>
        {
            entity.HasKey(e => e.Bomid).HasName("PK__BOM__CA12FCBBE9DDA23D");

            entity.ToTable("BOM");

            entity.Property(e => e.Bomid).HasColumnName("BOMID");
            entity.Property(e => e.ChildId).HasColumnName("ChildID");
            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.QuantityPerUnit).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.Child).WithMany(p => p.BomChildren)
                .HasForeignKey(d => d.ChildId)
                .HasConstraintName("FK__BOM__ChildID__71D1E811");

            entity.HasOne(d => d.Parent).WithMany(p => p.BomParents)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK__BOM__ParentID__70DDC3D8");
        });

        modelBuilder.Entity<EmployeeSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Employee__9C8A5B69D0265039");

            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.MachineCode).HasMaxLength(50);
            entity.Property(e => e.ShiftId).HasColumnName("ShiftID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Shift).WithMany(p => p.EmployeeSchedules)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("FK__EmployeeS__Shift__208CD6FA");

            entity.HasOne(d => d.User).WithMany(p => p.EmployeeSchedules)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__EmployeeS__UserI__1F98B2C1");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__Inventor__C5061317FF78F2FD");

            entity.ToTable("Inventory");

            entity.Property(e => e.MaterialId)
                .ValueGeneratedNever()
                .HasColumnName("MaterialID");
            entity.Property(e => e.CurrentStock)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.WarehouseLocation).HasMaxLength(50);

            entity.HasOne(d => d.Material).WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Mater__778AC167");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.WarehouseId)
                .HasConstraintName("FK__Inventory__Wareh__29221CFB");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__Material__C5061317F41CDBDA");

            entity.HasIndex(e => e.MaterialCode, "UQ__Material__170C54BA03BF32BF").IsUnique();

            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MaterialCode).HasMaxLength(50);
            entity.Property(e => e.MaterialName).HasMaxLength(200);
            entity.Property(e => e.MinStock).HasDefaultValue(10);
            entity.Property(e => e.Unit).HasMaxLength(20);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotiId).HasName("PK__Notifica__EDC08EF256E350DB");

            entity.Property(e => e.NotiId).HasColumnName("NotiID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.RecipientId).HasColumnName("RecipientID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Recipient).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.RecipientId)
                .HasConstraintName("FK__Notificat__Recip__236943A5");

            entity.HasOne(d => d.Role).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Notificat__RoleI__245D67DE");
        });

        modelBuilder.Entity<QualityControl>(entity =>
        {
            entity.HasKey(e => e.Qcid).HasName("PK__QualityC__DC29BF92A67EA94A");

            entity.ToTable("QualityControl");

            entity.Property(e => e.Qcid).HasColumnName("QCID");
            entity.Property(e => e.FailedQty).HasDefaultValue(0);
            entity.Property(e => e.InspectionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.InspectorId).HasColumnName("InspectorID");
            entity.Property(e => e.PassedQty).HasDefaultValue(0);
            entity.Property(e => e.Woid).HasColumnName("WOID");

            entity.HasOne(d => d.Inspector).WithMany(p => p.QualityControls)
                .HasForeignKey(d => d.InspectorId)
                .HasConstraintName("FK__QualityCo__Inspe__0F624AF8");

            entity.HasOne(d => d.Wo).WithMany(p => p.QualityControls)
                .HasForeignKey(d => d.Woid)
                .HasConstraintName("FK__QualityCon__WOID__0C85DE4D");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A9C5AD3A2");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160E562BA39").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Routing>(entity =>
        {
            entity.HasKey(e => e.RoutingId).HasName("PK__Routings__A763F8A80BA1B9F0");

            entity.Property(e => e.RoutingId).HasColumnName("RoutingID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.StepName).HasMaxLength(100);

            entity.HasOne(d => d.Product).WithMany(p => p.Routings)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Routings__Produc__74AE54BC");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__Shifts__C0A838E1B1902443");

            entity.Property(e => e.ShiftId).HasColumnName("ShiftID");
            entity.Property(e => e.ShiftName).HasMaxLength(50);
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__StockTra__55433A4B1AE83A43");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenceCode).HasMaxLength(50);
            entity.Property(e => e.TransDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Type).HasMaxLength(10);

            entity.HasOne(d => d.Material).WithMany(p => p.StockTransactions)
                .HasForeignKey(d => d.MaterialId)
                .HasConstraintName("FK__StockTran__Mater__7C4F7684");

            entity.HasOne(d => d.TransByNavigation).WithMany(p => p.StockTransactions)
                .HasForeignKey(d => d.TransBy)
                .HasConstraintName("FK__StockTran__Trans__7D439ABD");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACD5200C46");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E418C8924B").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__RoleI__656C112C"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__6477ECF3"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF27604F0C898A9D");
                        j.ToTable("UserRoles");
                        j.IndexerProperty<int>("UserId").HasColumnName("UserID");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                    });
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFD9A6EB8906");

            entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.WarehouseName).HasMaxLength(100);
        });

        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.Woid).HasName("PK__WorkOrde__8D75747C73F86574");

            entity.HasIndex(e => e.Wocode, "UQ__WorkOrde__94AAFED9725446CE").IsUnique();

            entity.Property(e => e.Woid).HasColumnName("WOID");
            entity.Property(e => e.ActualQty).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Planned");
            entity.Property(e => e.Wocode)
                .HasMaxLength(50)
                .HasColumnName("WOCode");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__WorkOrder__Creat__04E4BC85");

            entity.HasOne(d => d.Product).WithMany(p => p.WorkOrders)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__WorkOrder__Produ__02084FDA");
        });

        modelBuilder.Entity<WorkOrderProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("PK__WorkOrde__BAE29C85157E5AB8");

            entity.ToTable("WorkOrderProgress");

            entity.Property(e => e.ProgressId).HasColumnName("ProgressID");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.MachineId)
                .HasMaxLength(50)
                .HasColumnName("MachineID");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Woid).HasColumnName("WOID");
            entity.Property(e => e.WorkerId).HasColumnName("WorkerID");

            entity.HasOne(d => d.Wo).WithMany(p => p.WorkOrderProgresses)
                .HasForeignKey(d => d.Woid)
                .HasConstraintName("FK__WorkOrderP__WOID__08B54D69");

            entity.HasOne(d => d.Worker).WithMany(p => p.WorkOrderProgresses)
                .HasForeignKey(d => d.WorkerId)
                .HasConstraintName("FK__WorkOrder__Worke__09A971A2");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
