import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getExpenseReport, ExpenseReport } from '../api/reports';

const BRAND = '#1971c2';
const CAT_COLOR: Record<string, string> = {
  Publicity: '#3b5bdb', Transport: '#f59f00', Food: '#e67700',
  Communication: '#7950f2', Printing: '#1971c2', Miscellaneous: '#868e96',
};

export default function ReportsScreen() {
  const [report,     setReport]     = useState<ExpenseReport | null>(null);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try { setReport(await getExpenseReport()); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  const r = report!;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <Text style={s.title}>Expense Report</Text>
        <Text style={s.sub}>EC budget utilisation summary</Text>
      </View>

      <ScrollView
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}>

        {/* Total Banner */}
        <View style={s.banner}>
          <View style={{ flex: 1 }}>
            <Text style={s.bannerLbl}>Total Spent</Text>
            <Text style={s.bannerVal}>?{r.totalAmount.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</Text>
          </View>
          <View style={{ alignItems: 'flex-end' }}>
            <Text style={s.bannerLbl}>EC Budget Limit</Text>
            <Text style={s.bannerSub}>?{r.ecBudgetLimit.toLocaleString('en-IN')}</Text>
          </View>
        </View>

        {/* EC Limit progress */}
        <View style={s.progressCard}>
          <View style={s.progressRow}>
            <Text style={s.progressLbl}>EC Limit Used</Text>
            <Text style={[s.progressPct, { color: r.ecBudgetPercent > 90 ? '#e03131' : '#2f9e44' }]}>{r.ecBudgetPercent}%</Text>
          </View>
          <View style={s.progBg}>
            <View style={[s.progFill, { width: `${r.ecBudgetPercent}%`, backgroundColor: r.ecBudgetPercent > 90 ? '#e03131' : '#2f9e44' }]} />
          </View>
        </View>

        {/* Category breakdown */}
        <Text style={s.sectionTitle}>By Category</Text>
        {r.categoryTotals.map(ct => {
          const color = CAT_COLOR[ct.category] ?? '#868e96';
          return (
            <View key={ct.category} style={s.catCard}>
              <View style={[s.catDot, { backgroundColor: color }]} />
              <View style={{ flex: 1, marginLeft: 12 }}>
                <View style={s.catRow}>
                  <Text style={s.catName}>{ct.category}</Text>
                  <Text style={[s.catAmt, { color }]}>?{ct.amount.toLocaleString('en-IN')}</Text>
                </View>
                <View style={s.progBg}>
                  <View style={[s.progFill, { width: `${ct.percent}%`, backgroundColor: color }]} />
                </View>
                <Text style={s.catPct}>{ct.percent.toFixed(1)}% of total</Text>
              </View>
            </View>
          );
        })}

        {/* Recent expenses */}
        <Text style={s.sectionTitle}>Recent Expenses</Text>
        {r.expenses.slice(0, 20).map(e => {
          const color = CAT_COLOR[e.category] ?? '#868e96';
          return (
            <View key={e.id} style={s.expCard}>
              <View style={[s.expDot, { backgroundColor: color }]} />
              <View style={{ flex: 1, marginLeft: 12 }}>
                <View style={s.expRow}>
                  <Text style={s.expDesc} numberOfLines={1}>{e.description}</Text>
                  <Text style={[s.expAmt, { color }]}>?{e.amount.toLocaleString('en-IN')}</Text>
                </View>
                <Text style={s.expMeta}>
                  {e.category}  ·  {new Date(e.expenseDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })}
                  {e.payeeName ? `  ·  ${e.payeeName}` : ''}
                </Text>
              </View>
            </View>
          );
        })}
      </ScrollView>
    </View>
  );
}

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16 },
  title:        { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  banner:       { backgroundColor: '#fff', borderRadius: 12, padding: 16, marginBottom: 12, flexDirection: 'row', elevation: 1 },
  bannerLbl:    { fontSize: 12, color: '#868e96' },
  bannerVal:    { fontSize: 26, fontWeight: '800', color: '#212529' },
  bannerSub:    { fontSize: 14, fontWeight: '700', color: '#212529' },
  progressCard: { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 12, elevation: 1 },
  progressRow:  { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 },
  progressLbl:  { fontSize: 13, fontWeight: '600', color: '#495057' },
  progressPct:  { fontSize: 18, fontWeight: '800' },
  progBg:       { height: 8, backgroundColor: '#f1f3f5', borderRadius: 4 },
  progFill:     { height: 8, borderRadius: 4 },
  sectionTitle: { fontSize: 13, fontWeight: '700', color: '#868e96', textTransform: 'uppercase', letterSpacing: 1, marginBottom: 8, marginTop: 4 },
  catCard:      { backgroundColor: '#fff', borderRadius: 12, padding: 12, marginBottom: 8, flexDirection: 'row', alignItems: 'center', elevation: 1 },
  catDot:       { width: 4, borderRadius: 2, alignSelf: 'stretch' },
  catRow:       { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 },
  catName:      { fontSize: 14, fontWeight: '700', color: '#212529' },
  catAmt:       { fontSize: 14, fontWeight: '800' },
  catPct:       { fontSize: 11, color: '#868e96', marginTop: 2 },
  expCard:      { backgroundColor: '#fff', borderRadius: 10, padding: 12, marginBottom: 6, flexDirection: 'row', alignItems: 'center', elevation: 1 },
  expDot:       { width: 4, borderRadius: 2, alignSelf: 'stretch' },
  expRow:       { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 2 },
  expDesc:      { fontSize: 13, fontWeight: '600', color: '#212529', flex: 1, marginRight: 8 },
  expAmt:       { fontSize: 13, fontWeight: '800' },
  expMeta:      { fontSize: 11, color: '#868e96' },
});
