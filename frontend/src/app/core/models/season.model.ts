export interface SeasonDto {
  id: number;
  name: string;
  startDate: string;        // "YYYY-MM-DD" from System.Text.Json DateOnly
  endDate: string;
  openingTime: string;      // "HH:mm:ss" from TimeOnly
  closingTime: string;
  slotDurationMinutes: number;
}

export interface UpdateSeasonRequest {
  name: string;
  startDate: string;        // "YYYY-MM-DD"
  endDate: string;
  openingTime: string;      // "HH:mm:ss"
  closingTime: string;
  slotDurationMinutes: number;
}
