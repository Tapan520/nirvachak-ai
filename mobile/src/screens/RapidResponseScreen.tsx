import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput, Alert,
  ScrollView, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getRapidResponseItems, createRapidResponse, updateRapidResponseStatus, RapidResponseItem, THREAT_LEVELS, RR_STATUSES, SOURCES } from '../api/rapidResponse';

const BRAND = '#e03131';

export default function RapidResponseScreen() {
  const [items,      setItems]      = useState<RapidResponseItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [showAdd,    setShowAdd]    = useState(false);
  const [actionItem, setActionItem] = useState<RapidResponseItem | null>(null);
  // form
  const [title,     setTitle]     = useState('');
  const [desc,      setDesc]      = useState('');
  const [source,    setSource]    = useState('');
  const [wards,     setWards]     = useState('');
  const [threat,    setThreat]    = useState('High');
  const [response,  setResponse]  = useState('');
  const [saving,    setSaving]    = useState(false);
  // action
  const [newStatus, setNewStatus] = useState('');
  const [newResp,   setNewResp]   = useState('');

  const load = useCallback(async () => {
    try { setItems(await getRapidResponseItems(statusFilter || undefined)); }
    finally { setLoading(false); setRefreshing(false); }
  }, [statusFilter]);

  useEffect(() => { load(); }, [load]);

  const handleCreate = async () => {
    if (!title.trim() || !desc.trim()) { Alert.alert('Required', 'Title and description required.'); return; }
    setSaving(true);
    try {
      await createRapidResponse({ title: title.trim(), description: desc.trim(), source: source || undefined, affectedWards: wards || undefined, threatLevel: threat, responseText: response || undefined });
      setShowAdd(false); setTitle(''); setDesc(''); setSource(''); setWards(''); setThreat('High'); setResponse('');
      load();
    } catch { Alert.alert('Error', 'Failed to log incident.');
    } finally { setSaving(false); }
  };

  const handleAction = async () => {
    if (!actionItem || !newStatus) return;
    setSaving(true);
    try {
      await updateRapidResponseStatus(actionItem.id, newStatus, newResp || undefined);
      setActionItem(null); setNewStatus(''); setNewResp('');
      load();
    } catch { Alert.alert('Error', 'Failed to update.');
    } finally { setSaving(false); }
  };

  const critical = items.filter(i => i.threatLevel === 'Critical' || i.threatLevel === 'High').length;

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Rapid Response</Text>
          <Text style={s.sub}>{items.length} incidents · {critical} critical/high</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowAdd(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      {/* Status filter */}
      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={s.filterBar}
        contentContainerStyle={{ paddingHorizontal: 12, gap: 8 }}>
        {[{ key: '', label: 'All' }, ...RR_STATUSES].map(st => {
          const active = statusFilter === st.key;
          return (
            <TouchableOpacity key={st.key}
              style={[s.chip, active && s.chipActive]}
              onPress={() => setStatusFilter(st.key)}>
              <Text style={[s.chipTxt, active && { color: '#fff' }]}>{st.label}</Text>
            </TouchableOpacity>
          );
        })}
      </ScrollView>

      <FlatList
        data={items}
        keyExtractor={i => i.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={<View style={s.empty}><Ionicons name="shield-checkmark-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No incidents logged.</Text></View>}
        renderItem={({ item: rr }) => {
          const t = THREAT_LEVELS.find(x => x.key === rr.threatLevel);
          const st = RR_STATUSES.find(x => x.key === rr.status);
          const tColor  = t?.color ?? '#868e96';
          const stColor = st?.color ?? '#868e96';
          return (
            <View style={[s.card, { borderLeftWidth: 3, borderLeftColor: tColor }]}>
              <View style={s.cardTop}>
                <Text style={s.cardTitle} numberOfLines={2}>{rr.title}</Text>
                <View style={[s.badge, { backgroundColor: tColor + '20' }]}>
                  <Text style={[s.badgeTxt, { color: tColor }]}>{rr.threatLevel}</Text>
                </View>
              </View>
              <Text style={s.desc} numberOfLines={2}>{rr.description}</Text>
              <View style={s.metaRow}>
                <View style={[s.badge, { backgroundColor: stColor + '20' }]}>
                  <Text style={[s.badgeTxt, { color: stColor }]}>{st?.label ?? rr.status}</Text>
                </View>
                {rr.source && <Text style={s.meta}>{rr.source}</Text>}
                {rr.affectedWards && <Text style={s.meta}>Wards: {rr.affectedWards}</Text>}
              </View>
              {rr.responseText && <Text style={s.response} numberOfLines={2}>? {rr.responseText}</Text>}
              <View style={s.actionRow}>
                <Text style={s.time}>{new Date(rr.detectedAt).toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}</Text>
                {rr.status !== 'Resolved' && (
                  <TouchableOpacity style={s.actionBtn} onPress={() => { setActionItem(rr); setNewStatus(rr.status); setNewResp(rr.responseText ?? ''); }}>
                    <Ionicons name="create-outline" size={14} color={BRAND} />
                    <Text style={s.actionTxt}>Update</Text>
                  </TouchableOpacity>
                )}
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
              <Text style={fm.title}>Log Incident</Text>
              <TouchableOpacity onPress={() => setShowAdd(false)}><Ionicons name="close" size={24} color="#212529" /></TouchableOpacity>
            </View>
            <ScrollView contentContainerStyle={{ padding: 16 }}>
              <Text style={fm.label}>Title *</Text>
              <TextInput style={fm.input} value={title} onChangeText={setTitle} placeholder="Brief incident title" />
              <Text style={fm.label}>Description *</Text>
              <TextInput style={[fm.input, { height: 80, textAlignVertical: 'top' }]} value={desc} onChangeText={setDesc} multiline placeholder="What happened?" />
              <Text style={fm.label}>Source</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 16 }}>
                {SOURCES.map(src => (
                  <TouchableOpacity key={src} style={[fm.chip, source === src && fm.chipActive]} onPress={() => setSource(source === src ? '' : src)}>
                    <Text style={[fm.chipTxt, source === src && { color: '#fff' }]}>{src}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
              <Text style={fm.label}>Affected Wards</Text>
              <TextInput style={fm.input} value={wards} onChangeText={setWards} placeholder="e.g. 3, 5, 7" />
              <Text style={fm.label}>Threat Level</Text>
              <View style={{ flexDirection: 'row', gap: 8, marginBottom: 16 }}>
                {THREAT_LEVELS.map(t => (
                  <TouchableOpacity key={t.key} style={[fm.chip, threat === t.key && { backgroundColor: t.color, borderColor: t.color }]} onPress={() => setThreat(t.key)}>
                    <Text style={[fm.chipTxt, threat === t.key && { color: '#fff' }]}>{t.label}</Text>
                  </TouchableOpacity>
                ))}
              </View>
              <Text style={fm.label}>Initial Response</Text>
              <TextInput style={[fm.input, { height: 70, textAlignVertical: 'top' }]} value={response} onChangeText={setResponse} multiline placeholder="Response drafted / action taken..." />
              <TouchableOpacity style={[fm.saveBtn, saving && { opacity: 0.6 }]} onPress={handleCreate} disabled={saving}>
                {saving ? <ActivityIndicator color="#fff" /> : <Text style={fm.saveTxt}>Log Incident</Text>}
              </TouchableOpacity>
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
      </Modal>

      {/* Update Status Modal */}
      <Modal visible={!!actionItem} transparent animationType="slide">
        <View style={fm.overlay}>
          <View style={fm.sheet}>
            <Text style={fm.title}>Update — {actionItem?.title}</Text>
            <Text style={fm.label}>Status</Text>
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 16 }}>
              {RR_STATUSES.map(st => (
                <TouchableOpacity key={st.key} style={[fm.chip, newStatus === st.key && { backgroundColor: st.color, borderColor: st.color }]} onPress={() => setNewStatus(st.key)}>
                  <Text style={[fm.chipTxt, newStatus === st.key && { color: '#fff' }]}>{st.label}</Text>
                </TouchableOpacity>
              ))}
            </View>
            <Text style={fm.label}>Response / Actions Taken</Text>
            <TextInput style={[fm.input, { height: 80, textAlignVertical: 'top' }]} value={newResp} onChangeText={setNewResp} multiline placeholder="What was done..." />
            <TouchableOpacity style={[fm.saveBtn, saving && { opacity: 0.6 }]} onPress={handleAction} disabled={saving}>
              {saving ? <ActivityIndicator color="#fff" /> : <Text style={fm.saveTxt}>Update Status</Text>}
            </TouchableOpacity>
            <TouchableOpacity style={{ padding: 12 }} onPress={() => setActionItem(null)}>
              <Text style={{ color: '#868e96', fontWeight: '600', textAlign: 'center' }}>Cancel</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </View>
  );
}

const s = StyleSheet.create({
  container:  { flex: 1, backgroundColor: '#f0f2f5' },
  center:     { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:     { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:      { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:        { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:     { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  filterBar:  { maxHeight: 52, paddingVertical: 8 },
  chip:       { paddingHorizontal: 14, paddingVertical: 6, borderRadius: 20, borderWidth: 1, borderColor: '#dee2e6', backgroundColor: '#fff' },
  chipActive: { backgroundColor: '#1a1f2e', borderColor: '#1a1f2e' },
  chipTxt:    { fontSize: 12, fontWeight: '600', color: '#495057' },
  card:       { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10, elevation: 1 },
  cardTop:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 6 },
  cardTitle:  { fontSize: 14, fontWeight: '700', color: '#212529', flex: 1, marginRight: 8 },
  badge:      { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  badgeTxt:   { fontSize: 10, fontWeight: '800' },
  desc:       { fontSize: 12, color: '#495057', marginBottom: 8 },
  metaRow:    { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 6 },
  meta:       { fontSize: 11, color: '#adb5bd' },
  response:   { fontSize: 12, color: '#3b5bdb', marginBottom: 6, fontStyle: 'italic' },
  actionRow:  { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  time:       { fontSize: 11, color: '#adb5bd' },
  actionBtn:  { flexDirection: 'row', alignItems: 'center', gap: 4 },
  actionTxt:  { fontSize: 12, color: BRAND, fontWeight: '600' },
  empty:      { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:   { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
const fm = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529', marginBottom: 8 },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 14, color: '#212529', marginBottom: 16 },
  chip:      { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20, borderWidth: 1, borderColor: '#dee2e6' },
  chipActive:{ backgroundColor: BRAND, borderColor: BRAND },
  chipTxt:   { fontSize: 12, fontWeight: '600', color: '#495057' },
  overlay:   { flex: 1, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'flex-end' },
  sheet:     { backgroundColor: '#fff', borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 20 },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, alignItems: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});
