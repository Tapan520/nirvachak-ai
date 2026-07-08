import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput, Alert,
  ScrollView, Switch,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getResults, addResult, deleteResult, ElectionResultSummary, ElectionResultItem } from '../api/results';
import { useAuth } from '../context/AuthContext';

const BRAND = '#1971c2';

export default function ResultsScreen() {
  const { user } = useAuth();
  const [summary,    setSummary]    = useState<ElectionResultSummary | null>(null);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showAdd,    setShowAdd]    = useState(false);
  // form
  const [booth,    setBooth]    = useState('');
  const [round,    setRound]    = useState('1');
  const [candVote, setCandVote] = useState('');
  const [c1Votes,  setC1Votes]  = useState('');
  const [c1Name,   setC1Name]   = useState('');
  const [c2Votes,  setC2Votes]  = useState('');
  const [c2Name,   setC2Name]   = useState('');
  const [total,    setTotal]    = useState('');
  const [isFinal,  setIsFinal]  = useState(false);
  const [saving,   setSaving]   = useState(false);

  const load = useCallback(async () => {
    try { setSummary(await getResults()); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async () => {
    if (!booth || !candVote) { Alert.alert('Required', 'Booth and candidate votes are required.'); return; }
    setSaving(true);
    try {
      await addResult({
        boothNumber: parseInt(booth, 10), roundNumber: parseInt(round, 10),
        candidateVotes: parseInt(candVote, 10),
        competitor1Votes: c1Votes ? parseInt(c1Votes, 10) : undefined, competitor1Name: c1Name || undefined,
        competitor2Votes: c2Votes ? parseInt(c2Votes, 10) : undefined, competitor2Name: c2Name || undefined,
        totalVotesCast: total ? parseInt(total, 10) : undefined, isFinal,
      });
      setShowAdd(false);
      setBooth(''); setRound('1'); setCandVote(''); setC1Votes(''); setC1Name(''); setC2Votes(''); setC2Name(''); setTotal(''); setIsFinal(false);
      load();
    } catch { Alert.alert('Error', 'Failed to save result.');
    } finally { setSaving(false); }
  };

  const handleDelete = (item: ElectionResultItem) => {
    Alert.alert('Delete Result', `Delete result for Booth ${item.boothNumber} Round ${item.roundNumber}?`, [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Delete', style: 'destructive', onPress: async () => {
          try { await deleteResult(item.id); load(); } catch { Alert.alert('Error', 'Failed to delete.'); }
        }},
    ]);
  };

  const canDelete = user?.role === 'Admin' || user?.role === 'SuperAdmin';

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  const sm = summary!;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Election Results</Text>
          <Text style={s.sub}>{sm.results.length} booth results entered</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowAdd(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      {sm.results.length > 0 && (
        <View style={[s.leadBanner, { backgroundColor: sm.isLeading ? '#d3f9d8' : '#fff5f5' }]}>
          <Ionicons name={sm.isLeading ? 'trending-up' : 'trending-down'} size={24} color={sm.isLeading ? '#2f9e44' : '#e03131'} />
          <View style={{ marginLeft: 12 }}>
            <Text style={[s.leadTxt, { color: sm.isLeading ? '#2f9e44' : '#e03131' }]}>
              {sm.isLeading ? `Leading by ${sm.leadMargin.toLocaleString('en-IN')} votes` : `Trailing by ${sm.leadMargin.toLocaleString('en-IN')} votes`}
            </Text>
            <Text style={s.leadSub}>
              Candidate: {sm.totalCandidateVotes.toLocaleString('en-IN')}
              {sm.competitor1Name ? `  ·  ${sm.competitor1Name}: ${sm.totalCompetitor1Votes.toLocaleString('en-IN')}` : ''}
              {sm.competitor2Name ? `  ·  ${sm.competitor2Name}: ${sm.totalCompetitor2Votes.toLocaleString('en-IN')}` : ''}
            </Text>
          </View>
        </View>
      )}

      <FlatList
        data={sm.results}
        keyExtractor={r => r.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={<View style={s.empty}><Ionicons name="bar-chart-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No results entered yet.</Text></View>}
        renderItem={({ item: r }) => {
          const lead = r.candidateVotes > Math.max(r.competitor1Votes ?? 0, r.competitor2Votes ?? 0);
          return (
            <View style={s.card}>
              <View style={[s.boothBox, { backgroundColor: lead ? '#d3f9d8' : '#fff5f5' }]}>
                <Text style={[s.boothNum, { color: lead ? '#2f9e44' : '#e03131' }]}>B{r.boothNumber}</Text>
                <Text style={s.roundTxt}>R{r.roundNumber}</Text>
              </View>
              <View style={{ flex: 1, marginLeft: 12 }}>
                <Text style={s.candVotes}>{r.candidateVotes.toLocaleString('en-IN')} <Text style={s.candLbl}>our votes</Text></Text>
                {r.competitor1Name && <Text style={s.compVotes}>{r.competitor1Name}: {(r.competitor1Votes ?? 0).toLocaleString('en-IN')}</Text>}
                {r.competitor2Name && <Text style={s.compVotes}>{r.competitor2Name}: {(r.competitor2Votes ?? 0).toLocaleString('en-IN')}</Text>}
                {r.totalVotesCast && <Text style={s.totalVotes}>Total cast: {r.totalVotesCast.toLocaleString('en-IN')}</Text>}
              </View>
              {r.isFinal && <Ionicons name="checkmark-circle" size={18} color="#2f9e44" style={{ marginRight: 4 }} />}
              {canDelete && (
                <TouchableOpacity onPress={() => handleDelete(r)} style={s.delBtn}>
                  <Ionicons name="trash-outline" size={16} color="#e03131" />
                </TouchableOpacity>
              )}
            </View>
          );
        }}
      />

      <Modal visible={showAdd} transparent animationType="slide">
        <View style={fm.overlay}>
          <View style={fm.sheet}>
            <Text style={fm.title}>Enter Result</Text>
            <ScrollView showsVerticalScrollIndicator={false}>
              <View style={{ flexDirection: 'row', gap: 10, marginBottom: 12 }}>
                {[['Booth *', booth, setBooth], ['Round', round, setRound]].map(([label, val, setter]: any) => (
                  <View key={label} style={{ flex: 1 }}>
                    <Text style={fm.label}>{label}</Text>
                    <TextInput style={fm.input} value={val} onChangeText={setter} keyboardType="numeric" placeholder="0" />
                  </View>
                ))}
              </View>
              <Text style={fm.label}>Candidate Votes *</Text>
              <TextInput style={fm.input} value={candVote} onChangeText={setCandVote} keyboardType="numeric" placeholder="0" />
              <View style={{ flexDirection: 'row', gap: 10 }}>
                <View style={{ flex: 1 }}><Text style={fm.label}>Comp 1 Name</Text><TextInput style={fm.input} value={c1Name} onChangeText={setC1Name} placeholder="Name" /></View>
                <View style={{ flex: 1 }}><Text style={fm.label}>Comp 1 Votes</Text><TextInput style={fm.input} value={c1Votes} onChangeText={setC1Votes} keyboardType="numeric" placeholder="0" /></View>
              </View>
              <View style={{ flexDirection: 'row', gap: 10 }}>
                <View style={{ flex: 1 }}><Text style={fm.label}>Comp 2 Name</Text><TextInput style={fm.input} value={c2Name} onChangeText={setC2Name} placeholder="Name" /></View>
                <View style={{ flex: 1 }}><Text style={fm.label}>Comp 2 Votes</Text><TextInput style={fm.input} value={c2Votes} onChangeText={setC2Votes} keyboardType="numeric" placeholder="0" /></View>
              </View>
              <Text style={fm.label}>Total Votes Cast</Text>
              <TextInput style={fm.input} value={total} onChangeText={setTotal} keyboardType="numeric" placeholder="Optional" />
              <View style={fm.switchRow}>
                <Text style={fm.label}>Mark as Final</Text>
                <Switch value={isFinal} onValueChange={setIsFinal} trackColor={{ true: BRAND }} />
              </View>
              <TouchableOpacity style={[fm.saveBtn, saving && { opacity: 0.6 }]} onPress={handleAdd} disabled={saving}>
                {saving ? <ActivityIndicator color="#fff" /> : <Text style={fm.saveTxt}>Save Result</Text>}
              </TouchableOpacity>
              <TouchableOpacity style={{ padding: 12 }} onPress={() => setShowAdd(false)}>
                <Text style={{ color: '#868e96', fontWeight: '600', textAlign: 'center' }}>Cancel</Text>
              </TouchableOpacity>
            </ScrollView>
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
  leadBanner: { margin: 12, borderRadius: 12, padding: 14, flexDirection: 'row', alignItems: 'center', elevation: 1 },
  leadTxt:    { fontSize: 16, fontWeight: '800' },
  leadSub:    { fontSize: 12, color: '#495057', marginTop: 2 },
  card:       { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', elevation: 1 },
  boothBox:   { width: 50, height: 50, borderRadius: 10, justifyContent: 'center', alignItems: 'center' },
  boothNum:   { fontSize: 14, fontWeight: '800' },
  roundTxt:   { fontSize: 10, color: '#868e96' },
  candVotes:  { fontSize: 18, fontWeight: '800', color: '#212529' },
  candLbl:    { fontSize: 11, fontWeight: '400', color: '#868e96' },
  compVotes:  { fontSize: 12, color: '#495057' },
  totalVotes: { fontSize: 11, color: '#adb5bd', marginTop: 2 },
  delBtn:     { padding: 6 },
  empty:      { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:   { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
const fm = StyleSheet.create({
  overlay:   { flex: 1, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'flex-end' },
  sheet:     { backgroundColor: '#fff', borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 20, maxHeight: '90%' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529', marginBottom: 12 },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 14, color: '#212529', marginBottom: 12 },
  switchRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, alignItems: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});
