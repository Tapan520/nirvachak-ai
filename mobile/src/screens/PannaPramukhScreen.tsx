import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput, Alert,
  ScrollView, Linking, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getPannaPramukhs, createPannaPramukh, updatePannaContact, PannaPramukhItem } from '../api/pannaPramukh';

const BRAND = '#0c8599';

export default function PannaPramukhScreen() {
  const [items,      setItems]      = useState<PannaPramukhItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showAdd,    setShowAdd]    = useState(false);
  const [updateItem, setUpdateItem] = useState<PannaPramukhItem | null>(null);
  const [contacted,  setContacted]  = useState('');
  // form
  const [name,   setName]   = useState('');
  const [phone,  setPhone]  = useState('');
  const [booth,  setBooth]  = useState('');
  const [panna,  setPanna]  = useState('');
  const [total,  setTotal]  = useState('');
  const [notes,  setNotes]  = useState('');
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try { setItems(await getPannaPramukhs()); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async () => {
    if (!name.trim() || !phone.trim() || !booth || !panna) {
      Alert.alert('Required', 'Name, phone, booth and panna number are required.'); return;
    }
    setSaving(true);
    try {
      await createPannaPramukh({
        name: name.trim(), phone: phone.trim(),
        boothNumber: parseInt(booth, 10), pannaNumber: panna.trim(),
        totalVotersAssigned: parseInt(total || '0', 10), notes: notes || undefined,
      });
      setShowAdd(false);
      setName(''); setPhone(''); setBooth(''); setPanna(''); setTotal(''); setNotes('');
      load();
    } catch { Alert.alert('Error', 'Failed to add.');
    } finally { setSaving(false); }
  };

  const handleUpdateContact = async () => {
    if (!updateItem) return;
    setSaving(true);
    try {
      await updatePannaContact(updateItem.id, parseInt(contacted, 10));
      setUpdateItem(null); setContacted('');
      load(); Alert.alert('Updated', 'Contact count updated.');
    } catch { Alert.alert('Error', 'Failed to update.');
    } finally { setSaving(false); }
  };

  const totalAssigned   = items.reduce((s, i) => s + i.totalVotersAssigned, 0);
  const totalContacted  = items.reduce((s, i) => s + i.votersContacted, 0);
  const overallPct      = totalAssigned > 0 ? Math.round((totalContacted / totalAssigned) * 100) : 0;

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Panna Pramukh</Text>
          <Text style={s.sub}>{items.length} pramukhs · {overallPct}% coverage · {totalContacted}/{totalAssigned} voters</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowAdd(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      {/* Overall progress bar */}
      <View style={s.progressCard}>
        <View style={s.progressRow}>
          <Text style={s.progressLbl}>Overall Voter Coverage</Text>
          <Text style={[s.progressPct, { color: overallPct >= 80 ? '#2f9e44' : overallPct >= 50 ? '#f59f00' : '#e03131' }]}>{overallPct}%</Text>
        </View>
        <View style={s.progBg}>
          <View style={[s.progFill, { width: `${overallPct}%`, backgroundColor: overallPct >= 80 ? '#2f9e44' : overallPct >= 50 ? '#f59f00' : '#e03131' }]} />
        </View>
      </View>

      <FlatList
        data={items}
        keyExtractor={i => i.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={<View style={s.empty}><Ionicons name="people-circle-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No Panna Pramukhs registered.</Text></View>}
        renderItem={({ item: pp }) => {
          const pct = pp.totalVotersAssigned > 0 ? (pp.votersContacted / pp.totalVotersAssigned) * 100 : 0;
          const barColor = pct >= 80 ? '#2f9e44' : pct >= 50 ? '#f59f00' : '#e03131';
          return (
            <View style={s.card}>
              <View style={[s.avatar, { backgroundColor: BRAND + '20' }]}>
                <Ionicons name="person-outline" size={20} color={BRAND} />
              </View>
              <View style={{ flex: 1, marginLeft: 12 }}>
                <View style={s.nameRow}>
                  <Text style={s.name}>{pp.name}</Text>
                  <View style={[s.pannaBadge, { backgroundColor: BRAND + '20' }]}>
                    <Text style={[s.pannaTxt, { color: BRAND }]}>P-{pp.pannaNumber}</Text>
                  </View>
                </View>
                <Text style={s.meta}>Booth {pp.boothNumber} · {pp.phone}</Text>
                <View style={s.progBg}>
                  <View style={[s.progFill, { width: `${Math.min(pct, 100)}%`, backgroundColor: barColor }]} />
                </View>
                <Text style={s.progTxt}>{pp.votersContacted}/{pp.totalVotersAssigned} contacted ({pp.contactPercent.toFixed(1)}%)</Text>
              </View>
              <View style={{ gap: 6 }}>
                <TouchableOpacity style={s.iconBtn} onPress={() => Linking.openURL(`tel:${pp.phone}`)}>
                  <Ionicons name="call-outline" size={16} color="#2f9e44" />
                </TouchableOpacity>
                <TouchableOpacity style={[s.iconBtn, { backgroundColor: BRAND + '20' }]} onPress={() => { setUpdateItem(pp); setContacted(pp.votersContacted.toString()); }}>
                  <Ionicons name="create-outline" size={16} color={BRAND} />
                </TouchableOpacity>
              </View>
            </View>
          );
        }}
      />

      {/* Add Modal */}
      <Modal visible={showAdd} animationType="slide" presentationStyle="pageSheet" onRequestClose={() => setShowAdd(false)}>
        <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
          <View style={fm.container}>
            <View style={fm.header}>
              <Text style={fm.title}>Add Panna Pramukh</Text>
              <TouchableOpacity onPress={() => setShowAdd(false)}><Ionicons name="close" size={24} color="#212529" /></TouchableOpacity>
            </View>
            <ScrollView contentContainerStyle={{ padding: 16 }}>
              {[['Full Name *', name, setName, 'default', false], ['Phone *', phone, setPhone, 'phone-pad', false],
                ['Booth Number *', booth, setBooth, 'numeric', false], ['Panna Number *', panna, setPanna, 'default', false],
                ['Total Voters Assigned', total, setTotal, 'numeric', false], ['Notes', notes, setNotes, 'default', true]].map(([label, val, setter, kb, multi]: any) => (
                <View key={label}>
                  <Text style={fm.label}>{label}</Text>
                  <TextInput style={[fm.input, multi && { height: 70, textAlignVertical: 'top' }]}
                    value={val} onChangeText={setter} keyboardType={kb} multiline={multi} placeholder={label.replace(' *', '')} />
                </View>
              ))}
              <TouchableOpacity style={[fm.saveBtn, saving && { opacity: 0.6 }]} onPress={handleAdd} disabled={saving}>
                {saving ? <ActivityIndicator color="#fff" /> : <Text style={fm.saveTxt}>Add Panna Pramukh</Text>}
              </TouchableOpacity>
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
      </Modal>

      {/* Update Modal */}
      <Modal visible={!!updateItem} transparent animationType="slide">
        <View style={fm.overlay}>
          <View style={fm.sheet}>
            <Text style={fm.title}>Update Contacts — {updateItem?.name}</Text>
            <Text style={fm.label}>Voters Contacted (out of {updateItem?.totalVotersAssigned})</Text>
            <TextInput style={fm.input} value={contacted} onChangeText={setContacted} keyboardType="numeric" placeholder="0" />
            <TouchableOpacity style={[fm.saveBtn, saving && { opacity: 0.6 }]} onPress={handleUpdateContact} disabled={saving}>
              {saving ? <ActivityIndicator color="#fff" /> : <Text style={fm.saveTxt}>Update</Text>}
            </TouchableOpacity>
            <TouchableOpacity style={{ padding: 12 }} onPress={() => setUpdateItem(null)}>
              <Text style={{ color: '#868e96', fontWeight: '600', textAlign: 'center' }}>Cancel</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </View>
  );
}

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:        { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:       { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  progressCard: { backgroundColor: '#fff', margin: 12, borderRadius: 12, padding: 14, elevation: 1 },
  progressRow:  { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 },
  progressLbl:  { fontSize: 13, fontWeight: '600', color: '#495057' },
  progressPct:  { fontSize: 18, fontWeight: '800' },
  progBg:       { height: 6, backgroundColor: '#f1f3f5', borderRadius: 3, marginVertical: 4 },
  progFill:     { height: 6, borderRadius: 3 },
  card:         { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', elevation: 1 },
  avatar:       { width: 44, height: 44, borderRadius: 10, justifyContent: 'center', alignItems: 'center' },
  nameRow:      { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 2 },
  name:         { fontSize: 14, fontWeight: '700', color: '#212529' },
  pannaBadge:   { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  pannaTxt:     { fontSize: 11, fontWeight: '700' },
  meta:         { fontSize: 12, color: '#868e96', marginBottom: 6 },
  progTxt:      { fontSize: 11, color: '#868e96', marginTop: 2 },
  iconBtn:      { backgroundColor: '#d3f9d8', borderRadius: 8, padding: 8 },
  empty:        { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:     { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
const fm = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529', marginBottom: 8 },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 14, color: '#212529', marginBottom: 16 },
  overlay:   { flex: 1, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'flex-end' },
  sheet:     { backgroundColor: '#fff', borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 20 },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, alignItems: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});
