-- Links reservations to the user who made them. Run CreateUserTable.sql first. Safe to re-run.
-- Reservations made before accounts existed keep a NULL UserId and aren't shown to anyone.
IF COL_LENGTH('dbo.Reservation', 'UserId') IS NULL
BEGIN
    ALTER TABLE dbo.Reservation
        ADD UserId INT NULL CONSTRAINT FK_Reservation_User REFERENCES dbo.[User] (UserId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservation_UserId' AND object_id = OBJECT_ID('dbo.Reservation'))
BEGIN
    CREATE INDEX IX_Reservation_UserId ON dbo.Reservation (UserId);
END
