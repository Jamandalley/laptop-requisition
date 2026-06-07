namespace LaptopRequisition.Domain.Enums;

public enum ResponseCode
{
    Success = 0,
    UnknownError = 1,
    OtpValidationFailed = 3, // NEW: Added for OTP specific failure
    BadRequest = 21,
    Conflict = 22,

    InvalidCredentials = 1000,
    Unauthorized = 1001,
    Forbidden = 1002,

    ServerError = 5000,

    ValidationError = 2000,
    MissingField = 2001,

    NotFound = 3000,
    AlreadyExists = 3001
}

public enum ProductTypeEx
{
    Deposit = 1,
    Loan = 2,
    InternalGL = 3,
    Suspense = 4,
    Investment = 5
}
public enum ProductStatus { Draft = 1, Active = 2, Closed = 3 }
public enum InterestMethod { DailyBalance = 1, AverageDailyBalance = 2 }
public enum CompoundingFrequency { None = 0, Daily = 1, Monthly = 2 }
public enum PostingFrequency { Daily = 1, Monthly = 2 }
public enum BalanceBase { Ledger = 1, Available = 2 }
public enum LimitScope { PerTransaction = 1, Daily = 2, Monthly = 3 }


public enum AccountingEvent
{
    CashDeposit,
    DepositControl,
    CustomerWithdrawal,
    Transfer,
    FeeCharge,
    InterestAccrual,
    InterestPosting,
    LoanDisbursement,
    LoanRepayment,
    OverdraftUtilization,
    OverdraftRepayment,
    SuspenseParking,
    SuspenseClear,
    VaultToTill,
    TillToVault,
    Cheque,
    SavingsReference,
    OverdraftPortfolio,
    SavingsControl,
    SavingsTransferInSuspense,
    InterestOnSavings,
    WriteOff,
    IncomeFromFees,
    IncomeFromPenalties,
    OverdraftInterestIncome,
    OverdraftFeeIncome,
    FeesReceivable,
    PenaltiesReceivable,
    InterestPayable,
    Vat,
    InterestExpense,
    InterestReceivable,
    OverdraftExpense,
    OverdraftReceivable,
    WithHoldingTax,
    // ── CASA operational events ───────────────────────────────────────────────
    DepositReceived,
    WithdrawalProcessed,
    InterestAccrued,
    InterestPaid,
    StampDutyCharged,
    COTCharged,
    MaintenanceFeeCharged,
    NIPTransferInitiated,
    NIPTransferFeeCharged,
    NIPTransferSettled,
    NIPTransferFailed,
    AccountDormant,
    LienPlaced,
    LienLifted,
    WHTRemitted,
    StampDutyRemitted,
    ControlAccount,
    SettlementSuspense
}
public enum DayOfWeekType
{
    Monday = 1,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
}
public enum CurrencyType
{
    Fiat = 1,
    Crypto = 2,
    CommodityBacked = 3
}
public enum BranchState
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Closed = 4,
    Deactivated
}

public enum Gender{ Male = 1, Female = 2}

public enum Tiers
{
    Tier1,Tier2,Tier3
}

public enum CustomerType
{
    Corporate = 1,
    Group = 2
}

public enum AccountStatus
{
    Active = 1, Closed = 2, Pnd =3, Inactive = 4
}

public enum StatementDeliveryMode
{
    Daily =1,
    Weekly=2,
    Monthly=3,
    Yearly=4
}

public enum PostingType
{
    Credit,Debit
}

public enum PeriodStatus
{
    Open   = 1,
    Closed = 2,
    Locked = 3
}

public enum PeriodType
{
    Monthly   = 1,
    Quarterly = 2,
    Yearly    = 3
}