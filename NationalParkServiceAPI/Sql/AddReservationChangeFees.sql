-- Change fees charged when a reservation is moved to entirely new dates; part of Total and never refunded. Safe to re-run.
IF COL_LENGTH('dbo.Reservation', 'ChangeFees') IS NULL
BEGIN
    ALTER TABLE dbo.Reservation
        ADD ChangeFees INT NOT NULL CONSTRAINT DF_Reservation_ChangeFees DEFAULT 0;
END
