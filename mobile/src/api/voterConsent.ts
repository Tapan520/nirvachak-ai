import apiClient from './client';

export interface VoterConsentStats {
  totalVoters:       number;
  completedCount:    number;
  pendingCount:      number;
  completionRate:    number;
  couponsIssued:     number;
  couponsRedeemed:   number;
  consentThirdParty: number;
  consentCampaign:   number;
  consentWhatsApp:   number;
  consentScheme:     number;
  consentAnalytics:  number;
  availableBooths:   number[];
  availableWards:    string[];
  completionByBooth: { boothNumber: number; count: number }[];
}

export interface SurveyCompletedVoter {
  id:           number;
  name:         string;
  voterEpic:    string;
  mobileNumber?: string;
  boothNumber:  number;
  wardNumber?:  string;
  completedAt:  string;
  hasCoupon:    boolean;
  couponCode?:  string;
}

export interface SurveyPendingVoter {
  id:           number;
  name:         string;
  voterEpic:    string;
  mobileNumber?: string;
  boothNumber:  number;
  wardNumber?:  string;
}

export interface PagedResult<T> {
  items:      T[];
  total:      number;
  page:       number;
  pageSize:   number;
  totalPages: number;
}

export const getConsentStats = async (
  booth?: number,
  ward?: string,
): Promise<VoterConsentStats> => {
  const { data } = await apiClient.get<VoterConsentStats>('/voterConsent/stats', {
    params: { booth, ward },
  });
  return data;
};

export const getCompletedVoters = async (params: {
  booth?: number; ward?: string; search?: string; page?: number; pageSize?: number;
}): Promise<PagedResult<SurveyCompletedVoter>> => {
  const { data } = await apiClient.get<PagedResult<SurveyCompletedVoter>>(
    '/voterConsent/completed', { params },
  );
  return data;
};

export const getPendingVoters = async (params: {
  booth?: number; ward?: string; search?: string; page?: number; pageSize?: number;
}): Promise<PagedResult<SurveyPendingVoter>> => {
  const { data } = await apiClient.get<PagedResult<SurveyPendingVoter>>(
    '/voterConsent/pending', { params },
  );
  return data;
};
