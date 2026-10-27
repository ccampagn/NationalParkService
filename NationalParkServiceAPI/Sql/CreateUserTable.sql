-- "User" is a reserved word in SQL Server, so the table name is bracketed. Safe to re-run.
IF OBJECT_ID('dbo.[User]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[User]
    (
        UserId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_User PRIMARY KEY,
        FirstName    NVARCHAR(50)      NOT NULL,
        LastName     NVARCHAR(50)      NOT NULL,
        Email        NVARCHAR(256)     NOT NULL CONSTRAINT UQ_User_Email UNIQUE,
        PasswordHash NVARCHAR(200)     NOT NULL,
        AddressLine1 NVARCHAR(100)     NOT NULL,
        AddressLine2 NVARCHAR(100)     NULL,
        City         NVARCHAR(60)      NOT NULL,
        State        NCHAR(2)          NOT NULL,
        PostalCode   NVARCHAR(10)      NOT NULL,
        CreatedAt    DATETIME2         NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
