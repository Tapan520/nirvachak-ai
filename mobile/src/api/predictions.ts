import apiClient from './client';

export interface BoothPrediction {
  boothNumber: number;
  boothName: string;
  totalVoters: number;
  favourVoters: number;
  againstVoters: number;
  floatingVoters: number;
  contactedVoters: number;
  recentVisits: number;
  contactRate: number;
  predictedTurnoutPercent: number;
  predictedSupportPercent: number;
  estimatedFavourVotes: number;
  turnoutRisk: 'Low' | 'Medium' | 'High';
  supportConfidence: 'Weak' | 'Moderate' | 'Strong';
  strategyAlerts: string[];
}

export interface PredictionSummary {
  totalVoters: number;
  totalContacted: number;
  totalFavour: number;
  totalFloating: number;
  predictedOverallTurnout: number;
  predictedOverallSupport: number;
  estimatedTotalFavourVotes: number;
  atRiskBoothCount: number;
  weakSupportBoothCount: number;
  boothPredictions: BoothPrediction[];
}

export const getPredictions = async (): Promise<PredictionSummary> => {
  const { data } = await apiClient.get<PredictionSummary>('/predictiveanalytics/predictions');
  return data;
};
