export interface SeasonDto {
  id: number;
  name: string;
  startDate: string;        // "YYYY-MM-DD" from System.Text.Json DateOnly
  endDate: string;
  openingTime: string;      // "HH:mm:ss" from TimeOnly
  closingTime: string;
  slotDurationMinutes: number;
}
