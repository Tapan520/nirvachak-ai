import apiClient from './client';

export interface MessageTemplateItem {
  id: number;
  title: string;
  body: string;
  language: string;
  category: string;
  createdAt: string;
}

export interface BroadcastItem {
  id: number;
  templateId: number;
  templateTitle: string;
  targetDescription?: string;
  totalTargeted: number;
  sentCount: number;
  status: string;
  scheduledAt?: string;
  sentAt?: string;
  createdByName: string;
  createdAt: string;
}

export const LANGUAGES = ['English', 'Hindi', 'Marathi'];
export const MSG_CATEGORIES = [
  'ElectionReminder', 'EventInvite', 'VoterOutreach', 'Announcement', 'ThankYou',
];

export const getTemplates = async (): Promise<MessageTemplateItem[]> => {
  const { data } = await apiClient.get<MessageTemplateItem[]>('/broadcast/templates');
  return data;
};

export const createTemplate = async (req: {
  title: string; body: string; language: string; category: string;
}): Promise<void> => {
  await apiClient.post('/broadcast/templates', req);
};

export const getBroadcasts = async (): Promise<BroadcastItem[]> => {
  const { data } = await apiClient.get<BroadcastItem[]>('/broadcast');
  return data;
};

export const createBroadcast = async (req: {
  templateId: number; targetDescription?: string; scheduledAt?: string;
}): Promise<void> => {
  await apiClient.post('/broadcast', req);
};
