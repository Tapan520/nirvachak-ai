import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput, Alert, ScrollView,
  KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getTemplates, createTemplate, getBroadcasts, createBroadcast, MessageTemplateItem, BroadcastItem, LANGUAGES, MSG_CATEGORIES } from '../api/broadcast';

const BRAND = '#e67700';
const STATUS_COLOR: Record<string, string> = {
  Draft: '#868e96', Scheduled: '#3b5bdb', Sent: '#2f9e44', Failed: '#e03131',
};

export default function BroadcastScreen() {
  const [tab,        setTab]        = useState<'broadcasts' | 'templates'>('broadcasts');
  const [broadcasts, setBroadcasts] = useState<BroadcastItem[]>([]);
  const [templates,  setTemplates]  = useState<MessageTemplateItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showTmpl,   setShowTmpl]   = useState(false);
  const [showBC,     setShowBC]     = useState(false);
  // template form
  const [tTitle,  setTTitle]  = useState('');
  const [tBody,   setTBody]   = useState('');
  const [tLang,   setTLang]   = useState('English');
  const [tCat,    setTCat]    = useState('VoterOutreach');
  const [tSaving, setTSaving] = useState(false);
  // broadcast form
  const [bcTmpl,  setBcTmpl]  = useState('');
  const [bcDesc,  setBcDesc]  = useState('');
  const [bcSaving,setBcSaving]= useState(false);

  const load = useCallback(async () => {
    try {
      const [b, t] = await Promise.all([getBroadcasts(), getTemplates()]);
      setBroadcasts(b); setTemplates(t);
    } finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAddTemplate = async () => {
    if (!tTitle.trim() || !tBody.trim()) { Alert.alert('Required', 'Title and body are required.'); return; }
    setTSaving(true);
    try {
      await createTemplate({ title: tTitle.trim(), body: tBody.trim(), language: tLang, category: tCat });
      setShowTmpl(false); setTTitle(''); setTBody('');
      load(); Alert.alert('Saved', 'Template created.');
    } catch { Alert.alert('Error', 'Failed to save template.');
    } finally { setTSaving(false); }
  };

  const handleBroadcast = async () => {
    if (!bcTmpl) { Alert.alert('Required', 'Select a template.'); return; }
    setBcSaving(true);
    try {
      await createBroadcast({ templateId: parseInt(bcTmpl, 10), targetDescription: bcDesc || undefined });
      setShowBC(false); setBcTmpl(''); setBcDesc('');
      load(); Alert.alert('Created', 'Broadcast draft created.');
    } catch { Alert.alert('Error', 'Failed to create broadcast.');
    } finally { setBcSaving(false); }
  };

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Broadcast / Messaging</Text>
          <Text style={s.sub}>{broadcasts.length} broadcasts · {templates.length} templates</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => tab === 'templates' ? setShowTmpl(true) : setShowBC(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      <View style={s.tabBar}>
        {(['broadcasts', 'templates'] as const).map(t => (
          <TouchableOpacity key={t} style={[s.tab, tab === t && s.tabActive]} onPress={() => setTab(t)}>
            <Text style={[s.tabTxt, tab === t && s.tabTxtActive]}>{t === 'broadcasts' ? 'Broadcasts' : 'Templates'}</Text>
          </TouchableOpacity>
        ))}
      </View>

      {tab === 'broadcasts' ? (
        <FlatList
          data={broadcasts}
          keyExtractor={b => b.id.toString()}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
          ListEmptyComponent={<View style={s.empty}><Ionicons name="send-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No broadcasts yet.</Text></View>}
          renderItem={({ item: b }) => {
            const color = STATUS_COLOR[b.status] ?? '#868e96';
            return (
              <View style={s.card}>
                <View style={[s.statusDot, { backgroundColor: color }]} />
                <View style={{ flex: 1, marginLeft: 12 }}>
                  <Text style={s.cardTitle}>{b.templateTitle}</Text>
                  {b.targetDescription && <Text style={s.cardSub}>{b.targetDescription}</Text>}
                  <View style={s.metaRow}>
                    <View style={[s.badge, { backgroundColor: color + '20' }]}>
                      <Text style={[s.badgeTxt, { color }]}>{b.status}</Text>
                    </View>
                    <Text style={s.meta}>{b.totalTargeted} targeted</Text>
                    <Text style={s.meta}>{b.sentCount} sent</Text>
                  </View>
                  <Text style={s.date}>{new Date(b.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })} by {b.createdByName}</Text>
                </View>
              </View>
            );
          }}
        />
      ) : (
        <FlatList
          data={templates}
          keyExtractor={t => t.id.toString()}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
          ListEmptyComponent={<View style={s.empty}><Ionicons name="document-text-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No templates yet.</Text></View>}
          renderItem={({ item: t }) => (
            <View style={s.card}>
              <View style={[s.statusDot, { backgroundColor: BRAND }]} />
              <View style={{ flex: 1, marginLeft: 12 }}>
                <Text style={s.cardTitle}>{t.title}</Text>
                <Text style={s.cardSub} numberOfLines={2}>{t.body}</Text>
                <View style={s.metaRow}>
                  <View style={[s.badge, { backgroundColor: BRAND + '20' }]}>
                    <Text style={[s.badgeTxt, { color: BRAND }]}>{t.category}</Text>
                  </View>
                  <Text style={s.meta}>{t.language}</Text>
                </View>
              </View>
            </View>
          )}
        />
      )}

      {/* Template Modal */}
      <Modal visible={showTmpl} animationType="slide" presentationStyle="pageSheet" onRequestClose={() => setShowTmpl(false)}>
        <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
          <View style={ml.container}>
            <View style={ml.header}>
              <Text style={ml.title}>New Template</Text>
              <TouchableOpacity onPress={() => setShowTmpl(false)}><Ionicons name="close" size={24} color="#212529" /></TouchableOpacity>
            </View>
            <ScrollView contentContainerStyle={{ padding: 16 }}>
              <Text style={ml.label}>Title *</Text>
              <TextInput style={ml.input} value={tTitle} onChangeText={setTTitle} placeholder="Template title" />
              <Text style={ml.label}>Message Body *</Text>
              <TextInput style={[ml.input, { height: 100, textAlignVertical: 'top' }]} value={tBody} onChangeText={setTBody} multiline placeholder="Message content..." />
              <Text style={ml.label}>Language</Text>
              <View style={{ flexDirection: 'row', gap: 8, marginBottom: 16 }}>
                {LANGUAGES.map(l => (
                  <TouchableOpacity key={l} style={[ml.chip, tLang === l && ml.chipActive]} onPress={() => setTLang(l)}>
                    <Text style={[ml.chipTxt, tLang === l && { color: '#fff' }]}>{l}</Text>
                  </TouchableOpacity>
                ))}
              </View>
              <Text style={ml.label}>Category</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 16 }}>
                {MSG_CATEGORIES.map(c => (
                  <TouchableOpacity key={c} style={[ml.chip, tCat === c && ml.chipActive]} onPress={() => setTCat(c)}>
                    <Text style={[ml.chipTxt, tCat === c && { color: '#fff' }]}>{c.replace(/([A-Z])/g, ' $1').trim()}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
              <TouchableOpacity style={[ml.saveBtn, tSaving && { opacity: 0.6 }]} onPress={handleAddTemplate} disabled={tSaving}>
                {tSaving ? <ActivityIndicator color="#fff" /> : <Text style={ml.saveTxt}>Save Template</Text>}
              </TouchableOpacity>
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
      </Modal>

      {/* Broadcast Modal */}
      <Modal visible={showBC} transparent animationType="slide">
        <View style={ml.overlay}>
          <View style={ml.sheet}>
            <Text style={ml.title}>New Broadcast</Text>
            <Text style={ml.label}>Template *</Text>
            <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
              {templates.map(t => (
                <TouchableOpacity key={t.id} style={[ml.chip, bcTmpl === t.id.toString() && ml.chipActive]} onPress={() => setBcTmpl(t.id.toString())}>
                  <Text style={[ml.chipTxt, bcTmpl === t.id.toString() && { color: '#fff' }]}>{t.title}</Text>
                </TouchableOpacity>
              ))}
            </ScrollView>
            <Text style={ml.label}>Target Description</Text>
            <TextInput style={ml.input} value={bcDesc} onChangeText={setBcDesc} placeholder="e.g. All Booth 5 voters" />
            <TouchableOpacity style={[ml.saveBtn, bcSaving && { opacity: 0.6 }]} onPress={handleBroadcast} disabled={bcSaving}>
              {bcSaving ? <ActivityIndicator color="#fff" /> : <Text style={ml.saveTxt}>Create Broadcast</Text>}
            </TouchableOpacity>
            <TouchableOpacity style={{ padding: 12 }} onPress={() => setShowBC(false)}>
              <Text style={{ color: '#868e96', fontWeight: '600', textAlign: 'center' }}>Cancel</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </View>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:    { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:     { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:       { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:    { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  tabBar:    { flexDirection: 'row', margin: 12, backgroundColor: '#fff', borderRadius: 10, padding: 4, elevation: 1 },
  tab:       { flex: 1, paddingVertical: 8, borderRadius: 8, alignItems: 'center' },
  tabActive: { backgroundColor: BRAND },
  tabTxt:    { fontSize: 13, fontWeight: '600', color: '#868e96' },
  tabTxtActive: { color: '#fff' },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'flex-start', elevation: 1 },
  statusDot: { width: 4, borderRadius: 2, alignSelf: 'stretch' },
  cardTitle: { fontSize: 14, fontWeight: '700', color: '#212529', marginBottom: 4 },
  cardSub:   { fontSize: 12, color: '#868e96', marginBottom: 6 },
  metaRow:   { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 4 },
  badge:     { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  badgeTxt:  { fontSize: 11, fontWeight: '700' },
  meta:      { fontSize: 11, color: '#adb5bd' },
  date:      { fontSize: 11, color: '#adb5bd' },
  empty:     { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:  { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
const ml = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  overlay:   { flex: 1, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'flex-end' },
  sheet:     { backgroundColor: '#fff', borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 20 },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529', marginBottom: 16 },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 14, color: '#212529', marginBottom: 16 },
  chip:      { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20, borderWidth: 1, borderColor: '#dee2e6', marginRight: 8 },
  chipActive:{ backgroundColor: BRAND, borderColor: BRAND },
  chipTxt:   { fontSize: 12, fontWeight: '600', color: '#495057' },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, alignItems: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});
