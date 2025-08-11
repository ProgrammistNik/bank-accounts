using Hangfire.Dashboard;

namespace bank_accounts.Hangfire;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}