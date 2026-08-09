import React, { useEffect } from 'react';
import { StatusBar } from 'expo-status-bar';
import { AuthProvider, useAuth } from './src/context/AuthContext';
import { OfflineSyncProvider } from './src/context/OfflineSyncContext';
import { registerForPushNotificationsAsync } from './src/api/pushNotifications';
import AppNavigator from './src/navigation/AppNavigator';

function AppWithPush() {
  const { user } = useAuth();
  useEffect(() => {
    if (user) registerForPushNotificationsAsync();
  }, [user?.userId]);
  return <AppNavigator />;
}

export default function App() {
  return (
    <AuthProvider>
      <OfflineSyncProvider>
        <StatusBar style="light" />
        <AppWithPush />
      </OfflineSyncProvider>
    </AuthProvider>
  );
}

