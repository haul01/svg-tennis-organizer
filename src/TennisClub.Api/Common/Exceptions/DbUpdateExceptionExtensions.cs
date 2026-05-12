using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TennisClub.Api.Common.Exceptions;

public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// Detects PostgreSQL uniqueness violations from both classic unique
    /// indexes (23505) and GiST exclusion constraints (23P01). The latter
    /// is what catches overlapping reservations whose StartsAt differs.
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is not PostgresException pg) return false;
        return pg.SqlState is PostgresErrorCodes.UniqueViolation
            or PostgresErrorCodes.ExclusionViolation;
    }
}
