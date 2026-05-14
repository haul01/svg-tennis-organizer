namespace TennisClub.Api.Domain.Entities;

public class Court
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When true, members with role Guest may book this court. Default
    /// false - admins explicitly opt courts in (typically Platz 3/4) so
    /// existing seeds stay restrictive after migration.
    /// </summary>
    public bool IsGuestBookable { get; set; }
}
