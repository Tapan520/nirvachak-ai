import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput,
  ScrollView, Alert, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getBoothShifts, createBoothShift, confirmShift, BoothShiftItem, SHIFT_ROLES } from '../api/boothShifts';
import { getVolunteers, VolunteerItem } from '../api/volunteers';

const BRAND = '#1971c2';
const ROLE_COLOR: Record<string, string> = {
  BoothAgent: '#3b5bdb', Coordinator: '#7950f2', Transport: '#f59f00',
  Security: '#e03131', Observer: '#2f9e44', Other: '#868e96',
};

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' });
}
function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' });
}

export default function BoothShiftsScreen() {
  const [shifts,     setShifts]     = useState<BoothShiftItem[]>([]);
  const [volunteers, setVolunteers] = useState<VolunteerItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showModal,  setShowModal]  = useState(false);
  // form
  const [volId,    setVolId]    = useState('');
  const [booth,    setBooth]    = useState('');
  const [start,    setStart]    = useState('');
  const [end,      setEnd]      = useState('');
  const [role,     setRole]     = useState('BoothAgent');
  const [notes,    setNotes]    = useState('');
  const [saving,   setSaving]   = useState(false);

  const load = useCallback(async () => {
    try {
      const [s, v] = await Promise.all([getBoothShifts(), getVolunteers()]);
      setShifts(s); setVolunteers(v);
    } finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleConfirm = async (id: number) => {
    try { await confirmShift(id); load(); } catch { Alert.alert('Error', 'Could not confirm shift.'); }
  };

  const handleAdd = async () => {
    if (!volId || !booth || !start || !end) {
      Alert.alert('Required', 'Volunteer, booth, start and end time are required.'); return;
    }
    setSaving(true);
    try {
      await createBoothShift({
        volunteerId: parseInt(volId, 10), boothNumber: parseInt(booth, 10),
        shiftStart: new Date(start).toISOString(),
        shiftEnd:   new Date(end).toISOString(),
        role, notes: notes || undefined,
      });
      setShowModal(false);
      setVolId(''); setBooth(''); setStart(''); setEnd(''); setNotes('');
      load();
    } catch { Alert.alert('Error', 'Failed to assign shift.');
    } finally { setSaving(false); }
  };

  const confirmed   = shifts.filter(s => s.isConfirmed).length;
  const unconfirmed = shifts.filter(s => !s.isConfirmed).length;

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Booth Shifts</Text>
          <Text style={s.sub}>{confirmed} confirmed · {unconfirmed} pending</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowModal(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      <FlatList
        data={shifts}
        keyExtractor={i => i.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.empty}><Ionicons name="calendar-outline" size={48} color="#dee2e6" />
            <Text style={s.emptyTxt}>No shifts assigned yet.</Text></View>
        }
        renderItem={({ item: sh }) => {
          const color = ROLE_COLOR[sh.role] ?? '#868e96';
          return (
            <View style={[s.card, !sh.isConfirmed && s.unconfirmedCard]}>
              <View style={[s.roleBox, { backgroundColor: color + '20' }]}>
                <Ionicons name="person-outline" size={20} color={color} />
              </View>
              <View style={{ flex: 1, marginLeft: 12 }}>
                <Text style={s.name}>{sh.volunteerName}</Text>
                <Text style={s.phone}>{sh.volunteerPhone}</Text>
                <View style={s.row}>
                  <View style={[s.roleBadge, { backgroundColor: color + '18' }]}>
                    <Text style={[s.roleTxt, { color }]}>{sh.role}</Text>
                  </View>
                  <Text style={s.boothTxt}>Booth {sh.boothNumber}</Text>
                </View>
                <Text style={s.time}>
                  {formatDate(sh.shiftStart)}  {formatTime(sh.shiftStart)} – {formatTime(sh.shiftEnd)}
                </Text>
              </View>
              {!sh.isConfirmed ? (
                <TouchableOpacity style={s.confirmBtn} onPress={() => handleConfirm(sh.id)}>
                  <Ionicons name="checkmark" size={16} color="#2f9e44" />
                </TouchableOpacity>
              ) : (
                <Ionicons name="checkmark-circle" size={22} color="#2f9e44" />
              )}
            </View>
          );
        }}
      />

      <Modal visible={showModal} animationType="slide" presentationStyle="pageSheet" onRequestClose={() => setShowModal(false)}>
        <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
          <View style={m.container}>
            <View style={m.header}>
              <Text style={m.title}>Assign Shift</Text>
              <TouchableOpacity onPress={() => setShowModal(false)}>
                <Ionicons name="close" size={24} color="#212529" />
              </TouchableOpacity>
            </View>
            <ScrollView contentContainerStyle={{ padding: 16 }}>
              <Text style={m.label}>Volunteer <Text style={{ color: '#e03131' }}>*</Text></Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 16 }}>
                {volunteers.filter(v => v.isActive).map(v => (
                  <TouchableOpacity key={v.id}
                    style={[m.chip, volId === v.id.toString() && m.chipActive]}
                    onPress={() => setVolId(v.id.toString())}>
                    <Text style={[m.chipTxt, volId === v.id.toString() && { color: '#fff' }]}>{v.name}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>

              <Text style={m.label}>Booth Number <Text style={{ color: '#e03131' }}>*</Text></Text>
              <TextInput style={m.input} value={booth} onChangeText={setBooth} keyboardType="numeric" placeholder="e.g. 12" />

              <Text style={m.label}>Shift Start (YYYY-MM-DDTHH:MM) <Text style={{ color: '#e03131' }}>*</Text></Text>
              <TextInput style={m.input} value={start} onChangeText={setStart} placeholder="2025-11-20T06:00" />

              <Text style={m.label}>Shift End (YYYY-MM-DDTHH:MM) <Text style={{ color: '#e03131' }}>*</Text></Text>
              <TextInput style={m.input} value={end} onChangeText={setEnd} placeholder="2025-11-20T18:00" />

              <Text style={m.label}>Role</Text>
              <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 16 }}>
                {SHIFT_ROLES.map(r => (
                  <TouchableOpacity key={r}
                    style={[m.chip, role === r && { backgroundColor: BRAND, borderColor: BRAND }]}
                    onPress={() => setRole(r)}>
                    <Text style={[m.chipTxt, role === r && { color: '#fff' }]}>{r}</Text>
                  </TouchableOpacity>
                ))}
              </View>

              <Text style={m.label}>Notes</Text>
              <TextInput style={m.input} value={notes} onChangeText={setNotes} placeholder="Optional" />

              <TouchableOpacity style={[m.saveBtn, saving && { opacity: 0.6 }]} onPress={handleAdd} disabled={saving}>
                {saving ? <ActivityIndicator color="#fff" /> : <Text style={m.saveTxt}>Assign Shift</Text>}
              </TouchableOpacity>
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
      </Modal>
    </View>
  );
}

const s = StyleSheet.create({
  container:      { flex: 1, backgroundColor: '#f0f2f5' },
  center:         { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:         { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:          { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:            { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:         { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  card:           { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', elevation: 1 },
  unconfirmedCard:{ borderWidth: 1, borderColor: '#f59f00' },
  roleBox:        { width: 44, height: 44, borderRadius: 10, justifyContent: 'center', alignItems: 'center' },
  name:           { fontSize: 14, fontWeight: '700', color: '#212529' },
  phone:          { fontSize: 12, color: '#4dabf7', marginTop: 1 },
  row:            { flexDirection: 'row', alignItems: 'center', gap: 8, marginTop: 4, marginBottom: 4 },
  roleBadge:      { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  roleTxt:        { fontSize: 11, fontWeight: '700' },
  boothTxt:       { fontSize: 12, color: '#495057' },
  time:           { fontSize: 11, color: '#868e96' },
  confirmBtn:     { backgroundColor: '#d3f9d8', borderRadius: 8, padding: 8 },
  empty:          { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:       { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
const m = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529' },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 14, color: '#212529', marginBottom: 16 },
  chip:      { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20, borderWidth: 1, borderColor: '#dee2e6', marginRight: 8 },
  chipActive:{ backgroundColor: BRAND, borderColor: BRAND },
  chipTxt:   { fontSize: 12, fontWeight: '600', color: '#495057' },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, alignItems: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});
