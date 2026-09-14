import apiClient from './client';

export interface RewardItem {
  id: number;
  title: string;
  partnerBrand?: string | null;
  description?: string | null;
  expiryDate: string;
  isActive: boolean;
  totalCoupons: number;
  issuedCount: number;
  redeemedCount: number;
}

export const getRewards = async (): Promise<RewardItem[]> => {
  const { data } = await apiClient.get<RewardItem[]>('/rewards');
  return data;
};

export const toggleReward = (id: number) => apiClient.post(`/rewards/${id}/toggle`);
