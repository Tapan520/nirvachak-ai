import apiClient from './client';

export interface LeaderboardRow {
  userId: string;
  fullName: string;
  role: string;
  assignedBooths?: string | null;
  visits: number;
  calls: number;
  favourConversions: number;
  totalScore: number;
  totalVisitsAllTime: number;
  totalCallsAllTime: number;
  lastActivityAt?: string | null;
  rank: number;
  isCurrentUser: boolean;
}

export interface LeaderboardResponse {
  period: string;
  periodLabel: string;
  rows: LeaderboardRow[];
}

export type LeaderboardPeriod = 'today' | 'week' | 'month' | 'alltime';

export const getLeaderboard = async (period: LeaderboardPeriod = 'week'): Promise<LeaderboardResponse> => {
  const { data } = await apiClient.get<LeaderboardResponse>('/leaderboard', { params: { period } });
  return data;
};
