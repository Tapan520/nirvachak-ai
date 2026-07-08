import apiClient from './client';

export interface CompetitorActivityItem {
  id: number;
  competitorName: string;
  partyName?: string;
  activityTitle: string;
  activityType: string;
  location?: string;
  ward?: string;
  boothNumber?: number;
  activityDate: string;
  estimatedCrowd?: number;
  notes?: string;
  threatLevel: string;
}

export const ACTIVITY_TYPES = [
  'Rally', 'RoadShow', 'DoorToDoor', 'SmallMeeting',
  'Announcement', 'MediaCoverage', 'SocialMedia', 'Other',
];

export const THREAT_LEVELS = [
  { key: 'Low',      label: 'Low',      color: '#2f9e44' },
  { key: 'Medium',   label: 'Medium',   color: '#f59f00' },
  { key: 'High',     label: 'High',     color: '#e67700' },
  { key: 'Critical', label: 'Critical', color: '#e03131' },
];

export const getCompetitorActivities = async (
  competitor?: string,
  threat?: string
): Promise<CompetitorActivityItem[]> => {
  const { data } = await apiClient.get<CompetitorActivityItem[]>('/competitor', {
    params: { competitor, threat },
  });
  return data;
};

export const logCompetitorActivity = async (req: {
  competitorName: string;
  partyName?: string;
  activityTitle: string;
  activityType: string;
  location?: string;
  ward?: string;
  boothNumber?: number;
  activityDate: string;
  estimatedCrowd?: number;
  notes?: string;
  threatLevel: string;
}): Promise<void> => {
  await apiClient.post('/competitor', req);
};
