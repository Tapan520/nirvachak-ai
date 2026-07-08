import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput, Alert, ScrollView,
  KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getFieldReports, submitFieldReport, FieldReportItem } from '../api/fieldReports';

const BRAND = '#2f9e44';
const STATUS_COLOR: Record<string, string> = {
  Submitted: '#3b5bdb', Reviewed: '#2f9e44', Flagged: '#e03131',
};

export default function FieldReportsScreen() {
  const [reports,    setReports]    = useState<FieldReportItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showForm,   setShowForm]   = useState(false);
  const [contacts,   setContacts]   = useState('');
  const [favour,     setFavour]     = useState('');
  const [floating,   setFloating]   = useState('');
  const [against,    setAgainst]    = useState('');
  const [issues,     setIssues]     = useState('0');
  const [highlights, setHighlights] = useState('');
  const [challenges, setChallenges] = useState('');
  const [tomorrow,   setTomorrow]   = useState('');
  const [saving,     setSaving]     = useState(false);

  const load = useCallback(async () => {
    try { setReports(await getFieldReports()); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = async () => {
    if (!contacts.trim()) { Alert.alert('Required', 'Contacts made is required.'); return; }
    setSaving(true);
    try {
      await submitFieldReport({
        contactsMade:     parseInt(contacts, 10),
        favourContacts:   parseInt(favour || '0', 10),
        floatingContacts: parseInt(floating || '0', 10),
        againstContacts:  parseInt(against || '0', 10),
        issuesLogged:     parseInt(issues || '0', 10),
        highlights:       highlights || undefined,
        challenges:       challenges || undefined,
        plannedForTomorrow: tomorrow || undefined,
      });
      setShowForm(false);
      setContacts(''); setFavour(''); setFloating(''); setAgainst('');
      setIssues('0'); setHighlights(''); setChallenges(''); setTomorrow('');
      load(); Alert.alert('Submitted', 'Field report submitted.');
    } catch { Alert.alert('Error', 'Failed to submit report.');
    } finally { setSaving(false); }
  };

  const totalContacts  = reports.reduce((s, r) => s + r.contactsMade, 0);
  const totalFavour    = reports.reduce((s, r) => s + r.favourContacts, 0);

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Field Reports</Text>
          <Text style={s.sub}>{reports.length} reports · {totalContacts} total contacts · {totalFavour} favour</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowForm(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      <FlatList
        data={reports}
        keyExtractor={r => r.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={<View style={s.empty}><Ionicons name="document-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No reports yet. Submit your daily report.</Text></View>}
        renderItem={({ item: r }) => {
          const color = STATUS_COLOR[r.status] ?? '#868e96';
          return (
            <View style={s.card}>
              <View style={s.cardHeader}>
                <Text style={s.cardDate}>{new Date(r.reportDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}</Text>
                <View style={[s.statusBadge, { backgroundColor: color + '20' }]}>
                  <Text style={[s.statusTxt, { color }]}>{r.status}</Text>
                </View>
              </View>
              <Text style={s.worker}>{r.workerName}</Text>
              <View style={s.statsRow}>
                <View style={s.statBox}>
                  <Text style={[s.statVal, { color: '#3b5bdb' }]}>{r.contactsMade}</Text>
                  <Text style={s.statLbl}>Contacts</Text>
                </View>
                <View style={s.statBox}>
                  <Text style={[s.statVal, { color: '#2f9e44' }]}>{r.favourContacts}</Text>
                  <Text style={s.statLbl}>Favour</Text>
                </View>
                <View style={s.statBox}>
                  <Text style={[s.statVal, { color: '#f59f00' }]}>{r.floatingContacts}</Text>
                  <Text style={s.statLbl}>Floating</Text>
                </View>
                <View style={s.statBox}>
                  <Text style={[s.statVal, { color: '#e03131' }]}>{r.againstContacts}</Text>
                  <Text style={s.statLbl}>Against</Text>
                </View>
              </View>
              {r.highlights && <Text style={s.notes} numberOfLines={2}>? {r.highlights}</Text>}
              {r.challenges  && <Text style={[s.notes, { color: '#e67700' }]} numberOfLines={2}>? {r.challenges}</Text>}
            </View>
          );
        }}
      />

      <Modal visible={showForm} animationType="slide" presentationStyle="pageSheet" onRequestClose={() => setShowForm(false)}>
        <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
          <View style={fm.container}>
            <View style={fm.header}>
              <Text style={fm.title}>Daily Field Report</Text>
              <TouchableOpacity onPress={() => setShowForm(false)}><Ionicons name="close" size={24} color="#212529" /></TouchableOpacity>
            </View>
            <ScrollView contentContainerStyle={{ padding: 16 }}>
              <View style={fm.row}>
                {[['Contacts Made *', contacts, setContacts], ['In Favour', favour, setFavour],
                  ['Floating', floating, setFloating], ['Against', against, setAgainst],
                  ['Issues Logged', issues, setIssues]].map(([label, val, setter]: any) => (
                  <View key={label} style={{ flex: 1, marginHorizontal: 4 }}>
                    <Text style={fm.label}>{label}</Text>
                    <TextInput style={fm.numInput} value={val} onChangeText={setter} keyboardType="numeric" placeholder="0" />
                  </View>
                ))}
              </View>
              <Text style={fm.label}>Highlights of the Day</Text>
              <TextInput style={[fm.input, { height: 70, textAlignVertical: 'top' }]} value={highlights} onChangeText={setHighlights} multiline placeholder="Key achievements, voter wins..." />
              <Text style={fm.label}>Challenges Faced</Text>
              <TextInput style={[fm.input, { height: 70, textAlignVertical: 'top' }]} value={challenges} onChangeText={setChallenges} multiline placeholder="Any obstacles or issues..." />
              <Text style={fm.label}>Plan for Tomorrow</Text>
              <TextInput style={[fm.input, { height: 70, textAlignVertical: 'top' }]} value={tomorrow} onChangeText={setTomorrow} multiline placeholder="What will you focus on tomorrow..." />
              <TouchableOpacity style={[fm.saveBtn, saving && { opacity: 0.6 }]} onPress={handleSubmit} disabled={saving}>
                {saving ? <ActivityIndicator color="#fff" /> : <Text style={fm.saveTxt}>Submit Report</Text>}
              </TouchableOpacity>
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
      </Modal>
    </View>
  );
}

const s = StyleSheet.create({
  container:   { flex: 1, backgroundColor: '#f0f2f5' },
  center:      { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:      { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:       { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:         { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:      { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  card:        { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10, elevation: 1 },
  cardHeader:  { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 },
  cardDate:    { fontSize: 13, fontWeight: '700', color: '#212529' },
  statusBadge: { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  statusTxt:   { fontSize: 11, fontWeight: '700' },
  worker:      { fontSize: 12, color: '#868e96', marginBottom: 10 },
  statsRow:    { flexDirection: 'row', backgroundColor: '#f8f9fa', borderRadius: 10, padding: 10, marginBottom: 8 },
  statBox:     { flex: 1, alignItems: 'center' },
  statVal:     { fontSize: 18, fontWeight: '800' },
  statLbl:     { fontSize: 10, color: '#868e96', marginTop: 2 },
  notes:       { fontSize: 12, color: '#2f9e44', marginTop: 2 },
  empty:       { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:    { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
const fm = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529' },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  row:       { flexDirection: 'row', marginBottom: 12 },
  numInput:  { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 8, padding: 10, fontSize: 16, textAlign: 'center', color: '#212529' },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 14, color: '#212529', marginBottom: 16 },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, alignItems: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});
