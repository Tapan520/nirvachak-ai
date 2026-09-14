import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, FlatList, RefreshControl,
  TouchableOpacity, ActivityIndicator, Linking,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { getSwingVoters, SwingVoter, SwingVotersResponse } from '../api/swingVoters';

const BRAND = '#3b5bdb';
const TABS: { key: 'All' | 'Against' | 'Floating'; label: string; color: string }[] = [
  { key: 'All',      label: 'All',      color: BRAND },
  { key: 'Against',  label: 'Against',  color: '#e03131' },
  { key: 'Floating', label: 'Floating', color: '#f59f00' },
];

function fmtDate(iso?: string | null) {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleDateString();
}

export default function SwingVotersScreen() {
  const nav = useNavigation<any>();
  const [tab, setTab] = useState<'All' | 'Against' | 'Floating'>('All');
  const [data, setData] = useState<SwingVotersResponse>({ total: 0, critical: 0, floating: 0, items: [] });
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      const res = await getSwingVoters(tab === 'All' ? undefined : { sentiment: tab });
      setData(res);
    } catch {
      setData({ total: 0, critical: 0, floating: 0, items: [] });
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [tab]);

  useEffect(() => { setLoading(true); load(); }, [load]);

  const call = (phone?: string | null) => {
    if (!phone) return;
    Linking.openURL(`tel:${phone}`).catch(() => {});
  };

  const openDetail = (id: number) => {
    nav.navigate('VoterDetail', { voterId: id });
  };

  const renderItem = ({ item }: { item: SwingVoter }) => {
    const isAgainst = item.currentSentiment === 'Against';
    const badgeColor = isAgainst ? '#e03131' : '#f59f00';
    return (
      <TouchableOpacity onPress={() => openDetail(item.id)} style={s.card}>
        <View style={{ flexDirection: 'row', alignItems: 'flex-start' }}>
          <View style={{ flex: 1 }}>
            <Text style={s.name}>{item.name}</Text>
            <Text style={s.meta}>
              EPIC: {item.voterId} · Booth {item.boothNumber}
              {item.wardNumber ? ` · Ward ${item.wardNumber}` : ''}
              {item.pannaNumber ? ` · Panna ${item.pannaNumber}` : ''}
            </Text>
            <Text style={s.history}>
              Was Favour {item.favourVisitCount}× / {item.totalVisitCount} total visits · Last: {fmtDate(item.lastVisitedAt)}
              {item.lastWorkerName ? ` by ${item.lastWorkerName}` : ''}
            </Text>
          </View>
          <View style={[s.badge, { backgroundColor: badgeColor + '22', borderColor: badgeColor }]}>
            <Text style={[s.badgeTxt, { color: badgeColor }]}>{item.currentSentiment}</Text>
          </View>
        </View>
        {item.mobileNumber ? (
          <TouchableOpacity onPress={() => call(item.mobileNumber)} style={s.callBtn}>
            <Ionicons name="call" size={14} color="#2f9e44" />
            <Text style={s.callTxt}>Call {item.mobileNumber}</Text>
          </TouchableOpacity>
        ) : null}
      </TouchableOpacity>
    );
  };

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.title}>Swing Voters</Text>
        <Text style={s.sub}>Previously Favour ? now Floating / Against. Priority re-engagement.</Text>
      </View>

      <View style={s.statsRow}>
        <Stat label="Total"    value={data.total}    color={BRAND} />
        <Stat label="Against"  value={data.critical} color="#e03131" />
        <Stat label="Floating" value={data.floating} color="#f59f00" />
      </View>

      <View style={s.tabs}>
        {TABS.map(t => (
          <TouchableOpacity
            key={t.key}
            style={[s.tab, tab === t.key && { backgroundColor: t.color }]}
            onPress={() => setTab(t.key)}
          >
            <Text style={[s.tabTxt, tab === t.key && s.tabTxtActive]}>{t.label}</Text>
          </TouchableOpacity>
        ))}
      </View>

      {loading ? (
        <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
      ) : (
        <FlatList
          data={data.items}
          keyExtractor={i => String(i.id)}
          renderItem={renderItem}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          ItemSeparatorComponent={() => <View style={{ height: 8 }} />}
          ListEmptyComponent={
            <View style={s.center}>
              <Ionicons name="checkmark-done-circle-outline" size={48} color="#adb5bd" />
              <Text style={{ color: '#868e96', marginTop: 8 }}>No swing voters in this filter.</Text>
            </View>
          }
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(); }}
              colors={[BRAND]}
            />
          }
        />
      )}
    </SafeAreaView>
  );
}

function Stat({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <View style={s.stat}>
      <Text style={[s.statVal, { color }]}>{value}</Text>
      <Text style={s.statLbl}>{label}</Text>
    </View>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header:    { backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingBottom: 12, paddingTop: 8 },
  title:     { color: '#fff', fontSize: 20, fontWeight: '800' },
  sub:       { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  statsRow:  { flexDirection: 'row', gap: 8, paddingHorizontal: 12, paddingTop: 12 },
  stat:      { flex: 1, backgroundColor: '#fff', borderRadius: 12, padding: 12, alignItems: 'center', elevation: 1 },
  statVal:   { fontSize: 22, fontWeight: '900' },
  statLbl:   { fontSize: 11, color: '#868e96', marginTop: 2, textTransform: 'uppercase' },
  tabs:      { flexDirection: 'row', backgroundColor: '#fff', padding: 6, margin: 12,
               borderRadius: 12, elevation: 1 },
  tab:       { flex: 1, paddingVertical: 8, borderRadius: 8, alignItems: 'center' },
  tabTxt:    { fontSize: 13, fontWeight: '700', color: '#495057' },
  tabTxtActive: { color: '#fff' },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 12, elevation: 1 },
  name:      { fontSize: 15, fontWeight: '700', color: '#212529' },
  meta:      { fontSize: 11, color: '#495057', marginTop: 2 },
  history:   { fontSize: 11, color: '#868e96', marginTop: 4 },
  badge:     { paddingHorizontal: 8, paddingVertical: 3, borderRadius: 10, borderWidth: 1, marginLeft: 8 },
  badgeTxt:  { fontSize: 10, fontWeight: '800' },
  callBtn:   { flexDirection: 'row', alignItems: 'center', gap: 6, marginTop: 10,
               backgroundColor: '#e6f4ea', paddingHorizontal: 10, paddingVertical: 6,
               borderRadius: 8, alignSelf: 'flex-start' },
  callTxt:   { fontSize: 12, fontWeight: '700', color: '#2f9e44' },
});
