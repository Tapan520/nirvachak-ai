import apiClient from './client';

export interface ChecklistItem {
  boothNumber: number;
  boothName: string;
  address?: string | null;
  wardNumber?: string | null;
  assignedAgentName?: string | null;
  assignedAgentPhone?: string | null;
  agentPresent: boolean;
  bannerDisplayed: boolean;
  voterListPrinted: boolean;
  transportArranged: boolean;
  phoneCharged: boolean;
  boothClean: boolean;
  notes?: string | null;
  submittedByName?: string | null;
  submittedAt?: string | null;
  updatedAt?: string | null;
  isReady: boolean;
}

export interface ChecklistResponse {
  total: number;
  ready: number;
  items: ChecklistItem[];
}

export interface SaveChecklistRequest {
  boothNumber: number;
  agentPresent: boolean;
  bannerDisplayed: boolean;
  voterListPrinted: boolean;
  transportArranged: boolean;
  phoneCharged: boolean;
  boothClean: boolean;
  notes?: string | null;
}

export const getBoothChecklist = async (): Promise<ChecklistResponse> => {
  const { data } = await apiClient.get<ChecklistResponse>('/boothchecklist');
  return data;
};

export const saveBoothChecklist = async (req: SaveChecklistRequest): Promise<void> => {
  await apiClient.post('/boothchecklist', req);
};
