import apiClient from './client';

export interface VolunteerItem {
  id: number;
  name: string;
  phone: string;
  task: string;
  assignedArea?: string;
  assignedBoothNumbers?: string;
  isActive: boolean;
}

export const getVolunteers = async (): Promise<VolunteerItem[]> => {
  const { data } = await apiClient.get<VolunteerItem[]>('/volunteers');
  return data;
};

export const createVolunteer = async (req: {
  name: string; phone: string; email?: string; address?: string;
  task: string; assignedArea?: string; assignedBoothNumbers?: string; notes?: string;
}): Promise<void> => {
  await apiClient.post('/volunteers', req);
};
