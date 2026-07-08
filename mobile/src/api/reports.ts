import apiClient from './client';
import { ExpenseItem } from './expenses';

export interface ExpenseReport {
  totalAmount: number;
  ecBudgetLimit: number;
  ecBudgetPercent: number;
  categoryTotals: { category: string; amount: number; percent: number }[];
  expenses: ExpenseItem[];
}

export const getExpenseReport = async (from?: string, to?: string): Promise<ExpenseReport> => {
  const { data } = await apiClient.get<ExpenseReport>('/reports/expenses', {
    params: { from, to },
  });
  return data;
};
