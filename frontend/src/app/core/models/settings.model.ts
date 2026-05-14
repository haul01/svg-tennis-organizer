export interface PublicSettingsDto {
  maxAdvanceBookingDays: number;
  minCancellationHours: number;
  maxOpenReservationsPerMember: number;
  maxSlotsPerBooking: number;
  guestMembershipPromptText: string;
}

export interface UpdateSettingsRequest {
  maxAdvanceBookingDays: number;
  minCancellationHours: number;
  maxOpenReservationsPerMember: number;
  maxSlotsPerBooking: number;
  guestMembershipPromptText: string;
}
