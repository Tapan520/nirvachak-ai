import apiClient from './client';

export interface WhatsAppTemplate {
  id: number;
  title: string;
  body: string;
  language: string;
  category: string;
}

export const getWhatsAppTemplates = async (): Promise<WhatsAppTemplate[]> => {
  const { data } = await apiClient.get<WhatsAppTemplate[]>('/mobile/whatsapp-templates');
  return data;
};
