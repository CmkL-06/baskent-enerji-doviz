-- =============================================
-- TG Bayi Cari Hesap ROLLBACK
-- =============================================

-- 1. PartyAccountEntries temizle
DELETE FROM PartyAccountEntries
WHERE PartyAccountId IN (
    SELECT pa.Id FROM PartyAccounts pa
    JOIN TgDealers d ON d.PartyId = pa.PartyId
    WHERE d.PartyId IS NOT NULL
);
GO

-- 2. PartyAccounts temizle
DELETE FROM PartyAccounts
WHERE PartyId IN (SELECT PartyId FROM TgDealers WHERE PartyId IS NOT NULL);
GO

-- 3. Parties temizle
DELETE FROM Parties
WHERE Id IN (SELECT PartyId FROM TgDealers WHERE PartyId IS NOT NULL);
GO

-- 4. TgDealers kolonlari kaldir
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TgDealers') AND name = 'PartyId')
    ALTER TABLE TgDealers DROP COLUMN PartyId;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TgDealers') AND name = 'CommissionRate')
BEGIN
    ALTER TABLE TgDealers DROP CONSTRAINT DF__TgDealers__Commi__GENERATED;
    ALTER TABLE TgDealers DROP COLUMN CommissionRate;
END
GO

PRINT 'Rollback tamamlandi';
GO
