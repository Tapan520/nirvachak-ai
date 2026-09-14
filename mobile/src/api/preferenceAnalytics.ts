import apiClient from './client';

export interface PreferenceRow {
  id: number;
  name: string;
  subText?: string | null;
  count: number;
  percent: number;
}
export interface BoothPreference {
  boothNumber: number;
  totalResponses: number;
  topCandidate: string;
  topCount: number;
  topPct: number;
}
export interface PreferenceResponse {
  totalResponses: number;
  candidates: PreferenceRow[];
  candidateNoPreference: number;
  parties: PreferenceRow[];
  partyNoPreference: number;
  byBooth: BoothPreference[];
  ticketRecommendation?: string | null;
  ticketRecommendationReason?: string | null;
}

export const getPreferenceAnalytics = async (): Promise<PreferenceResponse> => {
  const { data } = await apiClient.get<PreferenceResponse>('/preferenceanalytics');
  return data;
};
