-- Columns for real Recreation.gov sites, filled by ImportRecreationGovSites.sql. Run AddReservationSiteId.sql first.
-- Safe to re-run.
-- Filtered indexes on dbo.Site need this; sqlcmd turns it off by default.
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Campground', 'RecreationGovId') IS NULL
BEGIN
    ALTER TABLE dbo.Campground ADD RecreationGovId INT NULL;
END
GO

IF COL_LENGTH('dbo.Site', 'Loop') IS NULL
BEGIN
    ALTER TABLE dbo.Site ADD Loop NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.Site', 'RecreationGovId') IS NULL
BEGIN
    ALTER TABLE dbo.Site ADD RecreationGovId INT NULL;
END
GO

-- Placeholder sites that reservations still point at are retired instead of deleted.
IF COL_LENGTH('dbo.Site', 'Retired') IS NULL
BEGIN
    ALTER TABLE dbo.Site ADD Retired BIT NOT NULL CONSTRAINT DF_Site_Retired DEFAULT 0;
END
GO

-- Real site names (e.g. "GROUP SITE 01") can be longer than the placeholders, and a retired placeholder can share a number
-- with the real site that replaced it, so the unique constraint becomes a filtered index.
IF OBJECT_ID('dbo.UQ_Site_Campground_Number', 'UQ') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Site DROP CONSTRAINT UQ_Site_Campground_Number;
END
GO

ALTER TABLE dbo.Site ALTER COLUMN SiteNumber NVARCHAR(30) NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Site_Campground_Number' AND object_id = OBJECT_ID('dbo.Site'))
BEGIN
    CREATE UNIQUE INDEX UX_Site_Campground_Number ON dbo.Site (CampgroundId, SiteNumber) WHERE Retired = 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Site_RecreationGovId' AND object_id = OBJECT_ID('dbo.Site'))
BEGIN
    CREATE UNIQUE INDEX UX_Site_RecreationGovId ON dbo.Site (RecreationGovId) WHERE RecreationGovId IS NOT NULL;
END
GO
