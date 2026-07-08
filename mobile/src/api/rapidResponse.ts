import apiClient from './client';

export interface RapidResponseItem {
  id: number;
  title: string;
  description: string;
  source?: string;
  affectedWards?: string;
  assignedToName?: string;
  responseText?: string;
  status: string;
  threatLevel: string;
  detectedAt: string;
  resolvedAt?: string;
}

export const THREAT_LEVELS = [
  { key: 'Low',      label: 'Low',      color: '#2f9e44' },
  { key: 'Medium',   label: 'Medium',   color: '#f59f00' },
  { key: 'High',     label: 'High',     color: '#e67700' },
  { key: 'Critical', label: 'Critical', color: '#e03131' },
];

export const RR_STATUSES = [
  { key: 'Detected',         label: 'Detected',          color: '#e03131' },
  { key: 'ResponseDrafted',  label: 'Response Drafted',  color: '#f59f00' },
  { key: 'Deployed',         label: 'Deployed',          color: '#3b5bdb' },
  { key: 'Resolved',         label: 'Resolved',          color: '#2f9e44' },
];

export const SOURCES = ['WhatsApp', 'Local Media', 'Competitor', 'Field Report', 'Other'];

export const getRapidResponseItems = async (status?: string): Promise<RapidResponseItem[]> => {
  const { data } = await apiClient.get<RapidResponseItem[]>('/rapidresponse', {
    params: status ? { status } : {},
  });
  return data;
};

export const createRapidResponse = async (req: {
  title: string; description: string; source?: string;
  affectedWards?: string; threatLevel: string; responseText?: string;
}): Promise<void> => {
  await apiClient.post('/rapidresponse', req);
};

export const updateRapidResponseStatus = async (
  id: number, status: string, responseText?: string
): Promise<void> => {
  await apiClient.put(`/rapidresponse/${id}/status`, { status, responseText });
};
