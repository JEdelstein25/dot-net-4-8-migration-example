-- Australian Tax Calculator Database Schema
-- .NET 4.8 Legacy Implementation

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TaxCalculator')
BEGIN
    CREATE DATABASE TaxCalculator;
END
GO

USE TaxCalculator;
GO

-- Tax Brackets for each financial year
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TaxBrackets' AND xtype='U')
BEGIN
    CREATE TABLE TaxBrackets (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FinancialYear VARCHAR(7) NOT NULL, -- '2024-25'
        MinIncome DECIMAL(15,2) NOT NULL,
        MaxIncome DECIMAL(15,2) NULL, -- NULL for highest bracket
        TaxRate DECIMAL(5,4) NOT NULL, -- 0.3250 for 32.5%
        FixedAmount DECIMAL(15,2) NOT NULL,
        BracketOrder INT NOT NULL,
        CreatedDate DATETIME2 DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1
    );
END
GO

-- Tax Offsets (LITO, etc.)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TaxOffsets' AND xtype='U')
BEGIN
    CREATE TABLE TaxOffsets (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FinancialYear VARCHAR(7) NOT NULL,
        OffsetType VARCHAR(50) NOT NULL, -- 'LITO', 'SAPTO'
        MaxIncome DECIMAL(15,2) NULL,
        MaxOffset DECIMAL(15,2) NOT NULL,
        PhaseOutStart DECIMAL(15,2) NULL,
        PhaseOutRate DECIMAL(5,4) NULL,
        IsActive BIT DEFAULT 1,
        CreatedDate DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Special Levies (Medicare, Budget Repair, etc.)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TaxLevies' AND xtype='U')
BEGIN
    CREATE TABLE TaxLevies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FinancialYear VARCHAR(7) NOT NULL,
        LevyType VARCHAR(50) NOT NULL, -- 'Medicare', 'BudgetRepair'
        ThresholdIncome DECIMAL(15,2) NOT NULL,
        LevyRate DECIMAL(5,4) NOT NULL,
        MaxIncome DECIMAL(15,2) NULL, -- For capped levies
        IsActive BIT DEFAULT 1,
        CreatedDate DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Users
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        FirstName VARCHAR(100) NOT NULL,
        LastName VARCHAR(100) NOT NULL,
        Email VARCHAR(255) UNIQUE NOT NULL,
        DateOfBirth DATE NULL,
        TFN VARCHAR(20) NULL, -- Tax File Number (encrypted)
        ResidencyStatus VARCHAR(20) DEFAULT 'Resident',
        CreatedDate DATETIME2 DEFAULT GETDATE(),
        LastModifiedDate DATETIME2 DEFAULT GETDATE(),
        IsActive BIT DEFAULT 1
    );
END
GO

-- Monthly Income Records
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserMonthlyIncome' AND xtype='U')
BEGIN
    CREATE TABLE UserMonthlyIncome (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        FinancialYear VARCHAR(7) NOT NULL, -- '2024-25'
        Month INT NOT NULL, -- 1-12
        GrossIncome DECIMAL(15,2) NOT NULL,
        TaxableIncome DECIMAL(15,2) NOT NULL,
        DeductionsAmount DECIMAL(15,2) DEFAULT 0,
        SuperContributions DECIMAL(15,2) DEFAULT 0,
        IncomeType VARCHAR(50) DEFAULT 'Salary', -- 'Salary', 'Business', 'Investment'
        PayPeriod VARCHAR(20) DEFAULT 'Monthly', -- 'Weekly', 'Fortnightly', 'Monthly'
        RecordedDate DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(UserId),
        CONSTRAINT UK_UserMonthlyIncome UNIQUE (UserId, FinancialYear, Month)
    );
END
GO

-- Annual Tax Summary
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserAnnualTaxSummary' AND xtype='U')
BEGIN
    CREATE TABLE UserAnnualTaxSummary (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        FinancialYear VARCHAR(7) NOT NULL,
        TotalGrossIncome DECIMAL(15,2) NOT NULL,
        TotalDeductions DECIMAL(15,2) NOT NULL,
        TotalTaxableIncome DECIMAL(15,2) NOT NULL,
        IncomeTaxPayable DECIMAL(15,2) NOT NULL,
        MedicareLevyPayable DECIMAL(15,2) NOT NULL,
        OtherLeviesPayable DECIMAL(15,2) DEFAULT 0,
        TotalTaxOffsets DECIMAL(15,2) DEFAULT 0,
        NetTaxPayable DECIMAL(15,2) NOT NULL,
        EffectiveTaxRate DECIMAL(5,4) NOT NULL,
        MarginalTaxRate DECIMAL(5,4) NOT NULL,
        CalculationDate DATETIME2 DEFAULT GETDATE(),
        LastModifiedDate DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(UserId),
        CONSTRAINT UK_UserAnnualTax UNIQUE (UserId, FinancialYear)
    );
END
GO

-- Tax Calculation Audit Trail
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TaxCalculationHistory' AND xtype='U')
BEGIN
    CREATE TABLE TaxCalculationHistory (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NULL, -- NULL for anonymous calculations
        FinancialYear VARCHAR(7) NOT NULL,
        TaxableIncome DECIMAL(15,2) NOT NULL,
        CalculatedTax DECIMAL(15,2) NOT NULL,
        CalculationDetails NVARCHAR(MAX), -- JSON breakdown
        CalculationDate DATETIME2 DEFAULT GETDATE(),
        ClientIP VARCHAR(45),
        UserAgent VARCHAR(500)
    );
END
GO

-- Create indexes for performance
CREATE NONCLUSTERED INDEX IX_TaxBrackets_FinancialYear 
ON TaxBrackets (FinancialYear, IsActive) 
INCLUDE (MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder);
GO

CREATE NONCLUSTERED INDEX IX_TaxOffsets_FinancialYear 
ON TaxOffsets (FinancialYear, IsActive);
GO

CREATE NONCLUSTERED INDEX IX_TaxLevies_FinancialYear 
ON TaxLevies (FinancialYear, IsActive);
GO

CREATE NONCLUSTERED INDEX IX_UserMonthlyIncome_User_Year 
ON UserMonthlyIncome (UserId, FinancialYear);
GO

CREATE NONCLUSTERED INDEX IX_UserAnnualTaxSummary_User 
ON UserAnnualTaxSummary (UserId, FinancialYear);
GO

PRINT 'Database schema created successfully.';
GO
