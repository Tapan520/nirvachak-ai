import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, FlatList, TouchableOpacity,
  TextInput, Linking, ActivityIndicator, Alert, ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getVoters, VoterListItem } from '../api/voters';
import { getWhatsAppTemplates, WhatsAppTemplate } from '../api/whatsapp';

const SENT_COLOR: Record<string, string> = {
  Favour: '#2f9e44', Against: '#e03131', Neutral: '#1971c2',
  Floating: '#e67700', Unknown: '#868e96',
};

export default function WhatsAppOutreachScreen() {
  const [templates,    setTemplates]    = useState<WhatsAppTemplate[]>([]);
  const [selected,     setSelected]     = useState<WhatsAppTemplate | null>(null);
  const [customMsg,    setCustomMsg]    = useState('');
  const [voters,       setVoters]       = useState<VoterListItem[]>([]);
  const [sentiment,    setSentiment]    = useState('Floating');
  const [loading,      setLoading]      = useState(true);
  const [sentCount,    setSentCount]    = useState(0);

  const SENTIMENTS = ['Floating', 'Favour', 'Unknown', 'Neutral'];

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [t, v] = await Promise.all([
        getWhatsAppTemplates(),
        getVoters({ sentiment, pageSize: 100 }),
      ]);
      setTemplates(t);
      setVoters(v.items.filter(x => !!x.mobileNumber));
    } finally { setLoading(false); }
  }, [sentiment]);

  useEffect(() => { load(); }, [load]);

  const getMessage = (voter: VoterListItem) => {
    const body = selected ? selected.body : customMsg;
    return body.replace(/\{name\}/gi, voter.name)
               .replace(/\{voterId\}/gi, voter.voterId)
               .replace(/\{booth\}/gi, voter.boothNumber.toString());
  };

  const openWhatsApp = (voter: VoterListItem) => {
    const msg = getMessage(voter);
    if (!msg.trim()) { Alert.alert('Message Required', 'Select a template or enter a custom message.'); return; }
    const phone = voter.mobileNumber!.replace(/\D/g, '');
    const number = phone.length === 10 ? `91${phone}` : phone;
    const url = `whatsapp://send?phone=${number}&text=${encodeURIComponent(msg)}`;
    Linking.openURL(url).then(() => setSentCount(c => c + 1))
      .catch(() => Alert.alert('WhatsApp not installed', 'Please install WhatsApp.'));
  };

  const sendAll = () => {
    const msg = selected ? selected.body : customMsg;
    if (!msg.trim()) { Alert.alert('Message Required', 'Select a template or enter a custom message.'); return; }
    const withPhone = voters.filter(v => !!v.mobileNumber);
    Alert.alert(
      'Send to All',
      `Open WhatsApp for ${withPhone.length} voters (${sentiment})? You will need to tap Send for each.`,
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Start', onPress: () => {
          // Open first voter — user will cycle through
          if (withPhone.length > 0) openWhatsApp(withPhone[0]);
        }},
      ]
    );
  };

  return (
    <View style={s.container}>
      <View style={s.header}>
        <Text style={s.title}>WhatsApp Outreach</Text>
        <Text style={s.sub}>{voters.length} voters with mobile ({sentiment})</Text>
      </View>

      <ScrollView style={{ flex: 1 }}>
        {/* Sentiment filter */}
        <Text style={s.sectionLabel}>Filter by Sentiment</Text>
        <View style={s.pillRow}>
          {SENTIMENTS.map(s_ => (
            <TouchableOpacity key={s_}
              style={[s.pill, { backgroundColor: sentiment === s_
                ? (SENT_COLOR[s_] ?? '#3b5bdb') : '#e9ecef' }]}
              onPress={() => setSentiment(s_)}>
              <Text style={[s.pillTxt, { color: sentiment === s_ ? '#fff' : '#495057' }]}>{s_}</Text>
            </TouchableOpacity>
          ))}
        </View>

        {/* Template selector */}
        <Text style={s.sectionLabel}>Message Template</Text>
        <View style={s.templateList}>
          <TouchableOpacity
            style={[s.templateItem, !selected && s.templateSelected]}
            onPress={() => { setSelected(null); }}>
            <Text style={s.templateTitle}>?? Custom Message</Text>
          </TouchableOpacity>
          {templates.map(t => (
            <TouchableOpacity key={t.id}
              style={[s.templateItem, selected?.id === t.id && s.templateSelected]}
              onPress={() => setSelected(t)}>
              <Text style={s.templateTitle}>{t.title}</Text>
              <Text style={s.templateLang}>{t.language} · {t.category}</Text>
            </TouchableOpacity>
          ))}
        </View>

        {/* Message editor */}
        {!selected ? (
          <TextInput
            style={s.messageInput}
            multiline
            placeholder={'Type your message...\nUse {name}, {voterId}, {booth} as placeholders.'}
            placeholderTextColor="#adb5bd"
            value={customMsg}
            onChangeText={setCustomMsg}
          />
        ) : (
          <View style={s.previewBox}>
            <Text style={s.previewLabel}>Preview:</Text>
            <Text style={s.previewText}>{selected.body}</Text>
            <Text style={s.previewHint}>Placeholders: &#123;name&#125;, &#123;voterId&#125;, &#123;booth&#125;</Text>
          </View>
        )}

        {/* Stats */}
        {sentCount > 0 && (
          <View style={s.statsRow}>
            <Ionicons name="checkmark-circle" size={18} color="#2f9e44" />
            <Text style={s.statsTxt}>Sent {sentCount} messages this session</Text>
          </View>
        )}

        {/* Send all button */}
        <TouchableOpacity style={s.sendAllBtn} onPress={sendAll}>
          <Ionicons name="logo-whatsapp" size={20} color="#fff" />
          <Text style={s.sendAllTxt}>Start Outreach ({voters.length} voters)</Text>
        </TouchableOpacity>

        {/* Voter list */}
        <Text style={s.sectionLabel}>Voter List</Text>
        {loading ? (
          <ActivityIndicator color="#3b5bdb" style={{ margin: 24 }} />
        ) : voters.length === 0 ? (
          <Text style={s.emptyTxt}>No voters with mobile numbers in this sentiment group.</Text>
        ) : (
          voters.map(voter => (
            <View key={voter.id} style={s.voterRow}>
              <View style={{ flex: 1 }}>
                <Text style={s.voterName}>{voter.name}</Text>
                <Text style={s.voterMeta}>
                  {voter.voterId} · Booth {voter.boothNumber} · {voter.mobileNumber}
                </Text>
              </View>
              <TouchableOpacity style={s.waBtn} onPress={() => openWhatsApp(voter)}>
                <Ionicons name="logo-whatsapp" size={20} color="#fff" />
              </TouchableOpacity>
            </View>
          ))
        )}
      </ScrollView>
    </View>
  );
}

const s = StyleSheet.create({
  container:       { flex: 1, backgroundColor: '#f0f2f5' },
  header:          { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16 },
  title:           { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:             { color: '#868e96', fontSize: 12, marginTop: 2 },
  sectionLabel:    { fontSize: 12, fontWeight: '700', color: '#868e96', textTransform: 'uppercase',
                     letterSpacing: 0.8, marginHorizontal: 16, marginTop: 16, marginBottom: 8 },
  pillRow:         { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginHorizontal: 16, marginBottom: 4 },
  pill:            { borderRadius: 20, paddingHorizontal: 14, paddingVertical: 7 },
  pillTxt:         { fontSize: 13, fontWeight: '600' },
  templateList:    { marginHorizontal: 16, gap: 6 },
  templateItem:    { backgroundColor: '#fff', borderRadius: 10, padding: 12,
                     borderWidth: 1.5, borderColor: 'transparent' },
  templateSelected:{ borderColor: '#25D366' },
  templateTitle:   { fontSize: 14, fontWeight: '700', color: '#212529' },
  templateLang:    { fontSize: 11, color: '#868e96', marginTop: 2 },
  messageInput:    { backgroundColor: '#fff', margin: 16, borderRadius: 12, padding: 14,
                     fontSize: 14, color: '#212529', minHeight: 100, textAlignVertical: 'top' },
  previewBox:      { backgroundColor: '#f8fff9', margin: 16, borderRadius: 12, padding: 14,
                     borderLeftWidth: 3, borderLeftColor: '#25D366' },
  previewLabel:    { fontSize: 11, color: '#868e96', fontWeight: '700', marginBottom: 4 },
  previewText:     { fontSize: 14, color: '#212529', lineHeight: 20 },
  previewHint:     { fontSize: 11, color: '#adb5bd', marginTop: 6, fontStyle: 'italic' },
  statsRow:        { flexDirection: 'row', alignItems: 'center', gap: 8,
                     marginHorizontal: 16, marginBottom: 4 },
  statsTxt:        { color: '#2f9e44', fontSize: 13, fontWeight: '600' },
  sendAllBtn:      { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8,
                     backgroundColor: '#25D366', marginHorizontal: 16, marginBottom: 8,
                     borderRadius: 12, paddingVertical: 14 },
  sendAllTxt:      { color: '#fff', fontSize: 15, fontWeight: '700' },
  voterRow:        { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff',
                     marginHorizontal: 16, marginBottom: 6, borderRadius: 10, padding: 12, elevation: 1 },
  voterName:       { fontSize: 14, fontWeight: '700', color: '#212529' },
  voterMeta:       { fontSize: 11, color: '#868e96', marginTop: 2 },
  waBtn:           { backgroundColor: '#25D366', borderRadius: 10, padding: 10 },
  emptyTxt:        { textAlign: 'center', color: '#adb5bd', margin: 24, fontSize: 13 },
});
