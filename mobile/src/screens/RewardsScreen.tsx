import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, FlatList, RefreshControl, Alert,
  TouchableOpacity, ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { getRewards, toggleReward, RewardItem } from '../api/rewards';

const BRAND = '#3b5bdb';

export default function RewardsScreen() {
  const [items, setItems] = useState<RewardItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try { setItems(await getRewards()); }
    catch { /* ignore */ }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { setLoading(true); load(); }, [load]);

  const doToggle = async (id: number) => {
    try { await toggleReward(id); await load(); }
    catch { Alert.alert('Error', 'Update failed'); }
  };

  const renderItem = ({ item }: { item: RewardItem }) => {
    const availableCoupons = item.totalCoupons - item.issuedCount;
    return (
      <View style={[s.card, !item.isActive && { opacity: 0.6 }]}>
        <View style={{ flexDirection: 'row', alignItems: 'flex-start' }}>
          <View style={{ flex: 1 }}>
            <Text style={s.title}>{item.title}</Text>
            {item.partnerBrand ? <Text style={s.brand}>{item.partnerBrand}</Text> : null}
            {item.description ? <Text style={s.desc}>{item.description}</Text> : null}
            <Text style={s.expiry}>
              Expires: {new Date(item.expiryDate).toLocaleDateString()}
              {new Date(item.expiryDate) < new Date() ? '  ? Expired' : ''}
            </Text>
          </View>
          <TouchableOpacity onPress={() => doToggle(item.id)} style={s.toggleBtn}>
            <Ionicons name={item.isActive ? 'toggle' : 'toggle-outline'} size={30} color={item.isActive ? '#2f9e44' : '#adb5bd'} />
          </TouchableOpacity>
        </View>
        <View style={s.statsRow}>
          <Stat label="Total"     value={item.totalCoupons}   color={BRAND} />
          <Stat label="Issued"    value={item.issuedCount}    color="#f59f00" />
          <Stat label="Redeemed"  value={item.redeemedCount}  color="#2f9e44" />
          <Stat label="Available" value={availableCoupons}    color="#1971c2" />
        </View>
      </View>
    );
  };

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.hTitle}>Rewards</Text>
        <Text style={s.hSub}>Coupon programs for voter survey completions</Text>
      </View>

      {loading ? (
        <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
      ) : (
        <FlatList
          data={items}
          keyExtractor={i => String(i.id)}
          renderItem={renderItem}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          ItemSeparatorComponent={() => <View style={{ height: 10 }} />}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} colors={[BRAND]} />}
          ListEmptyComponent={
            <View style={s.center}>
              <Ionicons name="gift-outline" size={40} color="#adb5bd" />
              <Text style={{ color: '#868e96', marginTop: 8 }}>No reward programs configured.</Text>
            </View>
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
  hTitle:    { color: '#fff', fontSize: 20, fontWeight: '800' },
  hSub:      { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 14, elevation: 1 },
  title:     { fontSize: 15, fontWeight: '800', color: '#212529' },
  brand:     { fontSize: 12, color: '#495057', marginTop: 2 },
  desc:      { fontSize: 11, color: '#868e96', marginTop: 4 },
  expiry:    { fontSize: 11, color: '#868e96', marginTop: 6 },
  toggleBtn: { padding: 4 },
  statsRow:  { flexDirection: 'row', gap: 6, marginTop: 12 },
  stat:      { flex: 1, backgroundColor: '#f8f9fa', borderRadius: 8, padding: 8, alignItems: 'center' },
  statVal:   { fontSize: 16, fontWeight: '900' },
  statLbl:   { fontSize: 9, color: '#868e96', marginTop: 2, textTransform: 'uppercase' },
});
