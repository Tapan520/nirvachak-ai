import apiClient from './client';

export interface WardItem {
  id: number;
  wardNumber: string;
  wardName: string;
  description?: string | null;
}
export interface WardRequest {
  wardNumber: string;
  wardName: string;
  description?: string | null;
}

export const getWards = async (): Promise<WardItem[]> => {
  const { data } = await apiClient.get<WardItem[]>('/wards');
  return data;
};

export const createWard = (req: WardRequest) => apiClient.post('/wards', req);
export const updateWard = (id: number, req: WardRequest) => apiClient.put(`/wards/${id}`, req);
export const deleteWard = (id: number) => apiClient.delete(`/wards/${id}`);
