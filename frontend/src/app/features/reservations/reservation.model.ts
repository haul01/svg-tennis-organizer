// Mirrors TennisClub.Api.Domain.Enums.ReservationStatus
export enum ReservationStatus {
  Active = 0,
  Cancelled = 1
}

/**
 * Week-grid projection. For foreign reservations the server sets
 * isMine=false and strips guestName - never show names to other members.
 */
export interface WeekReservationDto {
  id: string;
  courtId: number;
  startsAt: string; // ISO 8601 with offset
  endsAt: string;
  isMine: boolean;
  guestName: string | null;
}

/**
 * Member-owned reservation listing.
 */
export interface MyReservationDto {
  id: string;
  courtId: number;
  courtName: string;
  startsAt: string;
  endsAt: string;
  status: ReservationStatus;
  cancelledAt: string | null;
  hasGuest: boolean;
  guestName: string | null;
}

export interface CreateReservationRequest {
  courtId: number;
  startsAt: string;
  endsAt: string;
  guestPlayerId: string | null;
  hasGuest: boolean;
}

export interface CreateReservationResponse {
  id: string;
}

export interface ListMineOptions {
  upcomingOnly?: boolean;
  status?: ReservationStatus;
}
