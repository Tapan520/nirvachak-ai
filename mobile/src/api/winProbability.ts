import apiClient from './client';

export interface WinProbabilityData {
  score: number;
  tier: string;
  tierColor: string;
  totalVoters: number;
  favourVoters: number;
  floatingVoters: number;
  againstVoters: number;
  contactedVoters: number;
  contactCoverage: number;
  favourRate: number;
  floatingConversionPotential: number;
  estimatedWinVotes: number;
  boothsAtRisk: number;
  strengthPoints: string[];
  riskPoints: string[];
}

export const getWinProbability = async (): Promise<WinProbabilityData> => {
  const { data } = await apiClient.get<WinProbabilityData>('/winprobability');
  return data;
};
