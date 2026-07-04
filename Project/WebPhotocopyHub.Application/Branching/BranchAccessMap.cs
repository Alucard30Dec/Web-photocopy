namespace WebPhotocopyHub.Application.Branching;

public sealed record BranchAccessRule(string? FeatureCode, string? PermissionCode, bool RequiresSelectedBranch = true);

public static class BranchAccessMap
{
    public static BranchAccessRule? Resolve(string area, string controller, string action)
    {
        if (string.Equals(area, "Shop", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveShop(controller, action);
        }

        if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveAdmin(controller, action);
        }

        if (string.Equals(area, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveCustomer(controller, action);
        }

        return null;
    }

    private static BranchAccessRule? ResolveShop(string controller, string action)
    {
        if (controller.Equals("Account", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (controller.Equals("Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(null, BranchPermissionCodes.DashboardView);
        }

        if (controller.Equals("PrintJobs", StringComparison.OrdinalIgnoreCase))
        {
            var permission = action.Equals("Refund", StringComparison.OrdinalIgnoreCase)
                ? BranchPermissionCodes.PrintJobsRefund
                : action is "PreviewFile" or "DownloadFile"
                    ? BranchPermissionCodes.PrintJobsFiles
                    : action.Equals("UpdateStatus", StringComparison.OrdinalIgnoreCase)
                        ? BranchPermissionCodes.PrintJobsManage
                        : BranchPermissionCodes.PrintJobsView;
            return new BranchAccessRule(BranchFeatureCodes.PrintOrders, permission);
        }

        if (controller.Equals("ProductOrders", StringComparison.OrdinalIgnoreCase))
        {
            var permission = action.Equals("Refund", StringComparison.OrdinalIgnoreCase)
                ? BranchPermissionCodes.ProductOrdersRefund
                : action.Equals("UpdateStatus", StringComparison.OrdinalIgnoreCase)
                    ? BranchPermissionCodes.ProductOrdersManage
                    : BranchPermissionCodes.ProductOrdersView;
            return new BranchAccessRule(
                BranchFeatureCodes.ProductSales,
                permission);
        }

        if (controller.Equals("SupportOrders", StringComparison.OrdinalIgnoreCase))
        {
            var permission = action.Equals("Refund", StringComparison.OrdinalIgnoreCase)
                ? BranchPermissionCodes.SupportOrdersRefund
                : action.Equals("UpdateStatus", StringComparison.OrdinalIgnoreCase)
                    ? BranchPermissionCodes.SupportOrdersManage
                    : BranchPermissionCodes.SupportOrdersView;
            return new BranchAccessRule(
                BranchFeatureCodes.SupportServices,
                permission);
        }

        if (controller.Equals("Inventory", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(
                BranchFeatureCodes.Inventory,
                action.Equals("AdjustStock", StringComparison.OrdinalIgnoreCase)
                    ? BranchPermissionCodes.InventoryAdjust
                    : BranchPermissionCodes.InventoryView);
        }

        if (controller.Equals("TopUpRequests", StringComparison.OrdinalIgnoreCase))
        {
            var permission = action.Equals("Review", StringComparison.OrdinalIgnoreCase)
                ? BranchPermissionCodes.TopUpsReview
                : action.Equals("CounterTopUp", StringComparison.OrdinalIgnoreCase)
                    ? BranchPermissionCodes.CounterTopUp
                    : BranchPermissionCodes.TopUpsView;
            return new BranchAccessRule(BranchFeatureCodes.TopUps, permission);
        }

        return new BranchAccessRule(null, null);
    }

    private static BranchAccessRule? ResolveAdmin(string controller, string action)
    {
        if (controller is "Account" or "Dashboard" or "Branches" or "Users" or "SystemMonitoring" or "AuditLogs")
        {
            return null;
        }

        if (controller.Equals("PrintJobs", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(BranchFeatureCodes.PrintOrders, null);
        }

        if (controller is "ProductOrders" or "Products")
        {
            return new BranchAccessRule(BranchFeatureCodes.ProductSales, null);
        }

        if (controller is "SupportOrders" or "SupportServices")
        {
            return new BranchAccessRule(BranchFeatureCodes.SupportServices, null);
        }

        if (controller.Equals("Inventory", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(BranchFeatureCodes.Inventory, null);
        }

        if (controller.Equals("PricingRules", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(BranchFeatureCodes.Pricing, null);
        }

        if (controller.Equals("TopUpRequests", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(BranchFeatureCodes.TopUps, null);
        }

        if (controller is "WalletTransactions" or "Reconciliation")
        {
            return new BranchAccessRule(BranchFeatureCodes.Wallet, null);
        }

        return new BranchAccessRule(null, null);
    }

    private static BranchAccessRule? ResolveCustomer(string controller, string action)
    {
        if (controller.Equals("PrintJobs", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(BranchFeatureCodes.PrintOrders, null);
        }

        if (controller.Equals("Products", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(BranchFeatureCodes.ProductSales, null);
        }

        if (controller.Equals("SupportOrders", StringComparison.OrdinalIgnoreCase))
        {
            return new BranchAccessRule(BranchFeatureCodes.SupportServices, null);
        }

        if (controller.Equals("Wallet", StringComparison.OrdinalIgnoreCase))
        {
            var featureCode = action.StartsWith("TopUp", StringComparison.OrdinalIgnoreCase)
                ? BranchFeatureCodes.TopUps
                : BranchFeatureCodes.Wallet;
            return new BranchAccessRule(featureCode, null);
        }

        return null;
    }
}
