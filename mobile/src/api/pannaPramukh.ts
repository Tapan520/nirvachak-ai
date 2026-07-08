import apiClient from './client';

export interface PannaPramukhItem {
  id: number;
  name: string;
  phone: string;
  boothNumber: number;
  pannaNumber: string;
  totalVotersAssigned: number;
  votersContacted: number;
  contactPercent: number;
  isActive: boolean;
  notes?: string;
}

export const getPannaPramukhs = async (booth?: number): Promise<PannaPramukhItem[]> => {
  const { data } = await apiClient.get<PannaPramukhItem[]>('/pannapramukh', {
    params: booth ? { booth } : {},
  });
  return data;
};

export const createPannaPramukh = async (req: {
  name: string; phone: string; email?: string; address?: string;
  boothNumber: number; pannaNumber: string;
  totalVotersAssigned: number; notes?: string;
}): Promise<void> => {
  await apiClient.post('/pannapramukh', req);
};

export const updatePannaContact = async (id: number, votersContacted: number): Promise<void> => {
  await apiClient.put(`/pannapramukh/${id}/contact`, { id, votersContacted });
};
