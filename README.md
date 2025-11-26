<div id="top"></div>

![Build][build-badge]

<!-- PROJECT LOGO -->

<br />
<div align="center">
  <a href="https://github.kfh.com/kfh/credit-card">
    <img src="images/logo.svg" alt="Logo" width="160" height="40">
  </a>

<h3 align="center">Aurora Credit Card System</h3>

<p align="center">
    <a href="https://creditcards.kfh.dev" target="_blank">View Demo</a>
    ·
    <a href="https://github.kfh.com/kfh/credit-card/issues">Report Bug</a>

  </p>
</div>


<!-- GETTING STARTED -->

## Getting Started

### Prerequisites

- [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/)
- [SQL Server 2019](https://docs.microsoft.com/en-us/sql)

<!-- Recommended tools -->
### Recommended tools

* [Visual Studio 2022](https://www.visualstudio.com/vs/) (Windows and macOS)
* [Visual Studio Code](https://code.visualstudio.com/) (Others)
* [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (Windows)
* [Azure Data Studio](https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio) (Others)

After you have completed the above steps, you should be ready to launch your development environment.

* Ensure your connection strings in `appsettings.json` points to a local SQL Server instance.

* Open the Package Manager Console (`Tools -> NuGet Package Manager -> Package Manager Console`) and execute the following command:
   
   ```powershell
   Update-Database -Context ApplicationDbContext
   ```

* Set `CreditCardsSystem.Web.Server` as startup project
<br/> ![startup project][project-structure]
<br/>
* Run the application (F5)
<br/> ![startup project][product-screenshot1]
<br/> ![startup project][product-screenshot2]
<br/>
<p align="right">(<a href="#top">back to top</a>)</p>


# Deployment Guide
<br>

## User name details by environment wise

| Resource            | Username            | Password (Development & Testing)      | Password (Production)     |
| :---                |    :----            | :---                                  |:---                       |
| SQL Database        | SRV_ARCCS           |                                       |                           |
| Oracle Database     | SRV_ARCCS           |                                       |                           |
| Integration Services| ARCCS               |                                       |                           |

<br>

## Aurora Servers

https://wiki.kfh.com/display/FRT/Aurora+Servers


<br>

## Aurora Access Control Permissions

| Permission Name            | Description | Status   |
| :---                |    :----    | :---                |
|          |        |     |
 
 


<br>

## Integration services 
user `ARCCS` needs to get access on below integration services


| Sno  | Service                               | Url                                                                    |
| :--- | :----                                 | :----                                                                  |
|1     |BankingCustomerProfileService          | https://soa_apigw/kfhsoa/ws/BankingCustomerProfileService/1.0          |
|2     |CreditCardInquiryService               | https://soa_apigw/kfhsoa/ws/CreditCardInquiryService/1.0               |
|3     |CustomerAccountsService                | https://soa_apigw/kfhsoa/ws/CustomerAccountsService/1.0                |
|4     |HrService                              | https://soa_apigw/kfhsoa/ws/HrService/1.0                              |
|5     |CustomerFinancialPositionService       | https://soa_apigw/kfhsoa/ws/CustomerFinancialPositionService/1.0       |
|6     |HoldManagementService                  | https://soa_apigw/kfhsoa/ws/HoldManagementService/1.0                  |
|7     |CustomerCustomizationService           | https://soa_apigw/kfhsoa/ws/CustomerCustomizationService/1.0           |
|8     |CreditCardUpdateServicesService        | https://soa_apigw/kfhsoa/ws/CreditCardUpdateServicesService/1.0        |
|9     |StandingOrderService                   | https://soa_apigw/kfhsoa/ws/StandingOrderService/2.0                   |
|10    |CreditCardPromotionServicesService     | https://soa_apigw/kfhsoa/ws/CreditCardPromotionServicesService/1.0     |
|11    |ViewSOByCivilIDAndChannelID            | https://soa_apigw/kfhsoa/ws/ViewSOByCivilIDAndChannelID/1.0            |
|12    |EditCreditCardStandingOrder            | https://soa_apigw/kfhsoa/ws/EditCreditCardStandingOrder/1.0            |
|13    |CloseCreditCardStandingOrder           | https://soa_apigw/kfhsoa/ws/CloseCreditCardStandingOrder/1.0           |
|14    |ServiceFeesManagement                  | https://soa_apigw/kfhsoa/ws/ServiceFeesManagement/1.0                  |
|15    |AddCreditCardStandingOrder             | https://soa_apigw/kfhsoa/ws/AddCreditCardStandingOrder/1.0             |
|16    |MonetaryTransferService                | https://soa_apigw/kfhsoa/ws/MonetaryTransferService/1.0                |
|17    |CorporateCreditCardService             | https://soa_apigw/kfhsoa/ws/CorporateCreditCardService/1.0             |
|18    |CreditCardUpdateProfileServicesService | https://soa_apigw/kfhsoa/ws/CreditCardUpdateProfileServicesService/1.0 |
|19    |CustmerStatementService                | https://soa_apigw/kfhsoa/ws/CustmerStatementService/1.0                |
|20    |CurrencyInformationService             | https://soa_apigw/kfhsoa/ws/CurrencyInformationService/1.0             |
|21    |AccountHoldMgmt                        | https://soa_apigw/kfhsoa/ws/AccountHoldMgmt/1.0                        |
|22    |StaticDataInquiries                    | https://integration/ws/WLS_StaticDataInquiriesService/1.0?wsdl         |
|23    |DelegateRequest                        | https://soa_apigw/kfhsoa/ws/CreditCardDelegationServicesService/1.0    |
|24    |DocumentManagement                     | https://soa_apigq/kfhsoa/ws/WLS_DocumentManagementSerivce/1.0          |
|25    |AUB                                    | https://soa_apigw/kfhsoa/rs/AUBKFHAcctMappingService/1.0               |
|26    |InformaticaManagement                  | https://soa_apigw/kfhsoa/ws/InformaticaManagementService/1.0           |
                                                                                                                        
 

<br>

## Databases ( <a href="#FDR">FDR</a> ,  <a href="#SQL">SQL Db</a>)

<br>

## Database Users Matrix
 https://wiki.kfh.com/display/FRT/Database+Users+Matrix

<br>
<div id="FDR"></div>

### **FDR**

>development environment
>- service name is "FDR"
>- user name is "SRV_ARCCS"
<br>

db user `SRV_ARCCS` needs to get access on below tables & sequences
<br>

### **`Tables`**

| Sno  | Table                              | Permissions                      | 
| :--- | :----                              | :----                            |
| 1    | FDR.AREA_CODES                     | SELECT                           |
| 2    | FDR.CARD_CURRENCY                  | SELECT                           |
| 3    | FDR.CARD_DEF                       | SELECT, INSERT, UPDATE, DELETE   |
| 4    | FDR.CARD_DEF_EXT                   | SELECT, INSERT, UPDATE           |
| 5    | FDR.REQUEST	                    | SELECT, INSERT, Update, DELETE   |
| 6    | FDR.MEMBERSHIP_INFO                | SELECT, INSERT, UPDATE, DELETE   |
| 7    | FDR.REQUEST_PARAMETERS             | SELECT, INSERT, UPDATE, DELETE   |
| 8    | FDR.REQUEST_STATUS                 | SELECT                           |
| 9    | FDR.REQUEST_DELIVERY               | SELECT                           |
| 10   | FDR.REQUEST_ACTIVITY               | SELECT, INSERT, UPDATE, DELETE   |
| 11   | FDR.PROFILE                        | SELECT, INSERT, UPDATE           |
| 12   | FDR.PREREGISTERED_PAYEE            | SELECT                           |
| 13   | FDR.PREREGISTERED_PAYEE_STATUS     | SELECT                           |
| 14   | FDR.PREREGISTERED_PAYEE_TYPE       | SELECT                           |
| 15   | PROMO.PROMOTION                    | SELECT, INSERT, UPDATE, DELETE   |
| 16   | PROMO.CARDTYPE_ELIGIBILITY_MATIX   | SELECT, INSERT, UPDATE, DELETE   |
| 17   | PROMO.PROMOTION_BENEFICIARIES      | SELECT, INSERT, UPDATE, DELETE   |
| 18   | PROMO.PROMOTION_CARD               | SELECT, INSERT, UPDATE, DELETE   |
| 19   | PROMO.CONFIG_PARAMETERS            | SELECT, UPDATE                   |
| 20   | FDR.CORPORATE_PROFILE              | SELECT, INSERT, UPDATE           |
| 21   | VPBCD.STATEMENT_DETAILS            | SELECT                           |
| 22   | FDR.MEMBERSHIP_DELETE_REQUEST      | SELECT, INSERT, UPDATE, DELETE   |
| 23   | FDR.COMPANY                        | SELECT                           |
| 24   | FDR.RELATIONSHIP                   | SELECT                           |
| 25   | FDR.REQUEST_ACTIVITY_DETAILS       | SELECT, INSERT, UPDATE, DELETE   |
| 26   | FDR.EXTERNAL_STATUS                | SELECT, INSERT, UPDATE, DELETE   |
| 27   | FDR.INTERNAL_STATUS                | SELECT, INSERT, UPDATE, DELETE   |
| 28   | PROMO.COLLATERAL		            | SELECT			               |
| 29   | PROMO.GROUP_ATTRIBUTES	            | SELECT, INSERT, UPDATE, DELETE   |
| 30   | PROMO.LOYALTYPOINTSSETUP	        | SELECT, INSERT, UPDATE           |
| 31   | PROMO.PCT			                | SELECT, INSERT, UPDATE, DELETE   |
| 32   | PROMO.PROMOTION_GROUP		        | SELECT, INSERT, UPDATE, DELETE   |
| 33   | PROMO.SERVICES			            | SELECT, INSERT, UPDATE, DELETE   |
| 34   | FDR.CHANGE_LIMIT_HISTORY		    | SELECT, INSERT, UPDATE, DELETE   |
| 35   | FDR.TAYSEER_CREDIT_CHECKING		| SELECT, INSERT, UPDATE, DELETE   |
| 36   | FDR.CREDIT_REVERSE         		| SELECT, INSERT, UPDATE, DELETE   |
| 37   | VPBCD.REQUEST_BOARDING_LOG         | SELECT, INSERT, UPDATE, DELETE   |
| 38   | FDR.CBK_CARDS                      | SELECT                           |
| 39   | PROMO.REQUEST_ACTIVITY             | SELECT, INSERT, UPDATE, DELETE   |
| 40   | PROMO.REQUEST_ACTIVITY_DETAILS     | SELECT, INSERT, UPDATE, DELETE   |
| 41   | FDR.CFU_ACTIVITY                   | SELECT                           |
| 42   | VPBCD.STATEMENT_MASTER             | SELECT                           |
| 43   | VPBCD.STATEMENT_DETAILS            | SELECT                           |
| 43   | FDR.FCMF                           | SELECT                           |
| 44   | VPBCD.DIRECTDEBIT_GENERATIONOPTIONS| SELECT , INSERT                  |
| 45   | VPBCD.AUB_CARD_MAPPING             | SELECT                           |
| 46   | FDR.MIGS_GENERATE_FILE	            | SELECT, INSERT, UPDATE, DELETE   |
| 47   | FDR.MIGS_LOAD_STATUS               | SELECT                           |
| 48   | FDR.MIGS_MASTER                    | SELECT                           |

### **`Stored Procedures`**
| Sno  | Stored Procedure        |Permissions |
| :--- | :---                    | :---       |
| 1    | SEARCH_REQUEST_ACTIVITY | Execute    |


### **`Functions` **
| Sno  | Function        |Permissions |
| :--- | :---                    | :---       |
| 1    | SPLIT_STRING            | Execute    |


### **`Sequences`**

| Sno | Sequence                                |Permissions   |
| :---| :---                                    | :---         |
| 1   | SEQ                                     | SELECT       |
| 2	  | PROMO.CARDTYPE_ELIGIBILITY_MATIX_SEQ	| SELECT	   |
| 3	  | PROMO.PCT_SEQ	       		            | SELECT	   |
| 4	  | PROMO.PROMOTION_CARD_SEQ	       	    | SELECT	   |
| 5	  | PROMO.PROMOTION_CLASS_SEQ	       	    | SELECT	   |
| 6	  | PROMO.PROMOTION_GROUP_SEQ	       	    | SELECT	   |
| 7	  | PROMO.PROMOTION_PRODUCT_SEQ	            | SELECT	   |
| 8	  | PROMO.REQUEST_ACTIVITY_DETAILS_SEQ	    | SELECT	   |
| 9	  | PROMO.REQUEST_ACTIVITY_SEQ	       	    | SELECT	   |
| 10  | PROMO.SERVICES_SEQ	       		        | SELECT	   |
| 12  | FDR.CHANGE_LIMIT_HISTORY_SEQ	        | SELECT	   |
| 13  | FDR.SEQ_TAYSEER_CREDIT_CHECKING	        | SELECT	   |
| 14  | VPBCD.STATEMENT_PRINT_SEQ               | SELECT       |

<br>

db user `SRV_ARCCS` needs to get access on below tables in phoenix DB

| Sno	| Table			| Permissions
| :---  | :---         		| :---        		       
| 1	| ad_gb_appl_type	| SELECT




## Sql scripts for new table and sequence
<br>


### `Tables`
``` sql
	CREATE TABLE EXTERNAL_STATUS (
        CODE VARCHAR(10) NOT NULL,
        DESCRIPTION_EN VARCHAR(200) NOT NULL,
        DESCRIPTION_AR VARCHAR(200) NOT NULL,
        LOCAL_STATUS_ID NUMBER(22) NULL
        )

        INSERT ALL
INTO EXTERNAL_STATUS VALUES('Empty','Card is active','فعالة',5)
INTO EXTERNAL_STATUS VALUES('A','Authorization prohibited - in equation system','إيقاف مؤقت',12)
INTO EXTERNAL_STATUS VALUES('C','Cancel/Close','ملغي/مغلق',2)
INTO EXTERNAL_STATUS VALUES('L','Lost/Stolen','ضائع/مسروق',6)
INTO EXTERNAL_STATUS VALUES('X','Temporary closed','إيقاف مؤقت',12)
INTO EXTERNAL_STATUS VALUES('E','Delinquent 150 Days','تأخير في السداد 150 يوم',13)
INTO EXTERNAL_STATUS VALUES('F','Delinquent 120 Days','تأخير في السداد 120 يوم',14)
INTO EXTERNAL_STATUS VALUES('U','Lost/Stolen - cards B4 VP','ضائع/مسروق',6)
INTO EXTERNAL_STATUS VALUES('Z','Charge-Off > 240 Days of delinquency','قانونية',10)
SELECT * FROM dual;


	CREATE TABLE INTERNAL_STATUS
(
CODE VARCHAR(10) NOT NULL,
DESCRIPTION_EN VARCHAR(200) NOT NULL,
DESCRIPTION_AR VARCHAR(200) NOT NULL,
LOCAL_STATUS_ID NUMBER(22)  NULL
)

INSERT ALL
INTO INTERNAL_STATUS VALUES('P','60 Days in Delinquent','فعالة','')
INTO INTERNAL_STATUS VALUES('D','In arrears 90 Days','تأخير في السداد','')
INTO INTERNAL_STATUS VALUES('E','Delinquent 150 Days','تأخير في السداد',13)
INTO INTERNAL_STATUS VALUES('F','Delinquent 120 Days','تأخير في السداد',14)
INTO INTERNAL_STATUS VALUES('G','Delinquent 180 Days','تأخير في السداد',15)
INTO INTERNAL_STATUS VALUES('H','Delinquent 210 Days','تأخير في السداد',16)
INTO INTERNAL_STATUS VALUES('I','Delinquent 240 Days','تأخير في السداد',17)
INTO INTERNAL_STATUS VALUES('Z','Charge-Off > 240 Days of delinquency','قانونية',10)
INTO INTERNAL_STATUS VALUES('O','OverLimit','تجاوز الحد الائتماني','')
SELECT * FROM dual;

```


### `Sequence`
``` sql
CREATE SEQUENCE  "FDR"."REQUEST_PARAMTERS_LOOKUP_SEQ"  MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 14581 CACHE 20 ORDER  NOCYCLE ;
```

### `Tables`
``` sql

  CREATE TABLE "FDR"."REQUEST_PARAMETERS_LOOKUP" (
    "ID" NUMBER(28, 0) NOT NULL ENABLE,
    "PARAMETER_NAME" VARCHAR2(40 BYTE) NOT NULL ENABLE,
    "PARAMETER_VALUE" VARCHAR2(250 BYTE),
    CONSTRAINT "REQUEST_PARAMETERS_LOOKUP_PK" PRIMARY KEY ( "ID" )
        USING INDEX PCTFREE 10 INITRANS 2 MAXTRANS 255 COMPUTE STATISTICS
            STORAGE ( INITIAL 65536 NEXT 1048576 MINEXTENTS 1 MAXEXTENTS 2147483645 PCTINCREASE 0 FREELISTS 1 FREELIST GROUPS 1 BUFFER_POOL
            DEFAULT FLASH_CACHE DEFAULT CELL_FLASH_CACHE DEFAULT )
        TABLESPACE "FDR"
    ENABLE
)
SEGMENT CREATION IMMEDIATE
PCTFREE 10 PCTUSED 40 INITRANS 1 MAXTRANS 255 NOCOMPRESS LOGGING
    STORAGE ( INITIAL 65536 NEXT 1048576 MINEXTENTS 1 MAXEXTENTS 2147483645 PCTINCREASE 0 FREELISTS 1 FREELIST GROUPS 1 BUFFER_POOL DEFAULT
    FLASH_CACHE DEFAULT CELL_FLASH_CACHE DEFAULT )
TABLESPACE "FDR";

CREATE OR REPLACE TRIGGER "FDR"."REQUEST_PARAMETERS_LOOKUP_TRG" BEFORE
    INSERT ON request_parameters_lookup
    FOR EACH ROW
BEGIN
    SELECT
        fdr.request_paramters_lookup_seq.nextval
    INTO :new.id
    FROM
        dual;

END;
/

ALTER TRIGGER "FDR"."REQUEST_PARAMETERS_LOOKUP_TRG" ENABLE;

INSERT INTO REQUEST_PARAMETERS_LOOKUP (ID,PARAMETER_NAME,PARAMETER_VALUE)
SELECT REQUEST_PARAMTERS_LOOKUP_SEQ.nextval ,SS.PARAMETER,'' FROM 
(SELECT DISTINCT PARAMETER FROM REQUEST_PARAMETERS) SS

```
<p align="right">(<a href="#top">back to top</a>)</p>

<div id="SQL"></div>


### `Permission Script`
``` sql
GRANT SELECT ON FDR.AREA_CODES  TO  SRV_ARCCS;
GRANT SELECT ON FDR.CARD_CURRENCY  TO  SRV_ARCCS;
GRANT SELECT ON FDR.CARD_DEF  TO  SRV_ARCCS;
GRANT SELECT ON FDR.CARD_DEF_EXT  TO  SRV_ARCCS;
GRANT SELECT, INSERT, Update, DELETE ON FDR.REQUEST  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON FDR.MEMBERSHIP_INFO  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON FDR.REQUEST_PARAMETERS  TO  SRV_ARCCS;
GRANT SELECT ON FDR.REQUEST_STATUS  TO  SRV_ARCCS;
GRANT SELECT ON FDR.REQUEST_DELIVERY  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON FDR.REQUEST_ACTIVITY  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE ON FDR.PROFILE  TO  SRV_ARCCS;
GRANT SELECT ON FDR.PREREGISTERED_PAYEE  TO  SRV_ARCCS;
GRANT SELECT ON FDR.PREREGISTERED_PAYEE_STATUS  TO  SRV_ARCCS;
GRANT SELECT ON FDR.PREREGISTERED_PAYEE_TYPE  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON PROMO.PROMOTION  TO  SRV_ARCCS;
GRANT SELECT ON PROMO.CARDTYPE_ELIGIBILITY_MATIX  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON PROMO.PROMOTION_BENEFICIARIES  TO  SRV_ARCCS;
GRANT SELECT ON PROMO.PROMOTION_CARD  TO  SRV_ARCCS;
GRANT SELECT,UPDATE ON PROMO.CONFIG_PARAMETERS  TO  SRV_ARCCS;
GRANT SELECT ON FDR.CORPORATE_PROFILE  TO  SRV_ARCCS;
GRANT SELECT ON VPBCD.STATEMENT_DETAILS  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON FDR.MEMBERSHIP_DELETE_REQUEST  TO  SRV_ARCCS;
GRANT SELECT ON FDR.COMPANY  TO  SRV_ARCCS;
GRANT SELECT ON FDR.RELATIONSHIP  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON FDR.REQUEST_ACTIVITY_DETAILS  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON FDR.EXTERNAL_STATUS  TO  SRV_ARCCS;
GRANT SELECT, INSERT, UPDATE, DELETE ON FDR.INTERNAL_STATUS  TO  SRV_ARCCS;
GRANT Execute ON FDR.SEARCH_REQUEST_ACTIVITY  TO  SRV_ARCCS;
GRANT SELECT ON FDR.SEQ  TO  SRV_ARCCS;

GRANT SELECT  ON	FDR.FCMF TO SRV_ARCCS
GRANT SELECT, INSERTON	VPBCD.DIRECTDEBIT_GENERATIONOPTIONS	TO SRV_ARCCS
GRANT SELECT  ON	VPBCD.AUB_CARD_MAPPING	TO SRV_ARCCS




```

### **CreditCardsSystem DB**

This sql database is new and only access by credit card system application

> development environment
>- database name is "CreditCardsSystem-Code" 
>- user name is "ARCCS"

<br>

### ``Tables``
1. __EFMigrationsHistory
2. Cache
3. DataProtectionKeys
4. UserSessions

<br>

### ``SQL Script``
```sql
USE [CreditCardsSystem]

GO
CREATE USER [ARCCS] FOR LOGIN [ARCCS] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [ARCCS]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cache](
	[Id] [nvarchar](449) NOT NULL,
	[Value] [varbinary](max) NOT NULL,
	[ExpiresAtTime] [datetimeoffset](7) NOT NULL,
	[SlidingExpirationInSeconds] [bigint] NULL,
	[AbsoluteExpiration] [datetimeoffset](7) NULL,
 CONSTRAINT [PK_Cache] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DataProtectionKeys](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FriendlyName] [nvarchar](max) NULL,
	[Xml] [nvarchar](max) NULL,
 CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserSessions](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ApplicationName] [nvarchar](200) NULL,
	[SubjectId] [nvarchar](200) NOT NULL,
	[SessionId] [nvarchar](450) NULL,
	[Created] [datetime2](7) NOT NULL,
	[Renewed] [datetime2](7) NOT NULL,
	[Expires] [datetime2](7) NULL,
	[Ticket] [nvarchar](max) NOT NULL,
	[Key] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_UserSessions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

/****** Object:  Index [IX_UserSessions_ApplicationName_Key]    Script Date: 6/26/2022 12:12:11 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_UserSessions_ApplicationName_Key] ON [dbo].[UserSessions]
(
	[ApplicationName] ASC,
	[Key] ASC
)
WHERE ([ApplicationName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

SET ANSI_PADDING ON
GO

/****** Object:  Index [IX_UserSessions_ApplicationName_SessionId]    Script Date: 6/26/2022 12:12:57 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_UserSessions_ApplicationName_SessionId] ON [dbo].[UserSessions]
(
	[ApplicationName] ASC,
	[SessionId] ASC
)
WHERE ([ApplicationName] IS NOT NULL AND [SessionId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

SET ANSI_PADDING ON
GO

/****** Object:  Index [IX_UserSessions_ApplicationName_SubjectId_SessionId]    Script Date: 6/26/2022 12:13:04 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_UserSessions_ApplicationName_SubjectId_SessionId] ON [dbo].[UserSessions]
(
	[ApplicationName] ASC,
	[SubjectId] ASC,
	[SessionId] ASC
)
WHERE ([ApplicationName] IS NOT NULL AND [SessionId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20230206100912_Initial-Migration', N'7.0.1')
GO

```


<p align="right">(<a href="#top">back to top</a>)</p>
<!-- MARKDOWN LINKS & IMAGES -->






## Conventions

### Generating Dynamic Controller
In order to generate a dynamic controller from an `AppService`, do the following:
- Suffix the AppService with `AppService`
- Inherit from `IAppService`
```cs
public class AccountsAppService : IAppService
{
    [HttpGet]
    public async Task<List<string>> GetAccountById(string id)
    {
        return await _accounts.GetAccountById(id);
    }

    [HttpPost]
    public async Task<CustomerDto?> SearchById([FromBody] CustomerSearchCriteria searchCriteria)
    {
        return await _accounts.SearchById(searchCriteria);
    }
}
```
To prevent a public method in your `AppService` from being exposed by the controller, decorate the method with **`[NonAction]`** attribute

```cs
[NonAction]
public async Task<List<string>> GetAccountById(string id)
{
    return await _accounts.GetAccountById(id);
}
```
 If you want to prevent an `AppService` from being exposed as a controller, decorate the `AppService` with **`[NonController]`** attribute

 ```cs
 [NonController]
public class AccountsAppService : IAppService
{
}
 ```
<p align="right">(<a href="#top">back to top</a>)</p>

<!-- Migrations -->
## Migrations

### Adding migration

You can add a migration by using the following command:

```powershell
Add-Migration {Migration} -Context ApplicationdDbContext
```

### Remove migration

```powershell
Remove-Migration -Context ApplicationdDbContext
```

### Update database

```powershell
Update-Database -Context ApplicationdDbContext
```

[Learn more about Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing?tabs=vs)

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- Troubleshooting -->
## Troubleshooting

### Adding Service Reference

To add a new integration service:

1. Add the wsdl file to the SOA folder
2. Install svcutil using below command (if it is not installed in your machine)
```powershell
dotnet tool install --global dotnet-svcutil --version 2.1.0
```

3. Use the below command and adjust the names accordingly
```powershell
dotnet-svcutil .\CreditCardTransactionInquiryService.wsdl -n "*,CreditCardTransactionInquiryServiceReference" -d "..\..\Connected Services\CreditCardTransactionInquiryService" -mc -o CreditCardTransactionInquiryReference.cs
```

4. Rename the json file in the output folder to `ConnectedService.json`

### Connection String

In case you receive below error when connecting to the database

```powershell
SqlException: A connection was successfully established with the server, but then an error occurred during the login process. (provider: SSL Provider, error: 0 - The certificate chain was issued by an authority that is not trusted.)
```

Add **`TrustServerCertificate=True`** to your connection string to mitigate this issue

```powershell
Server=localhost;Database=database_name;User Id=user;Password=123;TrustServerCertificate=True;
```

<p align="right">(<a href="#top">back to top</a>)</p>
<br>
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->

[product-screenshot1]: images/screenshot1.png
[product-screenshot2]: images/screenshot2.png
[project-structure]: images/project-structure.png
[build-badge]: https://github.kfh.com/kfh/credit-card/actions/workflows/dotnet.yml/badge.svg
