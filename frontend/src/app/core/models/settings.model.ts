export interface PublicSettingsDto {
  maxAdvanceBookingDays: number;
  minCancellationHours: number;
  maxOpenReservationsPerMember: number;
}

export interface UpdateSettingsRequest {
  maxAdvanceBookingDays: number;
  minCancellationHours: number;
  maxOpenReservationsPerMember: number;
}
