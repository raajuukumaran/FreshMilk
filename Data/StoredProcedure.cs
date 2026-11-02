namespace FreshMilk.API.Data;

public static class StoredProcedure
{
    public static class Auth
    {
        public const string GenerateMobileOTP = "dbo.GenerateMobileOTP";
        public const string VerifyMobileOTP = "dbo.VerifyMobileOTP";
    }

 public static class Users
{
    public const string ManageUsers = "dbo.ManageUsers";
}
  public static class MilkTypes
    {
        public const string Manage = "dbo.ManageMilkTypes";
    }

    public static class MilkRates
    {
        public const string Manage = "dbo.ManageMilkRates";
    }

public static class Supplies
{
    public const string Manage = "dbo.ManageSupplies";
}
public static class Chat
{
    public const string Manage = "dbo.ManageChat";
    public const string ManageRequests = "dbo.ManageChatRequests";
}
public static class Notifications
{
    public const string Manage = "dbo.ManageAppNotifications";
    public const string ManagePushTokens = "dbo.ManageExpoPushTokens";
}
 public static class ConsumerOrders
    {
        public const string Manage = "dbo.ManageConsumerOrders";
    }
}