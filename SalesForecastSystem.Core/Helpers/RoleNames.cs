namespace SalesForecastSystem.Core.Helpers;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string WarehouseManager = "WarehouseManager";
    public const string SalesStaff = "SalesStaff";
    public const string ProductManagers = Admin + "," + WarehouseManager;
    public const string All = Admin + "," + WarehouseManager + "," + SalesStaff;
    public static readonly string[] Values = [Admin, WarehouseManager, SalesStaff];
}
