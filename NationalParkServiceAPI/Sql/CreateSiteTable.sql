-- Run CreateCampgroundTable.sql first. Safe to re-run.
IF OBJECT_ID('dbo.Site', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Site
    (
        SiteId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Site PRIMARY KEY,
        CampgroundId INT               NOT NULL CONSTRAINT FK_Site_Campground REFERENCES dbo.Campground (CampgroundId),
        SiteNumber   NVARCHAR(10)      NOT NULL,
        SiteType     NVARCHAR(20)      NOT NULL,
        MaxGuests    INT               NOT NULL,
        NightlyRate  INT               NOT NULL,
        CONSTRAINT UQ_Site_Campground_Number UNIQUE (CampgroundId, SiteNumber),
        CONSTRAINT CK_Site_MaxGuests CHECK (MaxGuests > 0)
    );
END
GO

-- Ten sites in every campground: six tent sites at the campground's rate, three RV sites ($10 more, up to 8 guests)
-- and one group site (double the rate, up to 12 guests). Adjust per campground as needed. Safe to re-run.
INSERT INTO dbo.Site (CampgroundId, SiteNumber, SiteType, MaxGuests, NightlyRate)
SELECT c.CampgroundId,
       RIGHT('0' + CAST(v.Number AS NVARCHAR(2)), 2),
       v.SiteType,
       COALESCE(v.MaxGuests, c.MaxGuests),
       c.NightlyRate * v.RateMultiplier + v.RateExtra
FROM dbo.Campground AS c
CROSS JOIN (VALUES
    (1,  N'Tent',  NULL, 1, 0),
    (2,  N'Tent',  NULL, 1, 0),
    (3,  N'Tent',  NULL, 1, 0),
    (4,  N'Tent',  NULL, 1, 0),
    (5,  N'Tent',  NULL, 1, 0),
    (6,  N'Tent',  NULL, 1, 0),
    (7,  N'RV',    8,    1, 10),
    (8,  N'RV',    8,    1, 10),
    (9,  N'RV',    8,    1, 10),
    (10, N'Group', 12,   2, 0)
) AS v (Number, SiteType, MaxGuests, RateMultiplier, RateExtra)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Site s
    WHERE s.CampgroundId = c.CampgroundId AND s.SiteNumber = RIGHT('0' + CAST(v.Number AS NVARCHAR(2)), 2)
);
