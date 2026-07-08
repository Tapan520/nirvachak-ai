import apiClient from './client';

export interface InfluencerItem {
  id: number;
  name: string;
  mobileNumber?: string;
  category?: string;
  community?: string;
  estimatedFollowers?: number;
  ward?: string;
  boothNumber?: number;
  alignment: string;
  notes?: string;
  lastMetAt?: string;
  lastMeetingOutcome?: string;
}

export const ALIGNMENTS = [
  { key: 'Favour',   label: 'In Favour', color: '#2f9e44' },
  { key: 'Against',  label: 'Against',   color: '#e03131' },
  { key: 'Neutral',  label: 'Neutral',   color: '#868e96' },
  { key: 'Floating', label: 'Floating',  color: '#f59f00' },
  { key: 'Unknown',  label: 'Unknown',   color: '#adb5bd' },
];

export const INFLUENCER_CATEGORIES = [
  'Religious', 'Caste', 'Youth', 'Women', 'Farmer', 'Business', 'Media', 'Political', 'Other',
];

export const getInfluencers = async (alignment?: string): Promise<InfluencerItem[]> => {
  const { data } = await apiClient.get<InfluencerItem[]>('/influencers', {
    params: alignment ? { alignment } : {},
  });
  return data;
};

export const createInfluencer = async (req: {
  name: string;
  mobileNumber?: string;
  category?: string;
  community?: string;
  estimatedFollowers?: number;
  ward?: string;
  boothNumber?: number;
  alignment: string;
  notes?: string;
}): Promise<void> => {
  await apiClient.post('/influencers', req);
};

export const updateInfluencerMeeting = async (
  id: number,
  req: { alignment: string; outcomeNotes?: string; notes?: string }
): Promise<void> => {
  await apiClient.put(`/influencers/${id}/meeting`, req);
};
