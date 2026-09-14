import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, RefreshControl, ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { getPreferenceAnalytics, PreferenceResponse, PreferenceRow } from '../api/preferenceAnalytics';

const BRAND = '#3b5bdb';

function BarRow({ row, color }: { row: PreferenceRow; color: string }) {
  return (
    <View style={s.barRow}>
      <View style={s.barHeader}>
        <Text style={s.barName} numberOfLines={1}>{row.name}</Text>
        <Text style={s.barValue}>{row.count} · {row.percent}%</Text>
      </View>
      {row.subText ? <Text style={s.barSub}>{row.subText}</Text> : null}
      <View style={s.barTrack}>
        <View style={[s.barFill, { width: `${Math.max(2, row.percent)}%`, backgroundColor: color }]} />
      </View>
    </View>
  );
}

export default function PreferenceAnalyticsScreen() {
  const [data, setData] = useState<PreferenceResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try { setData(await getPreferenceAnalytics()); }
    catch { /* ignore */ }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { setLoading(true); load(); }, [load]);

  if (loading) {
    return <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
    </SafeAreaView>;
  }

  const d = data ?? {
    totalResponses: 0, candidates: [], candidateNoPreference: 0,
    parties: [], partyNoPreference: 0, byBooth: [],
    ticketRecommendation: null, ticketRecommendationReason: null,
  };

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.hTitle}>Preference Analytics</Text>
        <Text style={s.hSub}>Voter survey — candidate & party preference</Text>
      </View>

      <ScrollView
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} colors={[BRAND]} />}
      >
        <View style={s.card}>
          <Text style={s.cardTitle}>Total responses</Text>
          <Text style={s.bigNum}>{d.totalResponses}</Text>
        </View>

        {d.ticketRecommendation ? (
          <View style={[s.card, { borderLeftColor: '#2f9e44', borderLeftWidth: 4 }]}>
            <Text style={s.cardTitle}><Ionicons name="ribbon-outline" size={14} color="#2f9e44" /> Ticket recommendation</Text>
            <Text style={s.recName}>{d.ticketRecommendation}</Text>
            {d.ticketRecommendationReason ? <Text style={s.recReason}>{d.ticketRecommendationReason}</Text> : null}
          </View>
        ) : null}

        {d.candidates.length > 0 ? (
          <View style={s.card}>
            <Text style={s.cardTitle}>Candidate preference</Text>
            {d.candidates.map(r => <BarRow key={r.id} row={r} color="#3b5bdb" />)}
            {d.candidateNoPreference > 0 ? (
              <Text style={s.footnote}>Undecided / no preference: {d.candidateNoPreference}</Text>
            ) : null}
          </View>
        ) : null}

        {d.parties.length > 0 ? (
          <View style={s.card}>
            <Text style={s.cardTitle}>Party preference</Text>
            {d.parties.map(r => <BarRow key={r.id} row={r} color="#7950f2" />)}
            {d.partyNoPreference > 0 ? (
              <Text style={s.footnote}>Undecided / no preference: {d.partyNoPreference}</Text>
            ) : null}
          </View>
        ) : null}

        {d.byBooth.length > 0 ? (
          <View style={s.card}>
            <Text style={s.cardTitle}>Booth-wise top candidate</Text>
            {d.byBooth.map(b => (
              <View key={b.boothNumber} style={s.boothRow}>
                <View style={{ flex: 1 }}>
                  <Text style={s.boothTitle}>Booth {b.boothNumber}</Text>
                  <Text style={s.boothSub}>{b.topCandidate}</Text>
                </View>
                <View style={{ alignItems: 'flex-end' }}>
                  <Text style={s.boothCount}>{b.topCount}/{b.totalResponses}</Text>
                  <Text style={s.boothPct}>{b.topPct}%</Text>
                </View>
              </View>
            ))}
          </View>
        ) : null}

        {d.totalResponses === 0 ? (
          <View style={s.emptyBox}>
            <Ionicons name="stats-chart-outline" size={40} color="#adb5bd" />
            <Text style={s.emptyTxt}>No voter survey responses yet.</Text>
          </View>
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header:    { backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingBottom: 12, paddingTop: 8 },
  hTitle:    { color: '#fff', fontSize: 20, fontWeight: '800' },
  hSub:      { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10, elevation: 1 },
  cardTitle: { fontSize: 12, fontWeight: '800', color: '#495057', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 6 },
  bigNum:    { fontSize: 32, fontWeight: '900', color: BRAND },
  recName:   { fontSize: 18, fontWeight: '800', color: '#212529', marginTop: 4 },
  recReason: { fontSize: 12, color: '#495057', marginTop: 4, lineHeight: 18 },
  barRow:    { marginBottom: 10 },
  barHeader: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 },
  barName:   { fontSize: 13, fontWeight: '700', color: '#212529', flex: 1, marginRight: 8 },
  barValue:  { fontSize: 12, fontWeight: '700', color: '#495057' },
  barSub:    { fontSize: 11, color: '#868e96', marginBottom: 3 },
  barTrack:  { height: 8, backgroundColor: '#e9ecef', borderRadius: 4, overflow: 'hidden' },
  barFill:   { height: '100%' },
  footnote:  { fontSize: 11, color: '#868e96', marginTop: 4 },
  boothRow:  { flexDirection: 'row', paddingVertical: 8, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  boothTitle:{ fontSize: 13, fontWeight: '700', color: '#212529' },
  boothSub:  { fontSize: 11, color: '#495057' },
  boothCount:{ fontSize: 13, fontWeight: '800', color: BRAND },
  boothPct:  { fontSize: 11, color: '#868e96' },
  emptyBox:  { alignItems: 'center', padding: 40 },
  emptyTxt:  { color: '#868e96', marginTop: 8 },
});
