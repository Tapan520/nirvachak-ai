import apiClient from './client';

export interface VoterSlipItem {
  id: number;
  voterId: string;
  name: string;
  nameLocal?: string;
  boothNumber: number;
  wardNumber?: string;
  pannaNumber?: string;
  serialNumber: number;
  age: number;
  gender: string;
  address: string;
}

export interface VoterSlipsPage {
  items: VoterSlipItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const getVoterSlips = async (booth?: number, page = 1): Promise<VoterSlipsPage> => {
  const { data } = await apiClient.get<VoterSlipsPage>('/voterslips', {
    params: { booth, page },
  });
  return data;
};
