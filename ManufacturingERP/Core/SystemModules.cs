namespace ManufacturingERP.Core;

public static class SystemModules
{
    public const string Dashboard = "dashboard";
    public const string SystemAdmin = "system_admin";
    public const string MasterData = "master_data";
    public const string Production = "production";
    public const string QualityControl = "quality_control";
    public const string Warehouse = "warehouse";
    public const string HumanResources = "human_resources";
    public const string Finance = "finance";

    public static readonly (string Key, string DisplayName)[] All =
    [
        (Dashboard, "Bảng điều khiển"),
        (SystemAdmin, "Quản trị hệ thống"),
        (MasterData, "Dữ liệu gốc"),
        (Production, "Sản xuất"),
        (QualityControl, "Kiểm soát chất lượng"),
        (Warehouse, "Kho bãi"),
        (HumanResources, "Nhân sự & Lương"),
        (Finance, "Tài chính"),
    ];
}

