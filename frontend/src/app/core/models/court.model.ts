export interface CourtDto {
  id: number;
  name: string;
  displayOrder: number;
  isActive: boolean;
  isGuestBookable: boolean;
}

export interface CreateCourtRequest {
  name: string;
  displayOrder?: number | null;
  isGuestBookable?: boolean;
}

export interface UpdateCourtRequest {
  name: string;
  displayOrder: number;
  isActive: boolean;
  isGuestBookable: boolean;
}
