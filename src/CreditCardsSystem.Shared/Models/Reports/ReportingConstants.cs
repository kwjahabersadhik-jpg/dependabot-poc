namespace CreditCardsSystem.Domain.Shared.Models.Reports;

public class ReportingConstants
{
    //Application values
    public const int APPLICATION_ID = 15;

    //Event status values
    public const string EVENT_SUCCESSFUL = "successful";

    public const string EVENT_UNSUCCESSFUL = "unsuccessful";

    //Severity values
    public const string KEY_SEVERITY = "severity";

    public const string SEVERITY_LOW = "low";
    public const string SEVERITY_MEDIUM = "medium";
    public const string SEVERITY_HIGH = "high";
    public const string SEVERITY_VERY_HIGH = "very_high";

    //Event values
    public const string EVENT_PERFORM_DEDUCTON = "perform_deduction";

    public const string EVENT_VERIFY_APPROVAL = "verify_approval";
    public const string EVENT_LOAD_DATA = "load_data";
    public const string EVENT_SEARCH_PROFILE = "search_profile";
    public const string EVENT_SEARCH_REPORT = "search_report";
    public const string EVENT_DELETE_PAYEE = "delete_payee";
    public const string EVENT_MODIFY_PAYEE = "modify_payee";
    public const string EVENT_ADD_PAYEE = "add_payee";
    public const string EVENT_GET_STANDING_ORDER_LIST = "get_standing_order_list";
    public const string EVENT_ADD_STANDING_ORDER = "add_standing_order";
    public const string EVENT_EDIT_STANDING_ORDER = "edit_standing_order";
    public const string EVENT_UPDATE_SCHEDULED_SO_TRANSACTIONS = "update_scheduled_so_transactions";
    public const string EVENT_DELETE_STANDING_ORDER = "delete_standing_order";
    public const string EVENT_PERFORM_PAYMENT = "perform_payment";
    public const string EVENT_CREDIT_REVERSE = "credit_reverse";
    public const string EVENT_CREDIT_REVERSE_APPROVE = "credit_reverse_approve";
    public const string EVENT_CREDI_REVERSE_REJECT = "credit_reverse_reject";
    public const string EVENT_CREDIT_REVERSE_DELETE = "credit_reverse_delete";
    public const string EVENT_CREDIT_REVERSE_LOAD = "credit_reverse_load";
    public const string EVENT_LOCAL_UPDATE_CARD_NUNBER = "Local_Update_Card_Number";
    public const string EVENT_CARD_DETAILS = "card_details";
    public const string EVENT_CARD_Replacement_Print = "Replacement_tracking_Print_PDF";
    public const string EVENT_CARD_Replacement_Tracking_GetData = "Replacement_tracking_FillData";
    public const string EVENT_CARD_ACTIVATION = "card_activation";
    public const string EVENT_CARD_REACTIVATION = "card_reactivation";
    public const string EVENT_CARD_STATEMENT = "card_statement";
    public const string EVENT_CARD_STATEMENT_LOAD = "card_statement_load";
    public const string EVENT_CARD_STATEMENT_PRINT = "card_statement_print";
    public const string EVENT_CARD_STATEMENT_SEARCH = "card_statement_search";
    public const string EVENT_SUBMIT_NEW_CALCULATIONS = "submit_new_calculations";
    public const string EVENT_PRINT_LIMIT_CHANGE_REPORT = "print_limit_change_report";
    public const string EVENT_SEARCH_LIMIT_CHANGE_REPORT = "search_limit_change_report";
    public const string EVENT_PERFORM_ACCOUNT_BOARDING = "perform_account_boarding";

    public static string ForeignCurrencyPrepaidCards = "FOREIGN_CURRENCY_PREPAID_CARDS";
    public const string EVENT_VALIDATE_CREDIT_CARD_CURRENCY_RATE_validate_CreditCard_CurrencyRate = "validate_Credit_Card_Currency_Rate";
    public const string EVENT_PERFORM_FOREIGN_CURRENCY_CREDIT_CARD_PAYMENT_WITH_MQ = "Perform_ForeignCurrency(FC)_CreditCard_PaymentMQ";
    public const string EVENT_PERFORM_FOREIGN_CURRENCY_CREDIT_CARD_REFUND_WITH_MQ = "Perform_ForeignCurrency(FC)_CreditCard_RefundMQ";
    public const string EVENT_FXRATEACKNOWLEDGEMENT_PRINT_FORM = "event_fxrateacknowledgement_print_form";

    public const string EVENT_CARD_STATEMENT_CYCLETODATE = "card_statement_cycletodate";
    public const string EVENT_CARD_STATEMENT_MOMNTHLYSTATEMENT = "card_statement_monthlystatement";
    public const string EVENT_CARD_STATEMENT_CREDITCARDHISTORY = "card_statement_creditcardhistory";
    public const string EVENT_CARD_STOP = "card_stop";
    public const string EVENT_CARD_CLOSURE = "card_closure";
    public const string EVENT_VALIDATE_CLOSURE_BY_CHECKING_SO = "validate_closure_by_checking_so";
    public const string EVENT_CARD_LOST_STOLEN = "card_lost_stolen";
    public const string EVENT_DELETE_CARD_PREREGESTERED_PAYEE = "delete_card_preregestered_payee";
    public const string EVENT_GET_SUPPLEMENTARY_CARDS_LIST = "get_supplementary_cards_list";
    public const string EVENT_REPORT_CARD_AS_LOST_OR_STOLEN = "report_card_as_lost_or_stolen";
    public const string EVENT_GET_BANKING_CUSTOMER_PROFILE = "get_banking_customer_profile";
    public const string EVENT_GET_CUSTOMER_ACCOUNTS = "get_customer_accounts";
    public const string EVENT_GET_CUSTOMER_ACCOUNTS_BY_CIVIL_FOR_DP = "get_customer_accounts_by_civil_for_dp";
    public const string EVENT_GET_CUSTOMER_ACCOUNTS_BY_Account_FOR_DP = "get_customer_accounts_by_account_for_dp";
    public const string EVENT_GET_CUSTOMER_PROFILE = "get_customer_profile";

    public const string EVENT_GET_OWNED_CARDS = "get_owned_cards";
    public const string EVENT_BIND_CHARGE_BACK_DISPUTE = "bind_charge_back_dispute";
    public const string EVENT_ROW_BOUND_CHARGE_BACK_DISPUTE = "row_bound_charge_back_dispute";
    public const string EVENT_ROW_BOUND_EOD_STAFF = "row_bound_eod_staff";
    public const string EVENT_ITEM_BOUND_EOD_BRANCH = "item_bound_eod_branch";
    public const string EVENT_ROW_COMMAND_EOD_BRANCH = "row_command_eod_branch";
    public const string EVENT_ROW_COMMAND_EOD_STAFF = "row_command_eod_staff";
    public const string EVENT_SEARCH_CHARGE_BACK_DISPUTE = "search_charge_back_dispute";
    public const string EVENT_UPDATE_CHARGE_BACK_DISPUTE = "update_charge_back_dispute";
    public const string EVENT_UPDATE_CARD_STATUS = "update_card_status";
    public const string EVENT_CLOSE_CHARGE_BACK_DISPUTE = "close_charge_back_dispute";
    public const string EVENT_ROW_EDITING = "row_editing";
    public const string EVENT_GET_PAYEE_DETAILS = "get_payee_details";
    public const string EVENT_GET_CARD_DETAILS = "get_card_details";
    public const string EVENT_DELETE_CUSTOMER_PROFILE = "delete_customer_profile";
    public const string EVENT_GET_CREDIT_CARDS_REQUEST = "get_credit_card_request";
    public const string EVENT_GET_EXTERNAL_STATUS = "get_external_status";
    public const string EVENT_ADD_STANDING_ORDER_REQUEST_PARAMETERS = "add_standing_order_request_parameters";
    public const string EVENT_STANDING_ORDER_ADD_EDIT_DELETE = "standing_order_add_edit_delete";
    public const string EVENT_STANDING_ORDER_lIST = "standing_order_list";
    public const string EVENT_CUSTOMER_PROFILE = "customer_profile";
    public const string EVENT_FILL_CUSTOMER_PROFILE = "fill_customer_profile";
    public const string EVENT_FILL_LOYALTY_STATEMENT = "fill_loyalty_statement";
    public const string EVENT_EDIT_CUSTOMER_PROFILE = "edit_customer_profile";
    public const string EVENT_DELEGATE_REQUEST = "delegate_request";
    public const string EVENT_GET_CREDIT_CARD_STATEMENT = "get_credit_card_statement";
    public const string EVENT_FILL_CYCLE_TO_DATE = "fill_cycle_to_date";
    public const string EVENT_MONTHLY_STATEMENT = "fill_monthly_statement";
    public const string EVENT_CHANGE_SEARCH_CREITERIA = "change_search_creiteria";
    public const string EVENT_DATE_FORMAT = "change_date_format";
    public const string EVENT_CHANGE_BILLING_ADDRESS = "change_billing_address";
    public const string EVENT_EXPORT_EXCEL_REPORT = "export_excel_report";
    public const string EVENT_CREATE_CUSTOMER_PROFILE = "create_customer_profile";
    public const string EVENT_CREATE_MARGIN_ACCOUNT = "create_margin_account";
    public const string EVENT_REPLACEMENT_CARD_FOR_DAMAGE = "replacement_card_for_damage";
    public const string EVENT_REPLACEMENT_CARD_FOR_LOST_OR_STOLEN = "replacement_card_for_lost_or_stolen";
    public const string EVENT_ADD_PRE_REG_PAYEE_FOR_LOST_OSRA_SUPP = "add_preregistered_payee_for_lost_osra_supp";
    public const string EVENT_CREDIT_CHECKER_REJECTION = "credit_checker_rejection";
    public const string EVENT_CREDIT_CHECKING_REVIEWED = "credit_checking_reviewed";
    public const string EVENT_FINAL_CREDIT_CHECKING_APPROVAL = "final_credit_checking_approval";
    public const string EVENT_MINOR_CHARGE_CARD_APPROVAL = "minor_charge_card_approval";
    public const string EVENT_EOD_PAGE_LOAD = "eod_page_load";
    public const string EVENT_BIND_BRANCHES = "bind_branches";
    public const string EVENT_BIND_CFU_ACTIVITIES = "bind_cfu_activities";
    public const string EVENT_EOD_SEARCH = "eod_search";
    public const string EVENT_EOD_BIND_GRIDS = "eod_bind_grids";
    public const string EVENT_GET_EOD_SINGLE_STAFF_RESPONSE = "get_eod_single_staff_response";
    public const string EVENT_GET_EOD_BY_BRANCH_RESPONSE = "get_eod_by_branch_response";
    public const string EVENT_EOD_PRINT = "eod_print";


    //Dynamic Payment (boruj 05-08-2020)[CRC-5577]
    public const string EVENT_DYNAMIC_PAYMENT = "dynamic_payment";
    public const string EVENT_DYNAMIC_PAYMENT_PAGE_LOAD = "dynamic_payment_page_load";
    public const string EVENT_DYNAMIC_PAYMENT_PERFORM_PAYMENT = "dynamic_payment_perform_payment";

    //MIGS
    public const string EVENT_ORACLE_CONNECTION = "Oracle_Connection";
    public const string EVENT_MERCHANT_PROGRESS_REPORT = "Merchant_Progress_Report";
    public const string EVENT_EXPORT_PROGRESS_REPORT = "Export_Progress_Report";
    public const string EVENT_GET_COUNTRIES = "Get_Countries";
    public const string EVENT_GET_CURRENCIES = "Get_Currencies";
    public const string EVENT_LOAD_IDS = "Load_Ids";
    public const string EVENT_GET_MASTER_FILE = "Get_Master_File";
    public const string EVENT_APPLY_RULES = "apply_rules";
    public const string EVENT_GET_ISSUERS = "Get_Issuers";
    public const string EVENT_FILTER_ISSUERS = "Filter_Issuers";
    public const string EVENT_POST_ISSUER_BIN = "Post_Issuer_Bin";
    public const string EVENT_PUT_ISSUER_BIN = "Put_Issuer_Bin";
    public const string EVENT_DELETE_ISSUER_BIN = "Delete_Issuer_Bin";
    public const string EVENT_GET_MERCHANT_GROUPS = "Get_Merchant_Groups";
    public const string EVENT_POST_MERCHANT_GROUPS = "Post_Merchant_Groups";
    public const string EVENT_DELETE_MERCHANT_GROUPS = "Delete_Merchant_Groups";
    public const string EVENT_PUT_MERCHANT_GROUPS = "Put_Merchant_Groups";
    public const string EVENT_GET_MERCHANTS = "Get_Merchants";
    public const string EVENT_POST_MERCHANT = "Post_Merchant";
    public const string EVENT_DELETE_MERCHANT = "Delete_Merchant";
    public const string EVENT_PUT_MERCHANT = "Put_Merchant";
    public const string EVENT_FILTER_MERCHANTS = "Filter_Merchants";
    public const string EVENT_PUT_SUSPICIOUS_MERCHANTS = "Put_Suspicious_Merchants";
    public const string EVENT_GET_BLACK_CARDS = "Get_Black_Cards";
    public const string EVENT_POST_BLACK_CARDS = "Post_Black_Cards";
    public const string EVENT_PUT_BLACK_CARDS = "Put_Black_Cards";
    public const string EVENT_DELETE_BLACK_CARDS = "Delete_Black_Cards";
    public const string EVENT_PUT_SUSPICIOUS_CARDS = "Put_Suspicious_Cards";
    public const string EVENT_GENERATE_FILE = "Generate_File";
    public const string EVENT_FILTER_TRANSACTIONS = "Filter_Transactions";
    public const string EVENT_PUT_TRANSACTIONS_FRAUD_STATUS = "Put_Transactions_Fraud_Status";
    public const string EVENT_PUT_SEND_TO_FDR = "Put_Send_To_FDR";
    //public const string EVENT_EXPORT_FILTER_TRANSACTIONS = "Export_Filter_Transactions";        
    public const string EVENT_GET_FRAUDULENT_RULES = "Get_Fraudulent_Rules";
    public const string EVENT_PUT_FRAUDULENT_RULE = "Put_Fraudulent_Rule";
    public const string EVENT_APPROVE_FRAUDULENT_RULE = "Approve_Fraudulent_Rule";
    //public const string EVENT_GETISCHECKER                                                     
    //public const string EVENT_IS_USER_AUTHENTICATE = "IS_USER_AUTHENTICATE";                      


    // DocuWare
    public const string EVENT_STORE_DOCUMENT_DOCUWARE = "Store Document in Docuware";
    public const string EVENT_GET_DOCUMENT_DOCUWARE = "Get Document from Docuware";
    public const string EVENT_SEARCH_DOCUMENT_DETAILS_DOCUWARE = "Search Document Details from Docuware";
    public const string EVENT_GET_DOCUMENT_TYPE_DOCUWARE = "Get Document Type from Docuware";

    /// <summary>
    //Corporate card
    public const string EVENT_GET_CORPORATE_PROFILE = "get_corporate_profile";
    public const string EVENT_ADD_CORPORATE_PROFILE_REQUEST = "add_corporate_profile_request";
    public const string EVENT_UPDATE_CORPORATE_PROFILE_REQUEST = "update_corporate_profile_request";
    public const string EVENT_FillCorporateProfileWS = "get_corporate_profile_ws";
    public const string EVENT_FillCorporateAccountsWS = "get_corporate_accounts_ws";
    public const string EVENT_APPROVE_CORPORATE_PROFILE_ADD = "approve_corporate_profile_add_request";
    public const string EVENT_APPROVE_CORPORATE_PROFILE_UPDATE = "approve_corporate_profile_update_request";
    public const string EVENT_REJECT_CORPORATE_PROFILE = "reject_corporate_profile";
    public const string EVENT_ApproveRejectRequest_LOAD = "Approve_Reject_Request_Load";
    public const string EVENT_ApproveRequest = "Approve_Request";
    public const string EVENT_RejectRequest = "Reject_Request";
    public const string EVENT_ValidateGlobalLimitChange = "Validate_Global_Limit_Change";
    public const string EVENT_VALIDATE_MATURITY_DATE = "Validate_maturity_Date";
    /// </summary>

    //Request Admin Event Values
    public const string EVENT_REQUEST_ADMIN_LOAD = "Request_Admin_Load";

    //App Status Event Values
    public const string EVENT_CARD_ISSUE_AGAINST_CHANGED = "Card_Issue_Against_Changed";

    public const string EVENT_APP_STATUS_MARGINACCNO = "App_Status_MarginAccNo";
    public const string EVENT_APP_STATUS_SHOWADDRESS = "App_Status_ShowAddress";
    public const string EVENT_APP_STATUS_DEPOSITACCNO = "App_Status_DepositAccNo";
    public const string EVENT_APP_STATUS_HOLDLIST = "App_Status_HoldList";
    public const string EVENT_APP_STATUS_LOAD = "App_Status_Load";
    public const string EVENT_APP_STATUS = "App_Status";
    public const string EVENT_APP_STATUS_APPROVE = "approve_request";
    public const string EVENT_APP_CARD_UPGRADE = "card_upgrade";
    public const string EVENT_APP_CARD_UPGRADE_DETAIL = "card_upgrade_detail";
    public const string EVENT_APP_STATUS_FULL_EDIT = "full_edit_request";
    public const string EVENT_APP_STATUS_EDIT = "edit_request";
    public const string EVENT_APP_STATUS_DELETE = "delete_request";
    public const string EVENT_DELETE_PROMOTION_BENEFICIARY = "delete_promotion_beneficiary";
    public const string EVENT_CHANGE_TARGET_CREDIT_LIMIT_WHILE_APPROVING = "change_target_credit_limit_while_approving";
    public const string EVENT_CHANGE_TARGET_CREDIT_LIMIT_OR_COLLECTION_DAY = "change_target_credit_limit_or_collection_day";
    public const string EVENT_APP_STATUS_FILL_CUSTOMER_PROFILE = "Fill_Customer_Profile_Fields";
    public const string EVENT_APP_STATUS_GET_CARD_INSTALLMENTS = "Get_Card_Installments_Count_By_Card_Type";
    public const string EVENT_SET_PREVIOUS_KFH_LIMIT_AND_INSTALLMENTS = "Set_Previous_KFH_Limit_And_Installments";
    public const string EVENT_APP_STATUS_GET_TAYSEER_CREDIT_CHECKING_LOG = "Get_Tayseer_Credit_Checking_Approval_Log";
    public const string EVENT_APP_STATUS_INSERT_TAYSEER_CREDIT_CHECKING_RECORD = "Insert_Tayseer_Credit_Checking_Approval_Record";
    public const string EVENT_CHANGE_LIMIT_INSERT_TAYSEER_CREDIT_CHECKING_RECORD = "change_limit_Insert_Tayseer_Credit_Checking_Approval_Record";
    public const string EVENT_CHANGE_LIMIT_PAGE_LOAD = "change_limit_page_load";
    public const string EVENT_CHANGE_LIMIT_HISTORY_COMMAND = "change_limit_history_command";
    public const string EVENT_APP_STATUS_CHANGE_LIMIT = "change_limit";
    public const string EVENT_APP_STATUS_CHANGE_MQ_CREDIT_LIMIT = "change_limit_Call_Change_MqCreditLimit";
    public const string EVENT_LIMIT_CHANGE_APPROVE_REJECT = "limit_change_approve_reject";
    public const string EVENT_LIMIT_CHANGE_CREDIT_REVIEW_CHECKING_APPROVE_REJECT = "limit_change_Credit_Review_Checking_approve_reject";
    public const string EVENT_LIMIT_CHANGE_INSERT = "limit_change_insert";
    public const string EVENT_LIMIT_CHANGE_DELETE = "limit_change_delete";
    public const string EVENT_CANCEL_LIMIT_CHANGE_APPROVE_REJECT = "cancel_limit_change_approve_reject";

    //Search Request Event Values
    public const string EVENT_SEARCH_REQUEST = "search_request";

    public const string EVENT_INSERT_CHARGE_BACK_DISPUTE = "insert_charge_back_dispute";
    public const string EVENT_GET_USER_INFO = "get_user_info";

    public const string EVENT_SELECT_CARD_CURRENCY = "select_card_currency";
    public const string EVENT_SUPPLIMETARY_CARD_APPROVAL = "suuplimentary_card_approval";
    public const string EVENT_DOWNPAYMENT_CARD_AGAINST_INCREMENTAL_MARGIN = "downpayment_card_against_incremental_margin";
    public const string EVENT_DOWNPAYMENT_CARD_AGAINST_INCREMENTAL_MARGIN_ROLLBACK = "rollback_downpayment_card_against_incremental_margin";

    //Request Admin Event Values

    public const string EVENT_ADMIN_CHANGE_CARD_STATUS = "admin_change_card_status";
    public const string EVENT_ADMIN_CHANGE_CARD_NUMBER = "admin_change_card_number";
    public const string EVENT_ADMIN_INSERT_SECONDARY_CARD_NUMBER = "admin_insert_secondary_card_number";
    public const string EVENT_ADMIN_OLD_CARD_CLOSURE = "admin_old_card_closure";
    public const string EVENT_DELETE_REQUEST_PARAMETER = "delete_request_parameter";

    //events cards operations ccso-839
    public const string EVENT_LOAD_UPDATE_CARD_DASHBOARD = "load_update_card_dashboard";
    public const string EVENT_UPDATE_CARD_DASHBOARD = "EVENT_UPDATE_CARD_DASHBOARD";
    public const string EVENT_UPDATE_CARD_EMBOSSED_NAME = "update_card_embossed_name";
    public const string EVENT_UPDATE_CARD_LINKED_ACCOUNT = "update_card_linked_account";
    public const string EVENT_APPROVE_REJECT_CARD_REPLACEMENT = "approve_reject_card_replacement";
    public const string EVENT_CARD_HOLDER_NAME_AND_ACCT = "card_holder_name_and_acct";


    // scattered
    public const string EVENT_GET_CARD_NEW_INSTALLMENT = "get_card_new_installment";

    public const string EVENT_REQUEST_ACTIVITY_CREDIT_REVERSE = "request_activity_credit_reverse";

    public const string EVENT_POSTING_FEE = "Posting_Fee";
    public const string EVENT_REVERSE_POSTING_FEE = "Reverse_Posting_Fee";
    public const string EVENT_GET_SELECTED_DD_CONFIG = "get_selected_dd_config";
    public const string EVENT_SELECT_FILE_LOAD = "select_file_load";
    public const string EVENT_VALIDATE_ADD = "validate_add";
    public const string EVENT_ADD_DD_OPTION = "add_dd_option";
    public const string KEY_SELECTED_FILE_LOAD = "selected_file_load";

    //Key values
    public const string KEY_USER_ID = "user_id";
    public const string KEY_USER_NAME = "user_name";
    public const string KEY_CFU_ACTIVITY_ID = "cfu_activity_id";
    public const string KEY_PARAMETER = "parameter";
    public const string KEY_STATUS_ID = "status_id";
    public const string KEY_STAFF_ID = "staff_id";
    public const string KEY_IS_KFH_STAFF = "is_KFH_staff";
    public const string KEY_TYPE_ID = "type_id";
    public const string KEY_ACCT_NO = "acct_no";
    public const string KEY_PRIMARY_CIVIL_ID = "primary_civil_id";
    public const string KEY_CIVIL_ID = "civil_id";
    public const string KEY_CORPORATE_CIVIL_ID = "corporate_civil_id";
    public const string KEY_PRIMARY_CARD_NO = "primary_credit_card_no";
    public const string KEY_CREDIT_CARD_NO = "credit_card_no";
    public const string KEY_IS_AUB_CARD = "isAub";
    public const string KEY_AUB_CREDIT_CARD_NO = "aub_credit_card_no";
    public const string KEY_RIM_NO = "rim_no";
    public const string KEY_RECORDS_DELETED = "records_deleted";
    public const string KEY_COMMERCIAL_REGISTRAR = "commercial_registrar";
    public const string KEY_DESCRIPTION = "description";
    public const string KEY_FULL_NAME = "full_name";
    public const string KEY_CHARGE_ACCOUNT = "charge_account";
    public const string KEY_AMOUNT = "amount";
    public const string KEY_NUMBER_OF_TRANSFERS = "number_of _transfers";
    public const string KEY_DATE_START = "date_start";
    public const string KEY_DATE_END = "date_end";
    public const string KEY_SO_ID = "so_id";
    public const string KEY_SO_REQUEST_ID = "SO_REQUEST_ID";
    public const string KEY_SO_CREATE_DATE = "SO_CREATE_DATE";
    //  public const string KEY_SO_CREATE_DATE2 = "so_create_date";
    public const string KEY_SO_START_DATE = "SO_START_DATE";
    public const string KEY_SO_START_DATE2 = "so_start_date";
    public const string KEY_SO_EXPIRY_DATE = "SO_EXPIRY_DATE";
    public const string KEY_SO_EXPIRY_DATE2 = "so_expiry_date";
    public const string KEY_SO_COUNT = "so_count";
    public const string KEY_SO_SELECTED_BENEFICIARY_CARD = "so_selected_beneficiary_card";
    public const string KEY_SO_SELECTED_CHARGE_ACCOUNT = "so_selected_charger_account";
    public const string KEY_SO_DEBIT_ACCOUNT = "so_debit_account";
    public const string KEY_SO_AMOUNT = "so_amount";
    public const string KEY_SO_VAT_Fee = "so_VATFee";
    public const string KEY_SO_Fee = "so_fee";
    public const string KEY_SO_TOTALFEE = "so_totalfee";
    public const string KEY_SO_CURRENCY = "so_currency";
    public const string KEY_DEBIT_ACCOUNT_NO = "debit_account_no";
    public const string KEY_MARGIN_ACCOUNT_NO = "margin_account_no";
    public const string KEY_NO_OF_RETRIES = "no_of_retries";
    public const string KEY_SHIFT_FAILED_PAYMENTS = "shift_failed_payments";
    public const string NO_OF_SUCCESSIVE_RETRIES = "no_of_successive_retries";
    public const string KEY_OLD_TARGET_CREDIT_LIMIT = "old_target_credit_limit";
    public const string KEY_NEW_TARGET_CREDIT_LIMIT = "new_target_credit_limit";
    public const string KEY_NEW_COLLECTION_DAY = "new_collection_day";
    //public const string EVENT_SO_ADD_EDIT_DELETE = "so_add_edit_delete";
    public const string KEY_CARD_TYPE = "card_type";
    public const string KEY_CARD_STATUS = "card_status";
    public const string KEY_OLD_CARD_STATUS = "old_card_status";
    public const string KEY_NEW_CARD_STATUS = "new_card_status";
    public const string KEY_NEW_CARD_NO = "new_card_no";
    public const string KEY_APPROVED_LIMIT = "approved_limit";
    public const string KEY_APPROVE_DATE = "approve_date";
    public const string KEY_REJECT_DATE = "reject_date";
    public const string KEY_OPEN_DATE = "open_date";
    public const string KEY_REQUEST_DATE = "request_date";
    public const string KEY_EXPIRY_DATE = "expiry_date";
    public const string KEY_CLOSE_DATE = "close_date";
    public const string KEY_DEDUCTED_AMOUNT = "deducted_amount";
    public const string KEY_BRANCH_ID = "branch_id";
    public const string KEY_ACCOUNT_TYPE = "account_type";
    public const string KEY_ACCOUNT_CLASS_CODE = "account_class_code";
    public const string KEY_CUSTOMER_CLASS_CODE = "get_customer_class_code";
    public const string KEY_CUSTOMER_NO = "customer_no";
    public const string KEY_TITLE1 = "title1";
    public const string KEY_TITLE2 = "title2";
    public const string KEY_CINET_ID = "CINet_id";
    public const string KEY_LIMIT_CHANGE_SEARCH = "limit_change_search";
    public const string KEY_CARD_HOLDER_NAME = "card_holder_name";
    public const string KEY_OLD_EMBOSSING_NAME = "OLD_EMBOSSING_NAME";
    public const string KEY_ISSUE_PIN_MAILER = "IssuePinMailer";
    public const string KEY_MEMBERSHIPID = "MembershipID";
    public const string KEY_OLD_MEMBERSHIPID = "Old_MembershipID";
    public const string KEY_CLUB_NAME = "ClubName";
    public const string KEY_COMPANY_ID = "CompanyID";
    public const string KEY_DUPLICATE_MEMBERSHIPID = "Duplicate_MembershipID";
    public const string KEY_MEMBERSHIP_DELETE_REQUEST_ID = "Membership_Delete_request_ID";
    public const string KEY_New_CARD_HOLDER_NAME = "New card_holder_name";
    public const string KEY_Old_CARD_HOLDER_NAME = "Old card_holder_name";
    public const string KEY_PO_BOX = "po_box";
    public const string KEY_ZIP_CODE = "zip_code";
    public const string KEY_HOME_PHONE = "home_phone_no";
    public const string KEY_MOBILE_NO = "mobile_no";
    public const string KEY_WORK_PHONE_NO = "work_phone_no";
    public const string KEY_STREET = "street";
    public const string KEY_AREA = "area";
    public const string KEY_ADDRESSLINE1 = "ADDRESSLINE1";
    public const string KEY_ADDRESSLINE2 = "ADDRESSLINE2";
    public const string KEY_BILLING_ACCOUNT_NO = "BILLING_ACCOUNT_NO";
    public const string KEY_Is_Waive_Fees = "Is_Waive_Fees";
    public const string KEY_Replacement_Reason = "Replacement_Reason";
    public const string KEY_Limit_Change_Type = "Limit Change Type";
    public const string KEY_Current_Limit = "Current Limit";
    public const string KEY_New_Limit = "New Limit";
    public const string KEY_Purge_Days = "Purge Days";
    public const string KEY_User_Comments = "User Comments";
    public const string KEY_KFH_Salary = "KFH Salary";
    public const string KEY_Is_Retiree = "Is Retiree";
    public const string KEY_Is_Guarantor = "Is Guarantor";
    public const string KEY_Cinet_Salary = "Cinet Salary";
    public const string KEY_Cinet_Installment = "Cinet Installment";
    public const string KEY_Other_Bank_Limit = "Other Bank Limit";
    public const string KEY_Caps_Type = "Caps Type";
    public const string KEY_Caps_Date = "Caps Date";
    public const string KEY_In_Delinquent_List = "In Delinquent List";
    public const string KEY_In_Black_List = "In Black List";
    public const string KEY_In_Cinet_Black_List = "In Cinet Black List";
    public const string KEY_Exception = "Exception";


    //App Status key Values
    public const string KEY_REQUEST_ID = "request_id";
    public const string KEY_REQUEST_ACTIVITY_ID = "request_activity_id";

    public const string KEY_LIMIT_CHANGE_ID = "limit_change_id";

    public const string KEY_FD_ACCOUNT_NO = "fd_account_no";
    public const string KEY_DELEGATE_ID = "delegate_id";
    public const string KEY_REQUEST_AGAINST = "request_against";
    public const string KEY_REQUEST_AGAINST_OLD = "request_against_old";
    public const string KEY_GUARANTEE_AMOUNT = "guarantee_amount";
    public const string KEY_GUARANTEE_ACCOUNT = "guarantee_account_number";
    public const string KEY_GUARANTEE_NUMBER = "Guarantee_number";
    public const string AGAINST_CORPORATE_CARD = "AGAINST_CORPORATE_CARD";
    public const string EVENT_Print_E_Application = "Print E-Application";
    public const string AGAINST_MARGIN_INCREMENTAL = "AGAINST_MARGIN_INCREMENTAL";
    public const string NUMBER_OF_MONTHLY_INSTALLMENTS = "NUMBER_OF_MONTHLY_INSTALLMENTS";
    public const string MONTHLY_INSTALLMENT_AMOUNT = "MONTHLY_INSTALLMENT_AMOUNT";
    public const string FIRST_MONTHLY_INSTALLMENT_AMOUNT = "FIRST_MONTHLY_INSTALLMENT_AMOUNT";
    public const string LAST_MONTHLY_INSTALLMENT_AMOUNT = "LAST_MONTHLY_INSTALLMENT_AMOUNT";
    public const string FIRST_INSTALLMENT_DATE = "FIRST_INSTALLMENT_DATE";
    public const string SECOND_INSTALLMENT_DATE = "SECOND_INSTALLMENT_DATE";
    public const string LAST_INSTALLMENT_DATE = "LAST_INSTALLMENT_DATE";
    public const string MAXIMUM_TARGET_LIMIT = "MAXIMUM_TARGET_LIMIT";
    public const string INITIAL_PAYMENT_AMOUNT = "INITIAL_PAYMENT_AMOUNT";
    public const string TARGET_CREDIT_LIMIT = "TARGET_CREDIT_LIMIT";
    public const string IS_SALARY_BASED = "IS_SALARY_BASED";
    public const string SECOND_INSTALLMENT_AMOUNT = "SECOND_INSTALLMENT_AMOUNT";
    public const string SOID_FOR_MARGIN = "SOID_FOR_MARGIN";
    public const string SELLER_GENDER_CODE = "SELLER_GENDER_CODE";
    public const string EMPLOYMENT = "EMPLOYMENT";
    public const string CUSTOMER_CLASS_CODE = "CUSTOMER_CLASS_CODE";
    public const string ISSUING_OPTION = "ISSUING_OPTION";
    public const string ISSUED_USING = "ISSUED_USING";
    public const string MARGIN_ACCOUNT_NO = "MARGIN_ACCOUNT_NO";
    public const string DEPOSIT_ACCOUNT_NO = "DEPOSIT_ACCOUNT_NO";
    public const string MARGIN_AMOUNT = "MARGIN_AMOUNT";
    public const string DEPOSIT_AMOUNT = "DEPOSIT_AMOUNT";
    public const string DEPOSIT_NUMBER = "DEPOSIT_NUMBER";
    public const string CUSTOM_STANDING_ORDER_SERVICE_NAME = "credit_card_standing_order_limit_increase";

    public const string TCD_AMOUNT = "TCD_AMOUNT";
    public const string ENABLE_MARGIN_TO_TCD_MIGRATION = "ENABLE_MARGIN_TO_TCD_MIGRATION";
    public const string TCD_TYPE_ID = "TCD_TYPE_ID";
    public const string TCD_MATURITY_METHOD = "TCD_MATURITY_METHOD";
    public const string SALARY = "SALARY";
    public const string OLD_CARD_NO = "OLD_CARD_NO";

    public const string KEY_SALARY_ACCOUNT_NO = "salary_account_no";
    public const string KEY_Deposit_ACCOUNT_NO = "deposit_account_no";

    //Search request key values
    public const string KEY_REQUEST_TYPE = "request_type";

    public const string KEY_BRANCH = "branch";

    //Credit Card Statement key values
    public const string KEY_MONTH = "month";

    public const string KEY_YEAR = "year";

    public const string KEY_SECONDARY_CARD_NO = "SECONDARY_CARD_NO";
    public const string KEY_DELIVERY_OPTION = "DELIVERY_OPTION";
    public const string KEY_CHARGE = "CHARGE";

    public const string KEY_DOWNPAYMENT_FOR_MARGIN = "DOWNPAYMENT_FOR_MARGIN";
    public const string KEY_INITIATOR_ID = "INITIATOR_ID";
    public const string KEY_INITIATOR_NAME = "INITIATOR_NAME";
    public const string KEY_APPROVER_ID = "APPROVER_ID";
    public const string KEY_APPROVER_NAME = "APPROVER_NAME";
    public const string KEY_INITIATOR_BRANCH_ID = "INITIATOR_BRANCH_ID";
    public const string KEY_INITIATOR_BRANCH_NAME = "INITIATOR_BRANCH_NAME";
    public const string KEY_TRANSACTION_AMOUNT = "TRANSACTION_AMOUNT";
    public const string KEY_REMARKS = "remarks";
    public const string KEY_CHRG_BCK_DISP_STATUS_ID = "CHRG_BCK_DISP_STATUS_ID";
    public const string KEY_CHRG_BCK_DISP_ID = "CHARGE_BACK_DISPUTE_ID";

    public const string KEY_FROM_DATE = "from_date";
    public const string KEY_TO_DATE = "to_date";
    public const string KEY_TELLER_ID = "teller_id";
    public const string KEY_BY_STAFF = "by_staff";
    public const string KEY_BY_BRANCH = "by_branch";
    public const string KEY_LIMIT_CHANGE_TYPE = "limit_change_type";


    public static string CARD_STATUS_LOAD = "CardStatus.aspx - PageLoad";
    public static string CARD_STATUS_SEARCH = "CardStatus.aspx - Search";
    public static string CARD_STATUS_CHANGESTATUS = "CardStatus.aspx - ChangeCardStatus";
    public static string CARD_STATUS_GENERATELINK = "CardActivation.aspx - GenerateCardDeliveryLink";

    public static string GET_CARD_DELIVERY_STATUS = "get_card_delivery_status";
    public static string CREATE_CARD_DELIVERY = "create_card_delivery";
    public static string KEY_PRIMARY_LOGO = "primary_logo";
    public static string KEY_SECONDARY_LOGO = "secondary_logo";
    public static string KEY_PCT_FLAG = "pct_flag";
    public static string KEY_CASH_PLAN_NO = "cash_plan_no";
    public static string KEY_RETAIL_PLAN_NO = "retail_plan_no";
    public static string KEY_IS_MASTER_CARD="is_master_card";



    //Data Leakage Parameters                                  k
    public const string KEY_OLD_FIRST_NAME = "old_first_name";
    public const string KEY_NEW_FIRST_NAME = "new_first_name";
    public const string KEY_OLD_MIDDLE_NAME = "old_middle_name";
    public const string KEY_NEW_MIDDLE_NAME = "new_middle_name";
    public const string KEY_OLD_LAST_NAME = "old_last_name";
    public const string KEY_NEW_LAST_NAME = "new_last_name";

    public const string KEY_OLD_NATIONALITY = "old_nationality";
    public const string KEY_NEW_NATIONALITY = "new_nationality";
    public const string KEY_OLD_DOB = "old_DOB";
    public const string KEY_NEW_DOB = "new_DOB";
    public const string KEY_OLD_GENDER = "old_gender";
    public const string KEY_NEW_GENDER = "new_gender";
    public const string KEY_OLD_EMPLOYER = "old_employer";
    public const string KEY_NEW_EMPLOYER = "new_employer";

    // Credit Reverse Parameters

    public const string KEY_AMOUNT_KD_MAKER = "amount_KD_maker";
    public const string KEY_RATE_MAKER = "rate_maker";
    public const string KEY_AMOUNT_KD_CHECKER = "amount_KD_checker";
    public const string KEY_RATE_CHECKER = "rate_checker";
    public const string KEY_CARD_CURRENCY = "card_currency";

    //VAT
    public const string KEY_DEBIT_ACCT_NO = "debit_acct_no";
    public const string KEY_ORGINAL_FEE_AMOUNT = "Original_Fee_Amount";
    public const string KEY_OVERWRITE_FEE_AMOUNT = "Overwrite_Fee_Amount";
    public const string KEY_OVERWRITE_REASON = "Overwrite_Reason";
    public const string KEY_SERVICE_NAME = "Service_Name";
    public const string KEY_CURRENCY_RATE = "currency_rate";

    public const string KEY_TARGET_ISSUING_OPTION = "TARGET_ISSUING_OPTION";
    public const string KEY_PENDING_COLLATERAL_MIGRATION = "PENDING_COLLATERAL_MIGRATION ";
    public const string KEY_MIGRATED_BY_ID = "MIGRATED_BY_ID";
    public const string KEY_MIGRATED_BY_NAME = "MIGRATED_BY_NAME";
    public const string KEY_MIGRATOR_BRANCH_ID = "MIGRATOR_BRANCH_ID";
    public const string KEY_MIGRATOR_BRANCH_NAME = "MIGRATOR_BRANCH_NAME";
    public const string MARGIN_REFERENCE_NO = "MARGIN_REFERENCE_NO";
    public const string KEY_CURRENT_ISSUING_OPTION = "CURRENT_ISSUING_OPTION";
}

public class DetailKey
{
    //Application values
    public const int APPLICATION_ID = 15;

    //Event status values
    public const string EVENT_SUCCESSFUL = "successful";

    public const string EVENT_UNSUCCESSFUL = "unsuccessful";

    //Severity values
    public const string KEY_SEVERITY = "severity";

    public const string SEVERITY_LOW = "low";
    public const string SEVERITY_MEDIUM = "medium";
    public const string SEVERITY_HIGH = "high";
    public const string SEVERITY_VERY_HIGH = "very_high";

    //Event values
    public const string EVENT_PERFORM_DEDUCTON = "perform_deduction";

    public const string EVENT_VERIFY_APPROVAL = "verify_approval";
    public const string EVENT_LOAD_DATA = "load_data";
    public const string EVENT_SEARCH_PROFILE = "search_profile";
    public const string EVENT_SEARCH_REPORT = "search_report";
    public const string EVENT_DELETE_PAYEE = "delete_payee";
    public const string EVENT_MODIFY_PAYEE = "modify_payee";
    public const string EVENT_ADD_PAYEE = "add_payee";
    public const string EVENT_GET_STANDING_ORDER_LIST = "get_standing_order_list";
    public const string EVENT_ADD_STANDING_ORDER = "add_standing_order";
    public const string EVENT_EDIT_STANDING_ORDER = "edit_standing_order";
    public const string EVENT_UPDATE_SCHEDULED_SO_TRANSACTIONS = "update_scheduled_so_transactions";
    public const string EVENT_DELETE_STANDING_ORDER = "delete_standing_order";
    public const string EVENT_PERFORM_PAYMENT = "perform_payment";
    public const string EVENT_CREDIT_REVERSE = "credit_reverse";
    public const string EVENT_CREDIT_REVERSE_APPROVE = "credit_reverse_approve";
    public const string EVENT_CREDI_REVERSE_REJECT = "credit_reverse_reject";
    public const string EVENT_CREDIT_REVERSE_DELETE = "credit_reverse_delete";
    public const string EVENT_CREDIT_REVERSE_LOAD = "credit_reverse_load";
    public const string EVENT_LOCAL_UPDATE_CARD_NUNBER = "Local_Update_Card_Number";
    public const string EVENT_CARD_DETAILS = "card_details";
    public const string EVENT_CARD_Replacement_Print = "Replacement_tracking_Print_PDF";
    public const string EVENT_CARD_Replacement_Tracking_GetData = "Replacement_tracking_FillData";
    public const string EVENT_CARD_ACTIVATION = "card_activation";
    public const string EVENT_CARD_REACTIVATION = "card_reactivation";
    public const string EVENT_CARD_STATEMENT = "card_statement";
    public const string EVENT_CARD_STATEMENT_LOAD = "card_statement_load";
    public const string EVENT_CARD_STATEMENT_PRINT = "card_statement_print";
    public const string EVENT_CARD_STATEMENT_SEARCH = "card_statement_search";
    public const string EVENT_SUBMIT_NEW_CALCULATIONS = "submit_new_calculations";
    public const string EVENT_PRINT_LIMIT_CHANGE_REPORT = "print_limit_change_report";
    public const string EVENT_SEARCH_LIMIT_CHANGE_REPORT = "search_limit_change_report";
    public const string EVENT_PERFORM_ACCOUNT_BOARDING = "perform_account_boarding";

    public static string ForeignCurrencyPrepaidCards = "FOREIGN_CURRENCY_PREPAID_CARDS";
    public const string EVENT_VALIDATE_CREDIT_CARD_CURRENCY_RATE_validate_CreditCard_CurrencyRate = "validate_Credit_Card_Currency_Rate";
    public const string EVENT_PERFORM_FOREIGN_CURRENCY_CREDIT_CARD_PAYMENT_WITH_MQ = "Perform_ForeignCurrency(FC)_CreditCard_PaymentMQ";
    public const string EVENT_PERFORM_FOREIGN_CURRENCY_CREDIT_CARD_REFUND_WITH_MQ = "Perform_ForeignCurrency(FC)_CreditCard_RefundMQ";
    public const string EVENT_FXRATEACKNOWLEDGEMENT_PRINT_FORM = "event_fxrateacknowledgement_print_form";

    public const string EVENT_CARD_STATEMENT_CYCLETODATE = "card_statement_cycletodate";
    public const string EVENT_CARD_STATEMENT_MOMNTHLYSTATEMENT = "card_statement_monthlystatement";
    public const string EVENT_CARD_STATEMENT_CREDITCARDHISTORY = "card_statement_creditcardhistory";
    public const string EVENT_CARD_STOP = "card_stop";
    public const string EVENT_CARD_CLOSURE = "card_closure";
    public const string EVENT_VALIDATE_CLOSURE_BY_CHECKING_SO = "validate_closure_by_checking_so";
    public const string EVENT_CARD_LOST_STOLEN = "card_lost_stolen";
    public const string EVENT_DELETE_CARD_PREREGESTERED_PAYEE = "delete_card_preregestered_payee";
    public const string EVENT_GET_SUPPLEMENTARY_CARDS_LIST = "get_supplementary_cards_list";
    public const string EVENT_REPORT_CARD_AS_LOST_OR_STOLEN = "report_card_as_lost_or_stolen";
    public const string EVENT_GET_BANKING_CUSTOMER_PROFILE = "get_banking_customer_profile";
    public const string EVENT_GET_CUSTOMER_ACCOUNTS = "get_customer_accounts";
    public const string EVENT_GET_CUSTOMER_ACCOUNTS_BY_CIVIL_FOR_DP = "get_customer_accounts_by_civil_for_dp";
    public const string EVENT_GET_CUSTOMER_ACCOUNTS_BY_Account_FOR_DP = "get_customer_accounts_by_account_for_dp";
    public const string EVENT_GET_CUSTOMER_PROFILE = "get_customer_profile";

    public const string EVENT_GET_OWNED_CARDS = "get_owned_cards";
    public const string EVENT_BIND_CHARGE_BACK_DISPUTE = "bind_charge_back_dispute";
    public const string EVENT_ROW_BOUND_CHARGE_BACK_DISPUTE = "row_bound_charge_back_dispute";
    public const string EVENT_ROW_BOUND_EOD_STAFF = "row_bound_eod_staff";
    public const string EVENT_ITEM_BOUND_EOD_BRANCH = "item_bound_eod_branch";
    public const string EVENT_ROW_COMMAND_EOD_BRANCH = "row_command_eod_branch";
    public const string EVENT_ROW_COMMAND_EOD_STAFF = "row_command_eod_staff";
    public const string EVENT_SEARCH_CHARGE_BACK_DISPUTE = "search_charge_back_dispute";
    public const string EVENT_UPDATE_CHARGE_BACK_DISPUTE = "update_charge_back_dispute";
    public const string EVENT_UPDATE_CARD_STATUS = "update_card_status";
    public const string EVENT_CLOSE_CHARGE_BACK_DISPUTE = "close_charge_back_dispute";
    public const string EVENT_ROW_EDITING = "row_editing";
    public const string EVENT_GET_PAYEE_DETAILS = "get_payee_details";
    public const string EVENT_GET_CARD_DETAILS = "get_card_details";
    public const string EVENT_DELETE_CUSTOMER_PROFILE = "delete_customer_profile";
    public const string EVENT_GET_CREDIT_CARDS_REQUEST = "get_credit_card_request";
    public const string EVENT_GET_EXTERNAL_STATUS = "get_external_status";
    public const string EVENT_ADD_STANDING_ORDER_REQUEST_PARAMETERS = "add_standing_order_request_parameters";
    public const string EVENT_STANDING_ORDER_ADD_EDIT_DELETE = "standing_order_add_edit_delete";
    public const string EVENT_STANDING_ORDER_lIST = "standing_order_list";
    public const string EVENT_CUSTOMER_PROFILE = "customer_profile";
    public const string EVENT_FILL_CUSTOMER_PROFILE = "fill_customer_profile";
    public const string EVENT_FILL_LOYALTY_STATEMENT = "fill_loyalty_statement";
    public const string EVENT_EDIT_CUSTOMER_PROFILE = "edit_customer_profile";
    public const string EVENT_DELEGATE_REQUEST = "delegate_request";
    public const string EVENT_GET_CREDIT_CARD_STATEMENT = "get_credit_card_statement";
    public const string EVENT_FILL_CYCLE_TO_DATE = "fill_cycle_to_date";
    public const string EVENT_MONTHLY_STATEMENT = "fill_monthly_statement";
    public const string EVENT_CHANGE_SEARCH_CREITERIA = "change_search_creiteria";
    public const string EVENT_DATE_FORMAT = "change_date_format";
    public const string EVENT_CHANGE_BILLING_ADDRESS = "change_billing_address";
    public const string EVENT_EXPORT_EXCEL_REPORT = "export_excel_report";
    public const string EVENT_CREATE_CUSTOMER_PROFILE = "create_customer_profile";
    public const string EVENT_CREATE_MARGIN_ACCOUNT = "create_margin_account";
    public const string EVENT_REPLACEMENT_CARD_FOR_DAMAGE = "replacement_card_for_damage";
    public const string EVENT_REPLACEMENT_CARD_FOR_LOST_OR_STOLEN = "replacement_card_for_lost_or_stolen";
    public const string EVENT_ADD_PRE_REG_PAYEE_FOR_LOST_OSRA_SUPP = "add_preregistered_payee_for_lost_osra_supp";
    public const string EVENT_CREDIT_CHECKER_REJECTION = "credit_checker_rejection";
    public const string EVENT_CREDIT_CHECKING_REVIEWED = "credit_checking_reviewed";
    public const string EVENT_FINAL_CREDIT_CHECKING_APPROVAL = "final_credit_checking_approval";
    public const string EVENT_MINOR_CHARGE_CARD_APPROVAL = "minor_charge_card_approval";
    public const string EVENT_EOD_PAGE_LOAD = "eod_page_load";
    public const string EVENT_BIND_BRANCHES = "bind_branches";
    public const string EVENT_BIND_CFU_ACTIVITIES = "bind_cfu_activities";
    public const string EVENT_EOD_SEARCH = "eod_search";
    public const string EVENT_EOD_BIND_GRIDS = "eod_bind_grids";
    public const string EVENT_GET_EOD_SINGLE_STAFF_RESPONSE = "get_eod_single_staff_response";
    public const string EVENT_GET_EOD_BY_BRANCH_RESPONSE = "get_eod_by_branch_response";
    public const string EVENT_EOD_PRINT = "eod_print";


    //Dynamic Payment (boruj 05-08-2020)[CRC-5577]
    public const string EVENT_DYNAMIC_PAYMENT = "dynamic_payment";
    public const string EVENT_DYNAMIC_PAYMENT_PAGE_LOAD = "dynamic_payment_page_load";
    public const string EVENT_DYNAMIC_PAYMENT_PERFORM_PAYMENT = "dynamic_payment_perform_payment";

    //MIGS
    public const string EVENT_ORACLE_CONNECTION = "Oracle_Connection";
    public const string EVENT_MERCHANT_PROGRESS_REPORT = "Merchant_Progress_Report";
    public const string EVENT_EXPORT_PROGRESS_REPORT = "Export_Progress_Report";
    public const string EVENT_GET_COUNTRIES = "Get_Countries";
    public const string EVENT_GET_CURRENCIES = "Get_Currencies";
    public const string EVENT_LOAD_IDS = "Load_Ids";
    public const string EVENT_GET_MASTER_FILE = "Get_Master_File";
    public const string EVENT_APPLY_RULES = "apply_rules";
    public const string EVENT_GET_ISSUERS = "Get_Issuers";
    public const string EVENT_FILTER_ISSUERS = "Filter_Issuers";
    public const string EVENT_POST_ISSUER_BIN = "Post_Issuer_Bin";
    public const string EVENT_PUT_ISSUER_BIN = "Put_Issuer_Bin";
    public const string EVENT_DELETE_ISSUER_BIN = "Delete_Issuer_Bin";
    public const string EVENT_GET_MERCHANT_GROUPS = "Get_Merchant_Groups";
    public const string EVENT_POST_MERCHANT_GROUPS = "Post_Merchant_Groups";
    public const string EVENT_DELETE_MERCHANT_GROUPS = "Delete_Merchant_Groups";
    public const string EVENT_PUT_MERCHANT_GROUPS = "Put_Merchant_Groups";
    public const string EVENT_GET_MERCHANTS = "Get_Merchants";
    public const string EVENT_POST_MERCHANT = "Post_Merchant";
    public const string EVENT_DELETE_MERCHANT = "Delete_Merchant";
    public const string EVENT_PUT_MERCHANT = "Put_Merchant";
    public const string EVENT_FILTER_MERCHANTS = "Filter_Merchants";
    public const string EVENT_PUT_SUSPICIOUS_MERCHANTS = "Put_Suspicious_Merchants";
    public const string EVENT_GET_BLACK_CARDS = "Get_Black_Cards";
    public const string EVENT_POST_BLACK_CARDS = "Post_Black_Cards";
    public const string EVENT_PUT_BLACK_CARDS = "Put_Black_Cards";
    public const string EVENT_DELETE_BLACK_CARDS = "Delete_Black_Cards";
    public const string EVENT_PUT_SUSPICIOUS_CARDS = "Put_Suspicious_Cards";
    public const string EVENT_GENERATE_FILE = "Generate_File";
    public const string EVENT_FILTER_TRANSACTIONS = "Filter_Transactions";
    public const string EVENT_PUT_TRANSACTIONS_FRAUD_STATUS = "Put_Transactions_Fraud_Status";
    public const string EVENT_PUT_SEND_TO_FDR = "Put_Send_To_FDR";
    //public const string EVENT_EXPORT_FILTER_TRANSACTIONS = "Export_Filter_Transactions";        
    public const string EVENT_GET_FRAUDULENT_RULES = "Get_Fraudulent_Rules";
    public const string EVENT_PUT_FRAUDULENT_RULE = "Put_Fraudulent_Rule";
    public const string EVENT_APPROVE_FRAUDULENT_RULE = "Approve_Fraudulent_Rule";
    //public const string EVENT_GETISCHECKER                                                     
    //public const string EVENT_IS_USER_AUTHENTICATE = "IS_USER_AUTHENTICATE";                      


    // DocuWare
    public const string EVENT_STORE_DOCUMENT_DOCUWARE = "Store Document in Docuware";
    public const string EVENT_GET_DOCUMENT_DOCUWARE = "Get Document from Docuware";
    public const string EVENT_SEARCH_DOCUMENT_DETAILS_DOCUWARE = "Search Document Details from Docuware";
    public const string EVENT_GET_DOCUMENT_TYPE_DOCUWARE = "Get Document Type from Docuware";

    /// <summary>
    //Corporate card
    public const string EVENT_GET_CORPORATE_PROFILE = "get_corporate_profile";
    public const string EVENT_ADD_CORPORATE_PROFILE_REQUEST = "add_corporate_profile_request";
    public const string EVENT_UPDATE_CORPORATE_PROFILE_REQUEST = "update_corporate_profile_request";
    public const string EVENT_FillCorporateProfileWS = "get_corporate_profile_ws";
    public const string EVENT_FillCorporateAccountsWS = "get_corporate_accounts_ws";
    public const string EVENT_APPROVE_CORPORATE_PROFILE_ADD = "approve_corporate_profile_add_request";
    public const string EVENT_APPROVE_CORPORATE_PROFILE_UPDATE = "approve_corporate_profile_update_request";
    public const string EVENT_REJECT_CORPORATE_PROFILE = "reject_corporate_profile";
    public const string EVENT_ApproveRejectRequest_LOAD = "Approve_Reject_Request_Load";
    public const string EVENT_ApproveRequest = "Approve_Request";
    public const string EVENT_RejectRequest = "Reject_Request";
    public const string EVENT_ValidateGlobalLimitChange = "Validate_Global_Limit_Change";
    public const string EVENT_VALIDATE_MATURITY_DATE = "Validate_maturity_Date";
    /// </summary>

    //Request Admin Event Values
    public const string EVENT_REQUEST_ADMIN_LOAD = "Request_Admin_Load";

    //App Status Event Values
    public const string EVENT_CARD_ISSUE_AGAINST_CHANGED = "Card_Issue_Against_Changed";

    public const string EVENT_APP_STATUS_MARGINACCNO = "App_Status_MarginAccNo";
    public const string EVENT_APP_STATUS_SHOWADDRESS = "App_Status_ShowAddress";
    public const string EVENT_APP_STATUS_DEPOSITACCNO = "App_Status_DepositAccNo";
    public const string EVENT_APP_STATUS_HOLDLIST = "App_Status_HoldList";
    public const string EVENT_APP_STATUS_LOAD = "App_Status_Load";
    public const string EVENT_APP_STATUS = "App_Status";
    public const string EVENT_APP_STATUS_APPROVE = "approve_request";
    public const string EVENT_APP_CARD_UPGRADE = "card_upgrade";
    public const string EVENT_APP_CARD_UPGRADE_DETAIL = "card_upgrade_detail";
    public const string EVENT_APP_STATUS_FULL_EDIT = "full_edit_request";
    public const string EVENT_APP_STATUS_EDIT = "edit_request";
    public const string EVENT_APP_STATUS_DELETE = "delete_request";
    public const string EVENT_DELETE_PROMOTION_BENEFICIARY = "delete_promotion_beneficiary";
    public const string EVENT_CHANGE_TARGET_CREDIT_LIMIT_WHILE_APPROVING = "change_target_credit_limit_while_approving";
    public const string EVENT_CHANGE_TARGET_CREDIT_LIMIT_OR_COLLECTION_DAY = "change_target_credit_limit_or_collection_day";
    public const string EVENT_APP_STATUS_FILL_CUSTOMER_PROFILE = "Fill_Customer_Profile_Fields";
    public const string EVENT_APP_STATUS_GET_CARD_INSTALLMENTS = "Get_Card_Installments_Count_By_Card_Type";
    public const string EVENT_SET_PREVIOUS_KFH_LIMIT_AND_INSTALLMENTS = "Set_Previous_KFH_Limit_And_Installments";
    public const string EVENT_APP_STATUS_GET_TAYSEER_CREDIT_CHECKING_LOG = "Get_Tayseer_Credit_Checking_Approval_Log";
    public const string EVENT_APP_STATUS_INSERT_TAYSEER_CREDIT_CHECKING_RECORD = "Insert_Tayseer_Credit_Checking_Approval_Record";
    public const string EVENT_CHANGE_LIMIT_INSERT_TAYSEER_CREDIT_CHECKING_RECORD = "change_limit_Insert_Tayseer_Credit_Checking_Approval_Record";
    public const string EVENT_CHANGE_LIMIT_PAGE_LOAD = "change_limit_page_load";
    public const string EVENT_CHANGE_LIMIT_HISTORY_COMMAND = "change_limit_history_command";
    public const string EVENT_APP_STATUS_CHANGE_LIMIT = "change_limit";
    public const string EVENT_APP_STATUS_CHANGE_MQ_CREDIT_LIMIT = "change_limit_Call_Change_MqCreditLimit";
    public const string EVENT_LIMIT_CHANGE_APPROVE_REJECT = "limit_change_approve_reject";
    public const string EVENT_LIMIT_CHANGE_CREDIT_REVIEW_CHECKING_APPROVE_REJECT = "limit_change_Credit_Review_Checking_approve_reject";
    public const string EVENT_LIMIT_CHANGE_INSERT = "limit_change_insert";
    public const string EVENT_LIMIT_CHANGE_DELETE = "limit_change_delete";
    public const string EVENT_CANCEL_LIMIT_CHANGE_APPROVE_REJECT = "cancel_limit_change_approve_reject";

    //Search Request Event Values
    public const string EVENT_SEARCH_REQUEST = "search_request";

    public const string EVENT_INSERT_CHARGE_BACK_DISPUTE = "insert_charge_back_dispute";
    public const string EVENT_GET_USER_INFO = "get_user_info";

    public const string EVENT_SELECT_CARD_CURRENCY = "select_card_currency";
    public const string EVENT_SUPPLIMETARY_CARD_APPROVAL = "suuplimentary_card_approval";
    public const string EVENT_DOWNPAYMENT_CARD_AGAINST_INCREMENTAL_MARGIN = "downpayment_card_against_incremental_margin";
    public const string EVENT_DOWNPAYMENT_CARD_AGAINST_INCREMENTAL_MARGIN_ROLLBACK = "rollback_downpayment_card_against_incremental_margin";

    //Request Admin Event Values

    public const string EVENT_ADMIN_CHANGE_CARD_STATUS = "admin_change_card_status";
    public const string EVENT_ADMIN_CHANGE_CARD_NUMBER = "admin_change_card_number";
    public const string EVENT_ADMIN_INSERT_SECONDARY_CARD_NUMBER = "admin_insert_secondary_card_number";
    public const string EVENT_ADMIN_OLD_CARD_CLOSURE = "admin_old_card_closure";
    public const string EVENT_DELETE_REQUEST_PARAMETER = "delete_request_parameter";

    //events cards operations ccso-839
    public const string EVENT_LOAD_UPDATE_CARD_DASHBOARD = "load_update_card_dashboard";
    public const string EVENT_UPDATE_CARD_DASHBOARD = "EVENT_UPDATE_CARD_DASHBOARD";
    public const string EVENT_UPDATE_CARD_EMBOSSED_NAME = "update_card_embossed_name";
    public const string EVENT_UPDATE_CARD_LINKED_ACCOUNT = "update_card_linked_account";
    public const string EVENT_APPROVE_REJECT_CARD_REPLACEMENT = "approve_reject_card_replacement";
    public const string EVENT_CARD_HOLDER_NAME_AND_ACCT = "card_holder_name_and_acct";


    // scattered
    public const string EVENT_GET_CARD_NEW_INSTALLMENT = "get_card_new_installment";

    public const string EVENT_REQUEST_ACTIVITY_CREDIT_REVERSE = "request_activity_credit_reverse";

    public const string EVENT_POSTING_FEE = "Posting_Fee";
    public const string EVENT_REVERSE_POSTING_FEE = "Reverse_Posting_Fee";
    public const string EVENT_GET_SELECTED_DD_CONFIG = "get_selected_dd_config";
    public const string EVENT_SELECT_FILE_LOAD = "select_file_load";
    public const string EVENT_VALIDATE_ADD = "validate_add";
    public const string EVENT_ADD_DD_OPTION = "add_dd_option";
    public const string KEY_SELECTED_FILE_LOAD = "selected_file_load";

    //Key values
    public const string KEY_USER_ID = "user_id";
    public const string KEY_USER_NAME = "user_name";
    public const string KEY_CFU_ACTIVITY_ID = "cfu_activity_id";
    public const string KEY_PARAMETER = "parameter";
    public const string KEY_STATUS_ID = "status_id";
    public const string KEY_STAFF_ID = "staff_id";
    public const string KEY_IS_KFH_STAFF = "is_KFH_staff";
    public const string KEY_TYPE_ID = "type_id";
    public const string KEY_ACCT_NO = "acct_no";
    public const string KEY_PRIMARY_CIVIL_ID = "primary_civil_id";
    public const string CIVIL_ID = "civil_id";
    public const string KEY_CORPORATE_CIVIL_ID = "corporate_civil_id";
    public const string KEY_PRIMARY_CARD_NO = "primary_credit_card_no";
    public const string KEY_CREDIT_CARD_NO = "credit_card_no";
    public const string KEY_RIM_NO = "rim_no";
    public const string KEY_RECORDS_DELETED = "records_deleted";
    public const string KEY_COMMERCIAL_REGISTRAR = "commercial_registrar";
    public const string KEY_DESCRIPTION = "description";
    public const string KEY_FULL_NAME = "full_name";
    public const string KEY_CHARGE_ACCOUNT = "charge_account";
    public const string KEY_CreditReverseId = "CreditReverseId";
    public const string KEY_AMOUNT = "amount";
    public const string KEY_NUMBER_OF_TRANSFERS = "number_of _transfers";
    public const string KEY_DATE_START = "date_start";
    public const string KEY_DATE_END = "date_end";
    public const string KEY_SO_ID = "so_id";
    public const string KEY_SO_REQUEST_ID = "SO_REQUEST_ID";
    public const string KEY_SO_CREATE_DATE = "SO_CREATE_DATE";
    //  public const string KEY_SO_CREATE_DATE2 = "so_create_date";
    public const string KEY_SO_START_DATE = "SO_START_DATE";
    public const string KEY_SO_START_DATE2 = "so_start_date";
    public const string KEY_SO_EXPIRY_DATE = "SO_EXPIRY_DATE";
    public const string KEY_SO_EXPIRY_DATE2 = "so_expiry_date";
    public const string KEY_SO_COUNT = "so_count";
    public const string KEY_SO_SELECTED_BENEFICIARY_CARD = "so_selected_beneficiary_card";
    public const string KEY_SO_SELECTED_CHARGE_ACCOUNT = "so_selected_charger_account";
    public const string KEY_SO_DEBIT_ACCOUNT = "so_debit_account";
    public const string KEY_SO_AMOUNT = "so_amount";
    public const string KEY_SO_VAT_Fee = "so_VATFee";
    public const string KEY_SO_Fee = "so_fee";
    public const string KEY_SO_TOTALFEE = "so_totalfee";
    public const string KEY_SO_CURRENCY = "so_currency";
    public const string KEY_DEBIT_ACCOUNT_NO = "debit_account_no";
    public const string KEY_MARGIN_ACCOUNT_NO = "margin_account_no";
    public const string KEY_NO_OF_RETRIES = "no_of_retries";
    public const string KEY_SHIFT_FAILED_PAYMENTS = "shift_failed_payments";
    public const string NO_OF_SUCCESSIVE_RETRIES = "no_of_successive_retries";
    public const string KEY_OLD_TARGET_CREDIT_LIMIT = "old_target_credit_limit";
    public const string KEY_NEW_TARGET_CREDIT_LIMIT = "new_target_credit_limit";
    public const string KEY_NEW_COLLECTION_DAY = "new_collection_day";
    //public const string EVENT_SO_ADD_EDIT_DELETE = "so_add_edit_delete";
    public const string KEY_CARD_TYPE = "card_type";
    public const string KEY_CARD_STATUS = "card_status";
    public const string KEY_OLD_CARD_STATUS = "old_card_status";
    public const string KEY_NEW_CARD_STATUS = "new_card_status";
    public const string KEY_NEW_CARD_NO = "new_card_no";
    public const string KEY_APPROVED_LIMIT = "approved_limit";
    public const string KEY_APPROVE_DATE = "approve_date";
    public const string KEY_REJECT_DATE = "reject_date";
    public const string KEY_OPEN_DATE = "open_date";
    public const string KEY_REQUEST_DATE = "request_date";
    public const string KEY_EXPIRY_DATE = "expiry_date";
    public const string KEY_CLOSE_DATE = "close_date";
    public const string KEY_DEDUCTED_AMOUNT = "deducted_amount";
    public const string KEY_BRANCH_ID = "branch_id";
    public const string KEY_ACCOUNT_TYPE = "account_type";
    public const string KEY_ACCOUNT_CLASS_CODE = "account_class_code";
    public const string KEY_CUSTOMER_CLASS_CODE = "get_customer_class_code";
    public const string KEY_CUSTOMER_NO = "customer_no";
    public const string KEY_TITLE1 = "title1";
    public const string KEY_TITLE2 = "title2";
    public const string KEY_CINET_ID = "CINet_id";
    public const string KEY_LIMIT_CHANGE_SEARCH = "limit_change_search";
    public const string KEY_CARD_HOLDER_NAME = "card_holder_name";
    public const string KEY_OLD_EMBOSSING_NAME = "OLD_EMBOSSING_NAME";
    public const string KEY_ISSUE_PIN_MAILER = "IssuePinMailer";
    public const string KEY_MEMBERSHIPID = "MembershipID";
    public const string KEY_OLD_MEMBERSHIPID = "Old_MembershipID";
    public const string KEY_CLUB_NAME = "ClubName";
    public const string KEY_COMPANY_ID = "CompanyID";
    public const string KEY_DUPLICATE_MEMBERSHIPID = "Duplicate_MembershipID";
    public const string KEY_MEMBERSHIP_DELETE_REQUEST_ID = "Membership_Delete_request_ID";
    public const string KEY_New_CARD_HOLDER_NAME = "New card_holder_name";
    public const string KEY_Old_CARD_HOLDER_NAME = "Old card_holder_name";
    public const string KEY_PO_BOX = "po_box";
    public const string KEY_ZIP_CODE = "zip_code";
    public const string KEY_HOME_PHONE = "home_phone_no";
    public const string KEY_MOBILE_NO = "mobile_no";
    public const string KEY_WORK_PHONE_NO = "work_phone_no";
    public const string KEY_STREET = "street";
    public const string KEY_AREA = "area";
    public const string KEY_ADDRESSLINE1 = "ADDRESSLINE1";
    public const string KEY_ADDRESSLINE2 = "ADDRESSLINE2";
    public const string KEY_BILLING_ACCOUNT_NO = "BILLING_ACCOUNT_NO";
    public const string KEY_Is_Waive_Fees = "Is_Waive_Fees";
    public const string KEY_Replacement_Reason = "Replacement_Reason";
    public const string KEY_Limit_Change_Type = "Limit Change Type";
    public const string KEY_Current_Limit = "Current Limit";
    public const string KEY_New_Limit = "New Limit";
    public const string KEY_Purge_Days = "Purge Days";
    public const string KEY_User_Comments = "User Comments";
    public const string KEY_KFH_Salary = "KFH Salary";
    public const string KEY_Is_Retiree = "Is Retiree";
    public const string KEY_Is_Guarantor = "Is Guarantor";
    public const string KEY_Cinet_Salary = "Cinet Salary";
    public const string KEY_Cinet_Installment = "Cinet Installment";
    public const string KEY_Other_Bank_Limit = "Other Bank Limit";
    public const string KEY_Caps_Type = "Caps Type";
    public const string KEY_Caps_Date = "Caps Date";
    public const string KEY_In_Delinquent_List = "In Delinquent List";
    public const string KEY_In_Black_List = "In Black List";
    public const string KEY_In_Cinet_Black_List = "In Cinet Black List";
    public const string KEY_Exception = "Exception";


    //App Status key Values
    public const string KEY_REQUEST_ID = "request_id";
    public const string KEY_REQUEST_ACTIVITY_ID = "request_activity_id";

    public const string KEY_LIMIT_CHANGE_ID = "limit_change_id";

    public const string KEY_FD_ACCOUNT_NO = "fd_account_no";
    public const string KEY_DELEGATE_ID = "delegate_id";
    public const string KEY_REQUEST_AGAINST = "request_against";
    public const string KEY_REQUEST_AGAINST_OLD = "request_against_old";
    public const string KEY_GUARANTEE_AMOUNT = "guarantee_amount";
    public const string KEY_GUARANTEE_ACCOUNT = "guarantee_account_number";
    public const string KEY_GUARANTEE_NUMBER = "Guarantee_number";
    public const string AGAINST_CORPORATE_CARD = "AGAINST_CORPORATE_CARD";
    public const string EVENT_Print_E_Application = "Print E-Application";
    public const string AGAINST_MARGIN_INCREMENTAL = "AGAINST_MARGIN_INCREMENTAL";
    public const string NUMBER_OF_MONTHLY_INSTALLMENTS = "NUMBER_OF_MONTHLY_INSTALLMENTS";
    public const string MONTHLY_INSTALLMENT_AMOUNT = "MONTHLY_INSTALLMENT_AMOUNT";
    public const string FIRST_MONTHLY_INSTALLMENT_AMOUNT = "FIRST_MONTHLY_INSTALLMENT_AMOUNT";
    public const string LAST_MONTHLY_INSTALLMENT_AMOUNT = "LAST_MONTHLY_INSTALLMENT_AMOUNT";
    public const string FIRST_INSTALLMENT_DATE = "FIRST_INSTALLMENT_DATE";
    public const string SECOND_INSTALLMENT_DATE = "SECOND_INSTALLMENT_DATE";
    public const string LAST_INSTALLMENT_DATE = "LAST_INSTALLMENT_DATE";
    public const string MAXIMUM_TARGET_LIMIT = "MAXIMUM_TARGET_LIMIT";
    public const string INITIAL_PAYMENT_AMOUNT = "INITIAL_PAYMENT_AMOUNT";
    public const string TARGET_CREDIT_LIMIT = "TARGET_CREDIT_LIMIT";
    public const string IS_SALARY_BASED = "IS_SALARY_BASED";
    public const string SECOND_INSTALLMENT_AMOUNT = "SECOND_INSTALLMENT_AMOUNT";
    public const string SOID_FOR_MARGIN = "SOID_FOR_MARGIN";
    public const string SELLER_GENDER_CODE = "SELLER_GENDER_CODE";
    public const string EMPLOYMENT = "EMPLOYMENT";
    public const string CUSTOMER_CLASS_CODE = "CUSTOMER_CLASS_CODE";
    public const string ISSUING_OPTION = "ISSUING_OPTION";
    public const string ISSUED_USING = "ISSUED_USING";
    public const string MARGIN_ACCOUNT_NO = "MARGIN_ACCOUNT_NO";
    public const string DEPOSIT_ACCOUNT_NO = "DEPOSIT_ACCOUNT_NO";
    public const string MARGIN_AMOUNT = "MARGIN_AMOUNT";
    public const string DEPOSIT_AMOUNT = "DEPOSIT_AMOUNT";
    public const string DEPOSIT_NUMBER = "DEPOSIT_NUMBER";
    public const string CUSTOM_STANDING_ORDER_SERVICE_NAME = "credit_card_standing_order_limit_increase";

    public const string TCD_AMOUNT = "TCD_AMOUNT";
    public const string ENABLE_MARGIN_TO_TCD_MIGRATION = "ENABLE_MARGIN_TO_TCD_MIGRATION";
    public const string TCD_TYPE_ID = "TCD_TYPE_ID";
    public const string TCD_MATURITY_METHOD = "TCD_MATURITY_METHOD";
    public const string SALARY = "SALARY";
    public const string OLD_CARD_NO = "OLD_CARD_NO";

    public const string KEY_SALARY_ACCOUNT_NO = "salary_account_no";
    public const string KEY_Deposit_ACCOUNT_NO = "deposit_account_no";

    //Search request key values
    public const string KEY_REQUEST_TYPE = "request_type";

    public const string KEY_BRANCH = "branch";

    //Credit Card Statement key values
    public const string KEY_MONTH = "month";

    public const string KEY_YEAR = "year";

    public const string KEY_SECONDARY_CARD_NO = "SECONDARY_CARD_NO";
    public const string KEY_DELIVERY_OPTION = "DELIVERY_OPTION";
    public const string KEY_CHARGE = "CHARGE";

    public const string KEY_DOWNPAYMENT_FOR_MARGIN = "DOWNPAYMENT_FOR_MARGIN";
    public const string KEY_INITIATOR_ID = "INITIATOR_ID";
    public const string KEY_INITIATOR_NAME = "INITIATOR_NAME";
    public const string KEY_APPROVER_ID = "APPROVER_ID";
    public const string KEY_APPROVER_NAME = "APPROVER_NAME";
    public const string KEY_INITIATOR_BRANCH_ID = "INITIATOR_BRANCH_ID";
    public const string KEY_INITIATOR_BRANCH_NAME = "INITIATOR_BRANCH_NAME";
    public const string KEY_TRANSACTION_AMOUNT = "TRANSACTION_AMOUNT";
    public const string KEY_REMARKS = "remarks";
    public const string KEY_CHRG_BCK_DISP_STATUS_ID = "CHRG_BCK_DISP_STATUS_ID";
    public const string KEY_CHRG_BCK_DISP_ID = "CHARGE_BACK_DISPUTE_ID";

    public const string KEY_FROM_DATE = "from_date";
    public const string KEY_TO_DATE = "to_date";
    public const string KEY_TELLER_ID = "teller_id";
    public const string KEY_BY_STAFF = "by_staff";
    public const string KEY_BY_BRANCH = "by_branch";
    public const string KEY_LIMIT_CHANGE_TYPE = "limit_change_type";


    public static string CARD_STATUS_LOAD = "CardStatus.aspx - PageLoad";
    public static string CARD_STATUS_SEARCH = "CardStatus.aspx - Search";
    public static string CARD_STATUS_CHANGESTATUS = "CardStatus.aspx - ChangeCardStatus";
    public static string CARD_STATUS_GENERATELINK = "CardActivation.aspx - GenerateCardDeliveryLink";

    public static string GET_CARD_DELIVERY_STATUS = "get_card_delivery_status";
    public static string CREATE_CARD_DELIVERY = "create_card_delivery";
    public static string KEY_PRIMARY_LOGO = "primary_logo";
    public static string KEY_SECONDARY_LOGO = "secondary_logo";
    public static string KEY_PCT_FLAG = "pct_flag";
    public static string KEY_CASH_PLAN_NO = "cash_plan_no";
    public static string KEY_RETAIL_PLAN_NO = "retail_plan_no";



    //Data Leakage Parameters                                  k
    public const string KEY_OLD_FIRST_NAME = "old_first_name";
    public const string KEY_NEW_FIRST_NAME = "new_first_name";
    public const string KEY_OLD_MIDDLE_NAME = "old_middle_name";
    public const string KEY_NEW_MIDDLE_NAME = "new_middle_name";
    public const string KEY_OLD_LAST_NAME = "old_last_name";
    public const string KEY_NEW_LAST_NAME = "new_last_name";

    public const string KEY_OLD_NATIONALITY = "old_nationality";
    public const string KEY_NEW_NATIONALITY = "new_nationality";
    public const string KEY_OLD_DOB = "old_DOB";
    public const string KEY_NEW_DOB = "new_DOB";
    public const string KEY_OLD_GENDER = "old_gender";
    public const string KEY_NEW_GENDER = "new_gender";
    public const string KEY_OLD_EMPLOYER = "old_employer";
    public const string KEY_NEW_EMPLOYER = "new_employer";

    // Credit Reverse Parameters

    public const string KEY_AMOUNT_KD_MAKER = "amount_KD_maker";
    public const string KEY_RATE_MAKER = "rate_maker";
    public const string KEY_AMOUNT_KD_CHECKER = "amount_KD_checker";
    public const string KEY_RATE_CHECKER = "rate_checker";
    public const string KEY_CARD_CURRENCY = "card_currency";

    //VAT
    public const string KEY_DEBIT_ACCT_NO = "debit_acct_no";
    public const string KEY_ORGINAL_FEE_AMOUNT = "Original_Fee_Amount";
    public const string KEY_OVERWRITE_FEE_AMOUNT = "Overwrite_Fee_Amount";
    public const string KEY_OVERWRITE_REASON = "Overwrite_Reason";
    public const string KEY_SERVICE_NAME = "Service_Name";
    public const string KEY_CURRENCY_RATE = "currency_rate";

    public const string KEY_TARGET_ISSUING_OPTION = "TARGET_ISSUING_OPTION";
    public const string KEY_PENDING_COLLATERAL_MIGRATION = "PENDING_COLLATERAL_MIGRATION ";
    public const string KEY_MIGRATED_BY_ID = "MIGRATED_BY_ID";
    public const string KEY_MIGRATED_BY_NAME = "MIGRATED_BY_NAME";
    public const string KEY_MIGRATOR_BRANCH_ID = "MIGRATOR_BRANCH_ID";
    public const string KEY_MIGRATOR_BRANCH_NAME = "MIGRATOR_BRANCH_NAME";
    public const string MARGIN_REFERENCE_NO = "MARGIN_REFERENCE_NO";
    public const string KEY_CURRENT_ISSUING_OPTION = "CURRENT_ISSUING_OPTION";

    public const string CUSTOMER_NAME_EN = nameof(CUSTOMER_NAME_EN);
    public const string CUSTOMER_NAME_AR = nameof(CUSTOMER_NAME_AR);
    public const string NAME_EN = nameof(NAME_EN);
    public const string NAME_AR = nameof(NAME_AR);
    public const string EMBOSSING_NAME = nameof(EMBOSSING_NAME);
    public const string RIM_CODE = nameof(RIM_CODE);
    public const string CLASS_NAME = nameof(CLASS_NAME);
    public const string GLOBAL_LIMIT = nameof(GLOBAL_LIMIT);
    public const string RELATIONSHIP_NO = nameof(RELATIONSHIP_NO);
    public const string KFH_ACCOUNT_NO = nameof(KFH_ACCOUNT_NO);
    public const string CUSTOMER_NO = nameof(CUSTOMER_NO);
    public const string BILLING_ACCOUNT_NO = nameof(BILLING_ACCOUNT_NO);
    public const string ADDRESSLINE1 = nameof(ADDRESSLINE1);
    public const string ADDRESSLINE2 = nameof(ADDRESSLINE2);
    public const string OLD_EMBOSSING_NAME = nameof(OLD_EMBOSSING_NAME);
    public const string OLD_GLOBAL_LIMIT = nameof(OLD_GLOBAL_LIMIT);
}
