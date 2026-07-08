import apiClient from './client';

export interface BoothShiftItem {
  id: number;
  volunteerId: number;
  volunteerName: string;
  volunteerPhone: string;
  boothNumber: number;
  shiftStart: string;
  shiftEnd: string;
  role: string;
  isConfirmed: boolean;
  notes?: string;
}

export const SHIFT_ROLES = [
  'BoothAgent', 'Coordinator', 'Transport', 'Security', 'Observer', 'Other',
];

export const getBoothShifts = async (booth?: number): Promise<BoothShiftItem[]> => {
  const { data } = await apiClient.get<BoothShiftItem[]>('/boothshifts', {
    params: booth ? { booth } : {},
  });
  return data;
};

export const createBoothShift = async (req: {
  volunteerId: number; boothNumber: number;
  shiftStart: string; shiftEnd: string;
  role: string; notes?: string;
}): Promise<void> => {
  await apiClient.post('/boothshifts', req);
};

export const confirmShift = async (id: number): Promise<void> => {
  await apiClient.put(`/boothshifts/${id}/confirm`);
};
