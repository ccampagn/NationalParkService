-- Links reservations to the individual site booked. Run CreateSiteTable.sql first. Safe to re-run.
-- Reservations made before sites could be booked keep a NULL SiteId and still show their campground.
IF COL_LENGTH('dbo.Reservation', 'SiteId') IS NULL
BEGIN
    ALTER TABLE dbo.Reservation
        ADD SiteId INT NULL CONSTRAINT FK_Reservation_Site REFERENCES dbo.Site (SiteId);
END
GO

-- Availability checks look up a site's reservations by date.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservation_SiteId_Dates' AND object_id = OBJECT_ID('dbo.Reservation'))
BEGIN
    CREATE INDEX IX_Reservation_SiteId_Dates ON dbo.Reservation (SiteId, CheckIn, CheckOut);
END
