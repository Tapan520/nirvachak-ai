import apiClient from './client';

export interface ElectionResultItem {
  id: number;
  boothNumber: number;
  roundNumber: number;
  candidateVotes: number;
  competitor1Votes?: number;
  competitor1Name?: string;
  competitor2Votes?: number;
  competitor2Name?: string;
  totalVotesCast?: number;
  isFinal: boolean;
  enteredAt: string;
}

export interface ElectionResultSummary {
  totalCandidateVotes: number;
  totalCompetitor1Votes: number;
  totalCompetitor2Votes: number;
  competitor1Name?: string;
  competitor2Name?: string;
  isLeading: boolean;
  leadMargin: number;
  results: ElectionResultItem[];
}

export const getResults = async (round?: number): Promise<ElectionResultSummary> => {
  const { data } = await apiClient.get<ElectionResultSummary>('/results', {
    params: round ? { round } : {},
  });
  return data;
};

export const addResult = async (req: {
  boothNumber: number; roundNumber: number; candidateVotes: number;
  competitor1Votes?: number; competitor1Name?: string;
  competitor2Votes?: number; competitor2Name?: string;
  totalVotesCast?: number; isFinal: boolean;
}): Promise<void> => {
  await apiClient.post('/results', req);
};

export const deleteResult = async (id: number): Promise<void> => {
  await apiClient.delete(`/results/${id}`);
};
