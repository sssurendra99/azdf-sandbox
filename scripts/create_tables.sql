-- Create Employees table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Employees' AND xtype='U')
BEGIN
    CREATE TABLE Employees (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        EmployeeId NVARCHAR(100) NOT NULL,
        ClientId NVARCHAR(100) NOT NULL,
        EmployeeName NVARCHAR(200) NOT NULL,
        EmployeeAge INT NOT NULL,
        Email NVARCHAR(255) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,

        CONSTRAINT UQ_Employees_EmployeeId UNIQUE (EmployeeId),
        INDEX IX_Employees_ClientId (ClientId)
    );
END
GO

-- Create Certificates table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Certificates' AND xtype='U')
BEGIN
    CREATE TABLE Certificates (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CertificateId NVARCHAR(100) NOT NULL,
        CertificateName NVARCHAR(200) NOT NULL,
        EmployeeId NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,

        INDEX IX_Certificates_EmployeeId (EmployeeId),
        INDEX IX_Certificates_CertificateId (CertificateId)
    );
END
GO

