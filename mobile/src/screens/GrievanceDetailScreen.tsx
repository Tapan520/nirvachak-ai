import React, { useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity,
  ActivityIndicator, Alert, Modal, TextInput,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../context/AuthContext';
import { getGrievanceDetail, updateGrievanceStatus, GrievanceDetail } from '../api/grievances';

const STATUS_COLOR: Record<string, string> = {
  Open: '#e03131', InProgress: '#f59f00', Resolved: '#2f9e44', Closed: '#868e96',
};
const PRIORITY_COLOR: Record<string, string> = {
  Critical: '#e03131', High: '#f59f00', Medium: '#4dabf7', Low: '#adb5bd',
};
const STATUS_OPTIONS = ['Open', 'InProgress', 'Resolved', 'Closed'];

export default function GrievanceDetailScreen({ route }: any) {
  const { id } = route.params;
  const { user } = useAuth();
  const [grievance,    setGrievance]    = useState<GrievanceDetail | null>(null);
  const [loading,      setLoading]      = useState(true);
  const [statusModal,  setStatusModal]  = useState(false);
  const [newStatus,    setNewStatus]    = useState('');
  const [resolution,   setResolution]   = useState('');
  const [submitting,   setSubmitting]   = useState(false);

  const load = async () => {
    setLoading(true);
    try { setGrievance(await getGrievanceDetail(id)); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [id]);

  const openStatusModal = (status: string) => {
    setNewStatus(status);
    setResolution('');
    setStatusModal(true);
  };

  const submitStatus = async () => {
    if (!newStatus) return;
    if ((newStatus === 'Resolved' || newStatus === 'Closed') && !resolution.trim()) {
      Alert.alert('Required', 'Please provide resolution notes before closing/resolving.'); return;
    }
    try {
      setSubmitting(true);
      await updateGrievanceStatus(id, newStatus, resolution || undefined);
      setStatusModal(false);
      Alert.alert('Updated', `Status changed to ${newStatus}`);
      load();
    } catch {
      Alert.alert('Error', 'Failed to update status. Please try again.');
    } finally { setSubmitting(false); }
  };

  const canEdit = user?.role === 'Admin' || user?.role === 'CampaignManager';

  if (loading) return <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>;
  if (!grievance) return <View style={s.center}><Text>Grievance not found.</Text></View>;

  const statusColor   = STATUS_COLOR[grievance.status]   ?? '#adb5bd';
  const priorityColor = PRIORITY_COLOR[grievance.priority] ?? '#adb5bd';

  return (
    <ScrollView style={s.container}>
      {/* Status & priority header */}
      <View style={s.topBanner}>
        <View style={[s.statusPill, { backgroundColor: statusColor + '22' }]}>
          <Text style={[s.statusPillText, { color: statusColor }]}>{grievance.status}</Text>
        </View>
        <View style={[s.statusPill, { backgroundColor: priorityColor + '22' }]}>
          <Text style={[s.statusPillText, { color: priorityColor }]}>{grievance.priority} Priority</Text>
        </View>
      </View>

      {/* Title */}
      <View style={s.card}>
        <Text style={s.titleText}>{grievance.title}</Text>
        <Text style={s.bodyText}>{grievance.description}</Text>
      </View>

      {/* Details */}
      <View style={s.card}>
        <Text style={s.sectionLabel}>Details</Text>
        {[
          ['Reported By',    grievance.reportedBy    ?? '-'],
          ['Phone',          grievance.reporterPhone ?? '-'],
          ['Ward',           grievance.ward          ?? '-'],
          ['Location',       grievance.location      ?? '-'],
          ['Booth',          grievance.boothNumber?.toString() ?? '-'],
          ['Assigned To',    grievance.assignedToName ?? 'Unassigned'],
          ['Reported At',    new Date(grievance.reportedAt).toLocaleDateString('en-IN')],
          ['Resolved At',    grievance.resolvedAt ? new Date(grievance.resolvedAt).toLocaleDateString('en-IN') : '-'],
        ].map(([lbl, val]) => (
          <View key={lbl} style={s.infoRow}>
            <Text style={s.infoLbl}>{lbl}</Text>
            <Text style={s.infoVal}>{val}</Text>
          </View>
        ))}
      </View>

      {/* Resolution notes */}
      {!!grievance.resolutionNotes && (
        <View style={s.card}>
          <Text style={s.sectionLabel}>Resolution Notes</Text>
          <Text style={s.bodyText}>{grievance.resolutionNotes}</Text>
        </View>
      )}

      {/* Status update — Admin / CampaignManager only */}
      {canEdit && grievance.status !== 'Closed' && (
        <View style={s.card}>
          <Text style={s.sectionLabel}>Update Status</Text>
          <View style={s.statusBtnRow}>
            {STATUS_OPTIONS.filter(st => st !== grievance.status).map(st => (
              <TouchableOpacity
                key={st}
                style={[s.statusBtn, { borderColor: STATUS_COLOR[st] ?? '#adb5bd' }]}
                onPress={() => openStatusModal(st)}>
                <Text style={[s.statusBtnText, { color: STATUS_COLOR[st] ?? '#adb5bd' }]}>{st}</Text>
              </TouchableOpacity>
            ))}
          </View>
        </View>
      )}

      {/* Status update modal */}
      <Modal visible={statusModal} transparent animationType="slide">
        <View style={s.overlay}>
          <View style={s.modal}>
            <Text style={s.modalTitle}>Update to "{newStatus}"</Text>
            <Text style={s.modalLabel}>Resolution Notes{(newStatus === 'Resolved' || newStatus === 'Closed') ? ' *' : ''}</Text>
            <TextInput
              style={s.modalInput}
              placeholder="Describe how this was resolved..."
              value={resolution}
              onChangeText={setResolution}
              multiline
              numberOfLines={4}
              textAlignVertical="top"
            />
            <TouchableOpacity
              style={[s.modalConfirm, { backgroundColor: STATUS_COLOR[newStatus] ?? '#3b5bdb' }, submitting && { opacity: 0.6 }]}
              onPress={submitStatus}
              disabled={submitting}>
              {submitting
                ? <ActivityIndicator color="#fff" />
                : <Text style={s.modalConfirmText}>Confirm Update</Text>
              }
            </TouchableOpacity>
            <TouchableOpacity style={s.modalCancel} onPress={() => setStatusModal(false)}>
              <Text style={{ color: '#868e96', fontWeight: '600' }}>Cancel</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </ScrollView>
  );
}

const s = StyleSheet.create({
  container:       { flex: 1, backgroundColor: '#f0f2f5' },
  center:          { flex: 1, justifyContent: 'center', alignItems: 'center' },
  topBanner:       { flexDirection: 'row', gap: 10, padding: 16, backgroundColor: '#fff',
                     borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  statusPill:      { borderRadius: 20, paddingHorizontal: 14, paddingVertical: 6 },
  statusPillText:  { fontSize: 13, fontWeight: '700' },
  card:            { backgroundColor: '#fff', margin: 12, marginBottom: 0, borderRadius: 12, padding: 16, elevation: 1 },
  sectionLabel:    { fontSize: 12, fontWeight: '700', color: '#868e96', textTransform: 'uppercase',
                     letterSpacing: 0.5, marginBottom: 12 },
  titleText:       { fontSize: 17, fontWeight: '700', color: '#212529', marginBottom: 8 },
  bodyText:        { fontSize: 14, color: '#495057', lineHeight: 22 },
  infoRow:         { flexDirection: 'row', justifyContent: 'space-between',
                     paddingVertical: 7, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  infoLbl:         { fontSize: 13, color: '#868e96', fontWeight: '600' },
  infoVal:         { fontSize: 13, color: '#212529', flex: 1, textAlign: 'right' },
  statusBtnRow:    { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  statusBtn:       { paddingHorizontal: 16, paddingVertical: 8, borderRadius: 8, borderWidth: 1.5 },
  statusBtnText:   { fontSize: 13, fontWeight: '700' },
  overlay:         { flex: 1, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'flex-end' },
  modal:           { backgroundColor: '#fff', borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 20 },
  modalTitle:      { fontSize: 17, fontWeight: '700', marginBottom: 16, textAlign: 'center', color: '#212529' },
  modalLabel:      { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  modalInput:      { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10,
                     paddingHorizontal: 14, paddingVertical: 10, fontSize: 14,
                     color: '#212529', marginBottom: 16, height: 100 },
  modalConfirm:    { borderRadius: 10, padding: 14, alignItems: 'center', marginBottom: 8 },
  modalConfirmText:{ color: '#fff', fontSize: 15, fontWeight: '700' },
  modalCancel:     { padding: 14, alignItems: 'center' },
});
