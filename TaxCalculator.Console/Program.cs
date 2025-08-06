using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;

namespace TaxCalculator.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Australian Tax Calculator - Database Setup");
            System.Console.WriteLine("==========================================");

            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                
                // Replace |DataDirectory| with current directory
                connectionString = connectionString.Replace("|DataDirectory|", Environment.CurrentDirectory);
                
                System.Console.WriteLine("Creating database schema...");
                ExecuteSqlScript(connectionString, "..\\Database\\CreateDatabase.sql");
                
                System.Console.WriteLine("Seeding historical tax data...");
                ExecuteSqlScript(connectionString, "..\\Database\\SeedData.sql");
                
                System.Console.WriteLine("Database setup completed successfully!");
                System.Console.WriteLine("\nPress any key to exit...");
                System.Console.ReadKey();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
                System.Console.WriteLine("\nPress any key to exit...");
                System.Console.ReadKey();
            }
        }

        static void ExecuteSqlScript(string connectionString, string scriptPath)
        {
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"SQL script not found: {scriptPath}");
            }

            var script = File.ReadAllText(scriptPath);
            var batches = script.Split(new string[] { "GO\r\n", "GO\n", "GO" }, StringSplitOptions.RemoveEmptyEntries);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                foreach (var batch in batches)
                {
                    var trimmedBatch = batch.Trim();
                    if (!string.IsNullOrEmpty(trimmedBatch))
                    {
                        using (var command = new SqlCommand(trimmedBatch, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
