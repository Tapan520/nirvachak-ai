import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, ScrollView, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { getWinProbability, WinProbabilityData } from '../api/winProbability';

// ??? Tier config ????????????????????????????????????????????????????????????

const TIER_CONFIG: Record<string, { color: string; bg: string; icon: string; message: string }> = {
  Strong:   { color: '#2f9e44', bg: '#d3f9d8', icon: 'trending-up',    message: 'Campaign is well-positioned. Maintain momentum.' },
  Moderate: { color: '#f59f00', bg: '#fff3bf', icon: 'trending-up',    message: 'Competitive position. Focus on floating voters and at-risk booths.' },
  Weak:     { color: '#e67700', bg: '#fff4e6', icon: 'trending-down',  message: 'Significant effort needed. Urgently increase outreach.' },
  Critical: { color: '#e03131', bg: '#fff5f5', icon: 'trending-down',  message: 'Critical situation. Immediate campaign restructuring required.' },
};

// ??? Score Gauge ?????????????????????????????????????????????????????????????

function ScoreGauge({ score, tier }: { score: number; tier: string }) {
const cfg = TIER_CONFIG[tier] ?? TIER_CONFIG['Critical'];

  return (
    <View style={sg.wrapper}>
      {/* Arc drawn with nested views as a visual approximation */}
      <View style={[sg.circle, { borderColor: '#f1f3f5' }]}>
        <View style={[sg.fillArc, { borderColor: cfg.color, borderWidth: 8,
          transform: [{ rotate: `${-135 + (score / 100) * 270}deg` }] }]} />
        <View style={sg.center}>
          <Text style={[sg.scoreText, { color: cfg.color }]}>{score.toFixed(1)}</Text>
          <Text style={sg.scoreLabel}>%</Text>
          <View style={[sg.tierBadge, { backgroundColor: cfg.bg }]}>
            <Text style={[sg.tierText, { color: cfg.color }]}>{tier}</Text>
          </View>
        </View>
      </View>
      <Text style={[sg.message, { color: cfg.color }]}>{cfg.message}</Text>
    </View>
  );
}

const sg = StyleSheet.create({
  wrapper:    { alignItems: 'center', paddingVertical: 8 },
  circle:     { width: 160, height: 160, borderRadius: 80, borderWidth: 12,
                justifyContent: 'center', alignItems: 'center', marginBottom: 12 },
  fillArc:    { position: 'absolute', width: 160, height: 160, borderRadius: 80 },
  center:     { alignItems: 'center' },
  scoreText:  { fontSize: 36, fontWeight: '900' },
  scoreLabel: { fontSize: 16, fontWeight: '700', color: '#868e96', marginTop: -4 },
  tierBadge:  { borderRadius: 20, paddingHorizontal: 14, paddingVertical: 4, marginTop: 6 },
  tierText:   { fontSize: 13, fontWeight: '800' },
  message:    { fontSize: 12, textAlign: 'center', marginTop: 4, paddingHorizontal: 24, lineHeight: 18 },
});

// ??? Stat Card ????????????????????????????????????????????????????????????????

function StatCard({
  icon, label, value, sub, color,
}: { icon: string; label: string; value: string; sub: string; color: string }) {
  return (
    <View style={[sc.card, { borderTopColor: color }]}>
      <View style={[sc.iconBox, { backgroundColor: color + '18' }]}>
        <Ionicons name={icon as any} size={18} color={color} />
      </View>
      <Text style={[sc.value, { color }]}>{value}</Text>
      <Text style={sc.label}>{label}</Text>
      <Text style={sc.sub}>{sub}</Text>
    </View>
  );
}

const sc = StyleSheet.create({
  card:    { flex: 1, backgroundColor: '#fff', borderRadius: 12, padding: 12,
             borderTopWidth: 3, alignItems: 'center', elevation: 1 },
  iconBox: { width: 34, height: 34, borderRadius: 8, justifyContent: 'center',
             alignItems: 'center', marginBottom: 6 },
  value:   { fontSize: 20, fontWeight: '800' },
  label:   { fontSize: 11, fontWeight: '600', color: '#495057', marginTop: 2, textAlign: 'center' },
  sub:     { fontSize: 10, color: '#adb5bd', marginTop: 1, textAlign: 'center' },
});

// ??? Progress Bar ?????????????????????????????????????????????????????????????

function ProgressRow({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <View style={pr.row}>
      <View style={pr.labelRow}>
        <Text style={pr.label}>{label}</Text>
        <Text style={[pr.pct, { color }]}>{value.toFixed(1)}%</Text>
      </View>
      <View style={pr.track}>
        <View style={[pr.fill, { width: `${Math.min(value, 100)}%`, backgroundColor: color }]} />
      </View>
    </View>
  );
}

const pr = StyleSheet.create({
  row:      { marginBottom: 14 },
  labelRow: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 },
  label:    { fontSize: 12, color: '#495057', fontWeight: '500' },
  pct:      { fontSize: 12, fontWeight: '800' },
  track:    { height: 8, backgroundColor: '#f1f3f5', borderRadius: 4 },
  fill:     { height: 8, borderRadius: 4 },
});

// ??? Main Screen ?????????????????????????????????????????????????????????????

export default function WinProbabilityScreen() {
  const navigation = useNavigation();
  const [data,       setData]       = useState<WinProbabilityData | null>(null);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error,      setError]      = useState('');

  const load = useCallback(async () => {
    setError('');
    try { setData(await getWinProbability()); }
    catch { setError('Could not load win probability data.'); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) {
    return <View style={s.center}><ActivityIndicator color="#f59f00" size="large" /></View>;
  }

  if (error || !data) {
    return (
      <View style={s.center}>
        <Ionicons name="trophy-outline" size={52} color="#dee2e6" />
        <Text style={s.errorTxt}>{error || 'No data available.'}</Text>
        <TouchableOpacity style={s.retryBtn} onPress={load}>
          <Text style={s.retryTxt}>Retry</Text>
        </TouchableOpacity>
      </View>
    );
  }

  const tierCfg     = TIER_CONFIG[data.tier] ?? TIER_CONFIG['Critical'];
  const floatPct    = data.totalVoters > 0 ? (data.floatingVoters / data.totalVoters) * 100 : 0;
  const againstPct  = data.totalVoters > 0 ? (data.againstVoters  / data.totalVoters) * 100 : 0;

  return (
    <ScrollView
      style={s.container}
      contentContainerStyle={{ paddingBottom: 40 }}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
    >
      {/* Header */}
      <View style={s.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={s.backBtn}>
          <Ionicons name="arrow-back" size={24} color="#fff" />
        </TouchableOpacity>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Win Probability</Text>
          <Text style={s.sub}>AI-powered campaign assessment</Text>
        </View>
        <Ionicons name="trophy" size={28} color="#f59f00" />
      </View>

      {/* Score Gauge */}
      <View style={[s.gaugeCard, { borderColor: tierCfg.color }]}>
        <ScoreGauge score={data.score} tier={data.tier} />
      </View>

      {/* Key Stats — 3+3 grid */}
      <View style={s.statsGrid}>
        <View style={s.statsRow}>
          <StatCard icon="thumbs-up-outline"      label="In Favour"      value={data.favourVoters.toLocaleString('en-IN')}   sub={`${data.favourRate.toFixed(1)}% of total`}       color="#2f9e44" />
          <StatCard icon="swap-horizontal-outline" label="Floating"       value={data.floatingVoters.toLocaleString('en-IN')} sub={`+${Math.round(data.floatingConversionPotential)} potential`} color="#f59f00" />
          <StatCard icon="thumbs-down-outline"    label="Against"        value={data.againstVoters.toLocaleString('en-IN')}  sub="to be countered"                                 color="#e03131" />
        </View>
        <View style={s.statsRow}>
          <StatCard icon="checkmark-done-outline" label="Contacted"      value={data.contactedVoters.toLocaleString('en-IN')} sub={`${data.contactCoverage.toFixed(1)}% coverage`}  color="#3b5bdb" />
          <StatCard icon="warning-outline"        label="Booths At Risk"  value={data.boothsAtRisk.toString()}                sub="low turnout risk"                                color="#e67700" />
          <StatCard icon="checkmark-circle-outline" label="Est. Win Votes" value={data.estimatedWinVotes.toLocaleString('en-IN')} sub="if turnout holds"                           color="#7950f2" />
        </View>
      </View>

      {/* Campaign Progress bars */}
      <View style={s.card}>
        <Text style={s.cardTitle}>
          <Ionicons name="bar-chart-outline" size={14} color="#2f9e44" /> Campaign Progress
        </Text>
        <ProgressRow label="Favour Rate (of all voters)"    value={data.favourRate}        color="#2f9e44" />
        <ProgressRow label="Voter Contact Coverage"         value={data.contactCoverage}   color="#3b5bdb" />
        <ProgressRow label="Floating Voters (conv. pool)"   value={floatPct}               color="#f59f00" />
        <ProgressRow label="Against Voters (headwind)"      value={againstPct}             color="#e03131" />
      </View>

      {/* Strengths */}
      {data.strengthPoints.length > 0 && (
        <View style={s.card}>
          <Text style={[s.cardTitle, { color: '#2f9e44' }]}>
            <Ionicons name="checkmark-circle-outline" size={14} color="#2f9e44" /> Strengths
          </Text>
          {data.strengthPoints.map((pt, i) => (
            <View key={i} style={s.bulletRow}>
              <Ionicons name="checkmark" size={14} color="#2f9e44" style={{ marginTop: 2 }} />
              <Text style={[s.bulletTxt, { color: '#212529' }]}>{pt}</Text>
            </View>
          ))}
        </View>
      )}

      {/* Risks */}
      {data.riskPoints.length > 0 && (
        <View style={s.card}>
          <Text style={[s.cardTitle, { color: '#e03131' }]}>
            <Ionicons name="warning-outline" size={14} color="#e03131" /> Risks & Challenges
          </Text>
          {data.riskPoints.map((pt, i) => (
            <View key={i} style={s.bulletRow}>
              <Ionicons name="alert-circle-outline" size={14} color="#e03131" style={{ marginTop: 2 }} />
              <Text style={[s.bulletTxt, { color: '#495057' }]}>{pt}</Text>
            </View>
          ))}
        </View>
      )}

      {/* Recommended actions */}
      <View style={s.card}>
        <Text style={s.cardTitle}>
          <Ionicons name="flash-outline" size={14} color="#f59f00" /> Recommended Actions
        </Text>
        <View style={s.actionsGrid}>
          {[
            { label: 'Convert Floating Voters', icon: 'swap-horizontal-outline', color: '#f59f00', screen: 'Voters' },
            { label: 'Booth Predictions',        icon: 'bar-chart-outline',       color: '#3b5bdb', screen: 'PredictiveAnalytics' },
            { label: 'Phone Banking',            icon: 'call-outline',            color: '#1971c2', screen: 'PhoneBanking' },
            { label: 'Schedule Events',          icon: 'calendar-outline',        color: '#2f9e44', screen: 'CampaignEvents' },
          ].map(action => (
            <TouchableOpacity
              key={action.label}
              style={[s.actionBtn, { borderColor: action.color + '40', backgroundColor: action.color + '10' }]}
              onPress={() => (navigation as any).navigate(action.screen)}>
              <Ionicons name={action.icon as any} size={20} color={action.color} />
              <Text style={[s.actionTxt, { color: action.color }]}>{action.label}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
    </ScrollView>
  );
}

// ??? Styles ??????????????????????????????????????????????????????????????????

const s = StyleSheet.create({
  container:  { flex: 1, backgroundColor: '#f0f2f5' },
  center:     { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  header:     { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 20,
                paddingHorizontal: 16, flexDirection: 'row', alignItems: 'center', gap: 12 },
  backBtn:    { padding: 4 },
  title:      { color: '#fff', fontSize: 20, fontWeight: '700' },
  sub:        { color: '#868e96', fontSize: 12, marginTop: 1 },
  gaugeCard:  { backgroundColor: '#fff', margin: 12, borderRadius: 16,
                borderWidth: 2, padding: 16, elevation: 2 },
  statsGrid:  { marginHorizontal: 12, gap: 8 },
  statsRow:   { flexDirection: 'row', gap: 8 },
  card:       { backgroundColor: '#fff', margin: 12, marginTop: 0, borderRadius: 14, padding: 16, elevation: 1 },
  cardTitle:  { fontSize: 13, fontWeight: '700', color: '#495057', marginBottom: 14 },
  bulletRow:  { flexDirection: 'row', gap: 8, marginBottom: 10, alignItems: 'flex-start' },
  bulletTxt:  { flex: 1, fontSize: 13, lineHeight: 19 },
  actionsGrid:{ flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  actionBtn:  { flexDirection: 'row', alignItems: 'center', gap: 8, borderRadius: 10,
                borderWidth: 1, paddingVertical: 10, paddingHorizontal: 14, minWidth: '45%' },
  actionTxt:  { fontSize: 12, fontWeight: '700', flex: 1 },
  errorTxt:   { color: '#adb5bd', fontSize: 14, marginTop: 12, textAlign: 'center' },
  retryBtn:   { marginTop: 16, backgroundColor: '#f59f00', borderRadius: 10,
                paddingHorizontal: 24, paddingVertical: 10 },
  retryTxt:   { color: '#fff', fontWeight: '700', fontSize: 14 },
});
