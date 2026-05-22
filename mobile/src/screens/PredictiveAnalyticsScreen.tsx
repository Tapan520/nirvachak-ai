import React, { useEffect, useState, useCallback } from 'react';
import {
  View, Text, StyleSheet, ScrollView,
  ActivityIndicator, RefreshControl, TouchableOpacity,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getPredictions, PredictionSummary, BoothPrediction } from '../api/predictions';

// ?? Colour helpers ?????????????????????????????????????????????????????????????

const RISK_COLOR: Record<string, string> = {
  High:   '#e03131',
  Medium: '#f59f00',
  Low:    '#2f9e44',
};

const CONF_COLOR: Record<string, string> = {
  Strong:   '#2f9e44',
  Moderate: '#3b5bdb',
  Weak:     '#868e96',
};

// ?? Subcomponents ??????????????????????????????????????????????????????????????

function KpiCard({ label, value, sub, color, icon }: {
  label: string; value: string; sub: string; color: string; icon: string;
}) {
  return (
    <View style={[k.card, { borderTopColor: color }]}>
      <Ionicons name={icon as any} size={20} color={color} style={{ marginBottom: 4 }} />
      <Text style={[k.value, { color }]}>{value}</Text>
      <Text style={k.label}>{label}</Text>
      <Text style={k.sub}>{sub}</Text>
    </View>
  );
}

const k = StyleSheet.create({
  card:  { flex: 1, minWidth: 140, backgroundColor: '#fff', borderRadius: 10,
           padding: 12, margin: 4, elevation: 1, borderTopWidth: 3, alignItems: 'center' },
  value: { fontSize: 22, fontWeight: '800', marginBottom: 2 },
  label: { fontSize: 12, fontWeight: '700', color: '#212529', textAlign: 'center' },
  sub:   { fontSize: 10, color: '#868e96', textAlign: 'center', marginTop: 2 },
});

function ProgressBar({ pct, color }: { pct: number; color: string }) {
  return (
    <View style={pb.track}>
      <View style={[pb.fill, { width: `${Math.min(pct, 100)}%` as any, backgroundColor: color }]} />
    </View>
  );
}
const pb = StyleSheet.create({
  track: { flex: 1, height: 7, backgroundColor: '#e9ecef', borderRadius: 4, overflow: 'hidden' },
  fill:  { height: '100%', borderRadius: 4 },
});

function BoothCard({ booth, expanded, onToggle }: {
  booth: BoothPrediction; expanded: boolean; onToggle: () => void;
}) {
  const riskColor = RISK_COLOR[booth.turnoutRisk]  ?? '#868e96';
  const confColor = CONF_COLOR[booth.supportConfidence] ?? '#868e96';
  const contactPct = booth.totalVoters > 0
    ? Math.round((booth.contactedVoters / booth.totalVoters) * 100) : 0;

  return (
    <View style={bc.card}>
      <TouchableOpacity onPress={onToggle} activeOpacity={0.8}>
        <View style={bc.header}>
          <View style={bc.boothBadge}>
            <Text style={bc.boothNum}>#{booth.boothNumber}</Text>
          </View>
          <View style={{ flex: 1, marginLeft: 10 }}>
            <Text style={bc.boothName} numberOfLines={1}>{booth.boothName}</Text>
            <Text style={bc.boothSub}>{booth.totalVoters} voters · {contactPct}% contacted</Text>
          </View>
          <View style={{ alignItems: 'flex-end', marginLeft: 8 }}>
            <View style={[bc.riskBadge, { backgroundColor: riskColor + '22' }]}>
              <Text style={[bc.riskTxt, { color: riskColor }]}>{booth.turnoutRisk} Risk</Text>
            </View>
          </View>
          <Ionicons
            name={expanded ? 'chevron-up' : 'chevron-down'}
            size={16} color="#adb5bd" style={{ marginLeft: 6 }} />
        </View>

        {/* Always-visible forecast row */}
        <View style={bc.forecastRow}>
          <View style={bc.forecastItem}>
            <Text style={bc.forecastLabel}>Turnout</Text>
            <Text style={[bc.forecastVal, { color: riskColor }]}>
              {booth.predictedTurnoutPercent}%
            </Text>
            <ProgressBar pct={booth.predictedTurnoutPercent} color={riskColor} />
          </View>
          <View style={bc.divider} />
          <View style={bc.forecastItem}>
            <Text style={bc.forecastLabel}>Support</Text>
            <Text style={[bc.forecastVal, { color: confColor }]}>
              {booth.predictedSupportPercent}%
            </Text>
            <ProgressBar pct={booth.predictedSupportPercent} color={confColor} />
          </View>
          <View style={bc.divider} />
          <View style={bc.forecastItem}>
            <Text style={bc.forecastLabel}>Est. Votes</Text>
            <Text style={[bc.forecastVal, { color: '#0c8599' }]}>
              {booth.estimatedFavourVotes}
            </Text>
          </View>
        </View>
      </TouchableOpacity>

      {/* Expandable detail */}
      {expanded && (
        <View style={bc.detail}>
          {/* Stats grid */}
          <View style={bc.statsRow}>
            {[
              { label: 'Favour',   val: booth.favourVoters,   color: '#2f9e44' },
              { label: 'Against',  val: booth.againstVoters,  color: '#e03131' },
              { label: 'Floating', val: booth.floatingVoters, color: '#f59f00' },
              { label: 'Visits 7d',val: booth.recentVisits,   color: '#3b5bdb' },
            ].map(({ label, val, color }) => (
              <View key={label} style={bc.statBox}>
                <Text style={[bc.statVal, { color }]}>{val}</Text>
                <Text style={bc.statLabel}>{label}</Text>
              </View>
            ))}
          </View>

          {/* Strategy alerts */}
          {booth.strategyAlerts.length > 0 ? (
            <View style={bc.alertBox}>
              <View style={bc.alertHeader}>
                <Ionicons name="warning-outline" size={14} color="#e03131" />
                <Text style={bc.alertTitle}> Strategy Alerts</Text>
              </View>
              {booth.strategyAlerts.map((a, i) => (
                <Text key={i} style={bc.alertTxt}>• {a}</Text>
              ))}
            </View>
          ) : (
            <View style={bc.onTrack}>
              <Ionicons name="checkmark-circle-outline" size={14} color="#2f9e44" />
              <Text style={bc.onTrackTxt}> On track — no alerts for this booth.</Text>
            </View>
          )}
        </View>
      )}
    </View>
  );
}

const bc = StyleSheet.create({
  card:        { backgroundColor: '#fff', borderRadius: 12, marginBottom: 10,
                 elevation: 1, overflow: 'hidden' },
  header:      { flexDirection: 'row', alignItems: 'center', padding: 12, paddingBottom: 8 },
  boothBadge:  { backgroundColor: '#3b5bdb', borderRadius: 8, paddingHorizontal: 8,
                 paddingVertical: 4, minWidth: 36, alignItems: 'center' },
  boothNum:    { color: '#fff', fontSize: 12, fontWeight: '800' },
  boothName:   { fontSize: 13, fontWeight: '700', color: '#212529' },
  boothSub:    { fontSize: 11, color: '#868e96', marginTop: 1 },
  riskBadge:   { borderRadius: 6, paddingHorizontal: 7, paddingVertical: 2 },
  riskTxt:     { fontSize: 10, fontWeight: '800' },
  forecastRow: { flexDirection: 'row', borderTopWidth: 1, borderTopColor: '#f1f3f5',
                 paddingHorizontal: 12, paddingVertical: 10, alignItems: 'center' },
  forecastItem:{ flex: 1, alignItems: 'center', gap: 4 },
  forecastLabel:{ fontSize: 10, color: '#868e96', fontWeight: '600' },
  forecastVal: { fontSize: 16, fontWeight: '800' },
  divider:     { width: 1, height: 36, backgroundColor: '#f1f3f5', marginHorizontal: 4 },
  detail:      { borderTopWidth: 1, borderTopColor: '#f1f3f5', padding: 12 },
  statsRow:    { flexDirection: 'row', justifyContent: 'space-around', marginBottom: 12 },
  statBox:     { alignItems: 'center' },
  statVal:     { fontSize: 18, fontWeight: '800' },
  statLabel:   { fontSize: 10, color: '#868e96', marginTop: 2 },
  alertBox:    { backgroundColor: '#fff5f5', borderRadius: 8, padding: 10 },
  alertHeader: { flexDirection: 'row', alignItems: 'center', marginBottom: 6 },
  alertTitle:  { fontSize: 12, fontWeight: '700', color: '#e03131' },
  alertTxt:    { fontSize: 11, color: '#e03131', marginBottom: 3, lineHeight: 16 },
  onTrack:     { flexDirection: 'row', alignItems: 'center', backgroundColor: '#f0fdf4',
                 borderRadius: 8, padding: 8 },
  onTrackTxt:  { fontSize: 11, color: '#2f9e44', fontWeight: '600' },
});

// ?? Main screen ????????????????????????????????????????????????????????????????

export default function PredictiveAnalyticsScreen() {
  const [data, setData]           = useState<PredictionSummary | null>(null);
  const [loading, setLoading]     = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError]         = useState<string | null>(null);
  const [expandedBooth, setExpandedBooth] = useState<number | null>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      setData(await getPredictions());
    } catch (e: any) {
      setError(e?.response?.data?.message ?? 'Failed to load predictions. Please try again.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  if (loading) {
    return (
      <View style={s.center}>
        <ActivityIndicator color="#3b5bdb" size="large" />
        <Text style={s.loadingTxt}>Generating predictions…</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View style={s.center}>
        <Ionicons name="alert-circle-outline" size={48} color="#e03131" />
        <Text style={s.errorTxt}>{error}</Text>
        <TouchableOpacity style={s.retryBtn} onPress={load}>
          <Text style={s.retryTxt}>Retry</Text>
        </TouchableOpacity>
      </View>
    );
  }

  if (!data) return null;

  const atRiskBooths = data.boothPredictions.filter(b => b.strategyAlerts.length > 0);

  return (
    <ScrollView
      style={s.container}
      refreshControl={<RefreshControl refreshing={refreshing}
        onRefresh={() => { setRefreshing(true); load(); }} />}
    >
      {/* Header */}
      <View style={s.header}>
        <Ionicons name="trending-up-outline" size={28} color="#fff" />
        <View style={{ marginLeft: 10 }}>
          <Text style={s.title}>Predictive Analytics</Text>
          <Text style={s.subtitle}>AI-powered turnout &amp; support forecasts</Text>
        </View>
      </View>

      {/* Info banner */}
      <View style={s.infoBanner}>
        <Ionicons name="information-circle-outline" size={16} color="#1971c2" />
        <Text style={s.infoTxt}>
          {' '}Forecasts use contact rate, sentiment ratios, visit momentum and a 60% MLA baseline.
          Pull down to refresh.
        </Text>
      </View>

      {/* KPI cards — row 1 */}
      <View style={s.kpiRow}>
        <KpiCard label="Predicted Turnout"  value={`${data.predictedOverallTurnout}%`}
          sub={`${data.totalVoters.toLocaleString()} voters`}      color="#3b5bdb" icon="activity-outline" />
        <KpiCard label="Predicted Support"  value={`${data.predictedOverallSupport}%`}
          sub="voters expected in favour"                          color="#2f9e44" icon="thumbs-up-outline" />
      </View>
      {/* KPI cards — row 2 */}
      <View style={s.kpiRow}>
        <KpiCard label="Est. Favour Votes"  value={data.estimatedTotalFavourVotes.toLocaleString()}
          sub="estimated votes for you"                            color="#0c8599" icon="checkmark-circle-outline" />
        <KpiCard label="At-Risk Booths"     value={`${data.atRiskBoothCount}`}
          sub="high turnout risk"                                  color="#e03131" icon="warning-outline" />
      </View>
      <View style={s.kpiRow}>
        <KpiCard label="Total Contacted"    value={data.totalContacted.toLocaleString()}
          sub={`of ${data.totalVoters.toLocaleString()} eligible`} color="#7950f2" icon="people-outline" />
        <KpiCard label="Weak Coverage"      value={`${data.weakSupportBoothCount}`}
          sub="booths need more outreach"                          color="#f59f00" icon="person-remove-outline" />
      </View>

      {/* Priority alerts section */}
      {atRiskBooths.length > 0 && (
        <View style={s.section}>
          <View style={s.sectionHeader}>
            <Ionicons name="flash-outline" size={16} color="#e03131" />
            <Text style={[s.sectionTitle, { color: '#e03131' }]}>
              {' '}Priority Actions — {atRiskBooths.length} Booths Need Attention
            </Text>
          </View>
          {atRiskBooths
            .sort((a, b) => b.strategyAlerts.length - a.strategyAlerts.length)
            .slice(0, 5)
            .map(b => (
              <View key={b.boothNumber} style={s.alertCard}>
                <Text style={s.alertBoothName}>
                  #{b.boothNumber} {b.boothName}
                </Text>
                {b.strategyAlerts.map((a, i) => (
                  <Text key={i} style={s.alertItem}>• {a}</Text>
                ))}
              </View>
            ))}
        </View>
      )}

      {/* Booth-wise forecast */}
      <View style={s.section}>
        <View style={s.sectionHeader}>
          <Ionicons name="business-outline" size={16} color="#3b5bdb" />
          <Text style={s.sectionTitle}> Booth-wise Forecast</Text>
          <Text style={s.sectionSub}> — tap a booth to expand</Text>
        </View>
        {data.boothPredictions.map(b => (
          <BoothCard
            key={b.boothNumber}
            booth={b}
            expanded={expandedBooth === b.boothNumber}
            onToggle={() =>
              setExpandedBooth(prev => prev === b.boothNumber ? null : b.boothNumber)
            }
          />
        ))}
      </View>

      <View style={{ height: 32 }} />
    </ScrollView>
  );
}

const s = StyleSheet.create({
  container:   { flex: 1, backgroundColor: '#f0f2f5' },
  center:      { flex: 1, justifyContent: 'center', alignItems: 'center',
                 backgroundColor: '#f0f2f5', padding: 24 },
  loadingTxt:  { marginTop: 12, color: '#868e96', fontSize: 13 },
  errorTxt:    { color: '#e03131', fontSize: 14, textAlign: 'center', marginTop: 12, lineHeight: 20 },
  retryBtn:    { marginTop: 16, backgroundColor: '#3b5bdb', borderRadius: 8,
                 paddingHorizontal: 24, paddingVertical: 10 },
  retryTxt:    { color: '#fff', fontWeight: '700', fontSize: 14 },
  header:      { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 20,
                 paddingHorizontal: 16, flexDirection: 'row', alignItems: 'center' },
  title:       { color: '#fff', fontSize: 20, fontWeight: '800' },
  subtitle:    { color: '#868e96', fontSize: 12, marginTop: 2 },
  infoBanner:  { backgroundColor: '#e7f5ff', margin: 12, borderRadius: 10, padding: 10,
                 flexDirection: 'row', alignItems: 'flex-start' },
  infoTxt:     { flex: 1, fontSize: 11, color: '#1971c2', lineHeight: 16 },
  kpiRow:      { flexDirection: 'row', marginHorizontal: 8, marginBottom: 0 },
  section:     { margin: 12, marginTop: 16 },
  sectionHeader:{ flexDirection: 'row', alignItems: 'center', marginBottom: 10 },
  sectionTitle: { fontSize: 14, fontWeight: '700', color: '#212529' },
  sectionSub:  { fontSize: 12, color: '#868e96' },
  alertCard:   { backgroundColor: '#fff5f5', borderRadius: 10, padding: 12,
                 marginBottom: 8, borderLeftWidth: 3, borderLeftColor: '#e03131' },
  alertBoothName: { fontSize: 13, fontWeight: '700', color: '#c92a2a', marginBottom: 6 },
  alertItem:   { fontSize: 11, color: '#e03131', lineHeight: 18 },
});
