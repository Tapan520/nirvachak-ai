import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput,
  ScrollView, Alert, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import {
  getCompetitorActivities, logCompetitorActivity,
  CompetitorActivityItem, ACTIVITY_TYPES, THREAT_LEVELS,
} from '../api/competitor';

const BRAND = '#e03131';

const TYPE_ICON: Record<string, string> = {
  Rally: 'megaphone-outline', RoadShow: 'car-outline', DoorToDoor: 'home-outline',
  SmallMeeting: 'people-outline', Announcement: 'notifications-outline',
  MediaCoverage: 'tv-outline', SocialMedia: 'phone-portrait-outline', Other: 'ellipse-outline',
};

// ??? Threat Badge ????????????????????????????????????????????????????????????

function ThreatBadge({ level }: { level: string }) {
  const t = THREAT_LEVELS.find(x => x.key === level);
  const color = t?.color ?? '#868e96';
  return (
    <View style={[tb.badge, { backgroundColor: color + '20' }]}>
      <Text style={[tb.txt, { color }]}>{t?.label ?? level}</Text>
    </View>
  );
}
const tb = StyleSheet.create({
  badge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  txt:   { fontSize: 10, fontWeight: '800' },
});

// ??? Log Activity Modal ??????????????????????????????????????????????????????

interface LogModalProps {
  visible: boolean;
  onClose: () => void;
  onLogged: () => void;
}

function LogActivityModal({ visible, onClose, onLogged }: LogModalProps) {
  const [competitor, setCompetitor] = useState('');
  const [party,      setParty]      = useState('');
  const [title,      setTitle]      = useState('');
  const [actType,    setActType]    = useState('Rally');
  const [location,   setLocation]   = useState('');
  const [ward,       setWard]       = useState('');
  const [crowd,      setCrowd]      = useState('');
  const [threat,     setThreat]     = useState('Medium');
  const [notes,      setNotes]      = useState('');
  const [saving,     setSaving]     = useState(false);

  const reset = () => {
    setCompetitor(''); setParty(''); setTitle(''); setActType('Rally');
    setLocation(''); setWard(''); setCrowd(''); setThreat('Medium'); setNotes('');
  };

  const submit = async () => {
    if (!competitor.trim() || !title.trim()) {
      Alert.alert('Required', 'Competitor name and activity title are required.'); return;
    }
    setSaving(true);
    try {
      await logCompetitorActivity({
        competitorName: competitor.trim(),
        partyName: party.trim() || undefined,
        activityTitle: title.trim(),
        activityType: actType,
        location: location.trim() || undefined,
        ward: ward.trim() || undefined,
        estimatedCrowd: crowd ? parseInt(crowd, 10) : undefined,
        activityDate: new Date().toISOString(),
        notes: notes.trim() || undefined,
        threatLevel: threat,
      });
      reset();
      onLogged();
    } catch {
      Alert.alert('Error', 'Failed to log activity.');
    } finally { setSaving(false); }
  };

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={lm.container}>
          <View style={lm.header}>
            <Text style={lm.title}>Log Competitor Activity</Text>
            <TouchableOpacity onPress={() => { reset(); onClose(); }}>
              <Ionicons name="close" size={24} color="#212529" />
            </TouchableOpacity>
          </View>
          <ScrollView contentContainerStyle={{ padding: 16 }}>
            <Text style={lm.label}>Competitor Name <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={lm.input} placeholder="e.g. Rajesh Kumar"
              value={competitor} onChangeText={setCompetitor} />

            <Text style={lm.label}>Party Name</Text>
            <TextInput style={lm.input} placeholder="e.g. BJP, Congress…"
              value={party} onChangeText={setParty} />

            <Text style={lm.label}>Activity Title <Text style={{ color: '#e03131' }}>*</Text></Text>
            <TextInput style={lm.input} placeholder="Brief description of the activity"
              value={title} onChangeText={setTitle} />

            <Text style={lm.label}>Activity Type</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 16 }}>
              {ACTIVITY_TYPES.map(t => (
                <TouchableOpacity key={t}
                  style={[lm.chip, actType === t && { backgroundColor: BRAND, borderColor: BRAND }]}
                  onPress={() => setActType(t)}>
                  <Text style={[lm.chipTxt, actType === t && { color: '#fff' }]}>
                    {t.replace(/([A-Z])/g, ' $1').trim()}
                  </Text>
                </TouchableOpacity>
              ))}
            </ScrollView>

            <Text style={lm.label}>Location</Text>
            <TextInput style={lm.input} placeholder="Area / venue"
              value={location} onChangeText={setLocation} />

            <Text style={lm.label}>Ward</Text>
            <TextInput style={lm.input} placeholder="Ward number or name"
              value={ward} onChangeText={setWard} />

            <Text style={lm.label}>Estimated Crowd</Text>
            <TextInput style={lm.input} placeholder="e.g. 500"
              value={crowd} onChangeText={setCrowd} keyboardType="numeric" />

            <Text style={lm.label}>Threat Level</Text>
            <View style={{ flexDirection: 'row', gap: 8, marginBottom: 16 }}>
              {THREAT_LEVELS.map(t => {
                const active = threat === t.key;
                return (
                  <TouchableOpacity key={t.key}
                    style={[lm.chip, active && { backgroundColor: t.color, borderColor: t.color }]}
                    onPress={() => setThreat(t.key)}>
                    <Text style={[lm.chipTxt, active && { color: '#fff' }]}>{t.label}</Text>
                  </TouchableOpacity>
                );
              })}
            </View>

            <Text style={lm.label}>Notes</Text>
            <TextInput style={[lm.input, lm.textArea]} value={notes} onChangeText={setNotes}
              multiline numberOfLines={3} textAlignVertical="top"
              placeholder="Any additional observations…" />

            <TouchableOpacity style={[lm.saveBtn, saving && { opacity: 0.6 }]}
              onPress={submit} disabled={saving}>
              {saving
                ? <ActivityIndicator color="#fff" />
                : <><Ionicons name="alert-circle-outline" size={18} color="#fff" />
                   <Text style={lm.saveTxt}> Log Activity</Text></>}
            </TouchableOpacity>
          </ScrollView>
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const lm = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529' },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10,
    paddingHorizontal: 14, paddingVertical: 10, fontSize: 14,
    color: '#212529', marginBottom: 16 },
  textArea:  { height: 80, textAlignVertical: 'top' },
  chip:      { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20,
    borderWidth: 1, borderColor: '#dee2e6', marginRight: 8 },
  chipTxt:   { fontSize: 12, fontWeight: '600', color: '#495057' },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, flexDirection: 'row',
    alignItems: 'center', justifyContent: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});

// ??? Main Screen ?????????????????????????????????????????????????????????????

export default function CompetitorScreen() {
  const [items,      setItems]      = useState<CompetitorActivityItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [filterThreat, setFilterThreat] = useState('');
  const [showLog,    setShowLog]    = useState(false);

  const load = useCallback(async () => {
    try { setItems(await getCompetitorActivities(undefined, filterThreat || undefined)); }
    catch { Alert.alert('Error', 'Could not load competitor data.'); }
    finally { setLoading(false); setRefreshing(false); }
  }, [filterThreat]);

  useEffect(() => { load(); }, [load]);

  // Build summary stats
  const criticalCount = items.filter(i => i.threatLevel === 'Critical').length;
  const highCount     = items.filter(i => i.threatLevel === 'High').length;
  const competitors   = [...new Set(items.map(i => i.competitorName))].length;
  const totalCrowd    = items.reduce((s, i) => s + (i.estimatedCrowd ?? 0), 0);

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      {/* Header */}
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Competitor Tracker</Text>
          <Text style={s.sub}>
            {items.length} activities · {competitors} competitor{competitors !== 1 ? 's' : ''}
            {criticalCount > 0 ? ` · ${criticalCount} critical` : ''}
          </Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowLog(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      {/* Stats banner */}
      <View style={s.banner}>
        <View style={s.bannerStat}>
          <Text style={[s.bannerVal, { color: '#e03131' }]}>{criticalCount}</Text>
          <Text style={s.bannerLbl}>Critical</Text>
        </View>
        <View style={s.bannerDivider} />
        <View style={s.bannerStat}>
          <Text style={[s.bannerVal, { color: '#e67700' }]}>{highCount}</Text>
          <Text style={s.bannerLbl}>High Threat</Text>
        </View>
        <View style={s.bannerDivider} />
        <View style={s.bannerStat}>
          <Text style={[s.bannerVal, { color: '#212529' }]}>{competitors}</Text>
          <Text style={s.bannerLbl}>Competitors</Text>
        </View>
        <View style={s.bannerDivider} />
        <View style={s.bannerStat}>
          <Text style={[s.bannerVal, { color: '#3b5bdb' }]}>
            {totalCrowd > 1000 ? `${(totalCrowd / 1000).toFixed(1)}k` : totalCrowd}
          </Text>
          <Text style={s.bannerLbl}>Total Crowd</Text>
        </View>
      </View>

      {/* Threat filter */}
      <ScrollView horizontal showsHorizontalScrollIndicator={false}
        style={s.filterBar} contentContainerStyle={{ paddingHorizontal: 12, gap: 8 }}>
        <TouchableOpacity
          style={[s.filterChip, filterThreat === '' && s.filterChipActive]}
          onPress={() => setFilterThreat('')}>
          <Text style={[s.filterChipTxt, filterThreat === '' && { color: '#fff' }]}>All</Text>
        </TouchableOpacity>
        {THREAT_LEVELS.map(t => (
          <TouchableOpacity key={t.key}
            style={[s.filterChip, filterThreat === t.key && { backgroundColor: t.color, borderColor: t.color }]}
            onPress={() => setFilterThreat(filterThreat === t.key ? '' : t.key)}>
            <Text style={[s.filterChipTxt, filterThreat === t.key && { color: '#fff' }]}>{t.label}</Text>
          </TouchableOpacity>
        ))}
      </ScrollView>

      <FlatList
        data={items}
        keyExtractor={i => i.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing}
          onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.empty}>
            <Ionicons name="eye-off-outline" size={52} color="#dee2e6" />
            <Text style={s.emptyTxt}>No competitor activity logged.</Text>
          </View>
        }
        renderItem={({ item: act }) => {
          const tColor = THREAT_LEVELS.find(t => t.key === act.threatLevel)?.color ?? '#868e96';
          const icon   = TYPE_ICON[act.activityType] ?? 'ellipse-outline';
          return (
            <View style={[s.card, { borderLeftColor: tColor, borderLeftWidth: 3 }]}>
              <View style={[s.iconBox, { backgroundColor: tColor + '15' }]}>
                <Ionicons name={icon as any} size={22} color={tColor} />
              </View>
              <View style={{ flex: 1, marginLeft: 12 }}>
                <View style={s.cardTop}>
                  <Text style={s.cardTitle} numberOfLines={2}>{act.activityTitle}</Text>
                  <ThreatBadge level={act.threatLevel} />
                </View>
                <View style={s.competitorRow}>
                  <Text style={s.competitorName}>{act.competitorName}</Text>
                  {act.partyName && <Text style={s.partyName}> · {act.partyName}</Text>}
                </View>
                <View style={s.metaRow}>
                  <View style={[s.typeBadge, { backgroundColor: tColor + '10' }]}>
                    <Text style={[s.typeTxt, { color: tColor }]}>
                      {act.activityType.replace(/([A-Z])/g, ' $1').trim()}
                    </Text>
                  </View>
                  {act.location && (
                    <Text style={s.meta}>
                      <Ionicons name="location-outline" size={11} /> {act.location}
                    </Text>
                  )}
                  {act.estimatedCrowd != null && (
                    <Text style={s.meta}>
                      <Ionicons name="people-outline" size={11} /> {act.estimatedCrowd.toLocaleString('en-IN')}
                    </Text>
                  )}
                </View>
                <Text style={s.date}>
                  {new Date(act.activityDate).toLocaleDateString('en-IN', {
                    day: '2-digit', month: 'short', year: 'numeric',
                  })}
                </Text>
                {act.notes && <Text style={s.notes} numberOfLines={2}>{act.notes}</Text>}
              </View>
            </View>
          );
        }}
      />

      <LogActivityModal
        visible={showLog}
        onClose={() => setShowLog(false)}
        onLogged={() => { setShowLog(false); load(); Alert.alert('Logged', 'Activity recorded.'); }}
      />
    </View>
  );
}

// ??? Styles ??????????????????????????????????????????????????????????????????

const s = StyleSheet.create({
  container:      { flex: 1, backgroundColor: '#f0f2f5' },
  center:         { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:         { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16,
    paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:          { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:            { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:         { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  banner:         { backgroundColor: '#fff', margin: 12, borderRadius: 12,
    flexDirection: 'row', justifyContent: 'space-around', alignItems: 'center',
    paddingVertical: 14, elevation: 1 },
  bannerStat:     { alignItems: 'center', flex: 1 },
  bannerVal:      { fontSize: 22, fontWeight: '800' },
  bannerLbl:      { fontSize: 10, color: '#868e96', marginTop: 2 },
  bannerDivider:  { width: 1, height: 30, backgroundColor: '#f1f3f5' },
  filterBar:      { maxHeight: 52, paddingVertical: 8 },
  filterChip:     { paddingHorizontal: 14, paddingVertical: 6, borderRadius: 20,
    borderWidth: 1, borderColor: '#dee2e6', backgroundColor: '#fff' },
  filterChipActive:{ backgroundColor: '#1a1f2e', borderColor: '#1a1f2e' },
  filterChipTxt:  { fontSize: 12, fontWeight: '600', color: '#495057' },
  card:           { backgroundColor: '#fff', borderRadius: 12, padding: 14,
    marginBottom: 10, flexDirection: 'row', elevation: 1 },
  iconBox:        { width: 44, height: 44, borderRadius: 10,
    justifyContent: 'center', alignItems: 'center' },
  cardTop:        { flexDirection: 'row', justifyContent: 'space-between',
    alignItems: 'flex-start', marginBottom: 4, gap: 8 },
  cardTitle:      { fontSize: 14, fontWeight: '700', color: '#212529', flex: 1 },
  competitorRow:  { flexDirection: 'row', alignItems: 'center', marginBottom: 6 },
  competitorName: { fontSize: 13, fontWeight: '700', color: '#e03131' },
  partyName:      { fontSize: 12, color: '#868e96' },
  metaRow:        { flexDirection: 'row', alignItems: 'center', flexWrap: 'wrap', gap: 8, marginBottom: 4 },
  typeBadge:      { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  typeTxt:        { fontSize: 11, fontWeight: '600' },
  meta:           { fontSize: 11, color: '#868e96' },
  date:           { fontSize: 11, color: '#adb5bd' },
  notes:          { fontSize: 11, color: '#495057', marginTop: 4, fontStyle: 'italic' },
  empty:          { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:       { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
