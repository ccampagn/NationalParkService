-- Run CreateParkTable.sql and CreateCampgroundTable.sql first, then AddReservationUserId.sql and CreateSiteTable.sql + AddReservationSiteId.sql. Safe to re-run.
IF OBJECT_ID('dbo.Reservation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservation
    (
        ReservationId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reservation PRIMARY KEY,
        ConfirmationCode NVARCHAR(12)      NOT NULL CONSTRAINT UQ_Reservation_ConfirmationCode UNIQUE,
        CampgroundId     INT               NOT NULL CONSTRAINT FK_Reservation_Campground REFERENCES dbo.Campground (CampgroundId),
        CheckIn          DATE              NOT NULL,
        CheckOut         DATE              NOT NULL,
        Guests           INT               NOT NULL,
        NightlyRate      INT               NOT NULL,
        ReservationFee   INT               NOT NULL,
        Total            INT               NOT NULL,
        CreatedAt        DATETIME2         NOT NULL CONSTRAINT DF_Reservation_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Reservation_Dates CHECK (CheckOut > CheckIn),
        CONSTRAINT CK_Reservation_Guests CHECK (Guests > 0)
    );
END
