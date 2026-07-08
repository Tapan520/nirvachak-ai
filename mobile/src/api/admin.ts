import apiClient from './client';

export interface AdminUserItem {
  id: string;
  fullName: string;
  email?: string;
  role: string;
  constituencyName?: string;
  assignedWard?: string;
  isActive: boolean;
}

export const getAdminUsers = async (): Promise<AdminUserItem[]> => {
  const { data } = await apiClient.get<AdminUserItem[]>('/admin/users');
  return data;
};

export const toggleUser = async (id: string): Promise<void> => {
  await apiClient.put(`/admin/users/${id}/toggle`);
};
