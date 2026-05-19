import apiClient from './client';

export interface SurveyItem {
  id: number;
  title: string;
  description?: string;
  category: string;
  isActive: boolean;
  responseCount: number;
  createdAt: string;
}

export const getSurveys = async (): Promise<SurveyItem[]> => {
  const { data } = await apiClient.get<SurveyItem[]>('/surveys');
  return data;
};

export const submitSurveyResponse = async (
  surveyId: number,
  req: {
    respondentName?: string; respondentPhone?: string;
    ward?: string; boothNumber?: number;
    rating: number; feedback?: string;
  }
): Promise<void> => {
  await apiClient.post(`/surveys/${surveyId}/respond`, req);
};
