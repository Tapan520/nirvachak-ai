import apiClient from './client';

export interface CampaignEventItem {
  id: number;
  title: string;
  eventType: string;
  location: string;
  scheduledAt: string;
  expectedAttendance?: number;
  actualAttendance?: number;
  organizedByName?: string;
  isCompleted: boolean;
  targetWards?: string;
  description?: string;
}

export const getCampaignEvents = async (upcoming?: boolean): Promise<CampaignEventItem[]> => {
  const { data } = await apiClient.get<CampaignEventItem[]>('/campaignevents',
    { params: upcoming ? { upcoming: true } : {} });
  return data;
};

export const createCampaignEvent = async (req: {
  title: string; eventType: string; location: string;
  scheduledAt: string; expectedAttendance?: number;
  description?: string; targetWards?: string; targetBoothNumbers?: string;
}): Promise<void> => {
  await apiClient.post('/campaignevents', req);
};
