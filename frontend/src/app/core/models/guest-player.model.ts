export interface GuestPlayerDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateGuestPlayerRequest {
  firstName: string;
  lastName: string;
  email: string | null;
}
