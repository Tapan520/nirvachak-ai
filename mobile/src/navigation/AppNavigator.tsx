import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { ActivityIndicator, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import LoginScreen from '../screens/LoginScreen';
import DashboardScreen from '../screens/DashboardScreen';
import VoterListScreen from '../screens/VoterListScreen';
import VoterDetailScreen from '../screens/VoterDetailScreen';
import ElectionDayScreen from '../screens/ElectionDayScreen';
import BoothsScreen from '../screens/BoothsScreen';
import GrievancesScreen from '../screens/GrievancesScreen';
import AddGrievanceScreen from '../screens/AddGrievanceScreen';
import GrievanceDetailScreen from '../screens/GrievanceDetailScreen';
import VolunteersScreen from '../screens/VolunteersScreen';
import CampaignEventsScreen from '../screens/CampaignEventsScreen';
import AnalyticsScreen from '../screens/AnalyticsScreen';
import SurveysScreen from '../screens/SurveysScreen';
import ExpensesScreen from '../screens/ExpensesScreen';
import MoreScreen from '../screens/MoreScreen';
import AnnouncementsScreen from '../screens/AnnouncementsScreen';
import VoterConsentScreen from '../screens/VoterConsentScreen';
import EditVoterSurveyScreen from '../screens/EditVoterSurveyScreen';

const Stack = createNativeStackNavigator();
const Tab = createBottomTabNavigator();
const BRAND = '#3b5bdb';

const TAB_ICONS: Record<string, string> = {
  Dashboard:    'speedometer-outline',
  Voters:       'people-outline',
  'Election Day': 'checkmark-circle-outline',
  Grievances:   'alert-circle-outline',
  More:         'grid-outline',
};

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: BRAND,
        tabBarInactiveTintColor: '#adb5bd',
        tabBarStyle: { backgroundColor: '#fff', borderTopColor: '#f1f3f5' },
        tabBarIcon: ({ color, size }) => (
          <Ionicons
            name={(TAB_ICONS[route.name] ?? 'ellipse-outline') as any}
            size={size}
            color={color}
          />
        ),
      })}
    >
      <Tab.Screen name="Dashboard"    component={DashboardScreen} />
      <Tab.Screen name="Voters"       component={VoterListScreen} />
      <Tab.Screen name="Election Day" component={ElectionDayScreen} />
      <Tab.Screen name="Grievances"   component={GrievancesScreen} />
      <Tab.Screen name="More"         component={MoreScreen} />
    </Tab.Navigator>
  );
}

export default function AppNavigator() {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: '#1a1f2e' }}>
        <ActivityIndicator color={BRAND} size="large" />
      </View>
    );
  }

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {user ? (
          <>
            <Stack.Screen name="Main" component={MainTabs} />
            {/* Voter Stack */}
            <Stack.Screen name="VoterDetail" component={VoterDetailScreen}
              options={{ headerShown: true, title: 'Voter Details', headerTintColor: BRAND }} />
            {/* Grievance Stack */}
            <Stack.Screen name="AddGrievance" component={AddGrievanceScreen}
              options={{ headerShown: true, title: 'Add Grievance', headerTintColor: BRAND }} />
            <Stack.Screen name="GrievanceDetail" component={GrievanceDetailScreen}
              options={{ headerShown: true, title: 'Grievance Details', headerTintColor: BRAND }} />
            {/* More ? full screens */}
            <Stack.Screen name="Booths" component={BoothsScreen}
              options={{ headerShown: true, title: 'Booth Management', headerTintColor: BRAND }} />
            <Stack.Screen name="Volunteers" component={VolunteersScreen}
              options={{ headerShown: true, title: 'Volunteers', headerTintColor: BRAND }} />
            <Stack.Screen name="CampaignEvents" component={CampaignEventsScreen}
              options={{ headerShown: true, title: 'Campaign Events', headerTintColor: BRAND }} />
            <Stack.Screen name="Analytics" component={AnalyticsScreen}
              options={{ headerShown: true, title: 'Analytics', headerTintColor: BRAND }} />
            <Stack.Screen name="Surveys" component={SurveysScreen}
              options={{ headerShown: true, title: 'Surveys', headerTintColor: BRAND }} />
            <Stack.Screen name="Expenses" component={ExpensesScreen}
              options={{ headerShown: true, title: 'Expenses', headerTintColor: BRAND }} />
            <Stack.Screen name="Announcements" component={AnnouncementsScreen}
              options={{ headerShown: false }} />
            <Stack.Screen name="VoterConsent" component={VoterConsentScreen}
              options={{ headerShown: true, title: 'Voter Consent Analytics', headerTintColor: BRAND }} />
            <Stack.Screen name="EditVoterSurvey" component={EditVoterSurveyScreen}
              options={{ headerShown: true, title: 'Edit Survey Profile', headerTintColor: BRAND }} />
          </>
        ) : (
          <Stack.Screen name="Login" component={LoginScreen} />
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}
