import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, FlatList, RefreshControl, Alert,
  TouchableOpacity, ActivityIndicator, TextInput, Modal, ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { getWards, createWard, updateWard, deleteWard, WardItem } from '../api/wards';

const BRAND = '#3b5bdb';

export default function WardsScreen() {
  const [items, setItems] = useState<WardItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [editing, setEditing] = useState<WardItem | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [wardNumber, setWardNumber] = useState('');
  const [wardName, setWardName] = useState('');
  const [desc, setDesc] = useState('');

  const load = useCallback(async () => {
    try { setItems(await getWards()); }
    catch { /* ignore */ }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { setLoading(true); load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setWardNumber(''); setWardName(''); setDesc('');
    setModalOpen(true);
  };
  const openEdit = (w: WardItem) => {
    setEditing(w);
    setWardNumber(w.wardNumber); setWardName(w.wardName); setDesc(w.description ?? '');
    setModalOpen(true);
  };

  const save = async () => {
    if (!wardNumber.trim() || !wardName.trim()) {
      Alert.alert('Missing', 'Ward number and ward name are required.');
      return;
    }
    setBusy(true);
    const req = { wardNumber: wardNumber.trim(), wardName: wardName.trim(), description: desc.trim() || null };
    try {
      if (editing) await updateWard(editing.id, req);
      else await createWard(req);
      setModalOpen(false);
      await load();
    } catch { Alert.alert('Error', 'Save failed'); }
    finally { setBusy(false); }
  };

  const confirmDelete = (w: WardItem) => {
    Alert.alert(
      'Delete ward?', `Delete ward "${w.wardName}"?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Delete', style: 'destructive',
          onPress: async () => {
            try { await deleteWard(w.id); await load(); }
            catch { Alert.alert('Error', 'Delete failed'); }
          }
        }
      ]);
  };

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.hTitle}>Wards</Text>
        <Text style={s.hSub}>Manage wards in your constituency</Text>
      </View>

      {loading ? (
        <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
      ) : (
        <FlatList
          data={items}
          keyExtractor={i => String(i.id)}
          contentContainerStyle={{ padding: 12, paddingBottom: 100 }}
          ItemSeparatorComponent={() => <View style={{ height: 8 }} />}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} colors={[BRAND]} />}
          ListEmptyComponent={<View style={s.center}><Text style={{ color: '#868e96' }}>No wards added yet.</Text></View>}
          renderItem={({ item }) => (
            <View style={s.card}>
              <View style={{ flexDirection: 'row' }}>
                <View style={s.badge}>
                  <Text style={s.badgeTxt}>{item.wardNumber}</Text>
                </View>
                <View style={{ flex: 1, marginLeft: 10 }}>
                  <Text style={s.title}>{item.wardName}</Text>
                  {item.description ? <Text style={s.sub}>{item.description}</Text> : null}
                </View>
                <TouchableOpacity onPress={() => openEdit(item)} style={s.actBtn}>
                  <Ionicons name="pencil" size={18} color={BRAND} />
                </TouchableOpacity>
                <TouchableOpacity onPress={() => confirmDelete(item)} style={s.actBtn}>
                  <Ionicons name="trash-outline" size={18} color="#e03131" />
                </TouchableOpacity>
              </View>
            </View>
          )}
        />
      )}

      <TouchableOpacity style={s.fab} onPress={openAdd}>
        <Ionicons name="add" size={26} color="#fff" />
      </TouchableOpacity>

      <Modal visible={modalOpen} animationType="slide" onRequestClose={() => setModalOpen(false)}>
        <SafeAreaView style={{ flex: 1, backgroundColor: '#f0f2f5' }}>
          <View style={s.modalHeader}>
            <TouchableOpacity onPress={() => setModalOpen(false)}><Ionicons name="close" size={24} color="#fff" /></TouchableOpacity>
            <Text style={s.modalTitle}>{editing ? 'Edit Ward' : 'Add Ward'}</Text>
            <View style={{ width: 24 }} />
          </View>
          <ScrollView contentContainerStyle={{ padding: 16 }}>
            <Text style={s.lbl}>Ward number *</Text>
            <TextInput style={s.input} value={wardNumber} onChangeText={setWardNumber} placeholder="e.g. 1" placeholderTextColor="#adb5bd" />
            <Text style={s.lbl}>Ward name *</Text>
            <TextInput style={s.input} value={wardName} onChangeText={setWardName} placeholder="e.g. Gandhi Nagar" placeholderTextColor="#adb5bd" />
            <Text style={s.lbl}>Description</Text>
            <TextInput
              style={[s.input, { minHeight: 80, textAlignVertical: 'top' }]}
              value={desc} onChangeText={setDesc} multiline
              placeholder="Optional" placeholderTextColor="#adb5bd" />
            <TouchableOpacity style={[s.saveBtn, busy && { opacity: 0.6 }]} onPress={save} disabled={busy}>
              {busy ? <ActivityIndicator color="#fff" /> :
                <><Ionicons name="checkmark-circle" size={16} color="#fff" /><Text style={s.saveTxt}>Save</Text></>}
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
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 12, elevation: 1 },
  badge:     { width: 40, height: 40, borderRadius: 20, backgroundColor: BRAND + '18',
               justifyContent: 'center', alignItems: 'center' },
  badgeTxt:  { color: BRAND, fontWeight: '800' },
  title:     { fontSize: 15, fontWeight: '800', color: '#212529' },
  sub:       { fontSize: 12, color: '#495057', marginTop: 2 },
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
