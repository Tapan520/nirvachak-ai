import React, { useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, ActivityIndicator,
  TouchableOpacity, Alert,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getVoterDetail, VoterDetail } from '../api/voters';
import { getCurrentPosition, logVisitWithGps } from '../api/gpsLocation';
import { useOfflineSync } from '../context/OfflineSyncContext';

const SENT_COLOR: Record<string, string> = {
  Favour: '#2f9e44', Against: '#e03131', Neutral: '#1971c2',
  Floating: '#e67700', Unknown: '#868e96',
};
const VISIT_STATUSES = ['Visited', 'NotAtHome', 'Refused', 'NotVisited'];
const SENTIMENTS     = ['Favour', 'Floating', 'Neutral', 'Against', 'Unknown'];

export default function VoterDetailScreen({ route }: any) {
  const { id } = route.params;
  const { isOnline, queueVisit, pendingCount } = useOfflineSync();
  const [voter,       setVoter]       = useState<VoterDetail | null>(null);
  const [loading,     setLoading]     = useState(true);
  const [logging,     setLogging]     = useState(false);
  const [visitStatus, setVisitStatus] = useState('Visited');
  const [sentiment,   setSentiment]   = useState('Favour');
  const [notes,       setNotes]       = useState('');

  useEffect(() => {
    getVoterDetail(id).then(setVoter).finally(() => setLoading(false));
  }, [id]);

  if (loading) return <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>;
  if (!voter)  return <View style={s.center}><Text>Voter not found.</Text></View>;

  const sc = SENT_COLOR[voter.sentiment] ?? '#868e96';

  const handleLogVisit = async () => {
    setLogging(true);
    try {
      const pos = await getCurrentPosition();
      if (isOnline) {
        await logVisitWithGps({
          voterId:        voter.id,
          status:         visitStatus,
          sentiment,
          notes:          notes || undefined,
          latitude:       pos?.latitude,
          longitude:      pos?.longitude,
          accuracyMeters: pos?.accuracyMeters,
        });
        Alert.alert('Visit logged', `GPS: ${pos ? `${pos.latitude.toFixed(4)}, ${pos.longitude.toFixed(4)}` : 'unavailable'}`);
      } else {
        await queueVisit({
          voterId:   voter.id,
          status:    visitStatus,
          sentiment,
          notes:     notes || undefined,
          latitude:  pos?.latitude,
          longitude: pos?.longitude,
        });
        Alert.alert('Saved offline', 'Visit queued. Will sync when you reconnect.');
      }
      setNotes('');
      const updated = await getVoterDetail(id);
      setVoter(updated);
    } catch {
      Alert.alert('Error', 'Could not log visit. Please try again.');
    } finally {
      setLogging(false);
    }
  };

  return (
    <ScrollView style={s.container}>
      {!isOnline && (
        <View style={s.offlineBanner}>
          <Ionicons name="cloud-offline-outline" size={16} color="#e67700" />
          <Text style={s.offlineTxt}>Offline - visits will sync when connected ({pendingCount} queued)</Text>
        </View>
      )}

      <View style={s.profile}>
        <View style={s.avatar}><Text style={s.avatarTxt}>{voter.name[0]}</Text></View>
        <Text style={s.voterName}>{voter.name}</Text>
        {voter.nameLocal && <Text style={s.nameLocal}>{voter.nameLocal}</Text>}
        <Text style={s.epic}>{voter.voterId}</Text>
        <View style={[s.sentBadge, { backgroundColor: sc + '22' }]}>
          <Text style={[s.sentTxt, { color: sc }]}>{voter.sentiment}</Text>
        </View>
      </View>

      <View style={s.card}>
        {([
          ['Serial',          voter.serialNumber.toString()],
          ['Age',             voter.age.toString()],
          ['Gender',          voter.gender === 'M' ? 'Male' : voter.gender === 'F' ? 'Female' : 'Other'],
          ['Booth',           voter.boothNumber.toString()],
          ['Ward',            voter.wardNumber       ?? '-'],
          ['Mobile',          voter.mobileNumber     ?? '-'],
          ['Father/Husband',  voter.fatherHusbandName ?? '-'],
          ['Address',         voter.address],
          ['Election Status', voter.electionDayStatus],
        ] as [string, string][]).map(([lbl, val]) => (
          <View key={lbl} style={s.infoRow}>
            <Text style={s.infoLbl}>{lbl}</Text>
            <Text style={s.infoVal}>{val}</Text>
          </View>
        ))}
      </View>

      <View style={s.card}>
        <Text style={s.secTitle}>Log Visit {!isOnline && <Text style={s.offlineTag}>(Offline)</Text>}</Text>
        <Text style={s.fieldLabel}>Visit Status</Text>
        <View style={s.chipRow}>
          {VISIT_STATUSES.map(vs => (
            <TouchableOpacity key={vs} style={[s.chip, visitStatus === vs && s.chipActive]}
              onPress={() => setVisitStatus(vs)}>
              <Text style={[s.chipTxt, visitStatus === vs && s.chipActiveTxt]}>{vs}</Text>
            </TouchableOpacity>
          ))}
        </View>
        <Text style={[s.fieldLabel, { marginTop: 12 }]}>Sentiment</Text>
        <View style={s.chipRow}>
          {SENTIMENTS.map(sen => (
            <TouchableOpacity key={sen}
              style={[s.chip, sentiment === sen && {
                backgroundColor: (SENT_COLOR[sen] ?? '#3b5bdb') + '22',
                borderColor: SENT_COLOR[sen] ?? '#3b5bdb',
              }]}
              onPress={() => setSentiment(sen)}>
              <Text style={[s.chipTxt, sentiment === sen && {
                color: SENT_COLOR[sen] ?? '#3b5bdb', fontWeight: '700',
              }]}>{sen}</Text>
            </TouchableOpacity>
          ))}
        </View>
        <TouchableOpacity style={[s.logBtn, logging && { opacity: 0.6 }]}
          onPress={handleLogVisit} disabled={logging}>
          {logging
            ? <ActivityIndicator color="#fff" size="small" />
            : <Ionicons name={isOnline ? 'location' : 'cloud-upload-outline'} size={18} color="#fff" />}
          <Text style={s.logBtnTxt}>
            {logging ? 'Logging...' : isOnline ? 'Log Visit (GPS)' : 'Save Offline'}
          </Text>
        </TouchableOpacity>
      </View>

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
          ))}
      </View>
    </ScrollView>
  );
}

const s = StyleSheet.create({
  container:     { flex: 1, backgroundColor: '#f0f2f5' },
  center:        { flex: 1, justifyContent: 'center', alignItems: 'center' },
  offlineBanner: { flexDirection: 'row', alignItems: 'center', gap: 8,
                   backgroundColor: '#fff3bf', paddingHorizontal: 14, paddingVertical: 10 },
  offlineTxt:    { flex: 1, color: '#e67700', fontSize: 12, fontWeight: '600' },
  profile:       { backgroundColor: '#1a1f2e', alignItems: 'center', paddingVertical: 28, paddingHorizontal: 16 },
  avatar:        { width: 72, height: 72, borderRadius: 36, backgroundColor: '#3b5bdb',
                   justifyContent: 'center', alignItems: 'center', marginBottom: 10 },
  avatarTxt:     { color: '#fff', fontSize: 28, fontWeight: '800' },
  voterName:     { color: '#fff', fontSize: 20, fontWeight: '700' },
  nameLocal:     { color: '#adb5bd', fontSize: 14 },
  epic:          { color: '#4dabf7', fontSize: 12, fontFamily: 'monospace', marginTop: 4 },
  sentBadge:     { borderRadius: 20, paddingHorizontal: 16, paddingVertical: 6, marginTop: 10 },
  sentTxt:       { fontWeight: '700', fontSize: 13 },
  card:          { backgroundColor: '#fff', margin: 12, borderRadius: 12, padding: 16, elevation: 1 },
  infoRow:       { flexDirection: 'row', justifyContent: 'space-between',
                   paddingVertical: 7, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  infoLbl:       { fontSize: 13, color: '#868e96', fontWeight: '600' },
  infoVal:       { fontSize: 13, color: '#212529', flex: 1, textAlign: 'right' },
  secTitle:      { fontSize: 14, fontWeight: '700', color: '#343a40', marginBottom: 12 },
  offlineTag:    { color: '#e67700', fontSize: 12 },
  fieldLabel:    { fontSize: 12, fontWeight: '600', color: '#868e96', marginBottom: 8 },
  chipRow:       { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  chip:          { borderRadius: 20, paddingHorizontal: 12, paddingVertical: 7,
                   borderWidth: 1.5, borderColor: '#dee2e6', backgroundColor: '#f8f9fa' },
  chipActive:    { backgroundColor: '#d0ebff', borderColor: '#3b5bdb' },
  chipTxt:       { fontSize: 12, color: '#495057' },
  chipActiveTxt: { color: '#3b5bdb', fontWeight: '700' },
  logBtn:        { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8,
                   backgroundColor: '#3b5bdb', borderRadius: 12, paddingVertical: 14, marginTop: 16 },
  logBtnTxt:     { color: '#fff', fontWeight: '700', fontSize: 15 },
  emptyTxt:      { color: '#868e96', fontSize: 13, textAlign: 'center', padding: 8 },
  visitRow:      { flexDirection: 'row', alignItems: 'flex-start',
                   paddingVertical: 8, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  visitWorker:   { fontSize: 13, fontWeight: '600', color: '#343a40' },
  visitDate:     { fontSize: 11, color: '#868e96' },
  visitNote:     { fontSize: 12, color: '#495057', marginTop: 2 },
});
