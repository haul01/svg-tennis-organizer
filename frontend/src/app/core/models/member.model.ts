export type MemberRole = 'Admin' | 'Trainer' | 'Member' | 'Guest';

export const MEMBER_ROLES: readonly MemberRole[] = ['Admin', 'Trainer', 'Member', 'Guest'];

export interface MemberListItemDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: MemberRole;
  isActive: boolean;
  createdAt: string;
}

export interface MemberDetailDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: MemberRole;
  isActive: boolean;
  createdAt: string;
}

export interface CreateMemberRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: MemberRole;
}

export interface UpdateMemberRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: MemberRole;
}

export interface ListMembersOptions {
  search?: string;
  status?: 'active' | 'inactive';
  role?: MemberRole;
}
