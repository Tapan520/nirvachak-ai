import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, FlatList, RefreshControl,
  TouchableOpacity, ActivityIndicator, Linking,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import {
  getBoothHeatMap, HeatMapResponse, BoothHeat, HeatSort,
} from '../api/boothHeatMap';

const BRAND = '#3b5bdb';
const SORTS: { key: HeatSort; label: string }[] = [
  { key: 'booth',          label: 'Booth #' },
  { key: 'coverage_asc',   label: 'Coverage ?' },
  { key: 'coverage_desc',  label: 'Coverage ?' },
  { key: 'favour_desc',    label: 'Favour ?' },
  { key: 'heat',           label: 'Heat' },
];

function heatBg(color: string) {
  if (color === 'green') return '#e6f4ea';
  if (color === 'yellow') return '#fff8e1';
  return '#fdecea';
}
function heatFg(color: string) {
  if (color === 'green') return '#2f9e44';
  if (color === 'yellow') return '#e67700';
  return '#e03131';
}

export default function BoothHeatMapScreen() {
  const [sort, setSort]         = useState<HeatSort>('booth');
  const [data, setData]         = useState<HeatMapResponse>({
    totalVoters: 0, totalContacted: 0, totalFavour: 0, totalSwing: 0,
    green: 0, yellow: 0, red: 0, booths: [],
  });
  const [loading, setLoading]   = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      const res = await getBoothHeatMap(sort);
      setData(res);
    } catch {
      // keep prior state
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [sort]);

  useEffect(() => { setLoading(true); load(); }, [load]);

  const renderItem = ({ item }: { item: BoothHeat }) => {
    const bg = heatBg(item.heatColor);
    const fg = heatFg(item.heatColor);
    return (
      <View style={[s.card, { borderLeftColor: fg, borderLeftWidth: 4 }]}>
        <View style={s.cardHeader}>
          <View style={{ flex: 1 }}>
            <Text style={s.title}>Booth #{item.boothNumber}</Text>
            <Text style={s.subtitle} numberOfLines={2}>{item.boothName}</Text>
            {item.wardNumber ? <Text style={s.ward}>Ward {item.wardNumber}</Text> : null}
          </View>
          <View style={[s.badge, { backgroundColor: bg, borderColor: fg }]}>
            <Text style={[s.badgeTxt, { color: fg }]}>{item.heatLabel}</Text>
          </View>
        </View>

        <View style={s.barTrack}>
          <View style={[s.barFill, { width: `${Math.min(100, item.coveragePercent)}%`, backgroundColor: fg }]} />
        </View>
        <View style={s.barLabels}>
          <Text style={s.barLbl}>Coverage {item.coveragePercent}%</Text>
          <Text style={s.barLbl}>{item.contactedVoters}/{item.totalVoters}</Text>
        </View>

        <View style={s.pillsRow}>
          <Pill color="#2f9e44" label="Favour"   value={item.favourVoters} />
          <Pill color="#f59f00" label="Floating" value={item.floatingVoters} />
          <Pill color="#e03131" label="Against"  value={item.againstVoters} />
          <Pill color="#1971c2" label="Neutral"  value={item.neutralVoters} />
          <Pill color="#adb5bd" label="Unknown"  value={item.unknownVoters} />
        </View>

        <View style={s.footerRow}>
          <Text style={s.footerTxt}>
            Favour {item.favourPercent}% · This week: {item.visitsThisWeek} visits
          </Text>
          {item.assignedAgentPhone ? (
            <TouchableOpacity
              onPress={() => Linking.openURL(`tel:${item.assignedAgentPhone}`)}
              style={s.callBtn}
            >
              <Ionicons name="call" size={12} color="#2f9e44" />
              <Text style={s.callTxt}>{item.assignedAgentName ?? 'Call agent'}</Text>
            </TouchableOpacity>
          ) : null}
        </View>
      </View>
    );
  };

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.hTitle}>Booth Heat Map</Text>
        <Text style={s.hSub}>Coverage % per booth · Weak &lt; 30% · Strong ? 70%</Text>
      </View>

      <View style={s.statsRow}>
        <Stat label="Green" value={data.green}  color="#2f9e44" />
        <Stat label="Amber" value={data.yellow} color="#f59f00" />
        <Stat label="Red"   value={data.red}    color="#e03131" />
        <Stat label="Swing" value={data.totalSwing} color="#7950f2" />
      </View>

      <View style={s.sortsWrap}>
        <FlatList
          horizontal
          data={SORTS}
          keyExtractor={i => i.key}
          contentContainerStyle={{ paddingHorizontal: 12, gap: 8 }}
          showsHorizontalScrollIndicator={false}
          renderItem={({ item }) => (
            <TouchableOpacity
              onPress={() => setSort(item.key)}
              style={[s.chip, sort === item.key && s.chipActive]}
            >
              <Text style={[s.chipTxt, sort === item.key && s.chipTxtActive]}>{item.label}</Text>
            </TouchableOpacity>
          )}
        />
      </View>

      {loading ? (
        <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
      ) : (
        <FlatList
          data={data.booths}
          keyExtractor={b => String(b.boothNumber)}
          renderItem={renderItem}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          ItemSeparatorComponent={() => <View style={{ height: 10 }} />}
          ListEmptyComponent={
            <View style={s.center}>
              <Ionicons name="grid-outline" size={48} color="#adb5bd" />
              <Text style={{ color: '#868e96', marginTop: 8 }}>No booths available.</Text>
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
function Pill({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <View style={[s.pill, { backgroundColor: color + '18' }]}>
      <Text style={[s.pillVal, { color }]}>{value}</Text>
      <Text style={s.pillLbl}>{label}</Text>
    </View>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header:    { backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingBottom: 12, paddingTop: 8 },
  hTitle:    { color: '#fff', fontSize: 20, fontWeight: '800' },
  hSub:      { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  statsRow:  { flexDirection: 'row', gap: 8, paddingHorizontal: 12, paddingTop: 12 },
  stat:      { flex: 1, backgroundColor: '#fff', borderRadius: 12, padding: 10, alignItems: 'center', elevation: 1 },
  statVal:   { fontSize: 20, fontWeight: '900' },
  statLbl:   { fontSize: 10, color: '#868e96', marginTop: 2, textTransform: 'uppercase' },
  sortsWrap: { marginTop: 12 },
  chip:      { paddingHorizontal: 12, paddingVertical: 6, borderRadius: 16, backgroundColor: '#fff', elevation: 1 },
  chipActive:{ backgroundColor: BRAND },
  chipTxt:   { fontSize: 12, fontWeight: '700', color: '#495057' },
  chipTxtActive: { color: '#fff' },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 12, elevation: 1 },
  cardHeader:{ flexDirection: 'row', alignItems: 'flex-start' },
  title:     { fontSize: 15, fontWeight: '800', color: '#212529' },
  subtitle:  { fontSize: 12, color: '#495057', marginTop: 2 },
  ward:      { fontSize: 11, color: '#868e96', marginTop: 2 },
  badge:     { paddingHorizontal: 10, paddingVertical: 4, borderRadius: 10, borderWidth: 1, marginLeft: 8 },
  badgeTxt:  { fontSize: 11, fontWeight: '800' },
  barTrack:  { height: 8, borderRadius: 4, backgroundColor: '#e9ecef', marginTop: 10, overflow: 'hidden' },
  barFill:   { height: '100%', borderRadius: 4 },
  barLabels: { flexDirection: 'row', justifyContent: 'space-between', marginTop: 4 },
  barLbl:    { fontSize: 11, color: '#495057', fontWeight: '600' },
  pillsRow:  { flexDirection: 'row', flexWrap: 'wrap', gap: 6, marginTop: 10 },
  pill:      { paddingHorizontal: 8, paddingVertical: 4, borderRadius: 8, flexDirection: 'row', alignItems: 'baseline', gap: 4 },
  pillVal:   { fontSize: 12, fontWeight: '800' },
  pillLbl:   { fontSize: 10, color: '#495057' },
  footerRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginTop: 10 },
  footerTxt: { fontSize: 11, color: '#868e96', flex: 1 },
  callBtn:   { flexDirection: 'row', alignItems: 'center', gap: 4,
               backgroundColor: '#e6f4ea', paddingHorizontal: 8, paddingVertical: 4, borderRadius: 8 },
  callTxt:   { fontSize: 11, fontWeight: '700', color: '#2f9e44' },
});
