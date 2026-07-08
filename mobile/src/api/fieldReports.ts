import apiClient from './client';

export interface FieldReportItem {
  id: number;
  workerName: string;
  reportDate: string;
  contactsMade: number;
  favourContacts: number;
  floatingContacts: number;
  againstContacts: number;
  issuesLogged: number;
  highlights?: string;
  challenges?: string;
  plannedForTomorrow?: string;
  status: string;
}

export const getFieldReports = async (): Promise<FieldReportItem[]> => {
  const { data } = await apiClient.get<FieldReportItem[]>('/fieldreports');
  return data;
};

export const submitFieldReport = async (req: {
  contactsMade: number;
  favourContacts: number;
  floatingContacts: number;
  againstContacts: number;
  issuesLogged: number;
  highlights?: string;
  challenges?: string;
  plannedForTomorrow?: string;
}): Promise<void> => {
  await apiClient.post('/fieldreports', req);
};
