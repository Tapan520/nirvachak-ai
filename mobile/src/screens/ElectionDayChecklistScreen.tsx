import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, FlatList, RefreshControl,
  TouchableOpacity, ActivityIndicator, Switch, TextInput,
  Alert, Linking,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import {
  getBoothChecklist, saveBoothChecklist, ChecklistItem, ChecklistResponse,
} from '../api/boothChecklist';

const BRAND = '#3b5bdb';

const FIELDS: { key: keyof ChecklistItem; label: string; icon: string }[] = [
  { key: 'agentPresent',      label: 'Booth agent present',    icon: 'person-outline' },
  { key: 'bannerDisplayed',   label: 'Banner displayed',        icon: 'flag-outline' },
  { key: 'voterListPrinted',  label: 'Voter list printed',      icon: 'document-text-outline' },
  { key: 'transportArranged', label: 'Transport arranged',      icon: 'car-outline' },
  { key: 'phoneCharged',      label: 'Phone charged',           icon: 'battery-full-outline' },
  { key: 'boothClean',        label: 'Booth clean',             icon: 'sparkles-outline' },
];

export default function ElectionDayChecklistScreen() {
  const [data, setData] = useState<ChecklistResponse>({ total: 0, ready: 0, items: [] });
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [expandedBooth, setExpandedBooth] = useState<number | null>(null);
  const [draft, setDraft] = useState<ChecklistItem | null>(null);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      const res = await getBoothChecklist();
      setData(res);
    } catch {
      // keep prior state
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { setLoading(true); load(); }, [load]);

  const toggleExpand = (item: ChecklistItem) => {
    if (expandedBooth === item.boothNumber) {
      setExpandedBooth(null);
      setDraft(null);
    } else {
      setExpandedBooth(item.boothNumber);
      setDraft({ ...item });
    }
  };

  const updateDraft = (key: keyof ChecklistItem, value: boolean | string) => {
    if (!draft) return;
    setDraft({ ...draft, [key]: value } as ChecklistItem);
  };

  const save = async () => {
    if (!draft) return;
    setSaving(true);
    try {
      await saveBoothChecklist({
        boothNumber: draft.boothNumber,
        agentPresent: draft.agentPresent,
        bannerDisplayed: draft.bannerDisplayed,
        voterListPrinted: draft.voterListPrinted,
        transportArranged: draft.transportArranged,
        phoneCharged: draft.phoneCharged,
        boothClean: draft.boothClean,
        notes: draft.notes ?? null,
      });
      setExpandedBooth(null);
      setDraft(null);
      await load();
      Alert.alert('Saved', `Booth ${draft.boothNumber} checklist updated.`);
    } catch {
      Alert.alert('Error', 'Could not save the checklist. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  const renderItem = ({ item }: { item: ChecklistItem }) => {
    const isOpen = expandedBooth === item.boothNumber;
    const active = isOpen && draft ? draft : item;
    const checkedCount = FIELDS.filter(f => (active as any)[f.key]).length;
    const ready = checkedCount === FIELDS.length;
    const statusColor = ready ? '#2f9e44' : checkedCount === 0 ? '#e03131' : '#f59f00';

    return (
      <View style={[s.card, { borderLeftColor: statusColor, borderLeftWidth: 4 }]}>
        <TouchableOpacity onPress={() => toggleExpand(item)} style={s.cardHeader}>
          <View style={{ flex: 1 }}>
            <Text style={s.title}>Booth #{item.boothNumber}</Text>
            <Text style={s.subtitle} numberOfLines={2}>{item.boothName}</Text>
            {item.address ? <Text style={s.address} numberOfLines={2}>{item.address}</Text> : null}
          </View>
          <View style={s.statusBox}>
            <Text style={[s.statusTxt, { color: statusColor }]}>{checkedCount}/{FIELDS.length}</Text>
            <Text style={s.statusLbl}>{ready ? 'Ready' : 'Pending'}</Text>
            <Ionicons name={isOpen ? 'chevron-up' : 'chevron-down'} size={16} color="#adb5bd" />
          </View>
        </TouchableOpacity>

        {isOpen && draft ? (
          <View style={s.expand}>
            {FIELDS.map(f => (
              <View key={f.key} style={s.row}>
                <Ionicons name={f.icon as any} size={18} color={(draft as any)[f.key] ? '#2f9e44' : '#adb5bd'} />
                <Text style={s.rowLbl}>{f.label}</Text>
                <Switch
                  value={Boolean((draft as any)[f.key])}
                  onValueChange={(v) => updateDraft(f.key, v)}
                  trackColor={{ true: BRAND, false: '#dee2e6' }}
                  thumbColor="#fff"
                />
              </View>
            ))}

            <Text style={s.notesLbl}>Notes</Text>
            <TextInput
              style={s.notes}
              placeholder="Any issues or comments…"
              placeholderTextColor="#adb5bd"
              value={draft.notes ?? ''}
              onChangeText={(t) => updateDraft('notes', t)}
              multiline
            />

            {item.assignedAgentPhone ? (
              <TouchableOpacity
                style={s.callAgent}
                onPress={() => Linking.openURL(`tel:${item.assignedAgentPhone}`)}
              >
                <Ionicons name="call" size={14} color="#2f9e44" />
                <Text style={s.callAgentTxt}>Call {item.assignedAgentName ?? 'agent'}</Text>
              </TouchableOpacity>
            ) : null}

            {item.submittedByName ? (
              <Text style={s.meta}>
                Last saved by {item.submittedByName}
                {item.updatedAt ? ` · ${new Date(item.updatedAt).toLocaleString()}`
                  : item.submittedAt ? ` · ${new Date(item.submittedAt).toLocaleString()}` : ''}
              </Text>
            ) : null}

            <TouchableOpacity
              style={[s.saveBtn, saving && { opacity: 0.6 }]}
              onPress={save}
              disabled={saving}
            >
              {saving ? <ActivityIndicator color="#fff" /> : (
                <>
                  <Ionicons name="checkmark-circle" size={16} color="#fff" />
                  <Text style={s.saveTxt}>Save checklist</Text>
                </>
              )}
            </TouchableOpacity>
          </View>
        ) : null}
      </View>
    );
  };

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.hTitle}>Election Day Checklist</Text>
        <Text style={s.hSub}>Tick off booth-readiness items on the ground.</Text>
      </View>

      <View style={s.summary}>
        <Text style={s.summaryTxt}>
          <Text style={{ color: '#2f9e44', fontWeight: '800' }}>{data.ready}</Text>
          <Text> of </Text>
          <Text style={{ fontWeight: '800' }}>{data.total}</Text>
          <Text> booths ready</Text>
        </Text>
      </View>

      {loading ? (
        <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
      ) : (
        <FlatList
          data={data.items}
          keyExtractor={i => String(i.boothNumber)}
          renderItem={renderItem}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          ItemSeparatorComponent={() => <View style={{ height: 10 }} />}
          ListEmptyComponent={
            <View style={s.center}>
              <Ionicons name="clipboard-outline" size={48} color="#adb5bd" />
              <Text style={{ color: '#868e96', marginTop: 8 }}>No booths available.</Text>
            </View>
          }
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(); }}
              colors={[BRAND]}
            />
          }
        />
      )}
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header:    { backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingBottom: 12, paddingTop: 8 },
  hTitle:    { color: '#fff', fontSize: 20, fontWeight: '800' },
  hSub:      { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  summary:   { paddingHorizontal: 16, paddingTop: 12 },
  summaryTxt:{ color: '#212529', fontSize: 13 },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, elevation: 1, overflow: 'hidden' },
  cardHeader:{ flexDirection: 'row', alignItems: 'flex-start', padding: 12 },
  title:     { fontSize: 15, fontWeight: '800', color: '#212529' },
  subtitle:  { fontSize: 12, color: '#495057', marginTop: 2 },
  address:   { fontSize: 11, color: '#868e96', marginTop: 2 },
  statusBox: { alignItems: 'flex-end', paddingLeft: 8, minWidth: 60 },
  statusTxt: { fontSize: 16, fontWeight: '900' },
  statusLbl: { fontSize: 10, color: '#868e96', textTransform: 'uppercase' },
  expand:    { paddingHorizontal: 12, paddingBottom: 12, borderTopWidth: 1, borderTopColor: '#f1f3f5' },
  row:       { flexDirection: 'row', alignItems: 'center', gap: 10, paddingVertical: 8 },
  rowLbl:    { flex: 1, fontSize: 13, color: '#212529' },
  notesLbl:  { fontSize: 11, fontWeight: '700', color: '#868e96', marginTop: 8, textTransform: 'uppercase' },
  notes:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 8, padding: 10,
               minHeight: 60, textAlignVertical: 'top', marginTop: 4, color: '#212529', backgroundColor: '#fff' },
  callAgent: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 10,
               backgroundColor: '#e6f4ea', paddingHorizontal: 10, paddingVertical: 6,
               borderRadius: 8, alignSelf: 'flex-start' },
  callAgentTxt: { fontSize: 12, fontWeight: '700', color: '#2f9e44' },
  meta:      { fontSize: 11, color: '#868e96', marginTop: 8 },
  saveBtn:   { flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
               gap: 6, backgroundColor: BRAND, paddingVertical: 12, borderRadius: 10, marginTop: 12 },
  saveTxt:   { color: '#fff', fontWeight: '800' },
});
