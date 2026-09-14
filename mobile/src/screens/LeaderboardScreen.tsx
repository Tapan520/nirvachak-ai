import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, FlatList, RefreshControl,
  TouchableOpacity, ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import {
  getLeaderboard, LeaderboardPeriod, LeaderboardRow,
} from '../api/leaderboard';

const BRAND = '#3b5bdb';
const PERIODS: { key: LeaderboardPeriod; label: string }[] = [
  { key: 'today',   label: 'Today' },
  { key: 'week',    label: 'Week' },
  { key: 'month',   label: 'Month' },
  { key: 'alltime', label: 'All' },
];

function medalColor(rank: number) {
  if (rank === 1) return '#f59f00';
  if (rank === 2) return '#adb5bd';
  if (rank === 3) return '#d9822b';
  return '#868e96';
}

export default function LeaderboardScreen() {
  const [period, setPeriod]     = useState<LeaderboardPeriod>('week');
  const [label, setLabel]       = useState('This Week');
  const [rows, setRows]         = useState<LeaderboardRow[]>([]);
  const [loading, setLoading]   = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      const res = await getLeaderboard(period);
      setRows(res.rows);
      setLabel(res.periodLabel);
    } catch {
      setRows([]);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [period]);

  useEffect(() => { setLoading(true); load(); }, [load]);

  const renderItem = ({ item }: { item: LeaderboardRow }) => {
    const highlight = item.isCurrentUser;
    return (
      <View style={[s.row, highlight && s.rowMe]}>
        <View style={[s.rankBox, { backgroundColor: medalColor(item.rank) + '22' }]}>
          {item.rank <= 3 ? (
            <Ionicons name="trophy" size={20} color={medalColor(item.rank)} />
          ) : (
            <Text style={[s.rankTxt, { color: medalColor(item.rank) }]}>#{item.rank}</Text>
          )}
        </View>
        <View style={{ flex: 1, marginLeft: 12 }}>
          <Text style={s.name} numberOfLines={1}>
            {item.fullName}{highlight ? '  (You)' : ''}
          </Text>
          <Text style={s.role}>{item.role}{item.assignedBooths ? ` · Booth ${item.assignedBooths}` : ''}</Text>
          <View style={s.metricsRow}>
            <Metric icon="walk-outline"        value={item.visits} label="Visits" color="#3b5bdb" />
            <Metric icon="call-outline"        value={item.calls} label="Calls" color="#1971c2" />
            <Metric icon="thumbs-up-outline"   value={item.favourConversions} label="Favour" color="#2f9e44" />
          </View>
        </View>
        <View style={s.scoreBox}>
          <Text style={s.score}>{item.totalScore}</Text>
          <Text style={s.scoreLbl}>score</Text>
        </View>
      </View>
    );
  };

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.title}>Leaderboard</Text>
        <Text style={s.sub}>{label} · Visits × 2 + Calls × 1 + Favour × 3</Text>
      </View>

      <View style={s.tabs}>
        {PERIODS.map(p => (
          <TouchableOpacity
            key={p.key}
            style={[s.tab, period === p.key && s.tabActive]}
            onPress={() => setPeriod(p.key)}
          >
            <Text style={[s.tabTxt, period === p.key && s.tabTxtActive]}>{p.label}</Text>
          </TouchableOpacity>
        ))}
      </View>

      {loading ? (
        <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
      ) : (
        <FlatList
          data={rows}
          keyExtractor={r => r.userId}
          renderItem={renderItem}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          ItemSeparatorComponent={() => <View style={{ height: 8 }} />}
          ListEmptyComponent={
            <View style={s.center}>
              <Ionicons name="trophy-outline" size={48} color="#adb5bd" />
              <Text style={{ color: '#868e96', marginTop: 8 }}>No activity yet in this period.</Text>
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

function Metric({ icon, value, label, color }: { icon: string; value: number; label: string; color: string }) {
  return (
    <View style={s.metric}>
      <Ionicons name={icon as any} size={12} color={color} />
      <Text style={[s.metricVal, { color }]}>{value}</Text>
      <Text style={s.metricLbl}>{label}</Text>
    </View>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header:    { backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingBottom: 12, paddingTop: 8 },
  title:     { color: '#fff', fontSize: 20, fontWeight: '800' },
  sub:       { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  tabs:      { flexDirection: 'row', backgroundColor: '#fff', padding: 6, margin: 12,
               borderRadius: 12, elevation: 1 },
  tab:       { flex: 1, paddingVertical: 8, borderRadius: 8, alignItems: 'center' },
  tabActive: { backgroundColor: BRAND },
  tabTxt:    { fontSize: 13, fontWeight: '700', color: '#495057' },
  tabTxtActive: { color: '#fff' },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  row:       { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff',
               borderRadius: 12, padding: 12, elevation: 1 },
  rowMe:     { borderWidth: 2, borderColor: BRAND },
  rankBox:   { width: 44, height: 44, borderRadius: 22, justifyContent: 'center', alignItems: 'center' },
  rankTxt:   { fontSize: 13, fontWeight: '800' },
  name:      { fontSize: 15, fontWeight: '700', color: '#212529' },
  role:      { fontSize: 11, color: '#868e96', marginTop: 1 },
  metricsRow:{ flexDirection: 'row', marginTop: 6, gap: 12 },
  metric:    { flexDirection: 'row', alignItems: 'center', gap: 3 },
  metricVal: { fontSize: 12, fontWeight: '800' },
  metricLbl: { fontSize: 10, color: '#868e96' },
  scoreBox:  { alignItems: 'center', paddingLeft: 8 },
  score:     { fontSize: 20, fontWeight: '900', color: BRAND },
  scoreLbl:  { fontSize: 9, color: '#868e96', textTransform: 'uppercase', letterSpacing: 0.5 },
});
