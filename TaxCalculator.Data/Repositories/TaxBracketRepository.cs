using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using TaxCalculator.Core.Models;
using TaxCalculator.Data.Interfaces;

namespace TaxCalculator.Data.Repositories
{
    public class TaxBracketRepository : ITaxBracketRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        public TaxBracketRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<TaxBracket>> GetTaxBracketsAsync(string financialYear)
        {
            return await Task.Run(() =>
            {
                const string sql = @"
                    SELECT Id, FinancialYear, MinIncome, MaxIncome, TaxRate, FixedAmount, BracketOrder, CreatedDate, IsActive
                    FROM TaxBrackets 
                    WHERE FinancialYear = @FinancialYear AND IsActive = 1
                    ORDER BY BracketOrder";

                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    using (var command = new SqlCommand(sql, (SqlConnection)connection))
                    {
                        command.Parameters.AddWithValue("@FinancialYear", financialYear);

                        var brackets = new List<TaxBracket>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                brackets.Add(new TaxBracket
                                {
                                    Id = reader.GetInt32(0),
                                    FinancialYear = reader.GetString(1),
                                    MinIncome = reader.GetDecimal(2),
                                    MaxIncome = reader.IsDBNull(3) ? (decimal?)null : reader.GetDecimal(3),
                                    TaxRate = reader.GetDecimal(4),
                                    FixedAmount = reader.GetDecimal(5),
                                    BracketOrder = reader.GetInt32(6),
                                    CreatedDate = reader.GetDateTime(7),
                                    IsActive = reader.GetBoolean(8)
                                });
                            }
                        }
                        return brackets;
                    }
                }
            });
        }

        public async Task<List<TaxOffset>> GetTaxOffsetsAsync(string financialYear)
        {
            return await Task.Run(() =>
            {
                const string sql = @"
                    SELECT Id, FinancialYear, OffsetType, MaxIncome, MaxOffset, PhaseOutStart, PhaseOutRate, IsActive, CreatedDate
                    FROM TaxOffsets 
                    WHERE FinancialYear = @FinancialYear AND IsActive = 1";

                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    using (var command = new SqlCommand(sql, (SqlConnection)connection))
                    {
                        command.Parameters.AddWithValue("@FinancialYear", financialYear);

                        var offsets = new List<TaxOffset>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                offsets.Add(new TaxOffset
                                {
                                    Id = reader.GetInt32(0),
                                    FinancialYear = reader.GetString(1),
                                    OffsetType = reader.GetString(2),
                                    MaxIncome = reader.IsDBNull(3) ? (decimal?)null : reader.GetDecimal(3),
                                    MaxOffset = reader.GetDecimal(4),
                                    PhaseOutStart = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
                                    PhaseOutRate = reader.IsDBNull(6) ? (decimal?)null : reader.GetDecimal(6),
                                    IsActive = reader.GetBoolean(7),
                                    CreatedDate = reader.GetDateTime(8)
                                });
                            }
                        }
                        return offsets;
                    }
                }
            });
        }

        public async Task<List<TaxLevy>> GetTaxLeviesAsync(string financialYear)
        {
            return await Task.Run(() =>
            {
                const string sql = @"
                    SELECT Id, FinancialYear, LevyType, ThresholdIncome, LevyRate, MaxIncome, IsActive, CreatedDate
                    FROM TaxLevies 
                    WHERE FinancialYear = @FinancialYear AND IsActive = 1";

                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    using (var command = new SqlCommand(sql, (SqlConnection)connection))
                    {
                        command.Parameters.AddWithValue("@FinancialYear", financialYear);

                        var levies = new List<TaxLevy>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                levies.Add(new TaxLevy
                                {
                                    Id = reader.GetInt32(0),
                                    FinancialYear = reader.GetString(1),
                                    LevyType = reader.GetString(2),
                                    ThresholdIncome = reader.GetDecimal(3),
                                    LevyRate = reader.GetDecimal(4),
                                    MaxIncome = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
                                    IsActive = reader.GetBoolean(6),
                                    CreatedDate = reader.GetDateTime(7)
                                });
                            }
                        }
                        return levies;
                    }
                }
            });
        }

        public async Task<List<string>> GetAvailableFinancialYearsAsync()
        {
            return await Task.Run(() =>
            {
                const string sql = @"
                    SELECT DISTINCT FinancialYear 
                    FROM TaxBrackets 
                    WHERE IsActive = 1 
                    ORDER BY FinancialYear DESC";

                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    using (var command = new SqlCommand(sql, (SqlConnection)connection))
                    {
                        var years = new List<string>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                years.Add(reader.GetString(0));
                            }
                        }
                        return years;
                    }
                }
            });
        }
    }
}
