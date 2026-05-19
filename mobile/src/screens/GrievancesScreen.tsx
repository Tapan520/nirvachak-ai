import React, { useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { getGrievances, GrievanceItem } from '../api/grievances';

const STATUS_COLOR: Record<string, string> = {
  Open: '#e03131', InProgress: '#f59f00', Resolved: '#2f9e44', Closed: '#868e96',
};
const PRIORITY_COLOR: Record<string, string> = {
  Critical: '#e03131', High: '#f59f00', Medium: '#4dabf7', Low: '#adb5bd',
};

export default function GrievancesScreen() {
  const nav = useNavigation<any>();
  const [items, setItems] = useState<GrievanceItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = async () => {
    try { setItems(await getGrievances()); }
    finally { setLoading(false); setRefreshing(false); }
  };

  useEffect(() => { load(); }, []);

  if (loading) return <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View>
          <Text style={s.title}>Grievances</Text>
          <Text style={s.sub}>{items.length} total</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => nav.navigate('AddGrievance')}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      <FlatList
        data={items}
        keyExtractor={g => g.id.toString()}
        contentContainerStyle={{ padding: 12 }}
        refreshControl={<RefreshControl refreshing={refreshing}
          onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.center}><Text style={{ color: '#868e96' }}>No grievances found.</Text></View>
        }
        renderItem={({ item: g }) => (
          <TouchableOpacity
            style={[s.card, { borderLeftColor: STATUS_COLOR[g.status] ?? '#adb5bd' }]}
            onPress={() => nav.navigate('GrievanceDetail', { id: g.id })}>
            <View style={s.cardTop}>
              <Text style={s.cardTitle} numberOfLines={1}>{g.title}</Text>
              <View style={[s.badge, { backgroundColor: (PRIORITY_COLOR[g.priority] ?? '#adb5bd') + '25' }]}>
                <Text style={[s.badgeTxt, { color: PRIORITY_COLOR[g.priority] ?? '#868e96' }]}>
                  {g.priority}
                </Text>
              </View>
            </View>
            <View style={s.meta}>
              <Text style={s.metaTxt}>{g.reportedBy ?? 'Anonymous'}</Text>
              {g.ward && <Text style={s.metaTxt}>Ward {g.ward}</Text>}
              <Text style={s.metaTxt}>{new Date(g.reportedAt).toLocaleDateString('en-IN')}</Text>
            </View>
            <View style={[s.statusBadge, { backgroundColor: (STATUS_COLOR[g.status] ?? '#adb5bd') + '20' }]}>
              <Text style={[s.statusTxt, { color: STATUS_COLOR[g.status] ?? '#868e96' }]}>
                {g.status}
              </Text>
            </View>
          </TouchableOpacity>
        )}
      />
    </View>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  header: { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16,
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-end' },
  title: { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub: { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn: { backgroundColor: '#3b5bdb', borderRadius: 10, padding: 8 },
  card: { backgroundColor: '#fff', borderRadius: 12, padding: 14,
    marginBottom: 10, borderLeftWidth: 4, elevation: 1 },
  cardTop: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 },
  cardTitle: { fontSize: 14, fontWeight: '700', color: '#212529', flex: 1, marginRight: 8 },
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  badgeTxt: { fontSize: 11, fontWeight: '700' },
  meta: { flexDirection: 'row', gap: 12, flexWrap: 'wrap', marginBottom: 8 },
  metaTxt: { fontSize: 11, color: '#868e96' },
  statusBadge: { alignSelf: 'flex-start', borderRadius: 6, paddingHorizontal: 10, paddingVertical: 4 },
  statusTxt: { fontSize: 12, fontWeight: '700' },
});
