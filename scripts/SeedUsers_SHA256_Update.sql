-- =============================================================================
-- VERITABANI ADI (Database): mtt-moneyexchangeturkey
-- (Eski ad mtturkey_exchange KULLANILMAZ - sadece su anki ad gecerli.)
-- Tablolar sema ile: mtturkey_exchange.Users (mtturkey_exchange = SEMA, veritabani degil)
-- =============================================================================
-- API SHA256 veya BCrypt kabul eder. Bu betik ihtiyar/Damat/PERSONEL/Perso sifrelerini SHA256 hash ile gunceller.
-- Sifreler: owner1, admin1, personel1
-- Giris: Mail veya Username ile (API her ikisini de kabul eder). Isim degisiklikleri icin ISIM_DEGISIKLIK_REFERANS.md'ye bakin.
-- =============================================================================
USE [mtt-moneyexchangeturkey];
GO
PRINT N'Veritabani: mtt-moneyexchangeturkey';
GO

-- SHA256 hash'leri (PowerShell ile hesaplandı)
DECLARE @ownerHash   NVARCHAR(500) = N'1557f949eb074ee1de813d3598b394ea654e9066ba7dd5ffe290848c31a8c9c8';   -- owner1
DECLARE @adminHash   NVARCHAR(500) = N'25f43b1486ad95a1398e3eeb3d83bc4010015fcc9bedb35b432e00298d5021f7';   -- admin1
DECLARE @personelHash NVARCHAR(500) = N'9682100bd8f989a81e8c39560ce6b66ef935788982ba88ebe35bee7ce5fb2525'; -- personel1

-- Owner: ihtiyar
UPDATE mtturkey_exchange.Users SET Password = @ownerHash WHERE Username = N'ihtiyar';
IF @@ROWCOUNT = 0
  INSERT INTO mtturkey_exchange.Users (Id, Username, Mail, Password, Firstname, Lastname, Rank, IsEmailVerified, Gender, CreatedDate)
  VALUES (NEWID(), N'ihtiyar', N'owner@baskentenerji.com', @ownerHash, N'Cem', N'Kul', 100, 0, 0, GETUTCDATE());

-- Admin: Damat
UPDATE mtturkey_exchange.Users SET Password = @adminHash WHERE Username = N'Damat';
IF @@ROWCOUNT = 0
  INSERT INTO mtturkey_exchange.Users (Id, Username, Mail, Password, Firstname, Lastname, Rank, IsEmailVerified, Gender, CreatedDate)
  VALUES (NEWID(), N'Damat', N'admin@baskentenerji.com', @adminHash, N'Damat', N'Admin', 99, 0, 0, GETUTCDATE());

-- Personel (Username: PERSONEL) - Rank 50 = Staff (şube/personel paneli erişimi için gerekli)
UPDATE mtturkey_exchange.Users SET Password = @personelHash, Rank = 50 WHERE Username = N'PERSONEL';
IF @@ROWCOUNT = 0
  INSERT INTO mtturkey_exchange.Users (Id, Username, Mail, Password, Firstname, Lastname, Rank, IsEmailVerified, Gender, CreatedDate)
  VALUES (NEWID(), N'PERSONEL', N'personel@baskentenerji.com', @personelHash, N'Personel', N'User', 50, 0, 0, GETUTCDATE());

-- Personel alternatif isim (Perso) - Rank 50 = Staff
UPDATE mtturkey_exchange.Users SET Password = @personelHash, Rank = 50 WHERE Username = N'Perso';

PRINT N'SHA256 sifre guncellemesi tamamlandi. Giris: ihtiyar/owner1, Damat/admin1, PERSONEL veya Perso/personel1';
