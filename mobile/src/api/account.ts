import apiClient from './client';

export interface TwoFactorStatus {
  isEnabled: boolean;
  sharedKey: string;
  authenticatorUri: string;
}

export const get2FA = async (): Promise<TwoFactorStatus> => {
  const { data } = await apiClient.get<TwoFactorStatus>('/account/2fa');
  return data;
};

export const enable2FA = (code: string) => apiClient.post('/account/2fa/enable', { code });
export const disable2FA = () => apiClient.post('/account/2fa/disable');
