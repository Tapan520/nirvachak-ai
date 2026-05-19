import React, { useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput,
  ScrollView, Alert, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getVolunteers, createVolunteer, VolunteerItem } from '../api/volunteers';

const TASK_COLOR: Record<string, string> = {
  BoothManagement: '#3b5bdb', VoterOutreach: '#2f9e44', DataEntry: '#f59f00',
  Transport: '#e67700', Communication: '#7950f2', Other: '#868e96',
};

const TASK_OPTIONS = [
  'BoothManagement', 'VoterOutreach', 'DataEntry', 'Transport', 'Communication', 'Other',
];

// ??? Add Volunteer Modal ??????????????????????????????????????????????????????

interface AddModalProps {
  visible: boolean;
  onClose: () => void;
  onAdded: () => void;
}

function AddVolunteerModal({ visible, onClose, onAdded }: AddModalProps) {
  const [name,         setName]         = useState('');
  const [phone,        setPhone]        = useState('');
  const [email,        setEmail]        = useState('');
  const [task,         setTask]         = useState('BoothManagement');
  const [area,         setArea]         = useState('');
  const [booths,       setBooths]       = useState('');
  const [notes,        setNotes]        = useState('');
  const [submitting,   setSubmitting]   = useState(false);

  const reset = () => {
    setName(''); setPhone(''); setEmail(''); setTask('BoothManagement');
    setArea(''); setBooths(''); setNotes('');
  };

  const submit = async () => {
    if (!name.trim() || !phone.trim()) {
      Alert.alert('Required', 'Name and phone are required.'); return;
    }
    try {
      setSubmitting(true);
      await createVolunteer({
        name: name.trim(), phone: phone.trim(),
        email: email.trim() || undefined,
        task,
        assignedArea: area.trim() || undefined,
        assignedBoothNumbers: booths.trim() || undefined,
        notes: notes.trim() || undefined,
      });
      reset();
      onAdded();
    } catch {
      Alert.alert('Error', 'Failed to add volunteer. Please try again.');
    } finally { setSubmitting(false); }
  };

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={pm.container}>
          <View style={pm.header}>
            <Text style={pm.headerTitle}>Add Volunteer</Text>
            <TouchableOpacity onPress={() => { reset(); onClose(); }}>
              <Ionicons name="close" size={24} color="#212529" />
            </TouchableOpacity>
          </View>

          <ScrollView contentContainerStyle={{ padding: 16 }}>
            <Text style={pm.label}>Full Name <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={pm.input} placeholder="Enter name"
              value={name} onChangeText={setName} />

            <Text style={pm.label}>Phone <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={pm.input} placeholder="Mobile number"
              value={phone} onChangeText={setPhone} keyboardType="phone-pad" />

            <Text style={pm.label}>Email</Text>
            <TextInput style={pm.input} placeholder="Optional email"
              value={email} onChangeText={setEmail} keyboardType="email-address" autoCapitalize="none" />

            <Text style={pm.label}>Task / Role</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 16 }}>
              {TASK_OPTIONS.map(t => {
                const color = TASK_COLOR[t] ?? '#868e96';
                const active = task === t;
                return (
                  <TouchableOpacity key={t}
                    style={[pm.chip, active && { backgroundColor: color, borderColor: color }]}
                    onPress={() => setTask(t)}>
                    <Text style={[pm.chipText, active && { color: '#fff' }]}>
                      {t.replace(/([A-Z])/g, ' $1').trim()}
                    </Text>
                  </TouchableOpacity>
                );
              })}
            </ScrollView>

            <Text style={pm.label}>Assigned Area</Text>
            <TextInput style={pm.input} placeholder="Ward / Sector / Area"
              value={area} onChangeText={setArea} />

            <Text style={pm.label}>Assigned Booth Numbers</Text>
            <TextInput style={pm.input} placeholder="e.g. 12, 13, 15"
              value={booths} onChangeText={setBooths} />

            <Text style={pm.label}>Notes</Text>
            <TextInput style={[pm.input, pm.textArea]} placeholder="Any additional notes..."
              value={notes} onChangeText={setNotes} multiline numberOfLines={3}
              textAlignVertical="top" />

            <TouchableOpacity
              style={[pm.submitBtn, submitting && { opacity: 0.6 }]}
              onPress={submit} disabled={submitting}>
              {submitting
                ? <ActivityIndicator color="#fff" />
                : <><Ionicons name="person-add-outline" size={18} color="#fff" />
                   <Text style={pm.submitBtnText}> Add Volunteer</Text></>
              }
            </TouchableOpacity>
          </ScrollView>
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

// ??? Main Screen ?????????????????????????????????????????????????????????????

export default function VolunteersScreen() {
  const [volunteers, setVolunteers] = useState<VolunteerItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showModal,  setShowModal]  = useState(false);

  const load = async () => {
    try { setVolunteers(await getVolunteers()); }
    finally { setLoading(false); setRefreshing(false); }
  };

  useEffect(() => { load(); }, []);

  if (loading) return <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>;

  const active   = volunteers.filter(v => v.isActive);
  const inactive = volunteers.filter(v => !v.isActive);

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View>
          <Text style={s.title}>Volunteers</Text>
          <Text style={s.sub}>{active.length} active &bull; {inactive.length} inactive</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowModal(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      {/* Task summary */}
      <View style={s.summaryRow}>
        {Object.entries(
          volunteers.reduce((acc, v) => {
            acc[v.task] = (acc[v.task] || 0) + 1;
            return acc;
          }, {} as Record<string, number>)
        ).slice(0, 4).map(([task, count]) => (
          <View key={task} style={[s.summaryCard, { borderTopColor: TASK_COLOR[task] ?? '#868e96' }]}>
            <Text style={[s.summaryCount, { color: TASK_COLOR[task] ?? '#868e96' }]}>{count}</Text>
            <Text style={s.summaryLabel}>{task.replace(/([A-Z])/g, ' $1').trim()}</Text>
          </View>
        ))}
      </View>

      <FlatList
        data={volunteers}
        keyExtractor={v => v.id.toString()}
        contentContainerStyle={{ padding: 12 }}
        refreshControl={<RefreshControl refreshing={refreshing}
          onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.center}><Text style={{ color: '#868e96' }}>No volunteers found.</Text></View>
        }
        renderItem={({ item: v }) => (
          <View style={[s.card, !v.isActive && s.inactiveCard]}>
            <View style={[s.avatar, { backgroundColor: (TASK_COLOR[v.task] ?? '#868e96') + '22' }]}>
              <Ionicons name="person-outline" size={20} color={TASK_COLOR[v.task] ?? '#868e96'} />
            </View>
            <View style={{ flex: 1, marginLeft: 12 }}>
              <View style={s.nameRow}>
                <Text style={s.name}>{v.name}</Text>
                {!v.isActive && (
                  <View style={s.inactiveBadge}><Text style={s.inactiveTxt}>Inactive</Text></View>
                )}
              </View>
              <Text style={s.phone}>{v.phone}</Text>
              <View style={s.metaRow}>
                <View style={[s.taskBadge, { backgroundColor: (TASK_COLOR[v.task] ?? '#868e96') + '18' }]}>
                  <Text style={[s.taskTxt, { color: TASK_COLOR[v.task] ?? '#868e96' }]}>
                    {v.task.replace(/([A-Z])/g, ' $1').trim()}
                  </Text>
                </View>
                {v.assignedArea && <Text style={s.area}>{v.assignedArea}</Text>}
              </View>
              {v.assignedBoothNumbers && (
                <Text style={s.booths}>Booths: {v.assignedBoothNumbers}</Text>
              )}
            </View>
          </View>
        )}
      />

      <AddVolunteerModal
        visible={showModal}
        onClose={() => setShowModal(false)}
        onAdded={() => { setShowModal(false); load(); }}
      />
    </View>
  );
}

// ??? Styles ???????????????????????????????????????????????????????????????????

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16,
                  flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-end' },
  title:        { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:       { backgroundColor: '#3b5bdb', borderRadius: 10, padding: 8 },
  summaryRow:   { flexDirection: 'row', backgroundColor: '#fff', marginHorizontal: 12,
                  marginTop: 12, borderRadius: 12, overflow: 'hidden', elevation: 1 },
  summaryCard:  { flex: 1, alignItems: 'center', padding: 12, borderTopWidth: 3 },
  summaryCount: { fontSize: 20, fontWeight: '800' },
  summaryLabel: { fontSize: 10, color: '#868e96', textAlign: 'center', marginTop: 2 },
  card:         { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10,
                  flexDirection: 'row', alignItems: 'flex-start', elevation: 1 },
  inactiveCard: { opacity: 0.6 },
  avatar:       { width: 44, height: 44, borderRadius: 10, justifyContent: 'center', alignItems: 'center' },
  nameRow:      { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 2 },
  name:         { fontSize: 15, fontWeight: '700', color: '#212529' },
  inactiveBadge:{ backgroundColor: '#f1f3f5', borderRadius: 4, paddingHorizontal: 6, paddingVertical: 2 },
  inactiveTxt:  { fontSize: 10, color: '#868e96', fontWeight: '600' },
  phone:        { fontSize: 12, color: '#4dabf7', marginBottom: 6 },
  metaRow:      { flexDirection: 'row', alignItems: 'center', gap: 8 },
  taskBadge:    { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  taskTxt:      { fontSize: 11, fontWeight: '700' },
  area:         { fontSize: 11, color: '#868e96' },
  booths:       { fontSize: 11, color: '#495057', marginTop: 4 },
});

const pm = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#fff' },
  header:       { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
                  paddingHorizontal: 16, paddingVertical: 16,
                  borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  headerTitle:  { fontSize: 18, fontWeight: '700', color: '#212529' },
  label:        { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:        { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10,
                  paddingHorizontal: 14, paddingVertical: 10, fontSize: 14,
                  color: '#212529', marginBottom: 16 },
  textArea:     { height: 80, textAlignVertical: 'top' },
  chip:         { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20,
                  borderWidth: 1, borderColor: '#dee2e6', marginRight: 8 },
  chipText:     { fontSize: 12, fontWeight: '600', color: '#495057' },
  submitBtn:    { backgroundColor: '#3b5bdb', borderRadius: 12, flexDirection: 'row',
                  alignItems: 'center', justifyContent: 'center',
                  paddingVertical: 14, marginBottom: 16 },
  submitBtnText:{ color: '#fff', fontSize: 15, fontWeight: '700' },
});
