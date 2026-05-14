export interface CourtBlockDto {
  id: string;
  courtId: number;
  courtName: string;
  startsAt: string;
  endsAt: string;
  reason: string;
  seriesId: string | null;
}

export interface CreateCourtBlockOnceRequest {
  /** Ignored when allCourts === true. */
  courtId: number;
  startsAt: string;
  endsAt: string;
  reason: string;
  forceCancelConflicts: boolean;
  /** When true, the block is materialized for every active court. */
  allCourts?: boolean;
}

export type Weekday = 0 | 1 | 2 | 3 | 4 | 5 | 6; // Sun..Sat (ASP.NET DayOfWeek)

export interface CreateCourtBlockSeriesRequest {
  /** Ignored when allCourts === true. */
  courtId: number;
  weekday: Weekday;
  startTime: string;        // "HH:mm:ss"
  endTime: string;
  startDate: string;        // "YYYY-MM-DD"
  endDate: string;
  reason: string;
  forceCancelConflicts: boolean;
  /** When true, the series is materialized for every active court. */
  allCourts?: boolean;
}

export interface CreateOnceResponse {
  block: CourtBlockDto;
  cancelledReservations: number;
}

export interface CreateSeriesResponse {
  seriesId: string;
  blocksCreated: number;
  cancelledReservations: number;
}
