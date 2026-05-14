import { ReservationStatus } from '../../features/reservations/reservation.model';

export interface ReservationReportItemDto {
  id: string;
  startsAt: string;        // ISO 8601
  endsAt: string;
  courtName: string;
  memberFirstName: string;
  memberLastName: string;
  memberEmail: string;
  hasGuest: boolean;
  guestName: string | null;
  status: ReservationStatus;
  createdAt: string;
  cancelledAt: string | null;
}

export interface ListReservationsResponse {
  items: ReservationReportItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ListReservationsQuery {
  from?: Date;
  to?: Date;
  courtId?: number;
  status?: ReservationStatus;
  page?: number;
  pageSize?: number;
}
