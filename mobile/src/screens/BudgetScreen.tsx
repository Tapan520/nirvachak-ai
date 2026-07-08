import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput, Alert, ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getBudget, setBudgetItem, BudgetItem, EXPENSE_CATEGORIES } from '../api/budget';

const BRAND = '#e67700';
const CAT_COLOR: Record<string, string> = {
  Publicity: '#3b5bdb', Transport: '#f59f00', Food: '#e67700',
  Communication: '#7950f2', Printing: '#1971c2', Miscellaneous: '#868e96',
};

export default function BudgetScreen() {
  const [items,      setItems]      = useState<BudgetItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showModal,  setShowModal]  = useState(false);
  const [category,   setCategory]   = useState('Publicity');
  const [amount,     setAmount]     = useState('');
  const [notes,      setNotes]      = useState('');
  const [saving,     setSaving]     = useState(false);

  const load = useCallback(async () => {
    try { setItems(await getBudget()); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const totalPlanned = items.reduce((s, i) => s + i.plannedAmount, 0);
  const totalSpent   = items.reduce((s, i) => s + i.spentAmount, 0);
  const overallPct   = totalPlanned > 0 ? Math.round((totalSpent / totalPlanned) * 100) : 0;

  const handleSave = async () => {
    if (!amount.trim()) { Alert.alert('Required', 'Amount is required.'); return; }
    setSaving(true);
    try {
      await setBudgetItem({ category, plannedAmount: parseFloat(amount), notes: notes || undefined });
      setShowModal(false); setAmount(''); setNotes('');
      load(); Alert.alert('Saved', 'Budget updated.');
    } catch { Alert.alert('Error', 'Failed to save.');
    } finally { setSaving(false); }
  };

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Budget Planner</Text>
          <Text style={s.sub}>?{totalSpent.toLocaleString('en-IN')} of ?{totalPlanned.toLocaleString('en-IN')} spent</Text>
        </View>
        <TouchableOpacity style={s.addBtn} onPress={() => setShowModal(true)}>
          <Ionicons name="add" size={22} color="#fff" />
        </TouchableOpacity>
      </View>

      {/* Overall progress */}
      <View style={s.banner}>
        <View style={s.bannerRow}>
          <Text style={s.bannerLbl}>Overall Utilisation</Text>
          <Text style={[s.bannerPct, { color: overallPct > 90 ? '#e03131' : BRAND }]}>{overallPct}%</Text>
        </View>
        <View style={s.progressBg}>
          <View style={[s.progressFill, { width: `${Math.min(overallPct, 100)}%`, backgroundColor: overallPct > 90 ? '#e03131' : '#2f9e44' }]} />
        </View>
      </View>

      <FlatList
        data={items}
        keyExtractor={i => i.id.toString()}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.empty}><Ionicons name="wallet-outline" size={48} color="#dee2e6" />
            <Text style={s.emptyTxt}>No budget items set. Tap + to add.</Text></View>
        }
        renderItem={({ item: b }) => {
          const color = CAT_COLOR[b.category] ?? '#868e96';
          const pct   = b.plannedAmount > 0 ? Math.min(100, (b.spentAmount / b.plannedAmount) * 100) : 0;
          const over  = b.spentAmount > b.plannedAmount;
          return (
            <View style={s.card}>
              <View style={[s.catDot, { backgroundColor: color }]} />
              <View style={{ flex: 1, marginLeft: 12 }}>
                <View style={s.cardTop}>
                  <Text style={s.catName}>{b.category}</Text>
                  <Text style={[s.pctTxt, { color: over ? '#e03131' : color }]}>{b.utilisationPercent.toFixed(1)}%</Text>
                </View>
                <View style={s.amtRow}>
                  <Text style={s.spentTxt}>Spent ?{b.spentAmount.toLocaleString('en-IN')}</Text>
                  <Text style={s.plannedTxt}> / ?{b.plannedAmount.toLocaleString('en-IN')}</Text>
                </View>
                <View style={s.progBg}>
                  <View style={[s.progFill, { width: `${pct}%`, backgroundColor: over ? '#e03131' : color }]} />
                </View>
                {b.remaining < 0 && (
                  <Text style={s.overTxt}>? Over by ?{Math.abs(b.remaining).toLocaleString('en-IN')}</Text>
                )}
              </View>
            </View>
          );
        }}
      />

      <Modal visible={showModal} transparent animationType="slide">
        <View style={m.overlay}>
          <View style={m.modal}>
            <Text style={m.title}>Set Budget</Text>
            <ScrollView showsVerticalScrollIndicator={false}>
              <Text style={m.label}>Category</Text>
              <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 12 }}>
                {EXPENSE_CATEGORIES.map(c => (
                  <TouchableOpacity key={c}
                    style={[m.chip, category === c && { backgroundColor: CAT_COLOR[c] ?? BRAND, borderColor: CAT_COLOR[c] ?? BRAND }]}
                    onPress={() => setCategory(c)}>
                    <Text style={[m.chipTxt, category === c && { color: '#fff' }]}>{c}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
              <Text style={m.label}>Planned Amount (?) *</Text>
              <TextInput style={m.input} value={amount} onChangeText={setAmount} keyboardType="numeric" placeholder="0.00" />
              <Text style={m.label}>Notes</Text>
              <TextInput style={m.input} value={notes} onChangeText={setNotes} placeholder="Optional" />
              <TouchableOpacity style={[m.saveBtn, saving && { opacity: 0.6 }]} onPress={handleSave} disabled={saving}>
                {saving ? <ActivityIndicator color="#fff" /> : <Text style={m.saveTxt}>Save Budget</Text>}
              </TouchableOpacity>
              <TouchableOpacity style={m.cancelBtn} onPress={() => setShowModal(false)}>
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
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:    { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:     { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:       { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:    { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  banner:    { backgroundColor: '#fff', margin: 12, borderRadius: 12, padding: 16, elevation: 1 },
  bannerRow: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 },
  bannerLbl: { fontSize: 13, fontWeight: '600', color: '#495057' },
  bannerPct: { fontSize: 18, fontWeight: '800' },
  progressBg:{ height: 8, backgroundColor: '#f1f3f5', borderRadius: 4 },
  progressFill:{ height: 8, borderRadius: 4 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'flex-start', elevation: 1 },
  catDot:    { width: 4, borderRadius: 2, alignSelf: 'stretch', marginTop: 4 },
  cardTop:   { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 },
  catName:   { fontSize: 15, fontWeight: '700', color: '#212529' },
  pctTxt:    { fontSize: 14, fontWeight: '800' },
  amtRow:    { flexDirection: 'row', alignItems: 'baseline', marginBottom: 6 },
  spentTxt:  { fontSize: 14, fontWeight: '600', color: '#212529' },
  plannedTxt:{ fontSize: 12, color: '#868e96' },
  progBg:    { height: 6, backgroundColor: '#f1f3f5', borderRadius: 3 },
  progFill:  { height: 6, borderRadius: 3 },
  overTxt:   { fontSize: 11, color: '#e03131', marginTop: 4, fontWeight: '600' },
  empty:     { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:  { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
const m = StyleSheet.create({
  overlay:   { flex: 1, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'flex-end' },
  modal:     { backgroundColor: '#fff', borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 20, maxHeight: '80%' },
  title:     { fontSize: 18, fontWeight: '700', textAlign: 'center', color: '#212529', marginBottom: 16 },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 8, padding: 12, fontSize: 14, color: '#212529', backgroundColor: '#f8f9fa', marginBottom: 12 },
  chip:      { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 8, paddingHorizontal: 14, paddingVertical: 8, marginRight: 8 },
  chipTxt:   { fontSize: 13, fontWeight: '600', color: '#495057' },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 10, padding: 14, alignItems: 'center', marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
  cancelBtn: { padding: 12 },
});
