INSERT INTO [dbo].[CardEligiblityMatrix] ([id],[RimType],[Currency]
           ,[ProductType]
           ,[ProductID]
           ,[ProductName]
           ,[IsAllowSupplementary]
           ,[IsClientCheck]
           ,[IsCoBrand]
           ,[IsCorporate]
           ,[HasPromotion]
           ,[AllowedBranches]
           ,[Status]
           ,[Priority])
values   (NEWID(),NULL,'414','Charge','41','KFH Visa Corporate Platinum Charge Card',1,0,0,'0','0',0,1,1),
 (NEWID(),NULL,'414','Charge','48','VISA Co-brand',1,0,1,'0','0',0,1,2),
 (NEWID(),NULL,'414','Charge','39','Metal QR Star',1,0,0,'0','0',0,1,3),
 (NEWID(),NULL,'414','Charge','40','Metal QR Artisan silver',1,0,0,'0','0',0,1,4),
 (NEWID(),NULL,'414','Charge','42','KFH Visa Corporate Signature Charge Card',1,0,0,'0','0',0,1,5),
 (NEWID(),NULL,'414','Charge','49','MC KAC UPPER Premium',1,0,0,'0','0',0,1,6),
 (NEWID(),NULL,'949','Prepaid','51','TRY Prepaid',0,0,0,'0','0',0,1,7),
 (NEWID(),NULL,'414','Charge','50','MasterCard KAC World',1,0,0,'0','0',0,1,9),
 (NEWID(),NULL,'840','Prepaid','43','USD Prepaid',0,0,0,'0','0',0,1,10),
 (NEWID(),NULL,'826','Prepaid','44','GBP Prepaid',0,0,0,'0','0',0,1,11),
 (NEWID(),NULL,'978','Prepaid','45','EUR Prepaid',0,0,0,'0','0',0,1,12),
 (NEWID(),NULL,'784','Prepaid','46','AED Prepaid',0,0,0,'0','0',0,1,13),
 (NEWID(),NULL,'682','Prepaid','47','SAR Prepaid',0,0,0,'0','0',0,1,14),
 (NEWID(),NULL,'414','Prepaid','27','MasterCard Al Khair',0,0,0,'0','0',0,1,15),
 (NEWID(),NULL,'414','Prepaid','29','CO-BRAND PRE-PAID CARD - Ooredoo',0,0,1,'0','0',0,1,16),
 (NEWID(),NULL,'414','Prepaid','30','7esabi MasterCard',0,0,0,'0','0',0,1,17),
 (NEWID(),NULL,'414','Prepaid','28','Co-Brand Pre-Paid Card -   KAC',0,0,1,'0','0',0,1,18),
 (NEWID(),NULL,'414','Tayseer','2','TAYSEER T3 GOLD',0,0,0,'0','0',0,1,19),
 (NEWID(),NULL,'414','Charge','3','Visa Classic',1,0,0,'0','0',0,1,20),
 (NEWID(),NULL,'414','Tayseer','4','TAYSEER T3 CLASSIC',0,0,0,'0','0',0,1,21),
 (NEWID(),NULL,'414','Charge','5','Visa Platinum',1,0,0,'0','0',0,1,22),
 (NEWID(),NULL,'414','Charge','6','Visa Internet',1,0,0,'0','0',0,1,23),
 (NEWID(),NULL,'414','Charge','1','Visa Gold',1,0,0,'0','0',0,1,24),
 (NEWID(),NULL,'414','Charge','7','MasterCard Gold',1,0,0,'0','0',0,1,25),
 (NEWID(),NULL,'414','Charge','8','MasterCard Classic',1,0,0,'0','0',0,1,26),
 (NEWID(),NULL,'414','Charge','9','MasterCard Platinum',1,0,0,'0','0',0,1,27),
 (NEWID(),NULL,'414','Tayseer','10','TAYSEER T12 PLATINUM STANDARD',0,0,0,'0','0',0,1,28),
 (NEWID(),NULL,'414','Tayseer','11','TAYSEER T12 PLATINUM PREMIUM',0,0,0,'0','0',0,1,29),
 (NEWID(),NULL,'414','Tayseer','12','TAYSEER T12 CLASSIC',0,0,0,'0','0',0,1,30),
 (NEWID(),NULL,'414','Tayseer','13','TAYSEER T12 GOLD',0,0,0,'0','0',0,1,31),
 (NEWID(),NULL,'414','Tayseer','14','TAYSEER T12 PLATINUM STANDARD (Temp)',0,0,0,'0','0',0,1,32),
 (NEWID(),NULL,'414','Tayseer','15','TAYSEER T12 PLATINUM PREMIUM (Temp)',0,0,0,'0','0',0,1,33),
 (NEWID(),NULL,'414','Prepaid','18','7esabi',0,0,0,'0','0',0,1,34),
 (NEWID(),NULL,'414','Charge','17','MasterCard Gold(3000-30000)',1,0,0,'0','0',0,1,35),
 (NEWID(),NULL,'414','Prepaid','23','Al-Khair',0,0,0,'0','0',0,1,36),
 (NEWID(),NULL,'414','Charge','19','Visa Charge Diamond',1,0,0,'0','0',0,1,37),
 (NEWID(),NULL,'414','Prepaid','24','Al-Ousra Primary',0,0,0,'0','0',0,1,38),
 (NEWID(),NULL,'414','Prepaid','25','Al-Ousra Supplementary',0,0,0,'0','0',0,1,39),
 (NEWID(),NULL,'414','Tayseer','20','VISA TAYSEER DIAMOND',0,0,0,'0','0',0,1,40),
 (NEWID(),NULL,'840','Charge','26','USD Master Card Platinum',1,0,0,'0','0',0,1,41),
 (NEWID(),NULL,'414','Charge','31','VISA CLASSIC - DDA (TEMP)',1,0,0,'0','0',0,1,42),
 (NEWID(),NULL,'414','Charge','32','MASTER CLASSIC - DDA (TEMP)',1,0,0,'0','0',0,1,43),
 (NEWID(),NULL,'414','Charge','35','VISA SIGNATURE',1,0,0,'0','0',0,1,44),
 (NEWID(),NULL,'414','Prepaid','34','Sama World Prepaid Card',0,0,0,'0','0',0,1,45),
 (NEWID(),NULL,'414','Charge','33','World Elite MasterCard Card',1,0,0,'0','0',0,1,46),
 (NEWID(),NULL,'414','Charge','37','Visa Platinum Select',1,0,0,'0','0',0,1,47),
 (NEWID(),NULL,'414','Charge','38','Metal QR Mosaic',1,0,0,'0','0',0,1,48)
