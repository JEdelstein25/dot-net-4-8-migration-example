using System.Data;

namespace TaxCalculator.Data.Interfaces
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
        IDbConnection CreateConnection(string connectionString);
    }
}
