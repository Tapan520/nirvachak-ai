import apiClient from './client';

export interface SwingVoter {
  id: number;
  voterId: string;
  name: string;
  mobileNumber?: string | null;
  boothNumber: number;
  wardNumber?: string | null;
  pannaNumber?: string | null;
  currentSentiment: 'Against' | 'Floating';
  favourVisitCount: number;
  totalVisitCount: number;
  lastVisitedAt?: string | null;
  lastWorkerName?: string | null;
}

export interface SwingVotersResponse {
  total: number;
  critical: number;
  floating: number;
  items: SwingVoter[];
}

export const getSwingVoters = async (params?: {
  sentiment?: 'Against' | 'Floating';
  booth?: number;
  ward?: string;
}): Promise<SwingVotersResponse> => {
  const { data } = await apiClient.get<SwingVotersResponse>('/swingvoters', { params });
  return data;
};
