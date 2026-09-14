import apiClient from './client';

export interface Bucket { label: string; count: number; }

export interface DemographicsResponse {
  totalVoters: number;
  completedCount: number;
  pendingCount: number;
  completionRate: number;
  couponsIssued: number;
  couponsRedeemed: number;
  consentThirdParty: number;
  consentCampaign: number;
  consentWhatsApp: number;
  consentScheme: number;
  consentAnalytics: number;
  byCaste: Bucket[];
  byReligion: Bucket[];
  byEducation: Bucket[];
  byOccupation: Bucket[];
  byIncome: Bucket[];
  byAge: Bucket[];
  topConcerns: Bucket[];
}

export const getSurveyDemographics = async (params?: {
  booth?: number; ward?: string;
}): Promise<DemographicsResponse> => {
  const { data } = await apiClient.get<DemographicsResponse>('/surveydemographics', { params });
  return data;
};
