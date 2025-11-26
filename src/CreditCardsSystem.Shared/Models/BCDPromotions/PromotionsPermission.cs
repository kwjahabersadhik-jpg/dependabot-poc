namespace CreditCardsSystem.Domain.Shared.Models.BCDPromotions
{
    public static class PromotionsPermission
    {
        private const string PromotionsAdminGroup = "admin";

        public static class Request
        {
            private const string Default = PromotionsAdminGroup + "Request";

            public const string Make = Default + ".Make";

            public const string Approve = Default + ".Approve";

        }

        public static class LoyaltyPoint
        {
            private const string Default = PromotionsAdminGroup + "LoyaltyPointsRequest";

            public const string Make = Default + ".Make";

            public const string Approve = Default + ".Approve";

        }
    }
}
