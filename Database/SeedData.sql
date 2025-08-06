-- Seed Australian Tax Data for 2015-16 to 2024-25
-- Historical Tax Brackets and Rules

USE TaxCalculator;
GO

-- Clear existing data
DELETE FROM TaxCalculationHistory;
DELETE FROM UserAnnualTaxSummary;
DELETE FROM UserMonthlyIncome;
DELETE FROM TaxOffsets;
DELETE FROM TaxLevies;
DELETE FROM TaxBrackets;
GO

-- 2024-25 Tax Year (Current - Stage 3 Tax Cuts)
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2024-25', 0, 18200, 0.0000, 0, 1, 1),
('2024-25', 18201, 45000, 0.1600, 0, 2, 1),
('2024-25', 45001, 135000, 0.3000, 4288, 3, 1),
('2024-25', 135001, 190000, 0.3700, 31288, 4, 1),
('2024-25', 190001, NULL, 0.4500, 51638, 5, 1);

-- 2023-24 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2023-24', 0, 18200, 0.0000, 0, 1, 1),
('2023-24', 18201, 45000, 0.1900, 0, 2, 1),
('2023-24', 45001, 120000, 0.3250, 5092, 3, 1),
('2023-24', 120001, 180000, 0.3700, 29467, 4, 1),
('2023-24', 180001, NULL, 0.4500, 51667, 5, 1);

-- 2022-23 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2022-23', 0, 18200, 0.0000, 0, 1, 1),
('2022-23', 18201, 45000, 0.1900, 0, 2, 1),
('2022-23', 45001, 120000, 0.3250, 5092, 3, 1),
('2022-23', 120001, 180000, 0.3700, 29467, 4, 1),
('2022-23', 180001, NULL, 0.4500, 51667, 5, 1);

-- 2021-22 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2021-22', 0, 18200, 0.0000, 0, 1, 1),
('2021-22', 18201, 45000, 0.1900, 0, 2, 1),
('2021-22', 45001, 120000, 0.3250, 5092, 3, 1),
('2021-22', 120001, 180000, 0.3700, 29467, 4, 1),
('2021-22', 180001, NULL, 0.4500, 51667, 5, 1);

-- 2020-21 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2020-21', 0, 18200, 0.0000, 0, 1, 1),
('2020-21', 18201, 45000, 0.1900, 0, 2, 1),
('2020-21', 45001, 120000, 0.3250, 5092, 3, 1),
('2020-21', 120001, 180000, 0.3700, 29467, 4, 1),
('2020-21', 180001, NULL, 0.4500, 51667, 5, 1);

-- 2019-20 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2019-20', 0, 18200, 0.0000, 0, 1, 1),
('2019-20', 18201, 37000, 0.1900, 0, 2, 1),
('2019-20', 37001, 90000, 0.3250, 3572, 3, 1),
('2019-20', 90001, 180000, 0.3700, 20797, 4, 1),
('2019-20', 180001, NULL, 0.4500, 54097, 5, 1);

-- 2018-19 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2018-19', 0, 18200, 0.0000, 0, 1, 1),
('2018-19', 18201, 37000, 0.1900, 0, 2, 1),
('2018-19', 37001, 90000, 0.3250, 3572, 3, 1),
('2018-19', 90001, 180000, 0.3700, 20797, 4, 1),
('2018-19', 180001, NULL, 0.4500, 54097, 5, 1);

-- 2017-18 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2017-18', 0, 18200, 0.0000, 0, 1, 1),
('2017-18', 18201, 37000, 0.1900, 0, 2, 1),
('2017-18', 37001, 80000, 0.3250, 3572, 3, 1),
('2017-18', 80001, 180000, 0.3700, 17547, 4, 1),
('2017-18', 180001, NULL, 0.4500, 54547, 5, 1);

-- 2016-17 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2016-17', 0, 18200, 0.0000, 0, 1, 1),
('2016-17', 18201, 37000, 0.1900, 0, 2, 1),
('2016-17', 37001, 80000, 0.3250, 3572, 3, 1),
('2016-17', 80001, 180000, 0.3700, 17547, 4, 1),
('2016-17', 180001, NULL, 0.4500, 54547, 5, 1);

-- 2015-16 Tax Year
INSERT INTO TaxBrackets (FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, IsActive) VALUES
('2015-16', 0, 18200, 0.0000, 0, 1, 1),
('2015-16', 18201, 37000, 0.1900, 0, 2, 1),
('2015-16', 37001, 80000, 0.3250, 3572, 3, 1),
('2015-16', 80001, 180000, 0.3700, 17547, 4, 1),
('2015-16', 180001, NULL, 0.4500, 54547, 5, 1);

-- Medicare Levy (2% for all years)
INSERT INTO TaxLevies (FinancialYear, LevyType, ThresholdIncome, LevyRate, IsActive) VALUES
('2024-25', 'Medicare', 0, 0.0200, 1),
('2023-24', 'Medicare', 0, 0.0200, 1),
('2022-23', 'Medicare', 0, 0.0200, 1),
('2021-22', 'Medicare', 0, 0.0200, 1),
('2020-21', 'Medicare', 0, 0.0200, 1),
('2019-20', 'Medicare', 0, 0.0200, 1),
('2018-19', 'Medicare', 0, 0.0200, 1),
('2017-18', 'Medicare', 0, 0.0200, 1),
('2016-17', 'Medicare', 0, 0.0200, 1),
('2015-16', 'Medicare', 0, 0.0200, 1);

-- Temporary Budget Repair Levy (2014-2017 - 2% on income >$180k)
INSERT INTO TaxLevies (FinancialYear, LevyType, ThresholdIncome, LevyRate, IsActive) VALUES
('2017-18', 'BudgetRepair', 180000, 0.0200, 1),
('2016-17', 'BudgetRepair', 180000, 0.0200, 1),
('2015-16', 'BudgetRepair', 180000, 0.0200, 1);

-- Low Income Tax Offset (LITO) - varies by year
INSERT INTO TaxOffsets (FinancialYear, OffsetType, MaxOffset, PhaseOutStart, PhaseOutRate, IsActive) VALUES
('2024-25', 'LITO', 700, 37500, 0.0500, 1),
('2023-24', 'LITO', 700, 37500, 0.0500, 1),
('2022-23', 'LITO', 700, 37500, 0.0500, 1),
('2021-22', 'LITO', 700, 37500, 0.0500, 1),
('2020-21', 'LITO', 700, 37500, 0.0500, 1),
('2019-20', 'LITO', 445, 37000, 0.015, 1),
('2018-19', 'LITO', 445, 37000, 0.015, 1),
('2017-18', 'LITO', 445, 37000, 0.015, 1),
('2016-17', 'LITO', 445, 37000, 0.015, 1),
('2015-16', 'LITO', 445, 37000, 0.015, 1);

-- Create sample users for testing
INSERT INTO Users (UserId, FirstName, LastName, Email, ResidencyStatus) VALUES
(NEWID(), 'John', 'Smith', 'john.smith@email.com', 'Resident'),
(NEWID(), 'Jane', 'Doe', 'jane.doe@email.com', 'Resident'),
(NEWID(), 'Michael', 'Johnson', 'michael.johnson@email.com', 'Resident');

PRINT 'Historical tax data seeded successfully for 2015-16 to 2024-25.';
GO
