import React, { useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput,
  ScrollView, Alert, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getCampaignEvents, createCampaignEvent, CampaignEventItem } from '../api/campaign';

const TYPE_ICON: Record<string, string> = {
  Rally: 'megaphone-outline', DoorToDoor: 'walk-outline',
  SmallMeeting: 'people-outline', LargeMeeting: 'business-outline',
  PhoneCall: 'call-outline', Other: 'calendar-outline',
};
const TYPE_COLOR: Record<string, string> = {
  Rally: '#e03131', DoorToDoor: '#2f9e44', SmallMeeting: '#3b5bdb',
  LargeMeeting: '#7950f2', PhoneCall: '#f59f00', Other: '#868e96',
};
const EVENT_TYPES = ['Rally', 'DoorToDoor', 'SmallMeeting', 'LargeMeeting', 'PhoneCall', 'Other'];

// ??? Add Event Modal ??????????????????????????????????????????????????????????

interface AddModalProps {
  visible: boolean;
  onClose: () => void;
  onAdded: () => void;
}

function AddEventModal({ visible, onClose, onAdded }: AddModalProps) {
  const [title,     setTitle]     = useState('');
  const [eventType, setEventType] = useState('Rally');
  const [location,  setLocation]  = useState('');
  const [date,      setDate]      = useState('');
  const [expected,  setExpected]  = useState('');
  const [desc,      setDesc]      = useState('');
  const [wards,     setWards]     = useState('');
  const [submitting,setSubmitting]= useState(false);

  const reset = () => {
    setTitle(''); setEventType('Rally'); setLocation('');
    setDate(''); setExpected(''); setDesc(''); setWards('');
  };

  const submit = async () => {
    if (!title.trim() || !location.trim() || !date.trim()) {
      Alert.alert('Required', 'Title, location and scheduled date/time are required.'); return;
    }
    const scheduledAt = new Date(date);
    if (isNaN(scheduledAt.getTime())) {
      Alert.alert('Invalid Date', 'Use format: YYYY-MM-DDTHH:MM'); return;
    }
    try {
      setSubmitting(true);
      await createCampaignEvent({
        title: title.trim(),
        eventType,
        location: location.trim(),
        scheduledAt: scheduledAt.toISOString(),
        expectedAttendance: expected ? parseInt(expected) : undefined,
        description: desc.trim() || undefined,
        targetWards: wards.trim() || undefined,
      });
      reset();
      onAdded();
    } catch {
      Alert.alert('Error', 'Failed to create event. Please try again.');
    } finally { setSubmitting(false); }
  };

  const color = TYPE_COLOR[eventType] ?? '#868e96';

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={pm.container}>
          <View style={pm.header}>
            <Text style={pm.headerTitle}>New Campaign Event</Text>
            <TouchableOpacity onPress={() => { reset(); onClose(); }}>
              <Ionicons name="close" size={24} color="#212529" />
            </TouchableOpacity>
          </View>

          <ScrollView contentContainerStyle={{ padding: 16 }}>
            <Text style={pm.label}>Event Type</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 16 }}>
              {EVENT_TYPES.map(t => {
                const c = TYPE_COLOR[t] ?? '#868e96';
                const active = eventType === t;
                return (
                  <TouchableOpacity key={t}
                    style={[pm.chip, active && { backgroundColor: c, borderColor: c }]}
                    onPress={() => setEventType(t)}>
                    <Ionicons name={(TYPE_ICON[t] ?? 'calendar-outline') as any}
                      size={13} color={active ? '#fff' : c} />
                    <Text style={[pm.chipText, active && { color: '#fff' }]}>
                      {' '}{t.replace(/([A-Z])/g, ' $1').trim()}
                    </Text>
                  </TouchableOpacity>
                );
              })}
            </ScrollView>

            <Text style={pm.label}>Title <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={pm.input} placeholder="Event title"
              value={title} onChangeText={setTitle} />

            <Text style={pm.label}>Location <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={pm.input} placeholder="Venue / area"
              value={location} onChangeText={setLocation} />

            <Text style={pm.label}>Scheduled At <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={pm.input} placeholder="YYYY-MM-DDTHH:MM"
              value={date} onChangeText={setDate} />
            <Text style={pm.hint}>Format: 2025-11-14T10:30</Text>

            <Text style={pm.label}>Expected Attendance</Text>
            <TextInput style={pm.input} placeholder="Number of expected attendees"
              value={expected} onChangeText={setExpected} keyboardType="number-pad" />

            <Text style={pm.label}>Target Wards</Text>
            <TextInput style={pm.input} placeholder="e.g. Ward 3, Ward 5"
              value={wards} onChangeText={setWards} />

            <Text style={pm.label}>Description</Text>
            <TextInput style={[pm.input, pm.textArea]} placeholder="Event details..."
              value={desc} onChangeText={setDesc} multiline numberOfLines={4}
              textAlignVertical="top" />

            <TouchableOpacity
              style={[pm.submitBtn, { backgroundColor: color }, submitting && { opacity: 0.6 }]}
              onPress={submit} disabled={submitting}>
              {submitting
                ? <ActivityIndicator color="#fff" />
                : <><Ionicons name="add-circle-outline" size={18} color="#fff" />
                   <Text style={pm.submitBtnText}> Create Event</Text></>
              }
            </TouchableOpacity>
          </ScrollView>
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

// ??? Main Screen ?????????????????????????????????????????????????????????????

export default function CampaignEventsScreen() {
  const [events,      setEvents]      = useState<CampaignEventItem[]>([]);
  const [loading,     setLoading]     = useState(true);
  const [refreshing,  setRefreshing]  = useState(false);
  const [showUpcoming,setShowUpcoming]= useState(false);
  const [showModal,   setShowModal]   = useState(false);

  const load = async (upcoming = showUpcoming) => {
    try { setEvents(await getCampaignEvents(upcoming)); }
    finally { setLoading(false); setRefreshing(false); }
  };

  useEffect(() => { load(); }, []);

  const toggleFilter = () => {
    const next = !showUpcoming;
    setShowUpcoming(next);
    setLoading(true);
    load(next);
  };

  if (loading) return <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View>
          <Text style={s.title}>Campaign Events</Text>
          <Text style={s.sub}>{events.length} events</Text>
        </View>
        <View style={s.headerActions}>
          <TouchableOpacity style={[s.filterBtn, showUpcoming && s.filterActive]} onPress={toggleFilter}>
            <Ionicons name="calendar-outline" size={16} color={showUpcoming ? '#fff' : '#3b5bdb'} />
            <Text style={[s.filterTxt, showUpcoming && { color: '#fff' }]}>Upcoming</Text>
          </TouchableOpacity>
          <TouchableOpacity style={s.addBtn} onPress={() => setShowModal(true)}>
            <Ionicons name="add" size={22} color="#fff" />
          </TouchableOpacity>
        </View>
      </View>

      <FlatList
        data={events}
        keyExtractor={e => e.id.toString()}
        contentContainerStyle={{ padding: 12 }}
        refreshControl={<RefreshControl refreshing={refreshing}
          onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.center}><Text style={{ color: '#868e96' }}>No events found.</Text></View>
        }
        renderItem={({ item: ev }) => {
          const color = TYPE_COLOR[ev.eventType] ?? '#868e96';
          const icon  = TYPE_ICON[ev.eventType]  ?? 'calendar-outline';
          const date  = new Date(ev.scheduledAt);
          return (
            <View style={[s.card, ev.isCompleted && s.completedCard]}>
              <View style={[s.iconBox, { backgroundColor: color + '18' }]}>
                <Ionicons name={icon as any} size={22} color={color} />
              </View>
              <View style={{ flex: 1, marginLeft: 12 }}>
                <View style={s.cardTop}>
                  <Text style={s.cardTitle} numberOfLines={1}>{ev.title}</Text>
                  {ev.isCompleted && <Ionicons name="checkmark-circle" size={18} color="#2f9e44" />}
                </View>
                <View style={[s.typeBadge, { backgroundColor: color + '18' }]}>
                  <Text style={[s.typeTxt, { color }]}>{ev.eventType.replace(/([A-Z])/g, ' $1').trim()}</Text>
                </View>
                <Text style={s.location}>{ev.location}</Text>
                <Text style={s.dateTime}>
                  {date.toLocaleDateString('en-IN')}  {date.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' })}
                </Text>
                {(ev.expectedAttendance != null || ev.actualAttendance != null) && (
                  <Text style={s.attendance}>
                    Expected: {ev.expectedAttendance ?? '-'}
                    {ev.actualAttendance != null ? `  |  Actual: ${ev.actualAttendance}` : ''}
                  </Text>
                )}
                {ev.organizedByName && <Text style={s.organizer}>{ev.organizedByName}</Text>}
                {ev.targetWards && <Text style={s.wards}>Wards: {ev.targetWards}</Text>}
              </View>
            </View>
          );
        }}
      />

      <AddEventModal
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
  headerActions:{ flexDirection: 'row', alignItems: 'center', gap: 8 },
  filterBtn:    { flexDirection: 'row', alignItems: 'center', gap: 6,
                  borderWidth: 1, borderColor: '#3b5bdb', borderRadius: 8,
                  paddingHorizontal: 10, paddingVertical: 6 },
  filterActive: { backgroundColor: '#3b5bdb' },
  filterTxt:    { color: '#3b5bdb', fontSize: 13, fontWeight: '600' },
  addBtn:       { backgroundColor: '#3b5bdb', borderRadius: 10, padding: 8 },
  card:         { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10,
                  flexDirection: 'row', alignItems: 'flex-start', elevation: 1 },
  completedCard:{ opacity: 0.65 },
  iconBox:      { width: 48, height: 48, borderRadius: 10, justifyContent: 'center', alignItems: 'center' },
  cardTop:      { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 },
  cardTitle:    { fontSize: 15, fontWeight: '700', color: '#212529', flex: 1 },
  typeBadge:    { alignSelf: 'flex-start', borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3, marginBottom: 6 },
  typeTxt:      { fontSize: 11, fontWeight: '700' },
  location:     { fontSize: 12, color: '#495057', marginBottom: 2 },
  dateTime:     { fontSize: 12, color: '#495057', marginBottom: 2 },
  attendance:   { fontSize: 11, color: '#868e96', marginBottom: 2 },
  organizer:    { fontSize: 11, color: '#4dabf7' },
  wards:        { fontSize: 11, color: '#868e96', marginTop: 2 },
});

const pm = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#fff' },
  header:       { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
                  paddingHorizontal: 16, paddingVertical: 16,
                  borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  headerTitle:  { fontSize: 18, fontWeight: '700', color: '#212529' },
  label:        { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  hint:         { fontSize: 11, color: '#868e96', marginBottom: 14 },
  input:        { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10,
                  paddingHorizontal: 14, paddingVertical: 10, fontSize: 14,
                  color: '#212529', marginBottom: 16 },
  textArea:     { height: 100, textAlignVertical: 'top' },
  chip:         { flexDirection: 'row', alignItems: 'center',
                  paddingHorizontal: 12, paddingVertical: 8, borderRadius: 20,
                  borderWidth: 1, borderColor: '#dee2e6', marginRight: 8 },
  chipText:     { fontSize: 12, fontWeight: '600', color: '#495057' },
  submitBtn:    { borderRadius: 12, flexDirection: 'row', alignItems: 'center',
                  justifyContent: 'center', paddingVertical: 14, marginBottom: 16 },
  submitBtnText:{ color: '#fff', fontSize: 15, fontWeight: '700' },
});
