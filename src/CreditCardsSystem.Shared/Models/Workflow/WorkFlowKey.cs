namespace CreditCardsSystem.Domain.Models.Workflow;

public enum WorkFlowKey
{
    CardRequestWorkflow = 34,
    CardOperationWorkFlow = -1,
    ChangeBillingAddressWorkflow = 5,
    ChangeHolderNameWorkflow = 28,
    ChangeCardLinkAccountWorkflow = 29,
    CardClosureWorkflow = 1,
    CreditReverseWorkflow = 25,
    ChangeLimitWorkflow = 6,
    ReplaceLostCardWorkflow = 2,
    ReplaceDamagedCardWorkflow = 3,
    CorporateProfileAddWorkflow = 22,
    CorporateProfileUpdateWorkflow = 23,
    MigrateCollateralDepositWorkflow = 14,
    MigrateCollateralMarginWorkflow = 15,
    SupplementaryCardWorkflow = 33,
    MemberShipDeleteRequestWorkflow = 35,
    CardReActivationWorkflow = 27,
    TayseerCardRequestWorkflow = 36
}
