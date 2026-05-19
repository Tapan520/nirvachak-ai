import React, { useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getVoterDetail, VoterDetail } from '../api/voters';

const SENT_COLOR: Record<string, string> = {
  Favour: '#2f9e44', Against: '#e03131', Neutral: '#1971c2',
  Floating: '#e67700', Unknown: '#868e96',
};

export default function VoterDetailScreen({ route }: any) {
  const { id } = route.params;
  const [voter, setVoter] = useState<VoterDetail | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getVoterDetail(id)
      .then(setVoter)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>;
  if (!voter)  return <View style={s.center}><Text>Voter not found.</Text></View>;

  const sc = SENT_COLOR[voter.sentiment] ?? '#868e96';

  return (
    <ScrollView style={s.container}>
      {/* Profile header */}
      <View style={s.profile}>
        <View style={s.avatar}><Text style={s.avatarTxt}>{voter.name[0]}</Text></View>
        <Text style={s.voterName}>{voter.name}</Text>
        {voter.nameLocal && <Text style={s.nameLocal}>{voter.nameLocal}</Text>}
        <Text style={s.epic}>{voter.voterId}</Text>
        <View style={[s.sentBadge, { backgroundColor: sc + '22' }]}>
          <Text style={[s.sentTxt, { color: sc }]}>{voter.sentiment}</Text>
        </View>
      </View>

      {/* Voter info (view-only on mobile) */}
      <View style={s.card}>
        {([
          ['Serial',        voter.serialNumber.toString()],
          ['Age',           voter.age.toString()],
          ['Gender',        voter.gender === 'M' ? 'Male' : voter.gender === 'F' ? 'Female' : 'Other'],
          ['Booth',         voter.boothNumber.toString()],
          ['Ward',          voter.wardNumber     ?? '-'],
          ['Mobile',        voter.mobileNumber   ?? '-'],
          ['Father/Husband',voter.fatherHusbandName ?? '-'],
          ['Address',       voter.address],
          ['Election Status',voter.electionDayStatus],
        ] as [string, string][]).map(([lbl, val]) => (
          <View key={lbl} style={s.infoRow}>
            <Text style={s.infoLbl}>{lbl}</Text>
            <Text style={s.infoVal}>{val}</Text>
          </View>
        ))}
      </View>

      {/* Visit history */}
      <View style={s.card}>
        <Text style={s.secTitle}>Visit History ({voter.visits.length})</Text>
        {voter.visits.length === 0
          ? <Text style={s.emptyTxt}>No visits recorded.</Text>
          : voter.visits.map(v => (
            <View key={v.id} style={s.visitRow}>
              <Ionicons name="walk-outline" size={14} color="#4dabf7" />
              <View style={{ marginLeft: 8, flex: 1 }}>
                <Text style={s.visitWorker}>{v.workerName} - {v.status}</Text>
                <Text style={s.visitDate}>{new Date(v.visitedAt).toLocaleDateString('en-IN')}</Text>
                {v.notes && <Text style={s.visitNote}>{v.notes}</Text>}
              </View>
              <Text style={{ fontSize: 11, color: SENT_COLOR[v.sentiment] ?? '#868e96', fontWeight: '600' }}>
                {v.sentiment}
              </Text>
            </View>
          ))
        }
      </View>
    </ScrollView>
  );
}

const s = StyleSheet.create({
  container:   { flex: 1, backgroundColor: '#f0f2f5' },
  center:      { flex: 1, justifyContent: 'center', alignItems: 'center' },
  profile:     { backgroundColor: '#1a1f2e', alignItems: 'center', paddingVertical: 28, paddingHorizontal: 16 },
  avatar:      { width: 72, height: 72, borderRadius: 36, backgroundColor: '#3b5bdb',
                 justifyContent: 'center', alignItems: 'center', marginBottom: 10 },
  avatarTxt:   { color: '#fff', fontSize: 28, fontWeight: '800' },
  voterName:   { color: '#fff', fontSize: 20, fontWeight: '700' },
  nameLocal:   { color: '#adb5bd', fontSize: 14 },
  epic:        { color: '#4dabf7', fontSize: 12, fontFamily: 'monospace', marginTop: 4 },
  sentBadge:   { borderRadius: 20, paddingHorizontal: 16, paddingVertical: 6, marginTop: 10 },
  sentTxt:     { fontWeight: '700', fontSize: 13 },
  card:        { backgroundColor: '#fff', margin: 12, borderRadius: 12, padding: 16, elevation: 1 },
  infoRow:     { flexDirection: 'row', justifyContent: 'space-between',
                 paddingVertical: 7, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  infoLbl:     { fontSize: 13, color: '#868e96', fontWeight: '600' },
  infoVal:     { fontSize: 13, color: '#212529', flex: 1, textAlign: 'right' },
  secTitle:    { fontSize: 14, fontWeight: '700', color: '#343a40', marginBottom: 10 },
  emptyTxt:    { color: '#868e96', fontSize: 13, textAlign: 'center', padding: 8 },
  visitRow:    { flexDirection: 'row', alignItems: 'flex-start',
                 paddingVertical: 8, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  visitWorker: { fontSize: 13, fontWeight: '600', color: '#343a40' },
  visitDate:   { fontSize: 11, color: '#868e96' },
  visitNote:   { fontSize: 12, color: '#495057', marginTop: 2 },
});
