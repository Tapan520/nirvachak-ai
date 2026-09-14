import apiClient from './client';

export interface BoothHeat {
  boothNumber: number;
  boothName: string;
  wardNumber?: string | null;
  assignedAgentName?: string | null;
  assignedAgentPhone?: string | null;
  totalVoters: number;
  contactedVoters: number;
  favourVoters: number;
  againstVoters: number;
  floatingVoters: number;
  neutralVoters: number;
  unknownVoters: number;
  visitsThisWeek: number;
  coveragePercent: number;
  favourPercent: number;
  heatColor: 'green' | 'yellow' | 'red';
  heatLabel: 'Strong' | 'Moderate' | 'Weak';
}

export interface HeatMapResponse {
  totalVoters: number;
  totalContacted: number;
  totalFavour: number;
  totalSwing: number;
  green: number;
  yellow: number;
  red: number;
  booths: BoothHeat[];
}

export type HeatSort = 'booth' | 'coverage_asc' | 'coverage_desc' | 'favour_desc' | 'heat';

export const getBoothHeatMap = async (sort: HeatSort = 'booth'): Promise<HeatMapResponse> => {
  const { data } = await apiClient.get<HeatMapResponse>('/boothheatmap', { params: { sort } });
  return data;
};
