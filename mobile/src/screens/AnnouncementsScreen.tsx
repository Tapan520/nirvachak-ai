import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Alert, ScrollView,
  Modal, TextInput, Switch, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import {
  AnnouncementItem, CATEGORY_HEX, CATEGORY_ICONS,
  getAnnouncements, acknowledgeAnnouncement,
  createAnnouncement, deactivateAnnouncement,
} from '../api/announcements';
import { useAuth } from '../context/AuthContext';
import { useNavigation } from '@react-navigation/native';

// ??? Constants ???????????????????????????????????????????????????????????????

const CATEGORY_TABS = [
  { key: '',                     label: 'All' },
  { key: 'CriticalAlert',        label: 'Critical' },
  { key: 'CampaignAnnouncement', label: 'Campaign' },
  { key: 'ECComplianceNotice',   label: 'EC Notice' },
  { key: 'DailyBriefing',        label: 'Briefing' },
  { key: 'Motivation',           label: 'Motivation' },
  { key: 'LiveDataNudge',        label: 'Live Nudge' },
];

const CAT_COLOR_NAME: Record<string, string> = {
  CriticalAlert:        'danger',
  ECComplianceNotice:   'warning',
  DailyBriefing:        'info',
  Motivation:           'success',
  LiveDataNudge:        'primary',
  CampaignAnnouncement: 'secondary',
};

const CAT_HINT: Record<string, string> = {
  CampaignAnnouncement: 'Rally schedules, padyatra routes, event timings.',
  CriticalAlert:        'Booth incidents, EVM issues, urgent alerts. Will be PINNED.',
  ECComplianceNotice:   'EC rules, expense limits, model code updates. Recipients must acknowledge.',
  DailyBriefing:        'Morning briefings with voter contact targets.',
  Motivation:           "Candidate's personal motivational message.",
  LiveDataNudge:        'Auto-triggered data nudge based on campaign metrics.',
};

const ROLE_OPTIONS = [
  { key: 'Admin',           label: 'Admin' },
  { key: 'CampaignManager', label: 'Campaign Manager' },
  { key: 'Candidate',       label: 'Candidate' },
  { key: 'FieldWorker',     label: 'Field Worker' },
  { key: 'BoothAgent',      label: 'Booth Agent' },
];

function timeAgo(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const m = Math.floor(diff / 60000);
  if (m < 1)  return 'Just now';
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
}

// ??? Announcement Card ???????????????????????????????????????????????????????

interface CardProps {
  item: AnnouncementItem;
  currentRole: string;
  currentName: string;
  onAck: (id: number) => void;
  onRemove: (id: number) => void;
}

function AnnouncementCard({ item, currentRole, currentName, onAck, onRemove }: CardProps) {
  const hex  = CATEGORY_HEX[item.categoryColor] ?? '#868e96';
  const icon = CATEGORY_ICONS[item.category] ?? 'megaphone';
  const canRemove = currentRole === 'Admin' || item.createdByName === currentName;

  return (
    <View style={[
      s.card,
      item.isPinned && s.cardPinned,
      item.requiresAcknowledgement && !item.isAcknowledged && s.cardNeedsAck,
    ]}>
      {/* Header badges */}
      <View style={s.cardHeader}>
        <View style={[s.iconCircle, { backgroundColor: hex + '22' }]}>
          <Ionicons name={icon as any} size={18} color={hex} />
        </View>
        <View style={[s.catBadge, { backgroundColor: hex + '22' }]}>
          <Text style={[s.catBadgeText, { color: hex }]}>{item.categoryLabel}</Text>
        </View>
        {item.isPinned && (
          <View style={s.pinnedBadge}>
            <Ionicons name="pin" size={10} color="#fff" />
            <Text style={s.pinnedText}>PINNED</Text>
          </View>
        )}
        {item.requiresAcknowledgement && !item.isAcknowledged && (
          <View style={s.needsAckBadge}>
            <Text style={s.needsAckText}>Action Required</Text>
          </View>
        )}
        {item.isAcknowledged && (
          <View style={s.ackedBadge}>
            <Ionicons name="checkmark-circle" size={12} color="#2f9e44" />
            <Text style={s.ackedText}>Acknowledged</Text>
          </View>
        )}
        {item.expiresAt && (
          <View style={s.expiryBadge}>
            <Text style={s.expiryText}>Expires {new Date(item.expiresAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })}</Text>
          </View>
        )}
      </View>

      {/* Title & body */}
      <Text style={s.cardTitle}>{item.title}</Text>
      <Text style={s.cardBody}>{item.body}</Text>

      {/* Meta */}
      <View style={s.metaRow}>
        <Text style={s.metaTxt}><Ionicons name="person-outline" size={11} /> {item.createdByName}</Text>
        <Text style={s.metaTxt}><Ionicons name="time-outline" size={11} /> {timeAgo(item.createdAt)}</Text>
        <Text style={s.metaTxt}>
          <Ionicons name="people-outline" size={11} />{' '}
          {item.targetRoles === 'All' ? 'All roles' : item.targetRoles.split(',').join(', ')}
        </Text>
        {item.requiresAcknowledgement && (
          <Text style={s.metaTxt}><Ionicons name="checkmark-done-outline" size={11} /> {item.acknowledgementCount} acknowledged</Text>
        )}
      </View>

      {/* Actions */}
      <View style={s.actionRow}>
        {item.requiresAcknowledgement && !item.isAcknowledged && (
          <TouchableOpacity style={[s.ackBtn, { backgroundColor: hex }]} onPress={() => onAck(item.id)}>
            <Ionicons name="checkmark-done" size={15} color="#fff" />
            <Text style={s.ackBtnText}>Acknowledge & Confirm</Text>
          </TouchableOpacity>
        )}
        {canRemove && (
          <TouchableOpacity style={s.removeBtn} onPress={() => onRemove(item.id)}>
            <Ionicons name="trash-outline" size={16} color="#adb5bd" />
            <Text style={s.removeBtnText}>Remove</Text>
          </TouchableOpacity>
        )}
      </View>
    </View>
  );
}

// ??? Post Modal ???????????????????????????????????????????????????????????????

interface PostModalProps {
  visible: boolean;
  onClose: () => void;
  onPosted: () => void;
}

function PostModal({ visible, onClose, onPosted }: PostModalProps) {
  const [title,         setTitle]         = useState('');
  const [body,          setBody]          = useState('');
  const [category,      setCategory]      = useState('CampaignAnnouncement');
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [allRoles,      setAllRoles]      = useState(true);
  const [requiresAck,   setRequiresAck]   = useState(false);
  const [expiresAt,     setExpiresAt]     = useState('');
  const [posting,       setPosting]       = useState(false);

  const reset = () => {
    setTitle(''); setBody(''); setCategory('CampaignAnnouncement');
    setSelectedRoles([]); setAllRoles(true); setRequiresAck(false); setExpiresAt('');
  };

  const isEC      = category === 'ECComplianceNotice';
  const isCrit    = category === 'CriticalAlert';
  const effAck    = isEC || requiresAck;
  const hex       = CATEGORY_HEX[CAT_COLOR_NAME[category] ?? 'secondary'] ?? '#868e96';
  const hint      = CAT_HINT[category] ?? '';
  const cats      = CATEGORY_TABS.filter(c => c.key !== '');

  const toggleRole = (role: string) => {
    setAllRoles(false);
    setSelectedRoles(prev =>
      prev.includes(role) ? prev.filter(r => r !== role) : [...prev, role]
    );
  };

  const submit = async () => {
    if (!title.trim() || !body.trim()) {
      Alert.alert('Required', 'Title and message cannot be empty.'); return;
    }
    const targetRoles = allRoles || selectedRoles.length === 0
      ? undefined
      : selectedRoles.join(',');
    try {
      setPosting(true);
      await createAnnouncement({
        title: title.trim(), body: body.trim(), category,
        targetRoles,
        requiresAcknowledgement: effAck,
        expiresAt: expiresAt || undefined,
      });
      reset();
      onPosted();
    } catch {
      Alert.alert('Error', 'Failed to post. Please try again.');
    } finally { setPosting(false); }
  };

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={pm.container}>
          {/* Header */}
          <View style={pm.header}>
            <Text style={pm.headerTitle}>New Announcement</Text>
            <TouchableOpacity onPress={() => { reset(); onClose(); }}>
              <Ionicons name="close" size={24} color="#212529" />
            </TouchableOpacity>
          </View>

          <ScrollView contentContainerStyle={{ padding: 16 }}>

            {/* Category */}
            <Text style={pm.label}>Category <Text style={{ color: '#e03131' }}>*</Text></Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 8 }}>
              {cats.map(c => {
                const cHex = CATEGORY_HEX[CAT_COLOR_NAME[c.key] ?? 'secondary'] ?? '#868e96';
                const active = category === c.key;
                return (
                  <TouchableOpacity key={c.key}
                    style={[pm.catChip, active && { backgroundColor: cHex, borderColor: cHex }]}
                    onPress={() => { setCategory(c.key); if (c.key === 'ECComplianceNotice') setRequiresAck(true); }}>
                    <Ionicons name={(CATEGORY_ICONS[c.key] ?? 'megaphone') as any}
                      size={13} color={active ? '#fff' : cHex} />
                    <Text style={[pm.catChipText, active && { color: '#fff' }]}> {c.label}</Text>
                  </TouchableOpacity>
                );
              })}
            </ScrollView>

            {!!hint && (
              <View style={[pm.hintBox, { backgroundColor: hex + '15', borderLeftColor: hex }]}>
                <Text style={[pm.hintText, { color: hex }]}>{hint}</Text>
              </View>
            )}

            {/* Title */}
            <Text style={pm.label}>Title <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={pm.input} placeholder="Short, clear headline..."
              value={title} onChangeText={setTitle} maxLength={200} />

            {/* Body */}
            <Text style={pm.label}>Message <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={[pm.input, pm.textArea]} placeholder="Write your announcement..."
              value={body} onChangeText={setBody} multiline numberOfLines={6}
              textAlignVertical="top" />

            {/* Target roles */}
            <Text style={pm.label}>Send To</Text>
            <View style={pm.rolesContainer}>
              <TouchableOpacity
                style={[pm.roleChip, allRoles && pm.roleChipActive]}
                onPress={() => { setAllRoles(true); setSelectedRoles([]); }}>
                <Text style={[pm.roleChipText, allRoles && pm.roleChipTextActive]}>All Roles</Text>
              </TouchableOpacity>
              {ROLE_OPTIONS.map(r => {
                const selected = !allRoles && selectedRoles.includes(r.key);
                return (
                  <TouchableOpacity key={r.key}
                    style={[pm.roleChip, selected && pm.roleChipActive]}
                    onPress={() => toggleRole(r.key)}>
                    <Text style={[pm.roleChipText, selected && pm.roleChipTextActive]}>{r.label}</Text>
                  </TouchableOpacity>
                );
              })}
            </View>
            <Text style={pm.hint}>Leave "All Roles" to broadcast to everyone.</Text>

            {/* Expiry */}
            <Text style={pm.label}>Auto-expire at</Text>
            <TextInput style={pm.input}
              placeholder="YYYY-MM-DDTHH:MM (leave blank = no expiry)"
              value={expiresAt} onChangeText={setExpiresAt} />
            <Text style={pm.hint}>Format: 2025-11-15T18:00</Text>

            {/* Requires acknowledgement */}
            <View style={pm.switchRow}>
              <View style={{ flex: 1 }}>
                <Text style={pm.label}>Require acknowledgement</Text>
                <Text style={pm.hint}>Recipients must tap "Acknowledge" — recorded in audit log.
                  {isEC ? '\nAuto-enabled for EC Compliance notices.' : ''}</Text>
              </View>
              <Switch value={effAck} onValueChange={v => { if (!isEC) setRequiresAck(v); }}
                trackColor={{ true: hex }} disabled={isEC} />
            </View>

            {isCrit && (
              <View style={pm.warnBox}>
                <Ionicons name="warning" size={16} color="#e03131" />
                <Text style={pm.warnText}>Critical alerts are automatically pinned at the top of every dashboard.</Text>
              </View>
            )}

            <TouchableOpacity
              style={[pm.postBtn, { backgroundColor: hex }, posting && { opacity: 0.6 }]}
              onPress={submit} disabled={posting}>
              {posting
                ? <ActivityIndicator color="#fff" size="small" />
                : <><Ionicons name="send" size={18} color="#fff" /><Text style={pm.postBtnText}> Post Announcement</Text></>
              }
            </TouchableOpacity>
          </ScrollView>
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

// ??? Main Screen ?????????????????????????????????????????????????????????????

export default function AnnouncementsScreen() {
const { user } = useAuth();
const navigation = useNavigation();
  const [items,      setItems]      = useState<AnnouncementItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [category,   setCategory]   = useState('');
  const [showModal,  setShowModal]  = useState(false);

  const load = useCallback(async () => {
    try { setItems(await getAnnouncements(category || undefined)); }
    catch { /* keep stale data */ }
    finally { setLoading(false); setRefreshing(false); }
  }, [category]);

  useEffect(() => { load(); }, [load]);

  const handleAck = async (id: number) => {
    try {
      await acknowledgeAnnouncement(id);
      setItems(prev => prev.map(a =>
        a.id === id
          ? { ...a, isAcknowledged: true, acknowledgementCount: a.acknowledgementCount + 1 }
          : a
      ));
      Alert.alert('Acknowledged', 'Your acknowledgement has been recorded.');
    } catch {
      Alert.alert('Error', 'Could not acknowledge. Please try again.');
    }
  };

  const handleRemove = (id: number) => {
    Alert.alert('Remove Announcement', 'Are you sure you want to remove this announcement?', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Remove', style: 'destructive',
        onPress: async () => {
          try {
            await deactivateAnnouncement(id);
            setItems(prev => prev.filter(a => a.id !== id));
          } catch {
            Alert.alert('Error', 'Failed to remove. Please try again.');
          }
        },
      },
    ]);
  };

  const pinned  = items.filter(a => a.isPinned);
  const regular = items.filter(a => !a.isPinned);
  const unread  = items.filter(a => a.requiresAcknowledgement && !a.isAcknowledged).length;

  if (loading) {
    return <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>;
  }

  return (
    <View style={s.container}>
      {/* Header */}
      <View style={s.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={s.backBtn}>
          <Ionicons name="arrow-back" size={24} color="#fff" />
        </TouchableOpacity>
        <View>
          <Text style={s.headerTitle}>Announcements</Text>
          <Text style={s.headerSub}>
            {items.length} active{unread > 0 ? ` — ${unread} need your action` : ''}
          </Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowModal(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      {/* Unread nudge */}
      {unread > 0 && (
        <View style={s.nudgeBanner}>
          <Ionicons name="notifications" size={16} color="#e67700" />
          <Text style={s.nudgeText}>
            {unread} announcement{unread > 1 ? 's' : ''} require your acknowledgement
          </Text>
        </View>
      )}

      {/* Category filter */}
      <View style={s.tabBar}>
        <ScrollView horizontal showsHorizontalScrollIndicator={false}
          contentContainerStyle={{ paddingHorizontal: 12, gap: 8 }}>
          {CATEGORY_TABS.map(c => (
            <TouchableOpacity key={c.key}
              style={[s.tab, category === c.key && s.tabActive]}
              onPress={() => setCategory(c.key)}>
              <Text style={[s.tabText, category === c.key && s.tabTextActive]}>{c.label}</Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      {/* Feed */}
      <FlatList
        data={[...pinned, ...regular]}
        keyExtractor={a => a.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={
          <RefreshControl refreshing={refreshing}
            onRefresh={() => { setRefreshing(true); load(); }} />
        }
        ListEmptyComponent={
          <View style={s.empty}>
            <Ionicons name="megaphone-outline" size={52} color="#dee2e6" />
            <Text style={s.emptyTitle}>No announcements right now</Text>
            <Text style={s.emptySubtitle}>Check back later or post a new one.</Text>
          </View>
        }
        renderItem={({ item }) => (
          <AnnouncementCard
            item={item}
            currentRole={user?.role ?? ''}
            currentName={user?.fullName ?? ''}
            onAck={handleAck}
            onRemove={handleRemove}
          />
        )}
      />

      <PostModal
        visible={showModal}
        onClose={() => setShowModal(false)}
        onPosted={() => { setShowModal(false); load(); }}
      />
    </View>
  );
}

// ??? Styles ???????????????????????????????????????????????????????????????????

const s = StyleSheet.create({
  container:     { flex: 1, backgroundColor: '#f0f2f5' },
  center:        { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:        { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16,
                   paddingHorizontal: 16, flexDirection: 'row',
                   justifyContent: 'space-between', alignItems: 'center' },
  headerTitle:   { color: '#fff', fontSize: 22, fontWeight: '700' },
  headerSub:     { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:        { backgroundColor: '#3b5bdb', borderRadius: 10, padding: 8 },
  backBtn:       { padding: 4, marginRight: 8 },
  nudgeBanner:   { flexDirection: 'row', alignItems: 'center', gap: 8,
                   backgroundColor: '#fff3bf', paddingHorizontal: 16, paddingVertical: 10 },
  nudgeText:     { flex: 1, color: '#e67700', fontSize: 13, fontWeight: '600' },
  tabBar:        { backgroundColor: '#fff', borderBottomWidth: 1,
                   borderBottomColor: '#f1f3f5', paddingVertical: 10 },
  tab:           { paddingHorizontal: 12, paddingVertical: 6, borderRadius: 20,
                   backgroundColor: '#f1f3f5', borderWidth: 1, borderColor: '#dee2e6' },
  tabActive:     { backgroundColor: '#3b5bdb', borderColor: '#3b5bdb' },
  tabText:       { fontSize: 12, fontWeight: '600', color: '#495057' },
  tabTextActive: { color: '#fff' },
  card:          { backgroundColor: '#fff', borderRadius: 14, padding: 16, marginBottom: 12, elevation: 1 },
  cardPinned:    { borderWidth: 2, borderColor: '#e03131' },
  cardNeedsAck:  { borderWidth: 2, borderColor: '#f59f00' },
  cardHeader:    { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', gap: 6, marginBottom: 10 },
  iconCircle:    { width: 30, height: 30, borderRadius: 15, justifyContent: 'center', alignItems: 'center' },
  catBadge:      { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  catBadgeText:  { fontSize: 11, fontWeight: '700' },
  pinnedBadge:   { flexDirection: 'row', alignItems: 'center', gap: 3,
                   backgroundColor: '#e03131', borderRadius: 6, paddingHorizontal: 7, paddingVertical: 3 },
  pinnedText:    { color: '#fff', fontSize: 10, fontWeight: '800' },
  needsAckBadge: { backgroundColor: '#fff3bf', borderRadius: 6, paddingHorizontal: 7, paddingVertical: 3 },
  needsAckText:  { color: '#e67700', fontSize: 10, fontWeight: '700' },
  ackedBadge:    { flexDirection: 'row', alignItems: 'center', gap: 3,
                   backgroundColor: '#d3f9d8', borderRadius: 6, paddingHorizontal: 7, paddingVertical: 3 },
  ackedText:     { color: '#2f9e44', fontSize: 10, fontWeight: '700' },
  expiryBadge:   { backgroundColor: '#f1f3f5', borderRadius: 6, paddingHorizontal: 7, paddingVertical: 3 },
  expiryText:    { color: '#868e96', fontSize: 10 },
  cardTitle:     { fontSize: 15, fontWeight: '700', color: '#212529', marginBottom: 6 },
  cardBody:      { fontSize: 13, color: '#495057', lineHeight: 20, marginBottom: 10 },
  metaRow:       { flexDirection: 'row', flexWrap: 'wrap', gap: 10, marginBottom: 12 },
  metaTxt:       { fontSize: 11, color: '#adb5bd' },
  actionRow:     { flexDirection: 'row', alignItems: 'center', gap: 10 },
  ackBtn:        { flex: 1, flexDirection: 'row', alignItems: 'center', gap: 6,
                   paddingVertical: 10, borderRadius: 10, justifyContent: 'center' },
  ackBtnText:    { color: '#fff', fontSize: 13, fontWeight: '700' },
  removeBtn:     { flexDirection: 'row', alignItems: 'center', gap: 4, paddingVertical: 8, paddingHorizontal: 6 },
  removeBtnText: { fontSize: 12, color: '#adb5bd' },
  empty:         { alignItems: 'center', paddingVertical: 60 },
  emptyTitle:    { color: '#adb5bd', marginTop: 14, fontSize: 15, fontWeight: '600' },
  emptySubtitle: { color: '#ced4da', marginTop: 4, fontSize: 13 },
});

const pm = StyleSheet.create({
  container:       { flex: 1, backgroundColor: '#fff' },
  header:          { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
                     paddingHorizontal: 16, paddingVertical: 16,
                     borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  headerTitle:     { fontSize: 18, fontWeight: '700', color: '#212529' },
  label:           { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  hint:            { fontSize: 11, color: '#868e96', marginBottom: 14 },
  hintBox:         { borderLeftWidth: 3, borderRadius: 6, padding: 10, marginBottom: 16 },
  hintText:        { fontSize: 12, lineHeight: 18 },
  input:           { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10,
                     paddingHorizontal: 14, paddingVertical: 10, fontSize: 14,
                     color: '#212529', marginBottom: 6 },
  textArea:        { height: 130, textAlignVertical: 'top' },
  catChip:         { flexDirection: 'row', alignItems: 'center',
                     paddingHorizontal: 12, paddingVertical: 8, borderRadius: 20,
                     borderWidth: 1, borderColor: '#dee2e6', marginRight: 8 },
  catChipText:     { fontSize: 12, fontWeight: '600', color: '#495057' },
  rolesContainer:  { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 6 },
  roleChip:        { paddingHorizontal: 12, paddingVertical: 7, borderRadius: 20,
                     borderWidth: 1, borderColor: '#dee2e6', backgroundColor: '#f8f9fa' },
  roleChipActive:  { backgroundColor: '#3b5bdb', borderColor: '#3b5bdb' },
  roleChipText:    { fontSize: 12, fontWeight: '600', color: '#495057' },
  roleChipTextActive: { color: '#fff' },
  switchRow:       { flexDirection: 'row', alignItems: 'center', backgroundColor: '#f8f9fa',
                     borderRadius: 12, padding: 14, marginBottom: 16, gap: 12 },
  warnBox:         { flexDirection: 'row', alignItems: 'flex-start', gap: 10,
                     backgroundColor: '#fff5f5', borderRadius: 10, padding: 12, marginBottom: 16 },
  warnText:        { flex: 1, fontSize: 12, color: '#e03131', lineHeight: 18 },
  postBtn:         { flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
                     paddingVertical: 14, borderRadius: 12, marginBottom: 16 },
  postBtnText:     { color: '#fff', fontSize: 15, fontWeight: '700' },
});
