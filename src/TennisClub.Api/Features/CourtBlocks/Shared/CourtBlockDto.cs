namespace TennisClub.Api.Features.CourtBlocks.Shared;

public sealed record CourtBlockDto(
    Guid Id,
    int CourtId,
    string CourtName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Reason,
    Guid? SeriesId);

public sealed record ConflictingReservationDto(
    Guid Id,
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string MemberEmail);
