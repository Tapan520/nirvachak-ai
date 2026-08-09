import React, { useEffect, useState } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, ScrollView } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { useAuth } from '../context/AuthContext';
import { useOfflineSync } from '../context/OfflineSyncContext';
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
const { isOnline, pendingCount, syncNow } = useOfflineSync();
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
          {/* Offline / sync status */}
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, marginTop: 4 }}>
            <View style={{ width: 8, height: 8, borderRadius: 4,
              backgroundColor: isOnline ? '#2f9e44' : '#e67700' }} />
            <Text style={{ color: isOnline ? '#2f9e44' : '#e67700', fontSize: 11, fontWeight: '600' }}>
              {isOnline ? 'Online' : `Offline — ${pendingCount} queued`}
            </Text>
            {isOnline && pendingCount > 0 && (
              <TouchableOpacity onPress={() => syncNow()} style={{ marginLeft: 4 }}>
                <Text style={{ color: '#3b5bdb', fontSize: 11, fontWeight: '700' }}>Sync now</Text>
              </TouchableOpacity>
            )}
          </View>
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
        <MenuItem icon="call-outline"           label="Phone Banking"       desc="Call floating voters & log outcomes"       color="#1971c2" screen="PhoneBanking" />
        <MenuItem icon="logo-whatsapp"          label="WhatsApp Outreach"   desc="Send templated messages to voters"         color="#25D366" screen="WhatsAppOutreach" />
        <MenuItem icon="map-outline"            label="Volunteer Map"       desc="Live field worker location tracking"       color="#1971c2" screen="VolunteerMap" />
        {/* Voter Slips – not required on mobile */}
        {/* <MenuItem icon="card-outline"           label="Voter Slips"         desc="Browse & filter voter slip records"        color="#3b5bdb" screen="VoterSlips" /> */}
        <MenuItem icon="people-circle-outline"  label="Panna Pramukh"       desc="Panna-level voter coverage tracker"        color="#0c8599" screen="PannaPramukh" />
        <MenuItem icon="car-outline"            label="Transport"           desc="Voter pick-up & vehicle management"        color="#f59f00" screen="Transport" />
        <MenuItem icon="document-text-outline"  label="Field Reports"       desc="Submit & review daily field reports"       color="#2f9e44" screen="FieldReports" />
        <MenuItem icon="calendar-outline"       label="Booth Shifts"        desc="Assign & confirm booth shift rosters"      color="#1971c2" screen="BoothShifts" />
        {(user?.role === 'Admin' || user?.role === 'CampaignManager' || user?.role === 'Candidate' || user?.role === 'SuperAdmin') && (
          <>
            <MenuItem icon="trending-up-outline"  label="Predictive Analytics" desc="AI forecasts for turnout & support"       color="#e67700" screen="PredictiveAnalytics" />
            <MenuItem icon="trophy-outline"        label="Win Probability"      desc="AI win probability score & campaign analysis" color="#f59f00" screen="WinProbability" />
            <MenuItem icon="people-circle-outline" label="Influencers"          desc="Track community & political influencers"  color="#7950f2" screen="Influencers" />
            <MenuItem icon="eye-outline"           label="Competitor Tracker"   desc="Log & monitor rival campaign activities"  color="#e03131" screen="Competitor" />
            <MenuItem icon="shield-outline"        label="Rapid Response"       desc="Log & resolve campaign incidents"         color="#e03131" screen="RapidResponse" />
            <MenuItem icon="send-outline"          label="Broadcast"            desc="Message templates & voter broadcasts"     color="#e67700" screen="Broadcast" />
            <MenuItem icon="wallet-outline"        label="Budget Planner"       desc="Plan & track category-wise spend"         color="#e67700" screen="Budget" />
            <MenuItem icon="bar-chart-outline"     label="Expense Reports"      desc="EC budget utilisation summary"            color="#1971c2" screen="Reports" />
            <MenuItem icon="podium-outline"        label="Election Results"     desc="Enter & track counting round results"     color="#1971c2" screen="Results" />
          </>
        )}
        {(user?.role === 'Admin' || user?.role === 'SuperAdmin' || user?.role === 'CampaignManager') && (
          <MenuItem icon="settings-outline"      label="User Management"     desc="Manage team accounts & access"            color="#212529" screen="Admin" />
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
