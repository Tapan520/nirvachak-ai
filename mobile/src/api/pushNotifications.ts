import { Platform } from 'react-native';
import apiClient from './client';

/**
 * Registers the device for Expo push notifications and sends the token to the backend.
 * Uses dynamic imports so the app still builds even if expo-notifications is not installed.
 */
export async function registerForPushNotificationsAsync(): Promise<string | null> {
  try {
    const Notifications = await import('expo-notifications' as string).catch(() => null) as any;
    const Device        = await import('expo-device' as string).catch(() => null) as any;

    if (!Notifications || !Device) {
      console.log('[Push] expo-notifications/expo-device not installed — skipping.');
      return null;
    }
    if (!Device.isDevice) {
      console.log('[Push] Push requires a physical device.');
      return null;
    }

    Notifications.setNotificationHandler({
      handleNotification: async () => ({
        shouldShowAlert: true,
        shouldPlaySound: true,
        shouldSetBadge:  true,
      }),
    });

    const { status: existing } = await Notifications.getPermissionsAsync();
    let finalStatus = existing;
    if (existing !== 'granted') {
      const { status } = await Notifications.requestPermissionsAsync();
      finalStatus = status;
    }
    if (finalStatus !== 'granted') return null;

    if (Platform.OS === 'android') {
      await Notifications.setNotificationChannelAsync('default', {
        name: 'default',
        importance: Notifications.AndroidImportance?.MAX ?? 5,
        vibrationPattern: [0, 250, 250, 250],
        lightColor: '#3b5bdb',
      });
    }

    const token = (await Notifications.getExpoPushTokenAsync()).data;
    await apiClient.post('/mobile/push-token', {
      token,
      platform: Platform.OS,
      deviceId: Device.deviceName ?? undefined,
    });
    return token;
  } catch (err) {
    console.warn('[Push] Token registration failed:', err);
    return null;
  }
}
