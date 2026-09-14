import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, FlatList, RefreshControl, Alert,
  TouchableOpacity, ActivityIndicator, TextInput, Modal, ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import {
  getCatalog, CatalogResponse, addCandidate, addParty,
  toggleCandidate, toggleParty, deleteCandidate, deleteParty,
} from '../api/candidatesParties';

const BRAND = '#3b5bdb';

export default function CandidatesPartiesScreen() {
  const [data, setData] = useState<CatalogResponse>({ candidates: [], parties: [] });
  const [tab, setTab] = useState<'candidates' | 'parties'>('candidates');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [addOpen, setAddOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [name, setName] = useState('');
  const [extra, setExtra] = useState('');
  const [notes, setNotes] = useState('');
  const [order, setOrder] = useState('0');

  const load = useCallback(async () => {
    try { setData(await getCatalog()); }
    catch { /* ignore */ }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { setLoading(true); load(); }, [load]);

  const resetForm = () => { setName(''); setExtra(''); setNotes(''); setOrder('0'); };

  const submitAdd = async () => {
    if (!name.trim()) { Alert.alert('Missing', 'Name is required'); return; }
    setBusy(true);
    try {
      if (tab === 'candidates') {
        await addCandidate({
          name: name.trim(),
          partyAffiliation: extra.trim() || null,
          notes: notes.trim() || null,
          displayOrder: parseInt(order, 10) || 0,
        });
      } else {
        await addParty({ name: name.trim(), symbol: extra.trim() || null, notes: notes.trim() || null });
      }
      setAddOpen(false);
      resetForm();
      await load();
    } catch {
      Alert.alert('Error', 'Could not save. Please try again.');
    } finally {
      setBusy(false);
    }
  };

  const confirmDelete = (kind: 'candidate' | 'party', id: number, name: string) => {
    Alert.alert(
      `Delete ${kind}?`,
      `Delete "${name}"? This cannot be undone.`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Delete', style: 'destructive',
          onPress: async () => {
            try {
              if (kind === 'candidate') await deleteCandidate(id);
              else await deleteParty(id);
              await load();
            } catch { Alert.alert('Error', 'Delete failed'); }
          }
        }
      ]);
  };

  const doToggle = async (kind: 'candidate' | 'party', id: number) => {
    try {
      if (kind === 'candidate') await toggleCandidate(id);
      else await toggleParty(id);
      await load();
    } catch { Alert.alert('Error', 'Update failed'); }
  };

  const items = tab === 'candidates' ? data.candidates : data.parties;

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.hTitle}>Candidates & Parties</Text>
        <Text style={s.hSub}>Manage survey candidates and party list</Text>
      </View>

      <View style={s.tabs}>
        {(['candidates', 'parties'] as const).map(t => (
          <TouchableOpacity
            key={t}
            style={[s.tab, tab === t && s.tabActive]}
            onPress={() => setTab(t)}
          >
            <Text style={[s.tabTxt, tab === t && s.tabTxtActive]}>
              {t === 'candidates' ? 'Candidates' : 'Parties'} ({t === 'candidates' ? data.candidates.length : data.parties.length})
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {loading ? (
        <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
      ) : (
        <FlatList
          data={items as any[]}
          keyExtractor={(i) => String(i.id)}
          contentContainerStyle={{ padding: 12, paddingBottom: 100 }}
          ItemSeparatorComponent={() => <View style={{ height: 8 }} />}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} colors={[BRAND]} />}
          ListEmptyComponent={<View style={s.center}><Text style={{ color: '#868e96' }}>Nothing here yet.</Text></View>}
          renderItem={({ item }: any) => (
            <View style={[s.card, !item.isActive && { opacity: 0.55 }]}>
              <View style={{ flexDirection: 'row', alignItems: 'flex-start' }}>
                <View style={{ flex: 1 }}>
                  <Text style={s.itemName}>{item.name}</Text>
                  {tab === 'candidates'
                    ? (item.partyAffiliation ? <Text style={s.itemSub}>{item.partyAffiliation}</Text> : null)
                    : (item.symbol ? <Text style={s.itemSub}>Symbol: {item.symbol}</Text> : null)}
                  {tab === 'candidates' && item.displayOrder !== undefined
                    ? <Text style={s.itemMeta}>Order: {item.displayOrder}</Text> : null}
                  {item.notes ? <Text style={s.itemNotes}>{item.notes}</Text> : null}
                </View>
                <View style={s.actions}>
                  <TouchableOpacity onPress={() => doToggle(tab === 'candidates' ? 'candidate' : 'party', item.id)} style={s.actBtn}>
                    <Ionicons name={item.isActive ? 'toggle' : 'toggle-outline'} size={22} color={item.isActive ? '#2f9e44' : '#adb5bd'} />
                  </TouchableOpacity>
                  <TouchableOpacity
                    onPress={() => confirmDelete(tab === 'candidates' ? 'candidate' : 'party', item.id, item.name)}
                    style={s.actBtn}
                  >
                    <Ionicons name="trash-outline" size={20} color="#e03131" />
                  </TouchableOpacity>
                </View>
              </View>
            </View>
          )}
        />
      )}

      <TouchableOpacity style={s.fab} onPress={() => { resetForm(); setAddOpen(true); }}>
        <Ionicons name="add" size={26} color="#fff" />
      </TouchableOpacity>

      <Modal visible={addOpen} animationType="slide" onRequestClose={() => setAddOpen(false)}>
        <SafeAreaView style={{ flex: 1, backgroundColor: '#f0f2f5' }}>
          <View style={s.modalHeader}>
            <TouchableOpacity onPress={() => setAddOpen(false)}>
              <Ionicons name="close" size={24} color="#fff" />
            </TouchableOpacity>
            <Text style={s.modalTitle}>Add {tab === 'candidates' ? 'Candidate' : 'Party'}</Text>
            <View style={{ width: 24 }} />
          </View>
          <ScrollView contentContainerStyle={{ padding: 16 }}>
            <Text style={s.lbl}>Name *</Text>
            <TextInput style={s.input} value={name} onChangeText={setName} placeholder="Enter name" placeholderTextColor="#adb5bd" />

            <Text style={s.lbl}>{tab === 'candidates' ? 'Party affiliation' : 'Symbol'}</Text>
            <TextInput style={s.input} value={extra} onChangeText={setExtra}
              placeholder={tab === 'candidates' ? 'e.g. BJP, INC' : 'e.g. Lotus'}
              placeholderTextColor="#adb5bd" />

            {tab === 'candidates' ? (
              <>
                <Text style={s.lbl}>Display order</Text>
                <TextInput style={s.input} value={order} onChangeText={setOrder}
                  keyboardType="number-pad" placeholder="0" placeholderTextColor="#adb5bd" />
              </>
            ) : null}

            <Text style={s.lbl}>Notes</Text>
            <TextInput
              style={[s.input, { minHeight: 80, textAlignVertical: 'top' }]}
              value={notes} onChangeText={setNotes} multiline
              placeholder="Optional notes" placeholderTextColor="#adb5bd" />

            <TouchableOpacity style={[s.saveBtn, busy && { opacity: 0.6 }]} onPress={submitAdd} disabled={busy}>
              {busy ? <ActivityIndicator color="#fff" /> :
                <>
                  <Ionicons name="checkmark-circle" size={16} color="#fff" />
                  <Text style={s.saveTxt}>Save</Text>
                </>}
            </TouchableOpacity>
          </ScrollView>
        </SafeAreaView>
      </Modal>
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header:    { backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingBottom: 12, paddingTop: 8 },
  hTitle:    { color: '#fff', fontSize: 20, fontWeight: '800' },
  hSub:      { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  tabs:      { flexDirection: 'row', backgroundColor: '#fff', padding: 6, margin: 12, borderRadius: 12, elevation: 1 },
  tab:       { flex: 1, paddingVertical: 8, borderRadius: 8, alignItems: 'center' },
  tabActive: { backgroundColor: BRAND },
  tabTxt:    { fontSize: 13, fontWeight: '700', color: '#495057' },
  tabTxtActive: { color: '#fff' },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 12, elevation: 1 },
  itemName:  { fontSize: 15, fontWeight: '800', color: '#212529' },
  itemSub:   { fontSize: 12, color: '#495057', marginTop: 2 },
  itemMeta:  { fontSize: 11, color: '#868e96', marginTop: 2 },
  itemNotes: { fontSize: 11, color: '#868e96', marginTop: 4, fontStyle: 'italic' },
  actions:   { flexDirection: 'row', gap: 4, alignItems: 'center', paddingLeft: 8 },
  actBtn:    { padding: 6 },
  fab:       { position: 'absolute', right: 20, bottom: 20, backgroundColor: BRAND,
               width: 56, height: 56, borderRadius: 28, justifyContent: 'center', alignItems: 'center', elevation: 4 },
  modalHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
                 backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingVertical: 12 },
  modalTitle: { color: '#fff', fontSize: 16, fontWeight: '800' },
  lbl:       { fontSize: 12, fontWeight: '700', color: '#495057', marginTop: 12, marginBottom: 6, textTransform: 'uppercase' },
  input:     { borderWidth: 1, borderColor: '#dee2e6', backgroundColor: '#fff', borderRadius: 8, padding: 10, color: '#212529' },
  saveBtn:   { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 6,
               backgroundColor: BRAND, paddingVertical: 12, borderRadius: 10, marginTop: 20 },
  saveTxt:   { color: '#fff', fontWeight: '800' },
});
