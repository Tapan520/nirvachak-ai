import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, TextInput,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getVoterSlips, VoterSlipItem, VoterSlipsPage } from '../api/voterSlips';

const BRAND = '#3b5bdb';

export default function VoterSlipsScreen() {
  const [page,       setPage]       = useState<VoterSlipsPage | null>(null);
  const [currentPage,setCurrentPage]= useState(1);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [boothInput, setBoothInput] = useState('');
  const [boothFilter,setBoothFilter]= useState<number | undefined>(undefined);

  const load = useCallback(async (pg = 1) => {
    try {
      const data = await getVoterSlips(boothFilter, pg);
      setPage(data);
      setCurrentPage(pg);
    } finally { setLoading(false); setRefreshing(false); }
  }, [boothFilter]);

  useEffect(() => { load(1); }, [load]);

  const applyBooth = () => {
    const n = boothInput.trim() ? parseInt(boothInput, 10) : undefined;
    setBoothFilter(n);
  };

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <Text style={s.title}>Voter Slips</Text>
        <Text style={s.sub}>{page?.total ?? 0} total voters</Text>
      </View>

      {/* Booth filter */}
      <View style={s.filterRow}>
        <TextInput
          style={s.filterInput}
          value={boothInput}
          onChangeText={setBoothInput}
          placeholder="Filter by booth number"
          placeholderTextColor="#adb5bd"
          keyboardType="numeric"
          onSubmitEditing={applyBooth}
          returnKeyType="search"
        />
        <TouchableOpacity style={s.filterBtn} onPress={applyBooth}>
          <Ionicons name="search-outline" size={18} color="#fff" />
        </TouchableOpacity>
        {boothFilter !== undefined && (
          <TouchableOpacity style={s.clearBtn} onPress={() => { setBoothInput(''); setBoothFilter(undefined); }}>
            <Ionicons name="close-circle" size={20} color="#adb5bd" />
          </TouchableOpacity>
        )}
      </View>

      <FlatList
        data={page?.items ?? []}
        keyExtractor={v => v.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(1); }} />}
        ListEmptyComponent={<View style={s.empty}><Ionicons name="card-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No voter slips found.</Text></View>}
        renderItem={({ item: v }) => (
          <View style={s.card}>
            <View style={s.slipLeft}>
              <Text style={s.serial}>{v.serialNumber}</Text>
              <Text style={s.boothNum}>B{v.boothNumber}</Text>
            </View>
            <View style={{ flex: 1, marginLeft: 12 }}>
              <Text style={s.name}>{v.name}</Text>
              {v.nameLocal && <Text style={s.nameLocal}>{v.nameLocal}</Text>}
              <View style={s.metaRow}>
                <Text style={s.meta}>{v.age}y · {v.gender}</Text>
                {v.wardNumber && <Text style={s.meta}>Ward {v.wardNumber}</Text>}
                {v.pannaNumber && <Text style={s.meta}>Panna {v.pannaNumber}</Text>}
              </View>
              <Text style={s.voterId}>EPIC: {v.voterId}</Text>
              <Text style={s.address} numberOfLines={1}>{v.address}</Text>
            </View>
          </View>
        )}
        ListFooterComponent={
          page && page.totalPages > 1 ? (
            <View style={s.pagination}>
              <TouchableOpacity
                style={[s.pageBtn, currentPage <= 1 && s.pageBtnDisabled]}
                disabled={currentPage <= 1}
                onPress={() => load(currentPage - 1)}>
                <Ionicons name="chevron-back" size={18} color={currentPage <= 1 ? '#dee2e6' : BRAND} />
              </TouchableOpacity>
              <Text style={s.pageTxt}>{currentPage} / {page.totalPages}</Text>
              <TouchableOpacity
                style={[s.pageBtn, currentPage >= page.totalPages && s.pageBtnDisabled]}
                disabled={currentPage >= page.totalPages}
                onPress={() => load(currentPage + 1)}>
                <Ionicons name="chevron-forward" size={18} color={currentPage >= page.totalPages ? '#dee2e6' : BRAND} />
              </TouchableOpacity>
            </View>
          ) : null
        }
      />
    </View>
  );
}

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16 },
  title:        { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  filterRow:    { flexDirection: 'row', alignItems: 'center', margin: 12, gap: 8 },
  filterInput:  { flex: 1, backgroundColor: '#fff', borderRadius: 10, borderWidth: 1, borderColor: '#dee2e6', paddingHorizontal: 14, paddingVertical: 10, fontSize: 14, color: '#212529' },
  filterBtn:    { backgroundColor: BRAND, borderRadius: 10, padding: 10 },
  clearBtn:     { padding: 4 },
  card:         { backgroundColor: '#fff', borderRadius: 12, padding: 12, marginBottom: 8, flexDirection: 'row', elevation: 1 },
  slipLeft:     { width: 44, alignItems: 'center', justifyContent: 'center', backgroundColor: '#e7f0ff', borderRadius: 8, paddingVertical: 8 },
  serial:       { fontSize: 16, fontWeight: '800', color: BRAND },
  boothNum:     { fontSize: 10, color: '#868e96', marginTop: 2 },
  name:         { fontSize: 14, fontWeight: '700', color: '#212529' },
  nameLocal:    { fontSize: 12, color: '#495057', marginTop: 1 },
  metaRow:      { flexDirection: 'row', gap: 10, marginTop: 4, marginBottom: 2 },
  meta:         { fontSize: 11, color: '#868e96' },
  voterId:      { fontSize: 11, color: '#3b5bdb', marginTop: 2 },
  address:      { fontSize: 11, color: '#adb5bd', marginTop: 1 },
  pagination:   { flexDirection: 'row', justifyContent: 'center', alignItems: 'center', gap: 20, paddingVertical: 16 },
  pageBtn:      { backgroundColor: '#fff', borderRadius: 8, padding: 8, elevation: 1 },
  pageBtnDisabled: { opacity: 0.4 },
  pageTxt:      { fontSize: 14, fontWeight: '600', color: '#495057' },
  empty:        { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:     { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
