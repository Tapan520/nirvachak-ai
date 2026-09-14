import apiClient from './client';

export interface CandidateItem {
  id: number;
  name: string;
  partyAffiliation?: string | null;
  photoUrl?: string | null;
  notes?: string | null;
  displayOrder: number;
  isActive: boolean;
}
export interface PartyItem {
  id: number;
  name: string;
  symbol?: string | null;
  notes?: string | null;
  isActive: boolean;
}
export interface CatalogResponse {
  candidates: CandidateItem[];
  parties: PartyItem[];
}

export const getCatalog = async (): Promise<CatalogResponse> => {
  const { data } = await apiClient.get<CatalogResponse>('/candidatesparties');
  return data;
};

export const addCandidate = (req: { name: string; partyAffiliation?: string | null; notes?: string | null; displayOrder: number; }) =>
  apiClient.post('/candidatesparties/candidates', req);

export const toggleCandidate = (id: number) =>
  apiClient.post(`/candidatesparties/candidates/${id}/toggle`);

export const deleteCandidate = (id: number) =>
  apiClient.delete(`/candidatesparties/candidates/${id}`);

export const setCandidateOrder = (id: number, order: number) =>
  apiClient.post(`/candidatesparties/candidates/${id}/order`, order,
    { headers: { 'Content-Type': 'application/json' } });

export const addParty = (req: { name: string; symbol?: string | null; notes?: string | null; }) =>
  apiClient.post('/candidatesparties/parties', req);

export const toggleParty = (id: number) =>
  apiClient.post(`/candidatesparties/parties/${id}/toggle`);

export const deleteParty = (id: number) =>
  apiClient.delete(`/candidatesparties/parties/${id}`);
