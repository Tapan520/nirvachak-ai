import apiClient from './client';

export interface BudgetItem {
  id: number;
  category: string;
  plannedAmount: number;
  spentAmount: number;
  remaining: number;
  utilisationPercent: number;
  notes?: string;
}

export const EXPENSE_CATEGORIES = [
  'Publicity', 'Transport', 'Food', 'Communication', 'Printing', 'Miscellaneous',
];

export const getBudget = async (): Promise<BudgetItem[]> => {
  const { data } = await apiClient.get<BudgetItem[]>('/budget');
  return data;
};

export const setBudgetItem = async (req: {
  category: string; plannedAmount: number; notes?: string;
}): Promise<void> => {
  await apiClient.post('/budget', req);
};
