using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TennisClub.Api.Common.Exceptions;

public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// Detects SQL Server unique-constraint / unique-index violations.
    ///   2627 = Violation of UNIQUE KEY constraint
    ///   2601 = Cannot insert duplicate key row in object with unique index
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is not SqlException sql) return false;
        return sql.Number is 2627 or 2601;
    }
}
