-- Step 1: Add OfficeId as nullable
ALTER TABLE ExchangeRates ADD OfficeId uniqueidentifier NULL;

-- Step 2: Get the first office ID and update all existing exchange rates
DECLARE @DefaultOfficeId uniqueidentifier;
SELECT TOP 1 @DefaultOfficeId = Id FROM Offices WHERE IsActive = 1;

-- If no active office found, get any office
IF @DefaultOfficeId IS NULL
    SELECT TOP 1 @DefaultOfficeId = Id FROM Offices;

-- Update all existing exchange rates with the default office
UPDATE ExchangeRates SET OfficeId = @DefaultOfficeId WHERE OfficeId IS NULL;

-- Step 3: Make OfficeId non-nullable
ALTER TABLE ExchangeRates ALTER COLUMN OfficeId uniqueidentifier NOT NULL;

-- Step 4: Add foreign key constraint
ALTER TABLE ExchangeRates ADD CONSTRAINT FK_ExchangeRates_Offices_OfficeId 
    FOREIGN KEY (OfficeId) REFERENCES Offices(Id) ON DELETE NO ACTION;

-- Step 5: Drop old index
DROP INDEX IX_ExchangeRates_SourceCurrencyId_TargetCurrencyId_EffectiveFrom ON ExchangeRates;

-- Step 6: Create new index with OfficeId
CREATE INDEX IX_ExchangeRates_OfficeId_SourceCurrencyId_TargetCurrencyId_EffectiveFrom 
    ON ExchangeRates (OfficeId, SourceCurrencyId, TargetCurrencyId, EffectiveFrom);

-- Step 7: Create additional index on SourceCurrencyId
CREATE INDEX IX_ExchangeRates_SourceCurrencyId ON ExchangeRates (SourceCurrencyId);