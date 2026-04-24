namespace TennisClub.Api.Infrastructure.Persistence.Seed;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public AdminOptions Admin { get; set; } = new();
    public SeasonOptions Season { get; set; } = new();
    public List<CourtSeedOptions> Courts { get; set; } = [];

    public class AdminOptions
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = "Admin";
        public string LastName { get; set; } = "Admin";
    }

    public class SeasonOptions
    {
        public string Name { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly OpeningTime { get; set; }
        public TimeOnly ClosingTime { get; set; }
        public int SlotDurationMinutes { get; set; } = 60;
    }

    public class CourtSeedOptions
    {
        public string Name { get; set; } = null!;
        public int DisplayOrder { get; set; }
    }
}
