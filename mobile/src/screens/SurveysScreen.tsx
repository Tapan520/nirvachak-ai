import React, { useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput,
  ScrollView, Alert, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getSurveys, submitSurveyResponse, SurveyItem } from '../api/surveys';

const CAT_COLOR: Record<string, string> = {
  CandidateAwareness: '#3b5bdb', LocalIssues: '#e03131', PartySupport: '#f59f00',
  DevelopmentFeedback: '#2f9e44', GeneralOpinion: '#7950f2',
};

const RATINGS = [
  { value: 5, label: 'Very Positive', color: '#2f9e44' },
  { value: 4, label: 'Positive',      color: '#74c0fc' },
  { value: 3, label: 'Neutral',       color: '#868e96' },
  { value: 2, label: 'Negative',      color: '#f59f00' },
  { value: 1, label: 'Very Negative', color: '#e03131' },
];

// ??? Submit Response Modal ????????????????????????????????????????????????????

interface RespondModalProps {
  survey: SurveyItem;
  onClose: () => void;
  onSubmitted: () => void;
}

function RespondModal({ survey, onClose, onSubmitted }: RespondModalProps) {
  const [name,       setName]       = useState('');
  const [phone,      setPhone]      = useState('');
  const [ward,       setWard]       = useState('');
  const [rating,     setRating]     = useState(3);
  const [feedback,   setFeedback]   = useState('');
  const [submitting, setSubmitting] = useState(false);

  const reset = () => { setName(''); setPhone(''); setWard(''); setRating(3); setFeedback(''); };

  const submit = async () => {
    try {
      setSubmitting(true);
      await submitSurveyResponse(survey.id, {
        respondentName:  name.trim()  || undefined,
        respondentPhone: phone.trim() || undefined,
        ward:            ward.trim()  || undefined,
        rating,
        feedback:        feedback.trim() || undefined,
      });
      reset();
      onSubmitted();
    } catch {
      Alert.alert('Error', 'Failed to submit response. Please try again.');
    } finally { setSubmitting(false); }
  };

  const color = CAT_COLOR[survey.category] ?? '#868e96';
  const ratingMeta = RATINGS.find(r => r.value === rating);

  return (
    <Modal visible animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <View style={pm.container}>
          <View style={pm.header}>
            <View style={{ flex: 1 }}>
              <Text style={pm.headerTitle}>Submit Response</Text>
              <Text style={pm.headerSub} numberOfLines={1}>{survey.title}</Text>
            </View>
            <TouchableOpacity onPress={() => { reset(); onClose(); }}>
              <Ionicons name="close" size={24} color="#212529" />
            </TouchableOpacity>
          </View>

          <ScrollView contentContainerStyle={{ padding: 16 }}>
            <Text style={pm.label}>Your Name</Text>
            <TextInput style={pm.input} placeholder="Optional"
              value={name} onChangeText={setName} />

            <Text style={pm.label}>Phone Number</Text>
            <TextInput style={pm.input} placeholder="Optional"
              value={phone} onChangeText={setPhone} keyboardType="phone-pad" />

            <Text style={pm.label}>Ward</Text>
            <TextInput style={pm.input} placeholder="Optional"
              value={ward} onChangeText={setWard} />

            <Text style={pm.label}>Rating <Text style={{ color: '#e03131' }}>*</Text></Text>
            <View style={pm.ratingsRow}>
              {RATINGS.map(r => (
                <TouchableOpacity key={r.value}
                  style={[pm.ratingChip,
                    { borderColor: r.color },
                    rating === r.value && { backgroundColor: r.color }]}
                  onPress={() => setRating(r.value)}>
                  <Text style={[pm.ratingChipText,
                    { color: rating === r.value ? '#fff' : r.color }]}>
                    {r.label}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>
            {ratingMeta && (
              <View style={[pm.ratingPill, { backgroundColor: ratingMeta.color + '18' }]}>
                <Text style={[pm.ratingPillText, { color: ratingMeta.color }]}>
                  Selected: {ratingMeta.label} ({rating}/5)
                </Text>
              </View>
            )}

            <Text style={pm.label}>Feedback</Text>
            <TextInput style={[pm.input, pm.textArea]} placeholder="Share your thoughts..."
              value={feedback} onChangeText={setFeedback} multiline numberOfLines={4}
              textAlignVertical="top" />

            <TouchableOpacity
              style={[pm.submitBtn, { backgroundColor: color }, submitting && { opacity: 0.6 }]}
              onPress={submit} disabled={submitting}>
              {submitting
                ? <ActivityIndicator color="#fff" />
                : <><Ionicons name="send-outline" size={18} color="#fff" />
                   <Text style={pm.submitBtnText}> Submit Response</Text></>
              }
            </TouchableOpacity>
          </ScrollView>
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

// ??? Main Screen ?????????????????????????????????????????????????????????????

export default function SurveysScreen() {
  const [surveys,    setSurveys]    = useState<SurveyItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selected,   setSelected]   = useState<SurveyItem | null>(null);

  const load = async () => {
    try { setSurveys(await getSurveys()); }
    finally { setLoading(false); setRefreshing(false); }
  };

  useEffect(() => { load(); }, []);

  if (loading) return <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>;

  const active = surveys.filter(sv => sv.isActive);

  return (
    <View style={s.container}>
      <View style={s.header}>
        <Text style={s.title}>Surveys</Text>
        <Text style={s.sub}>{active.length} active &bull; {surveys.length - active.length} inactive</Text>
      </View>

      {/* Total responses banner */}
      <View style={s.banner}>
        <Ionicons name="stats-chart-outline" size={24} color="#fff" />
        <View style={{ marginLeft: 12 }}>
          <Text style={s.bannerVal}>{surveys.reduce((a, sv) => a + sv.responseCount, 0)}</Text>
          <Text style={s.bannerLbl}>Total Responses Collected</Text>
        </View>
      </View>

      <FlatList
        data={surveys}
        keyExtractor={sv => sv.id.toString()}
        contentContainerStyle={{ padding: 12 }}
        refreshControl={<RefreshControl refreshing={refreshing}
          onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.center}><Text style={{ color: '#868e96' }}>No surveys found.</Text></View>
        }
        renderItem={({ item: sv }) => {
          const color = CAT_COLOR[sv.category] ?? '#868e96';
          return (
            <View style={[s.card, { borderLeftColor: color }]}>
              <View style={s.cardTop}>
                <Text style={s.cardTitle} numberOfLines={2}>{sv.title}</Text>
                <View style={[s.activeBadge, { backgroundColor: sv.isActive ? '#d3f9d8' : '#f1f3f5' }]}>
                  <Text style={[s.activeTxt, { color: sv.isActive ? '#2f9e44' : '#868e96' }]}>
                    {sv.isActive ? 'Active' : 'Inactive'}
                  </Text>
                </View>
              </View>
              {sv.description && <Text style={s.desc} numberOfLines={2}>{sv.description}</Text>}
              <View style={s.metaRow}>
                <View style={[s.catBadge, { backgroundColor: color + '18' }]}>
                  <Text style={[s.catTxt, { color }]}>{sv.category.replace(/([A-Z])/g, ' $1').trim()}</Text>
                </View>
                <View style={s.responseRow}>
                  <Ionicons name="chatbubble-ellipses-outline" size={13} color="#868e96" />
                  <Text style={s.responseCount}>{sv.responseCount} responses</Text>
                </View>
              </View>
              <Text style={s.date}>Created {new Date(sv.createdAt).toLocaleDateString('en-IN')}</Text>
              {sv.isActive && (
                <TouchableOpacity
                  style={[s.respondBtn, { backgroundColor: color }]}
                  onPress={() => setSelected(sv)}>
                  <Ionicons name="create-outline" size={15} color="#fff" />
                  <Text style={s.respondBtnText}>Submit Response</Text>
                </TouchableOpacity>
              )}
            </View>
          );
        }}
      />

      {selected && (
        <RespondModal
          survey={selected}
          onClose={() => setSelected(null)}
          onSubmitted={() => {
            setSelected(null);
            Alert.alert('Submitted!', 'Your response has been recorded.');
            load();
          }}
        />
      )}
    </View>
  );
}

// ??? Styles ???????????????????????????????????????????????????????????????????

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16 },
  title:        { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  banner:       { backgroundColor: '#3b5bdb', margin: 12, borderRadius: 12, padding: 16,
                  flexDirection: 'row', alignItems: 'center' },
  bannerVal:    { color: '#fff', fontSize: 28, fontWeight: '800' },
  bannerLbl:    { color: 'rgba(255,255,255,0.75)', fontSize: 12 },
  card:         { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10,
                  borderLeftWidth: 4, elevation: 1 },
  cardTop:      { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 6 },
  cardTitle:    { fontSize: 15, fontWeight: '700', color: '#212529', flex: 1, marginRight: 8 },
  activeBadge:  { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  activeTxt:    { fontSize: 11, fontWeight: '700' },
  desc:         { fontSize: 12, color: '#495057', marginBottom: 8 },
  metaRow:      { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 },
  catBadge:     { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  catTxt:       { fontSize: 11, fontWeight: '700' },
  responseRow:  { flexDirection: 'row', alignItems: 'center', gap: 4 },
  responseCount:{ fontSize: 12, color: '#868e96' },
  date:         { fontSize: 11, color: '#adb5bd', marginBottom: 10 },
  respondBtn:   { flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
                  gap: 6, paddingVertical: 9, borderRadius: 8 },
  respondBtnText:{ color: '#fff', fontSize: 13, fontWeight: '700' },
});

const pm = StyleSheet.create({
  container:      { flex: 1, backgroundColor: '#fff' },
  header:         { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
                    paddingHorizontal: 16, paddingVertical: 16,
                    borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  headerTitle:    { fontSize: 18, fontWeight: '700', color: '#212529' },
  headerSub:      { fontSize: 12, color: '#868e96', marginTop: 2 },
  label:          { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:          { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10,
                    paddingHorizontal: 14, paddingVertical: 10, fontSize: 14,
                    color: '#212529', marginBottom: 16 },
  textArea:       { height: 100, textAlignVertical: 'top' },
  ratingsRow:     { flexDirection: 'column', gap: 8, marginBottom: 10 },
  ratingChip:     { paddingHorizontal: 16, paddingVertical: 10, borderRadius: 10,
                    borderWidth: 1.5 },
  ratingChipText: { fontSize: 14, fontWeight: '600' },
  ratingPill:     { borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, marginBottom: 16, alignSelf: 'flex-start' },
  ratingPillText: { fontSize: 13, fontWeight: '700' },
  submitBtn:      { borderRadius: 12, flexDirection: 'row', alignItems: 'center',
                    justifyContent: 'center', paddingVertical: 14, marginBottom: 16 },
  submitBtnText:  { color: '#fff', fontSize: 15, fontWeight: '700' },
});
