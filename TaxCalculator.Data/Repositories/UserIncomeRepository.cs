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
                await connection.OpenAsync();
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
                                Id = reader.GetInt64("Id"),
                                UserId = reader.GetGuid("UserId"),
                                FinancialYear = reader.GetString("FinancialYear"),
                                Month = reader.GetInt32("Month"),
                                GrossIncome = reader.GetDecimal("GrossIncome"),
                                TaxableIncome = reader.GetDecimal("TaxableIncome"),
                                DeductionsAmount = reader.GetDecimal("DeductionsAmount"),
                                SuperContributions = reader.GetDecimal("SuperContributions"),
                                IncomeType = reader.GetString("IncomeType"),
                                PayPeriod = reader.GetString("PayPeriod"),
                                RecordedDate = reader.GetDateTime("RecordedDate")
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
                await connection.OpenAsync();
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
                await connection.OpenAsync();
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
                await connection.OpenAsync();
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
                                Id = reader.GetInt64("Id"),
                                UserId = reader.GetGuid("UserId"),
                                FinancialYear = reader.GetString("FinancialYear"),
                                TotalGrossIncome = reader.GetDecimal("TotalGrossIncome"),
                                TotalDeductions = reader.GetDecimal("TotalDeductions"),
                                TotalTaxableIncome = reader.GetDecimal("TotalTaxableIncome"),
                                IncomeTaxPayable = reader.GetDecimal("IncomeTaxPayable"),
                                MedicareLevyPayable = reader.GetDecimal("MedicareLevyPayable"),
                                OtherLeviesPayable = reader.GetDecimal("OtherLeviesPayable"),
                                TotalTaxOffsets = reader.GetDecimal("TotalTaxOffsets"),
                                NetTaxPayable = reader.GetDecimal("NetTaxPayable"),
                                EffectiveTaxRate = reader.GetDecimal("EffectiveTaxRate"),
                                MarginalTaxRate = reader.GetDecimal("MarginalTaxRate"),
                                CalculationDate = reader.GetDateTime("CalculationDate"),
                                LastModifiedDate = reader.GetDateTime("LastModifiedDate")
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
                await connection.OpenAsync();
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
                await connection.OpenAsync();
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
                                Id = reader.GetInt64("Id"),
                                UserId = reader.GetGuid("UserId"),
                                FinancialYear = reader.GetString("FinancialYear"),
                                TotalGrossIncome = reader.GetDecimal("TotalGrossIncome"),
                                TotalDeductions = reader.GetDecimal("TotalDeductions"),
                                TotalTaxableIncome = reader.GetDecimal("TotalTaxableIncome"),
                                IncomeTaxPayable = reader.GetDecimal("IncomeTaxPayable"),
                                MedicareLevyPayable = reader.GetDecimal("MedicareLevyPayable"),
                                OtherLeviesPayable = reader.GetDecimal("OtherLeviesPayable"),
                                TotalTaxOffsets = reader.GetDecimal("TotalTaxOffsets"),
                                NetTaxPayable = reader.GetDecimal("NetTaxPayable"),
                                EffectiveTaxRate = reader.GetDecimal("EffectiveTaxRate"),
                                MarginalTaxRate = reader.GetDecimal("MarginalTaxRate"),
                                CalculationDate = reader.GetDateTime("CalculationDate"),
                                LastModifiedDate = reader.GetDateTime("LastModifiedDate")
                            });
                        }
                    }
                    return summaries;
                }
            }
        }
    }
}
