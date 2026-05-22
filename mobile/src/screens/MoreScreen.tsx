import React, { useEffect, useState } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, ScrollView } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { useAuth } from '../context/AuthContext';
import { getUnreadCount } from '../api/announcements';

interface MenuItemProps {
  icon: string; label: string; desc: string; color: string; screen: string;
}

function MenuItem({ icon, label, desc, color, screen }: MenuItemProps) {
  const nav = useNavigation<any>();
  return (
    <TouchableOpacity style={m.item} onPress={() => nav.navigate(screen)}>
      <View style={[m.iconBox, { backgroundColor: color + '18' }]}>
        <Ionicons name={icon as any} size={24} color={color} />
      </View>
      <View style={{ flex: 1, marginLeft: 14 }}>
        <Text style={m.label}>{label}</Text>
        <Text style={m.desc}>{desc}</Text>
      </View>
      <Ionicons name="chevron-forward" size={18} color="#adb5bd" />
    </TouchableOpacity>
  );
}

const m = StyleSheet.create({
  item: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff',
    borderRadius: 12, padding: 14, marginBottom: 10, elevation: 1 },
  iconBox: { width: 48, height: 48, borderRadius: 12, justifyContent: 'center', alignItems: 'center' },
  label: { fontSize: 15, fontWeight: '700', color: '#212529' },
  desc: { fontSize: 12, color: '#868e96', marginTop: 2 },
});

export default function MoreScreen() {
  const { user, logout } = useAuth();
  const nav = useNavigation<any>();
  const [unread, setUnread] = useState(0);

  useEffect(() => {
    getUnreadCount().then(setUnread).catch(() => {});
  }, []);

  return (
    <ScrollView style={s.container}>
      <View style={s.header}>
        <View style={s.avatar}>
          <Text style={s.avatarTxt}>{user?.fullName?.[0] ?? '?'}</Text>
        </View>
        <View style={{ flex: 1, marginLeft: 12 }}>
          <Text style={s.name}>{user?.fullName}</Text>
          <Text style={s.role}>{user?.role}</Text>
        </View>
      </View>

      <Text style={s.sectionTitle}>Modules</Text>
      <View style={s.section}>
        {/* Announcements — with unread badge */}
        <TouchableOpacity style={m.item} onPress={() => nav.navigate('Announcements')}>
          <View style={[m.iconBox, { backgroundColor: '#e03131' + '18' }]}>
            <Ionicons name="megaphone-outline" size={24} color="#e03131" />
          </View>
          <View style={{ flex: 1, marginLeft: 14 }}>
            <Text style={m.label}>Announcements</Text>
            <Text style={m.desc}>Campaign alerts & team broadcasts</Text>
          </View>
          {unread > 0 && (
            <View style={s.badge}>
              <Text style={s.badgeTxt}>{unread}</Text>
            </View>
          )}
          <Ionicons name="chevron-forward" size={18} color="#adb5bd" />
        </TouchableOpacity>
        <MenuItem icon="people-outline"        label="Volunteers"       desc="Field volunteer directory"        color="#3b5bdb" screen="Volunteers" />
        <MenuItem icon="megaphone-outline"     label="Campaign Events"  desc="Rallies, meetings & activities"   color="#e03131" screen="CampaignEvents" />
        <MenuItem icon="stats-chart-outline"   label="Analytics"        desc="Sentiment & voter insights"       color="#7950f2" screen="Analytics" />
        <MenuItem icon="clipboard-outline"     label="Surveys"          desc="Active surveys & responses"       color="#f59f00" screen="Surveys" />
        <MenuItem icon="wallet-outline"        label="Expenses"         desc="Campaign expense tracker"         color="#2f9e44" screen="Expenses" />
        <MenuItem icon="clipboard-check-outline" label="Voter Consent"   desc="Survey completions & pending outreach" color="#0c8599" screen="VoterConsent" />
        {(user?.role === 'Admin' || user?.role === 'CampaignManager' || user?.role === 'Candidate') && (
          <MenuItem icon="trending-up-outline" label="Predictive Analytics" desc="AI forecasts for turnout & support" color="#e67700" screen="PredictiveAnalytics" />
        )}
      </View>

      <Text style={s.sectionTitle}>Account</Text>
      <View style={s.section}>
        <TouchableOpacity style={m.item} onPress={logout}>
          <View style={[m.iconBox, { backgroundColor: '#fff0f0' }]}>
            <Ionicons name="log-out-outline" size={24} color="#e03131" />
          </View>
          <View style={{ flex: 1, marginLeft: 14 }}>
            <Text style={[m.label, { color: '#e03131' }]}>Sign Out</Text>
            <Text style={m.desc}>Log out of your account</Text>
          </View>
        </TouchableOpacity>
      </View>
    </ScrollView>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header: { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 20,
    paddingHorizontal: 16, flexDirection: 'row', alignItems: 'center' },
  avatar: { width: 52, height: 52, borderRadius: 26, backgroundColor: '#3b5bdb',
    justifyContent: 'center', alignItems: 'center' },
  avatarTxt: { color: '#fff', fontSize: 22, fontWeight: '800' },
  name: { color: '#fff', fontSize: 17, fontWeight: '700' },
  role: { color: '#868e96', fontSize: 12, marginTop: 2 },
  sectionTitle: { fontSize: 12, fontWeight: '700', color: '#868e96',
    textTransform: 'uppercase', letterSpacing: 1, marginHorizontal: 16,
    marginTop: 20, marginBottom: 8 },
  section: { marginHorizontal: 12 },
  badge:   { backgroundColor: '#f59f00', borderRadius: 10, minWidth: 20, height: 20,
             justifyContent: 'center', alignItems: 'center', paddingHorizontal: 5, marginRight: 6 },
  badgeTxt:{ color: '#fff', fontSize: 11, fontWeight: '800' },
});
