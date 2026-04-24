export interface CourtDto {
  id: number;
  name: string;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateCourtRequest {
  name: string;
  displayOrder?: number | null;
}

export interface UpdateCourtRequest {
  name: string;
  displayOrder: number;
  isActive: boolean;
}
