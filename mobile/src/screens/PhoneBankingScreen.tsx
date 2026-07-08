import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput,
  ScrollView, Alert, Linking, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import {
  getPhoneBankingStats, logCall, searchVoters,
  PhoneBankingStats, PendingCallVoter,
  CALL_OUTCOMES, SENTIMENTS,
} from '../api/phoneBanking';

const BRAND = '#3b5bdb';

// ??? Stat Card ??????????????????????????????????????????????????????????????

function StatCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <View style={[sc.card, { borderTopColor: color }]}>
      <Text style={[sc.val, { color }]}>{value}</Text>
      <Text style={sc.lbl}>{label}</Text>
    </View>
  );
}
const sc = StyleSheet.create({
  card: { flex: 1, backgroundColor: '#fff', borderRadius: 10, padding: 12,
    borderTopWidth: 3, alignItems: 'center', elevation: 1 },
  val:  { fontSize: 22, fontWeight: '800' },
  lbl:  { fontSize: 10, color: '#868e96', marginTop: 2, textAlign: 'center' },
});

// ??? Log Call Modal ?????????????????????????????????????????????????????????

interface LogModalProps {
  visible: boolean;
  voter: PendingCallVoter | null;
  onClose: () => void;
  onLogged: () => void;
}

function LogCallModal({ visible, voter, onClose, onLogged }: LogModalProps) {
  const [outcome,   setOutcome]   = useState('Talked');
  const [sentiment, setSentiment] = useState('');
  const [duration,  setDuration]  = useState('');
  const [notes,     setNotes]     = useState('');
  const [saving,    setSaving]    = useState(false);

  const reset = () => { setOutcome('Talked'); setSentiment(''); setDuration(''); setNotes(''); };

  const submit = async () => {
    if (!voter) return;
    setSaving(true);
    try {
      await logCall({
        voterId: voter.id,
        outcome,
        durationSeconds: parseInt(duration || '0', 10),
        notes: notes.trim() || undefined,
        sentimentAfterCall: sentiment || undefined,
      });
      reset();
      onLogged();
    } catch {
      Alert.alert('Error', 'Failed to log call. Please try again.');
    } finally { setSaving(false); }
  };

  if (!voter) return null;

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={lm.container}>
          <View style={lm.header}>
            <View>
              <Text style={lm.title}>Log Call</Text>
              <Text style={lm.sub}>{voter.name} · {voter.phone}</Text>
            </View>
            <TouchableOpacity onPress={() => { reset(); onClose(); }}>
              <Ionicons name="close" size={24} color="#212529" />
            </TouchableOpacity>
          </View>

          <ScrollView contentContainerStyle={{ padding: 16 }}>
            {/* Call Outcome */}
            <Text style={lm.label}>Call Outcome <Text style={{ color: '#e03131' }}>*</Text></Text>
            <View style={lm.chipRow}>
              {CALL_OUTCOMES.map(o => {
                const active = outcome === o.key;
                return (
                  <TouchableOpacity key={o.key}
                    style={[lm.chip, active && { backgroundColor: o.color, borderColor: o.color }]}
                    onPress={() => setOutcome(o.key)}>
                    <Text style={[lm.chipTxt, active && { color: '#fff' }]}>{o.label}</Text>
                  </TouchableOpacity>
                );
              })}
            </View>

            {/* Sentiment update — only shown when Talked */}
            {outcome === 'Talked' && (
              <>
                <Text style={lm.label}>Voter Sentiment After Call</Text>
                <View style={lm.chipRow}>
                  {SENTIMENTS.map(s => {
                    const active = sentiment === s.key;
                    return (
                      <TouchableOpacity key={s.key}
                        style={[lm.chip, active && { backgroundColor: s.color, borderColor: s.color }]}
                        onPress={() => setSentiment(active ? '' : s.key)}>
                        <Text style={[lm.chipTxt, active && { color: '#fff' }]}>{s.label}</Text>
                      </TouchableOpacity>
                    );
                  })}
                </View>
              </>
            )}

            <Text style={lm.label}>Duration (seconds)</Text>
            <TextInput style={lm.input} value={duration} onChangeText={setDuration}
              keyboardType="numeric" placeholder="e.g. 90" placeholderTextColor="#adb5bd" />

            <Text style={lm.label}>Notes</Text>
            <TextInput style={[lm.input, lm.textArea]} value={notes} onChangeText={setNotes}
              multiline numberOfLines={3} textAlignVertical="top"
              placeholder="Any notes about this call..." placeholderTextColor="#adb5bd" />

            <TouchableOpacity style={[lm.saveBtn, saving && { opacity: 0.6 }]}
              onPress={submit} disabled={saving}>
              {saving
                ? <ActivityIndicator color="#fff" />
                : <><Ionicons name="checkmark-circle-outline" size={18} color="#fff" />
                   <Text style={lm.saveTxt}> Save Call Log</Text></>}
            </TouchableOpacity>
          </ScrollView>
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const lm = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start',
    paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529' },
  sub:       { fontSize: 12, color: '#868e96', marginTop: 2 },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 8 },
  chipRow:   { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 16 },
  chip:      { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20,
    borderWidth: 1, borderColor: '#dee2e6' },
  chipTxt:   { fontSize: 12, fontWeight: '600', color: '#495057' },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10,
    paddingHorizontal: 14, paddingVertical: 10, fontSize: 14,
    color: '#212529', backgroundColor: '#f8f9fa', marginBottom: 16 },
  textArea:  { height: 80, textAlignVertical: 'top' },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, flexDirection: 'row',
    alignItems: 'center', justifyContent: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});

// ??? Voter Row ???????????????????????????????????????????????????????????????

function VoterRow({
  voter, onCall, onLog,
}: { voter: PendingCallVoter; onCall: () => void; onLog: () => void }) {
  const sentColor = voter.sentiment === 'Favour' ? '#2f9e44'
    : voter.sentiment === 'Against' ? '#e03131'
    : voter.sentiment === 'Floating' ? '#f59f00' : '#868e96';

  return (
    <View style={vr.card}>
      <View style={[vr.dot, { backgroundColor: sentColor }]} />
      <View style={{ flex: 1, marginLeft: 12 }}>
        <Text style={vr.name}>{voter.name}</Text>
        <Text style={vr.phone}>{voter.phone}</Text>
        <Text style={vr.meta}>
          Booth {voter.boothNumber}
          {voter.wardNumber ? `  ·  Ward ${voter.wardNumber}` : ''}
          {'  ·  '}
          <Text style={{ color: sentColor }}>{voter.sentiment}</Text>
        </Text>
      </View>
      <TouchableOpacity style={vr.dialBtn}
        onPress={() => Linking.openURL(`tel:${voter.phone}`)}>
        <Ionicons name="call-outline" size={18} color="#2f9e44" />
      </TouchableOpacity>
      <TouchableOpacity style={vr.logBtn} onPress={onLog}>
        <Ionicons name="create-outline" size={18} color={BRAND} />
      </TouchableOpacity>
    </View>
  );
}

const vr = StyleSheet.create({
  card:    { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8,
    flexDirection: 'row', alignItems: 'center', elevation: 1 },
  dot:     { width: 4, borderRadius: 2, alignSelf: 'stretch' },
  name:    { fontSize: 14, fontWeight: '700', color: '#212529' },
  phone:   { fontSize: 12, color: '#4dabf7', marginTop: 1 },
  meta:    { fontSize: 11, color: '#868e96', marginTop: 3 },
  dialBtn: { backgroundColor: '#d3f9d8', borderRadius: 8, padding: 10, marginLeft: 8 },
  logBtn:  { backgroundColor: '#e7f0ff', borderRadius: 8, padding: 10, marginLeft: 8 },
});

// ??? Main Screen ?????????????????????????????????????????????????????????????

export default function PhoneBankingScreen() {
  const [stats,      setStats]      = useState<PhoneBankingStats | null>(null);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [tab,        setTab]        = useState<'pending' | 'history'>('pending');
  const [search,     setSearch]     = useState('');
  const [searchRes,  setSearchRes]  = useState<PendingCallVoter[]>([]);
  const [searching,  setSearching]  = useState(false);
  const [logVoter,   setLogVoter]   = useState<PendingCallVoter | null>(null);

  const load = useCallback(async () => {
    try { setStats(await getPhoneBankingStats()); }
    catch { Alert.alert('Error', 'Could not load data.'); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (!search.trim()) { setSearchRes([]); return; }
    const timer = setTimeout(async () => {
      setSearching(true);
      try { setSearchRes(await searchVoters(search.trim())); }
      finally { setSearching(false); }
    }, 400);
    return () => clearTimeout(timer);
  }, [search]);

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  const listData = search.trim()
    ? searchRes
    : tab === 'pending'
      ? (stats?.pendingVoters ?? [])
      : [];

  return (
    <View style={s.container}>
      {/* Header */}
      <View style={s.header}>
        <View>
          <Text style={s.title}>Phone Banking</Text>
          <Text style={s.sub}>Today's calling drive</Text>
        </View>
      </View>

      {/* Stats row */}
      <View style={s.statsRow}>
        <StatCard label="Total Calls" value={stats?.totalCallsToday ?? 0} color={BRAND} />
        <StatCard label="Talked"      value={stats?.talkedCount ?? 0}     color="#2f9e44" />
        <StatCard label="No Answer"   value={stats?.noAnswerCount ?? 0}   color="#868e96" />
        <StatCard label="Call Back"   value={stats?.callBackCount ?? 0}   color="#f59f00" />
      </View>

      {/* Search bar */}
      <View style={s.searchRow}>
        <Ionicons name="search-outline" size={18} color="#adb5bd" style={s.searchIcon} />
        <TextInput
          style={s.searchInput}
          placeholder="Search voter by name or phone…"
          placeholderTextColor="#adb5bd"
          value={search}
          onChangeText={setSearch}
        />
        {searching && <ActivityIndicator size="small" color={BRAND} style={{ marginRight: 10 }} />}
        {!!search && (
          <TouchableOpacity onPress={() => setSearch('')} style={{ padding: 8 }}>
            <Ionicons name="close-circle" size={18} color="#adb5bd" />
          </TouchableOpacity>
        )}
      </View>

      {/* Tabs (only when not searching) */}
      {!search.trim() && (
        <View style={s.tabBar}>
          {(['pending', 'history'] as const).map(t => (
            <TouchableOpacity key={t} style={[s.tab, tab === t && s.tabActive]}
              onPress={() => setTab(t)}>
              <Text style={[s.tabTxt, tab === t && s.tabTxtActive]}>
                {t === 'pending' ? `Pending (${stats?.pendingVoters.length ?? 0})` : `Today's Log (${stats?.recentCalls.length ?? 0})`}
              </Text>
            </TouchableOpacity>
          ))}
        </View>
      )}

      {/* List */}
      {tab === 'pending' || search.trim() ? (
        <FlatList
          data={listData}
          keyExtractor={v => v.id.toString()}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          refreshControl={
            !search.trim()
              ? <RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />
              : undefined
          }
          ListEmptyComponent={
            <View style={s.empty}>
              <Ionicons name="call-outline" size={48} color="#dee2e6" />
              <Text style={s.emptyTxt}>
                {search.trim() ? 'No voters found' : 'All floating/unknown voters called today!'}
              </Text>
            </View>
          }
          renderItem={({ item }) => (
            <VoterRow
              voter={item}
              onCall={() => Linking.openURL(`tel:${item.phone}`)}
              onLog={() => setLogVoter(item)}
            />
          )}
        />
      ) : (
        <FlatList
          data={stats?.recentCalls ?? []}
          keyExtractor={c => c.id.toString()}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
          ListEmptyComponent={
            <View style={s.empty}>
              <Ionicons name="time-outline" size={48} color="#dee2e6" />
              <Text style={s.emptyTxt}>No calls logged today yet.</Text>
            </View>
          }
          renderItem={({ item: c }) => {
            const outcome = CALL_OUTCOMES.find(o => o.key === c.outcome);
            return (
              <View style={s.histCard}>
                <View style={[s.outcomeDot, { backgroundColor: outcome?.color ?? '#868e96' }]} />
                <View style={{ flex: 1, marginLeft: 12 }}>
                  <Text style={s.histName}>{c.voterName}</Text>
                  <View style={s.histMeta}>
                    <View style={[s.outcomeBadge, { backgroundColor: (outcome?.color ?? '#868e96') + '20' }]}>
                      <Text style={[s.outcomeTxt, { color: outcome?.color ?? '#868e96' }]}>
                        {outcome?.label ?? c.outcome}
                      </Text>
                    </View>
                    <Text style={s.histTime}>
                      {new Date(c.calledAt).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit' })}
                    </Text>
                    {c.durationSeconds > 0 && (
                      <Text style={s.histDur}>{c.durationSeconds}s</Text>
                    )}
                  </View>
                  {c.notes ? <Text style={s.histNotes} numberOfLines={1}>{c.notes}</Text> : null}
                </View>
              </View>
            );
          }}
        />
      )}

      <LogCallModal
        visible={!!logVoter}
        voter={logVoter}
        onClose={() => setLogVoter(null)}
        onLogged={() => { setLogVoter(null); load(); Alert.alert('Saved', 'Call logged successfully.'); }}
      />
    </View>
  );
}

// ??? Styles ??????????????????????????????????????????????????????????????????

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16,
    paddingHorizontal: 16 },
  title:        { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  statsRow:     { flexDirection: 'row', gap: 8, margin: 12 },
  searchRow:    { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff',
    marginHorizontal: 12, marginBottom: 8, borderRadius: 12,
    borderWidth: 1, borderColor: '#dee2e6', elevation: 1 },
  searchIcon:   { marginLeft: 12 },
  searchInput:  { flex: 1, paddingHorizontal: 10, paddingVertical: 12,
    fontSize: 14, color: '#212529' },
  tabBar:       { flexDirection: 'row', marginHorizontal: 12, marginBottom: 4,
    backgroundColor: '#fff', borderRadius: 10, padding: 4, elevation: 1 },
  tab:          { flex: 1, paddingVertical: 8, borderRadius: 8, alignItems: 'center' },
  tabActive:    { backgroundColor: BRAND },
  tabTxt:       { fontSize: 12, fontWeight: '600', color: '#868e96' },
  tabTxtActive: { color: '#fff' },
  empty:        { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:     { color: '#adb5bd', marginTop: 12, fontSize: 14 },
  histCard:     { backgroundColor: '#fff', borderRadius: 12, padding: 14,
    marginBottom: 8, flexDirection: 'row', alignItems: 'center', elevation: 1 },
  outcomeDot:   { width: 4, borderRadius: 2, alignSelf: 'stretch' },
  histName:     { fontSize: 14, fontWeight: '700', color: '#212529', marginBottom: 4 },
  histMeta:     { flexDirection: 'row', alignItems: 'center', gap: 8 },
  outcomeBadge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  outcomeTxt:   { fontSize: 11, fontWeight: '700' },
  histTime:     { fontSize: 11, color: '#adb5bd' },
  histDur:      { fontSize: 11, color: '#adb5bd' },
  histNotes:    { fontSize: 11, color: '#868e96', marginTop: 4 },
});
