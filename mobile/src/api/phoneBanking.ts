import apiClient from './client';

export interface PhoneCallItem {
  id: number;
  voterId: number;
  voterName: string;
  phone?: string;
  calledAt: string;
  outcome: string;
  durationSeconds: number;
  notes?: string;
  sentimentAfterCall?: string;
}

export interface PendingCallVoter {
  id: number;
  name: string;
  phone: string;
  boothNumber: number;
  wardNumber?: string;
  sentiment: string;
}

export interface PhoneBankingStats {
  totalCallsToday: number;
  talkedCount: number;
  noAnswerCount: number;
  callBackCount: number;
  recentCalls: PhoneCallItem[];
  pendingVoters: PendingCallVoter[];
}

export const CALL_OUTCOMES = [
  { key: 'Talked',   label: 'Talked',      color: '#2f9e44' },
  { key: 'NoAnswer', label: 'No Answer',   color: '#868e96' },
  { key: 'CallBack', label: 'Call Back',   color: '#f59f00' },
  { key: 'Wrong',    label: 'Wrong Number', color: '#e03131' },
  { key: 'Refused',  label: 'Refused',     color: '#7950f2' },
];

export const SENTIMENTS = [
  { key: 'Favour',   label: 'In Favour',  color: '#2f9e44' },
  { key: 'Against',  label: 'Against',    color: '#e03131' },
  { key: 'Neutral',  label: 'Neutral',    color: '#868e96' },
  { key: 'Floating', label: 'Floating',   color: '#f59f00' },
  { key: 'Unknown',  label: 'Unknown',    color: '#adb5bd' },
];

export const getPhoneBankingStats = async (): Promise<PhoneBankingStats> => {
  const { data } = await apiClient.get<PhoneBankingStats>('/phonebanking/stats');
  return data;
};

export const logCall = async (req: {
  voterId: number;
  outcome: string;
  durationSeconds: number;
  notes?: string;
  sentimentAfterCall?: string;
}): Promise<void> => {
  await apiClient.post('/phonebanking/log', req);
};

export const searchVoters = async (q: string): Promise<PendingCallVoter[]> => {
  const { data } = await apiClient.get<PendingCallVoter[]>('/phonebanking/search', {
    params: { q },
  });
  return data;
};
