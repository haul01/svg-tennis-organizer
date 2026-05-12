export interface PublicSettingsDto {
  maxAdvanceBookingDays: number;
  minCancellationHours: number;
  maxOpenReservationsPerMember: number;
  maxSlotsPerBooking: number;
}

export interface UpdateSettingsRequest {
  maxAdvanceBookingDays: number;
  minCancellationHours: number;
  maxOpenReservationsPerMember: number;
  maxSlotsPerBooking: number;
}
