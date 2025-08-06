using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;
using TaxCalculator.Data.Interfaces;

namespace TaxCalculator.Data.Repositories
{
    public class UserIncomeRepository : IUserIncomeRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public UserIncomeRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<UserMonthlyIncome>> GetMonthlyIncomeAsync(Guid userId, string financialYear)
        {
            const string sql = @"
                SELECT Id, UserId, FinancialYear, Month, GrossIncome, TaxableIncome, 
                       DeductionsAmount, SuperContributions, IncomeType, PayPeriod, RecordedDate
                FROM UserMonthlyIncome 
                WHERE UserId = @UserId AND FinancialYear = @FinancialYear
                ORDER BY Month";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@FinancialYear", financialYear);

                    var incomes = new List<UserMonthlyIncome>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            incomes.Add(new UserMonthlyIncome
                            {
                                Id = reader.GetInt64(0),
                                UserId = reader.GetGuid(1),
                                FinancialYear = reader.GetString(2),
                                Month = reader.GetInt32(3),
                                GrossIncome = reader.GetDecimal(4),
                                TaxableIncome = reader.GetDecimal(5),
                                DeductionsAmount = reader.GetDecimal(6),
                                SuperContributions = reader.GetDecimal(7),
                                IncomeType = reader.GetString(8),
                                PayPeriod = reader.GetString(9),
                                RecordedDate = reader.GetDateTime(10)
                            });
                        }
                    }
                    return incomes;
                }
            }
        }

        public async Task SaveMonthlyIncomeAsync(UserMonthlyIncome income)
        {
            income.RecordedDate = DateTime.UtcNow;

            const string sql = @"
                INSERT INTO UserMonthlyIncome (UserId, FinancialYear, Month, GrossIncome, TaxableIncome, 
                                             DeductionsAmount, SuperContributions, IncomeType, PayPeriod, RecordedDate)
                VALUES (@UserId, @FinancialYear, @Month, @GrossIncome, @TaxableIncome, 
                        @DeductionsAmount, @SuperContributions, @IncomeType, @PayPeriod, @RecordedDate)";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", income.UserId);
                    command.Parameters.AddWithValue("@FinancialYear", income.FinancialYear);
                    command.Parameters.AddWithValue("@Month", income.Month);
                    command.Parameters.AddWithValue("@GrossIncome", income.GrossIncome);
                    command.Parameters.AddWithValue("@TaxableIncome", income.TaxableIncome);
                    command.Parameters.AddWithValue("@DeductionsAmount", income.DeductionsAmount);
                    command.Parameters.AddWithValue("@SuperContributions", income.SuperContributions);
                    command.Parameters.AddWithValue("@IncomeType", income.IncomeType);
                    command.Parameters.AddWithValue("@PayPeriod", income.PayPeriod);
                    command.Parameters.AddWithValue("@RecordedDate", income.RecordedDate);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UpdateMonthlyIncomeAsync(UserMonthlyIncome income)
        {
            const string sql = @"
                UPDATE UserMonthlyIncome 
                SET GrossIncome = @GrossIncome, TaxableIncome = @TaxableIncome, 
                    DeductionsAmount = @DeductionsAmount, SuperContributions = @SuperContributions, 
                    IncomeType = @IncomeType, PayPeriod = @PayPeriod
                WHERE UserId = @UserId AND FinancialYear = @FinancialYear AND Month = @Month";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", income.UserId);
                    command.Parameters.AddWithValue("@FinancialYear", income.FinancialYear);
                    command.Parameters.AddWithValue("@Month", income.Month);
                    command.Parameters.AddWithValue("@GrossIncome", income.GrossIncome);
                    command.Parameters.AddWithValue("@TaxableIncome", income.TaxableIncome);
                    command.Parameters.AddWithValue("@DeductionsAmount", income.DeductionsAmount);
                    command.Parameters.AddWithValue("@SuperContributions", income.SuperContributions);
                    command.Parameters.AddWithValue("@IncomeType", income.IncomeType);
                    command.Parameters.AddWithValue("@PayPeriod", income.PayPeriod);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<UserAnnualTaxSummary> GetAnnualSummaryAsync(Guid userId, string financialYear)
        {
            const string sql = @"
                SELECT Id, UserId, FinancialYear, TotalGrossIncome, TotalDeductions, TotalTaxableIncome,
                       IncomeTaxPayable, MedicareLevyPayable, OtherLeviesPayable, TotalTaxOffsets,
                       NetTaxPayable, EffectiveTaxRate, MarginalTaxRate, CalculationDate, LastModifiedDate
                FROM UserAnnualTaxSummary 
                WHERE UserId = @UserId AND FinancialYear = @FinancialYear";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@FinancialYear", financialYear);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UserAnnualTaxSummary
                            {
                                Id = reader.GetInt64(0),
                                UserId = reader.GetGuid(1),
                                FinancialYear = reader.GetString(2),
                                TotalGrossIncome = reader.GetDecimal(3),
                                TotalDeductions = reader.GetDecimal(4),
                                TotalTaxableIncome = reader.GetDecimal(5),
                                IncomeTaxPayable = reader.GetDecimal(6),
                                MedicareLevyPayable = reader.GetDecimal(7),
                                OtherLeviesPayable = reader.GetDecimal(8),
                                TotalTaxOffsets = reader.GetDecimal(9),
                                NetTaxPayable = reader.GetDecimal(10),
                                EffectiveTaxRate = reader.GetDecimal(11),
                                MarginalTaxRate = reader.GetDecimal(12),
                                CalculationDate = reader.GetDateTime(13),
                                LastModifiedDate = reader.GetDateTime(14)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task SaveAnnualSummaryAsync(UserAnnualTaxSummary summary)
        {
            summary.CalculationDate = DateTime.UtcNow;
            summary.LastModifiedDate = DateTime.UtcNow;

            const string sql = @"
                MERGE UserAnnualTaxSummary AS target
                USING (SELECT @UserId, @FinancialYear) AS source (UserId, FinancialYear)
                ON (target.UserId = source.UserId AND target.FinancialYear = source.FinancialYear)
                WHEN MATCHED THEN
                    UPDATE SET TotalGrossIncome = @TotalGrossIncome, TotalDeductions = @TotalDeductions,
                               TotalTaxableIncome = @TotalTaxableIncome, IncomeTaxPayable = @IncomeTaxPayable,
                               MedicareLevyPayable = @MedicareLevyPayable, OtherLeviesPayable = @OtherLeviesPayable,
                               TotalTaxOffsets = @TotalTaxOffsets, NetTaxPayable = @NetTaxPayable,
                               EffectiveTaxRate = @EffectiveTaxRate, MarginalTaxRate = @MarginalTaxRate,
                               LastModifiedDate = @LastModifiedDate
                WHEN NOT MATCHED THEN
                    INSERT (UserId, FinancialYear, TotalGrossIncome, TotalDeductions, TotalTaxableIncome,
                            IncomeTaxPayable, MedicareLevyPayable, OtherLeviesPayable, TotalTaxOffsets,
                            NetTaxPayable, EffectiveTaxRate, MarginalTaxRate, CalculationDate, LastModifiedDate)
                    VALUES (@UserId, @FinancialYear, @TotalGrossIncome, @TotalDeductions, @TotalTaxableIncome,
                            @IncomeTaxPayable, @MedicareLevyPayable, @OtherLeviesPayable, @TotalTaxOffsets,
                            @NetTaxPayable, @EffectiveTaxRate, @MarginalTaxRate, @CalculationDate, @LastModifiedDate);";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", summary.UserId);
                    command.Parameters.AddWithValue("@FinancialYear", summary.FinancialYear);
                    command.Parameters.AddWithValue("@TotalGrossIncome", summary.TotalGrossIncome);
                    command.Parameters.AddWithValue("@TotalDeductions", summary.TotalDeductions);
                    command.Parameters.AddWithValue("@TotalTaxableIncome", summary.TotalTaxableIncome);
                    command.Parameters.AddWithValue("@IncomeTaxPayable", summary.IncomeTaxPayable);
                    command.Parameters.AddWithValue("@MedicareLevyPayable", summary.MedicareLevyPayable);
                    command.Parameters.AddWithValue("@OtherLeviesPayable", summary.OtherLeviesPayable);
                    command.Parameters.AddWithValue("@TotalTaxOffsets", summary.TotalTaxOffsets);
                    command.Parameters.AddWithValue("@NetTaxPayable", summary.NetTaxPayable);
                    command.Parameters.AddWithValue("@EffectiveTaxRate", summary.EffectiveTaxRate);
                    command.Parameters.AddWithValue("@MarginalTaxRate", summary.MarginalTaxRate);
                    command.Parameters.AddWithValue("@CalculationDate", summary.CalculationDate);
                    command.Parameters.AddWithValue("@LastModifiedDate", summary.LastModifiedDate);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<UserAnnualTaxSummary>> GetTaxHistoryAsync(Guid userId, int years = 5)
        {
            const string sql = @"
                SELECT TOP(@Years) Id, UserId, FinancialYear, TotalGrossIncome, TotalDeductions, TotalTaxableIncome,
                       IncomeTaxPayable, MedicareLevyPayable, OtherLeviesPayable, TotalTaxOffsets,
                       NetTaxPayable, EffectiveTaxRate, MarginalTaxRate, CalculationDate, LastModifiedDate
                FROM UserAnnualTaxSummary 
                WHERE UserId = @UserId
                ORDER BY FinancialYear DESC";

            using (var connection = _connectionFactory.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, (SqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Years", years);

                    var summaries = new List<UserAnnualTaxSummary>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            summaries.Add(new UserAnnualTaxSummary
                            {
                                Id = reader.GetInt64(0),
                                UserId = reader.GetGuid(1),
                                FinancialYear = reader.GetString(2),
                                TotalGrossIncome = reader.GetDecimal(3),
                                TotalDeductions = reader.GetDecimal(4),
                                TotalTaxableIncome = reader.GetDecimal(5),
                                IncomeTaxPayable = reader.GetDecimal(6),
                                MedicareLevyPayable = reader.GetDecimal(7),
                                OtherLeviesPayable = reader.GetDecimal(8),
                                TotalTaxOffsets = reader.GetDecimal(9),
                                NetTaxPayable = reader.GetDecimal(10),
                                EffectiveTaxRate = reader.GetDecimal(11),
                                MarginalTaxRate = reader.GetDecimal(12),
                                CalculationDate = reader.GetDateTime(13),
                                LastModifiedDate = reader.GetDateTime(14)
                            });
                        }
                    }
                    return summaries;
                }
            }
        }
    }
}
