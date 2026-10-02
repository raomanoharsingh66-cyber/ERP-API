namespace BizFlow.Domain.Constants;

public static class Permissions
{
    public static class Business
    {
        public const string View = "Business.View";
        public const string Update = "Business.Update";
    }

    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Update = "Users.Update";
        public const string Delete = "Users.Delete";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Delete = "Roles.Delete";
    }

    public static class Inventory
    {
        public const string View = "Inventory.View";
        public const string Create = "Inventory.Create";
        public const string Update = "Inventory.Update";
        public const string Delete = "Inventory.Delete";
        public const string AdjustStock = "Inventory.AdjustStock";
    }

    public static class Customers
    {
        public const string View = "Customers.View";
        public const string Create = "Customers.Create";
        public const string Update = "Customers.Update";
        public const string Delete = "Customers.Delete";
    }

    public static class Sales
    {
        public const string View = "Sales.View";
        public const string Create = "Sales.Create";
        public const string Update = "Sales.Update";
        public const string Cancel = "Sales.Cancel";
        public const string PrintInvoice = "Sales.PrintInvoice";
    }

    public static class Suppliers
    {
        public const string View = "Suppliers.View";
        public const string Create = "Suppliers.Create";
        public const string Update = "Suppliers.Update";
        public const string Delete = "Suppliers.Delete";
    }

    public static class Purchases
    {
        public const string View = "Purchases.View";
        public const string Create = "Purchases.Create";
        public const string Update = "Purchases.Update";
        public const string Approve = "Purchases.Approve";
    }

    public static class Accounting
    {
        public const string View = "Accounting.View";
        public const string CreateEntry = "Accounting.CreateEntry";
        public const string Reports = "Accounting.Reports";
    }

    public static class ClothHub
    {
        public const string View = "ClothHub.View";
        public const string PosBilling = "ClothHub.PosBilling";
        public const string Returns = "ClothHub.Returns";
        public const string Inventory = "ClothHub.Inventory";
        public const string Reports = "ClothHub.Reports";
    }

    public record PermissionDefinition(string Code, string Module, string Description);

    public static IReadOnlyList<PermissionDefinition> GetAll()
    {
        return new List<PermissionDefinition>
        {
            // Business
            new(Business.View, "Business", "View business profile and configuration"),
            new(Business.Update, "Business", "Update business profile, tax settings, and logo"),

            // Users
            new(Users.View, "Users", "View business employee user accounts"),
            new(Users.Create, "Users", "Create new user accounts and invite staff"),
            new(Users.Update, "Users", "Update user accounts, roles, and status"),
            new(Users.Delete, "Users", "Deactivate or remove user accounts"),

            // Roles
            new(Roles.View, "Roles", "View available roles and permissions"),
            new(Roles.Create, "Roles", "Create custom business roles"),
            new(Roles.Update, "Roles", "Modify roles and assign permissions"),
            new(Roles.Delete, "Roles", "Remove custom business roles"),

            // Customers
            new(Customers.View, "Customers", "View customer directory and accounts receivable"),
            new(Customers.Create, "Customers", "Create new customers and manage credit terms"),
            new(Customers.Update, "Customers", "Update customer details and credit limits"),
            new(Customers.Delete, "Customers", "Deactivate or remove customer records"),

            // Inventory
            new(Inventory.View, "Inventory", "View inventory items, categories, and stock balances"),
            new(Inventory.Create, "Inventory", "Create new inventory products and SKUs"),
            new(Inventory.Update, "Inventory", "Modify item details and pricing"),
            new(Inventory.Delete, "Inventory", "Archive or delete inventory products"),
            new(Inventory.AdjustStock, "Inventory", "Perform physical stock count adjustments"),

            // Sales
            new(Sales.View, "Sales", "View sales orders, quotations, and customer invoices"),
            new(Sales.Create, "Sales", "Generate new sales orders and invoices"),
            new(Sales.Update, "Sales", "Edit draft invoices and sales orders"),
            new(Sales.Cancel, "Sales", "Cancel or void sales transactions"),
            new(Sales.PrintInvoice, "Sales", "Print and export tax invoices (GST)"),

            // Purchases & Suppliers
            new(Suppliers.View, "Purchases", "View vendor directory and supplier accounts payable"),
            new(Suppliers.Create, "Purchases", "Create new vendor profiles and credit terms"),
            new(Suppliers.Update, "Purchases", "Update vendor profiles and contact details"),
            new(Suppliers.Delete, "Purchases", "Deactivate or remove vendor records"),
            new(Purchases.View, "Purchases", "View purchase orders, bills, and vendor records"),
            new(Purchases.Create, "Purchases", "Create vendor purchase orders"),
            new(Purchases.Update, "Purchases", "Update purchase entries and goods receipts"),
            new(Purchases.Approve, "Purchases", "Approve purchase orders for payment"),

            // Accounting
            new(Accounting.View, "Accounting", "View general ledger, journal vouchers, and charts of accounts"),
            new(Accounting.CreateEntry, "Accounting", "Record journal vouchers and ledger entries"),
            new(Accounting.Reports, "Accounting", "Generate balance sheet, profit & loss, and trial balance reports"),

            // Cloth Hub Retail Permissions
            new(ClothHub.View, "Cloth Hub", "Access Cloth Hub clothing retail management module"),
            new(ClothHub.PosBilling, "Cloth Hub", "Operate POS fast billing terminal and print receipts"),
            new(ClothHub.Returns, "Cloth Hub", "Process garment size/colour exchanges and returns"),
            new(ClothHub.Inventory, "Cloth Hub", "Manage 2D size-colour stock matrix and carton packs"),
            new(ClothHub.Reports, "Cloth Hub", "View retail sales, profit margins, and apparel GST reports")
        };
    }
}
