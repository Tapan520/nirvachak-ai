import apiClient from './client';

export interface GrievanceItem {
  id: number;
  title: string;
  status: string;
  priority: string;
  reportedBy?: string;
  reporterPhone?: string;
  ward?: string;
  location?: string;
  reportedAt: string;
}

export const getGrievances = async (status?: string): Promise<GrievanceItem[]> => {
  const { data } = await apiClient.get<GrievanceItem[]>('/grievances',
    { params: status ? { status } : {} });
  return data;
};

export interface GrievanceDetail {
  id: number;
  title: string;
  description: string;
  status: string;
  priority: string;
  reportedBy?: string;
  reporterPhone?: string;
  ward?: string;
  location?: string;
  boothNumber?: number;
  assignedToName?: string;
  resolutionNotes?: string;
  reportedAt: string;
  resolvedAt?: string;
}

export const getGrievanceDetail = async (id: number): Promise<GrievanceDetail> => {
  const { data } = await apiClient.get<GrievanceDetail>(`/grievances/${id}`);
  return data;
};

export const createGrievance = async (req: {
  title: string; description: string; reportedBy?: string;
  reporterPhone?: string; priority: string; ward?: string; location?: string;
}) => {
  const { data } = await apiClient.post('/grievances', req);
  return data;
};

export const updateGrievanceStatus = async (
  id: number, status: string, resolutionNotes?: string
): Promise<void> => {
  await apiClient.patch(`/grievances/${id}/status`, { status, resolutionNotes });
};
