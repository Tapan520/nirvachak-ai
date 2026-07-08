import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput,
  ScrollView, Alert, Linking, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import {
  getInfluencers, createInfluencer, updateInfluencerMeeting,
  InfluencerItem, ALIGNMENTS, INFLUENCER_CATEGORIES,
} from '../api/influencers';

const BRAND = '#7950f2';

// ??? Alignment pill ??????????????????????????????????????????????????????????

function AlignmentBadge({ alignment }: { alignment: string }) {
  const a = ALIGNMENTS.find(x => x.key === alignment);
  const color = a?.color ?? '#adb5bd';
  return (
    <View style={[ab.badge, { backgroundColor: color + '20' }]}>
      <Text style={[ab.txt, { color }]}>{a?.label ?? alignment}</Text>
    </View>
  );
}
const ab = StyleSheet.create({
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  txt:   { fontSize: 11, fontWeight: '700' },
});

// ??? Add Influencer Modal ????????????????????????????????????????????????????

interface AddModalProps {
  visible: boolean;
  onClose: () => void;
  onAdded: () => void;
}

function AddInfluencerModal({ visible, onClose, onAdded }: AddModalProps) {
  const [name,      setName]      = useState('');
  const [phone,     setPhone]     = useState('');
  const [category,  setCategory]  = useState('');
  const [community, setCommunity] = useState('');
  const [followers, setFollowers] = useState('');
  const [ward,      setWard]      = useState('');
  const [alignment, setAlignment] = useState('Unknown');
  const [notes,     setNotes]     = useState('');
  const [saving,    setSaving]    = useState(false);

  const reset = () => {
    setName(''); setPhone(''); setCategory(''); setCommunity('');
    setFollowers(''); setWard(''); setAlignment('Unknown'); setNotes('');
  };

  const submit = async () => {
    if (!name.trim()) { Alert.alert('Required', 'Name is required.'); return; }
    setSaving(true);
    try {
      await createInfluencer({
        name: name.trim(),
        mobileNumber: phone.trim() || undefined,
        category: category || undefined,
        community: community.trim() || undefined,
        estimatedFollowers: followers ? parseInt(followers, 10) : undefined,
        ward: ward.trim() || undefined,
        alignment,
        notes: notes.trim() || undefined,
      });
      reset();
      onAdded();
    } catch {
      Alert.alert('Error', 'Failed to add influencer.');
    } finally { setSaving(false); }
  };

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={am.container}>
          <View style={am.header}>
            <Text style={am.title}>Add Influencer</Text>
            <TouchableOpacity onPress={() => { reset(); onClose(); }}>
              <Ionicons name="close" size={24} color="#212529" />
            </TouchableOpacity>
          </View>
          <ScrollView contentContainerStyle={{ padding: 16 }}>
            <Text style={am.label}>Full Name <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={am.input} placeholder="Influencer name"
              value={name} onChangeText={setName} />

            <Text style={am.label}>Mobile Number</Text>
            <TextInput style={am.input} placeholder="Optional"
              value={phone} onChangeText={setPhone} keyboardType="phone-pad" />

            <Text style={am.label}>Category</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 16 }}>
              {INFLUENCER_CATEGORIES.map(c => (
                <TouchableOpacity key={c}
                  style={[am.chip, category === c && { backgroundColor: BRAND, borderColor: BRAND }]}
                  onPress={() => setCategory(category === c ? '' : c)}>
                  <Text style={[am.chipTxt, category === c && { color: '#fff' }]}>{c}</Text>
                </TouchableOpacity>
              ))}
            </ScrollView>

            <Text style={am.label}>Community / Caste Group</Text>
            <TextInput style={am.input} placeholder="e.g. Patel, Brahmin, Yadav…"
              value={community} onChangeText={setCommunity} />

            <Text style={am.label}>Estimated Reach / Followers</Text>
            <TextInput style={am.input} placeholder="e.g. 5000"
              value={followers} onChangeText={setFollowers} keyboardType="numeric" />

            <Text style={am.label}>Ward</Text>
            <TextInput style={am.input} placeholder="Assigned ward"
              value={ward} onChangeText={setWard} />

            <Text style={am.label}>Current Alignment</Text>
            <View style={am.chipRow}>
              {ALIGNMENTS.map(a => {
                const active = alignment === a.key;
                return (
                  <TouchableOpacity key={a.key}
                    style={[am.chip, active && { backgroundColor: a.color, borderColor: a.color }]}
                    onPress={() => setAlignment(a.key)}>
                    <Text style={[am.chipTxt, active && { color: '#fff' }]}>{a.label}</Text>
                  </TouchableOpacity>
                );
              })}
            </View>

            <Text style={am.label}>Notes</Text>
            <TextInput style={[am.input, am.textArea]} placeholder="Any notes…"
              value={notes} onChangeText={setNotes} multiline numberOfLines={3}
              textAlignVertical="top" />

            <TouchableOpacity style={[am.saveBtn, saving && { opacity: 0.6 }]}
              onPress={submit} disabled={saving}>
              {saving
                ? <ActivityIndicator color="#fff" />
                : <><Ionicons name="person-add-outline" size={18} color="#fff" />
                   <Text style={am.saveTxt}> Add Influencer</Text></>}
            </TouchableOpacity>
          </ScrollView>
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const am = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529' },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10,
    paddingHorizontal: 14, paddingVertical: 10, fontSize: 14,
    color: '#212529', marginBottom: 16 },
  textArea:  { height: 80 },
  chipRow:   { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 16 },
  chip:      { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20,
    borderWidth: 1, borderColor: '#dee2e6', marginRight: 8 },
  chipTxt:   { fontSize: 12, fontWeight: '600', color: '#495057' },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, flexDirection: 'row',
    alignItems: 'center', justifyContent: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});

// ??? Update Meeting Modal ????????????????????????????????????????????????????

interface MeetingModalProps {
  visible: boolean;
  influencer: InfluencerItem | null;
  onClose: () => void;
  onUpdated: () => void;
}

function UpdateMeetingModal({ visible, influencer, onClose, onUpdated }: MeetingModalProps) {
  const [alignment, setAlignment] = useState('Unknown');
  const [outcome,   setOutcome]   = useState('');
  const [notes,     setNotes]     = useState('');
  const [saving,    setSaving]    = useState(false);

  useEffect(() => {
    if (influencer) { setAlignment(influencer.alignment); setOutcome(''); setNotes(''); }
  }, [influencer]);

  const submit = async () => {
    if (!influencer) return;
    setSaving(true);
    try {
      await updateInfluencerMeeting(influencer.id, {
        alignment,
        outcomeNotes: outcome.trim() || undefined,
        notes: notes.trim() || undefined,
      });
      onUpdated();
    } catch {
      Alert.alert('Error', 'Failed to update.');
    } finally { setSaving(false); }
  };

  if (!influencer) return null;

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <View style={mm.container}>
        <View style={mm.header}>
          <View>
            <Text style={mm.title}>Log Meeting</Text>
            <Text style={mm.sub}>{influencer.name}</Text>
          </View>
          <TouchableOpacity onPress={onClose}>
            <Ionicons name="close" size={24} color="#212529" />
          </TouchableOpacity>
        </View>
        <ScrollView contentContainerStyle={{ padding: 16 }}>
          <Text style={mm.label}>Post-Meeting Alignment</Text>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 16 }}>
            {ALIGNMENTS.map(a => {
              const active = alignment === a.key;
              return (
                <TouchableOpacity key={a.key}
                  style={[am.chip, active && { backgroundColor: a.color, borderColor: a.color }]}
                  onPress={() => setAlignment(a.key)}>
                  <Text style={[am.chipTxt, active && { color: '#fff' }]}>{a.label}</Text>
                </TouchableOpacity>
              );
            })}
          </View>

          <Text style={mm.label}>Meeting Outcome / Notes</Text>
          <TextInput style={[am.input, { height: 100, textAlignVertical: 'top' }]}
            placeholder="What was discussed? Any commitments?" multiline
            value={outcome} onChangeText={setOutcome} />

          <Text style={mm.label}>Internal Notes (optional)</Text>
          <TextInput style={am.input} placeholder="Private notes"
            value={notes} onChangeText={setNotes} />

          <TouchableOpacity style={[am.saveBtn, saving && { opacity: 0.6 }]}
            onPress={submit} disabled={saving}>
            {saving
              ? <ActivityIndicator color="#fff" />
              : <Text style={am.saveTxt}>Save Meeting Log</Text>}
          </TouchableOpacity>
        </ScrollView>
      </View>
    </Modal>
  );
}

const mm = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start',
    paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529' },
  sub:       { fontSize: 12, color: '#868e96', marginTop: 2 },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 8 },
});

// ??? Main Screen ?????????????????????????????????????????????????????????????

export default function InfluencersScreen() {
  const [items,       setItems]       = useState<InfluencerItem[]>([]);
  const [loading,     setLoading]     = useState(true);
  const [refreshing,  setRefreshing]  = useState(false);
  const [filter,      setFilter]      = useState('');
  const [showAdd,     setShowAdd]     = useState(false);
  const [meetingItem, setMeetingItem] = useState<InfluencerItem | null>(null);

  const load = useCallback(async () => {
    try { setItems(await getInfluencers(filter || undefined)); }
    catch { Alert.alert('Error', 'Could not load influencers.'); }
    finally { setLoading(false); setRefreshing(false); }
  }, [filter]);

  useEffect(() => { load(); }, [load]);

  const totalReach   = items.reduce((s, i) => s + (i.estimatedFollowers ?? 0), 0);
  const favourCount  = items.filter(i => i.alignment === 'Favour').length;
  const unknownCount = items.filter(i => i.alignment === 'Unknown').length;

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      {/* Header */}
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Influencers</Text>
          <Text style={s.sub}>
            {items.length} active · reach {totalReach.toLocaleString('en-IN')} · {favourCount} in favour
          </Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowAdd(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      {/* Summary row */}
      <View style={s.summaryRow}>
        {ALIGNMENTS.map(a => {
          const count = items.filter(i => i.alignment === a.key).length;
          return (
            <TouchableOpacity key={a.key}
              style={[s.summaryCard, filter === a.key && { borderTopColor: a.color }]}
              onPress={() => setFilter(filter === a.key ? '' : a.key)}>
              <Text style={[s.summaryCount, { color: a.color }]}>{count}</Text>
              <Text style={s.summaryLbl}>{a.label}</Text>
            </TouchableOpacity>
          );
        })}
      </View>

      <FlatList
        data={items}
        keyExtractor={i => i.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing}
          onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.empty}>
            <Ionicons name="people-circle-outline" size={52} color="#dee2e6" />
            <Text style={s.emptyTxt}>No influencers found.</Text>
          </View>
        }
        renderItem={({ item: inf }) => {
          const aColor = ALIGNMENTS.find(a => a.key === inf.alignment)?.color ?? '#adb5bd';
          return (
            <View style={s.card}>
              <View style={[s.avatar, { backgroundColor: aColor + '20' }]}>
                <Ionicons name="person-circle-outline" size={24} color={aColor} />
              </View>
              <View style={{ flex: 1, marginLeft: 12 }}>
                <View style={s.nameRow}>
                  <Text style={s.name}>{inf.name}</Text>
                  <AlignmentBadge alignment={inf.alignment} />
                </View>
                {inf.category && (
                  <View style={s.catBadge}>
                    <Text style={s.catTxt}>{inf.category}</Text>
                    {inf.community ? <Text style={s.catTxt}> · {inf.community}</Text> : null}
                  </View>
                )}
                <View style={s.metaRow}>
                  {inf.estimatedFollowers != null && (
                    <Text style={s.meta}>
                      <Ionicons name="people-outline" size={11} /> {inf.estimatedFollowers.toLocaleString('en-IN')} reach
                    </Text>
                  )}
                  {inf.ward && <Text style={s.meta}><Ionicons name="location-outline" size={11} /> {inf.ward}</Text>}
                </View>
                {inf.lastMeetingOutcome && (
                  <Text style={s.lastMeeting} numberOfLines={1}>
                    Last: {inf.lastMeetingOutcome}
                  </Text>
                )}
              </View>
              <View style={s.actionsCol}>
                {inf.mobileNumber && (
                  <TouchableOpacity style={s.iconBtn}
                    onPress={() => Linking.openURL(`tel:${inf.mobileNumber}`)}>
                    <Ionicons name="call-outline" size={18} color="#2f9e44" />
                  </TouchableOpacity>
                )}
                <TouchableOpacity style={[s.iconBtn, { backgroundColor: BRAND + '20' }]}
                  onPress={() => setMeetingItem(inf)}>
                  <Ionicons name="create-outline" size={18} color={BRAND} />
                </TouchableOpacity>
              </View>
            </View>
          );
        }}
      />

      <AddInfluencerModal
        visible={showAdd}
        onClose={() => setShowAdd(false)}
        onAdded={() => { setShowAdd(false); load(); Alert.alert('Added', 'Influencer added successfully.'); }}
      />
      <UpdateMeetingModal
        visible={!!meetingItem}
        influencer={meetingItem}
        onClose={() => setMeetingItem(null)}
        onUpdated={() => { setMeetingItem(null); load(); Alert.alert('Updated', 'Meeting logged.'); }}
      />
    </View>
  );
}

// ??? Styles ??????????????????????????????????????????????????????????????????

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16,
    paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:        { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:       { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  summaryRow:   { flexDirection: 'row', backgroundColor: '#fff', marginHorizontal: 12,
    marginTop: 12, borderRadius: 12, overflow: 'hidden', elevation: 1 },
  summaryCard:  { flex: 1, alignItems: 'center', padding: 10, borderTopWidth: 3,
    borderTopColor: '#f1f3f5' },
  summaryCount: { fontSize: 18, fontWeight: '800' },
  summaryLbl:   { fontSize: 9, color: '#868e96', textAlign: 'center', marginTop: 2 },
  card:         { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10,
    flexDirection: 'row', alignItems: 'flex-start', elevation: 1 },
  avatar:       { width: 44, height: 44, borderRadius: 10, justifyContent: 'center', alignItems: 'center' },
  nameRow:      { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 4, flexWrap: 'wrap' },
  name:         { fontSize: 15, fontWeight: '700', color: '#212529' },
  catBadge:     { flexDirection: 'row', marginBottom: 4 },
  catTxt:       { fontSize: 12, color: '#495057' },
  metaRow:      { flexDirection: 'row', gap: 12, marginBottom: 4 },
  meta:         { fontSize: 11, color: '#868e96' },
  lastMeeting:  { fontSize: 11, color: '#adb5bd', fontStyle: 'italic' },
  actionsCol:   { gap: 8, marginLeft: 8 },
  iconBtn:      { backgroundColor: '#d3f9d8', borderRadius: 8, padding: 8 },
  empty:        { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:     { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
