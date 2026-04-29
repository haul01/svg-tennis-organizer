using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TennisClub.Api.Common.Exceptions;

public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// Detects PostgreSQL unique-constraint / unique-index violations.
    /// SqlState 23505 = unique_violation.
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is not PostgresException pg) return false;
        return pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
