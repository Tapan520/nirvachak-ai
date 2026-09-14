import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, RefreshControl, ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { getSurveyDemographics, DemographicsResponse, Bucket } from '../api/surveyDemographics';

const BRAND = '#3b5bdb';

function Section({ title, items, color }: { title: string; items: Bucket[]; color: string }) {
  if (!items?.length) return null;
  const max = Math.max(...items.map(i => i.count));
  return (
    <View style={s.card}>
      <Text style={s.cardTitle}>{title}</Text>
      {items.map(b => (
        <View key={b.label} style={{ marginBottom: 8 }}>
          <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
            <Text style={s.rowLbl}>{b.label}</Text>
            <Text style={s.rowVal}>{b.count}</Text>
          </View>
          <View style={s.barTrack}>
            <View style={[s.barFill, { width: `${Math.max(3, (b.count / max) * 100)}%`, backgroundColor: color }]} />
          </View>
        </View>
      ))}
    </View>
  );
}

export default function SurveyDemographicsScreen() {
  const [data, setData] = useState<DemographicsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try { setData(await getSurveyDemographics()); }
    catch { /* ignore */ }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { setLoading(true); load(); }, [load]);

  if (loading) {
    return <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
    </SafeAreaView>;
  }

  const d = data;
  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.hTitle}>Survey Demographics</Text>
        <Text style={s.hSub}>Voter self-survey completions & profiles</Text>
      </View>
      <ScrollView
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} colors={[BRAND]} />}
      >
        {d ? (
          <>
            <View style={s.gridRow}>
              <Stat label="Total"       value={d.totalVoters}       color={BRAND} />
              <Stat label="Completed"   value={d.completedCount}    color="#2f9e44" />
              <Stat label="Pending"     value={d.pendingCount}      color="#f59f00" />
            </View>
            <View style={s.gridRow}>
              <Stat label="Rate %"      value={`${d.completionRate}`} color="#7950f2" />
              <Stat label="Coupons"     value={d.couponsIssued}      color="#e67700" />
              <Stat label="Redeemed"    value={d.couponsRedeemed}    color="#1971c2" />
            </View>

            <View style={s.card}>
              <Text style={s.cardTitle}>Consents</Text>
              <ConsentRow label="Third-party ads"      value={d.consentThirdParty} total={d.completedCount} />
              <ConsentRow label="Campaign outreach"    value={d.consentCampaign}   total={d.completedCount} />
              <ConsentRow label="WhatsApp messages"    value={d.consentWhatsApp}   total={d.completedCount} />
              <ConsentRow label="Scheme notifications" value={d.consentScheme}     total={d.completedCount} />
              <ConsentRow label="Data for analytics"   value={d.consentAnalytics}  total={d.completedCount} />
            </View>

            <Section title="Age bracket"  items={d.byAge}        color="#3b5bdb" />
            <Section title="Caste"        items={d.byCaste}      color="#7950f2" />
            <Section title="Religion"     items={d.byReligion}   color="#e67700" />
            <Section title="Education"    items={d.byEducation}  color="#2f9e44" />
            <Section title="Occupation"   items={d.byOccupation} color="#1971c2" />
            <Section title="Income"       items={d.byIncome}     color="#f59f00" />
            <Section title="Top concerns" items={d.topConcerns}  color="#e03131" />
          </>
        ) : (
          <View style={s.emptyBox}>
            <Ionicons name="pie-chart-outline" size={40} color="#adb5bd" />
            <Text style={s.emptyTxt}>No data available.</Text>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

function Stat({ label, value, color }: { label: string; value: number | string; color: string }) {
  return (
    <View style={s.stat}>
      <Text style={[s.statVal, { color }]}>{value}</Text>
      <Text style={s.statLbl}>{label}</Text>
    </View>
  );
}

function ConsentRow({ label, value, total }: { label: string; value: number; total: number }) {
  const pct = total > 0 ? Math.round((value / total) * 100) : 0;
  return (
    <View style={{ marginBottom: 6 }}>
      <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
        <Text style={s.rowLbl}>{label}</Text>
        <Text style={s.rowVal}>{value} · {pct}%</Text>
      </View>
      <View style={s.barTrack}>
        <View style={[s.barFill, { width: `${Math.max(3, pct)}%`, backgroundColor: '#2f9e44' }]} />
      </View>
    </View>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header:    { backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingBottom: 12, paddingTop: 8 },
  hTitle:    { color: '#fff', fontSize: 20, fontWeight: '800' },
  hSub:      { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10, elevation: 1 },
  cardTitle: { fontSize: 12, fontWeight: '800', color: '#495057', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 8 },
  gridRow:   { flexDirection: 'row', gap: 8, marginBottom: 8 },
  stat:      { flex: 1, backgroundColor: '#fff', borderRadius: 12, padding: 12, alignItems: 'center', elevation: 1 },
  statVal:   { fontSize: 20, fontWeight: '900' },
  statLbl:   { fontSize: 10, color: '#868e96', marginTop: 2, textTransform: 'uppercase' },
  rowLbl:    { fontSize: 12, color: '#212529', flex: 1 },
  rowVal:    { fontSize: 12, fontWeight: '700', color: '#495057' },
  barTrack:  { height: 6, backgroundColor: '#e9ecef', borderRadius: 3, marginTop: 3, overflow: 'hidden' },
  barFill:   { height: '100%' },
  emptyBox:  { alignItems: 'center', padding: 40 },
  emptyTxt:  { color: '#868e96', marginTop: 8 },
});
