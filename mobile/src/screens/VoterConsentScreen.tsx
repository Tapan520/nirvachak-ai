import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, FlatList, TextInput,
  TouchableOpacity, ActivityIndicator, RefreshControl, Linking,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { API_BASE_URL } from '../api/client';
import {
  getConsentStats, getCompletedVoters, getPendingVoters,
  VoterConsentStats, SurveyCompletedVoter, SurveyPendingVoter,
} from '../api/voterConsent';

type Tab = 'stats' | 'completed' | 'pending';

// ?? Consent progress bar ??????????????????????????????????????????
function ConsentBar({ label, count, total, color }: {
  label: string; count: number; total: number; color: string;
}) {
  const pct = total > 0 ? Math.round((count / total) * 100) : 0;
  return (
    <View style={{ marginBottom: 12 }}>
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 }}>
        <Text style={{ fontSize: 12, color: '#495057', fontWeight: '600' }}>{label}</Text>
        <Text style={{ fontSize: 12, color: '#212529', fontWeight: '700' }}>
          {count}{' '}
          <Text style={{ color: '#868e96', fontWeight: '400' }}>({pct}%)</Text>
        </Text>
      </View>
      <View style={{ height: 8, backgroundColor: '#e9ecef', borderRadius: 4, overflow: 'hidden' }}>
        <View style={{ width: `${pct}%` as any, height: '100%', backgroundColor: color, borderRadius: 4 }} />
      </View>
    </View>
  );
}

// ?? Overview / Stats tab ??????????????????????????????????????????
function StatsTab({ stats }: { stats: VoterConsentStats }) {
  const surveyUrl = `${API_BASE_URL}/Survey`;
  return (
    <ScrollView contentContainerStyle={{ padding: 12 }}>
      {/* Summary mini-cards */}
      <View style={s.cardRow}>
        {[
          { label: 'Total',     val: stats.totalVoters,    color: '#3b5bdb' },
          { label: 'Completed', val: stats.completedCount, color: '#2f9e44' },
          { label: 'Pending',   val: stats.pendingCount,   color: '#e03131' },
          { label: 'Coupons',   val: stats.couponsIssued,  color: '#f59f00' },
        ].map(({ label, val, color }) => (
          <View key={label} style={[s.miniCard, { borderTopColor: color }]}>
            <Text style={[s.miniVal, { color }]}>{val}</Text>
            <Text style={s.miniLbl}>{label}</Text>
          </View>
        ))}
      </View>

      {/* Completion rate */}
      <View style={s.card}>
        <Text style={s.cardTitle}>Survey Completion Rate</Text>
        <Text style={{ fontSize: 36, fontWeight: '800', color: '#3b5bdb', marginBottom: 6 }}>
          {stats.completionRate}%
        </Text>
        <View style={{ height: 10, backgroundColor: '#e9ecef', borderRadius: 5, overflow: 'hidden' }}>
          <View style={{
            width: `${stats.completionRate}%` as any,
            height: '100%', backgroundColor: '#3b5bdb', borderRadius: 5,
          }} />
        </View>
        <Text style={{ fontSize: 11, color: '#868e96', marginTop: 6 }}>
          {stats.completedCount} completed � {stats.pendingCount} pending � {stats.couponsRedeemed} coupons redeemed
        </Text>
      </View>

      {/* Consent rates */}
      <View style={s.card}>
        <Text style={s.cardTitle}>Consent Rates</Text>
        <ConsentBar label="3rd-Party Advertising (Mandatory)" count={stats.consentThirdParty}
          total={stats.completedCount} color="#f59f00" />
        <ConsentBar label="Campaign Outreach"    count={stats.consentCampaign}  total={stats.completedCount} color="#3b5bdb" />
        <ConsentBar label="WhatsApp Messages"    count={stats.consentWhatsApp}  total={stats.completedCount} color="#2f9e44" />
        <ConsentBar label="Scheme Notifications" count={stats.consentScheme}    total={stats.completedCount} color="#4dabf7" />
        <ConsentBar label="Data for Analytics"   count={stats.consentAnalytics} total={stats.completedCount} color="#868e96" />
      </View>

      {/* Completions by booth */}
      {stats.completionByBooth.length > 0 && (
        <View style={s.card}>
          <Text style={s.cardTitle}>Completions by Booth</Text>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
            {stats.completionByBooth.map(b => (
              <View key={b.boothNumber} style={s.boothChip}>
                <Text style={s.boothChipNum}>#{b.boothNumber}</Text>
                <Text style={s.boothChipVal}>{b.count}</Text>
              </View>
            ))}
          </View>
        </View>
      )}

      {/* Survey link */}
      <View style={[s.card, { backgroundColor: '#1a1f2e' }]}>
        <Text style={{ color: '#adb5bd', fontSize: 12, marginBottom: 4 }}>Survey Link to Share</Text>
        <Text style={{ color: '#4dabf7', fontSize: 12 }} numberOfLines={1}>{surveyUrl}</Text>
        <TouchableOpacity style={s.shareBtn}
          onPress={() => Linking.openURL(surveyUrl)}>
          <Ionicons name="open-outline" size={14} color="#fff" />
          <Text style={{ color: '#fff', fontSize: 12, fontWeight: '600', marginLeft: 6 }}>
            Open Survey
          </Text>
        </TouchableOpacity>
      </View>
    </ScrollView>
  );
}

// ?? Completed voters tab ??????????????????????????????????????????
function CompletedTab({ filterBooth, filterWard }: { filterBooth?: number; filterWard?: string }) {
  const nav = useNavigation<any>();
  const [items,      setItems]      = useState<SurveyCompletedVoter[]>([]);
  const [search,     setSearch]     = useState('');
  const [page,       setPage]       = useState(1);
  const [total,      setTotal]      = useState(0);
  const [loading,    setLoading]    = useState(true);
  const [moreLoad,   setMoreLoad]   = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async (p = 1, q = '', reset = false) => {
    try {
      const res = await getCompletedVoters({
        booth: filterBooth, ward: filterWard,
        search: q || undefined, page: p, pageSize: 30,
      });
      setTotal(res.total);
      setItems(prev => reset || p === 1 ? res.items : [...prev, ...res.items]);
      setPage(p);
    } finally {
      setLoading(false); setMoreLoad(false); setRefreshing(false);
    }
  }, [filterBooth, filterWard]);

  useEffect(() => { load(1, '', true); }, [load]);

  const onSearch = (t: string) => { setSearch(t); setLoading(true); load(1, t, true); };

  return (
    <View style={{ flex: 1 }}>
      <View style={s.searchRow}>
        <Ionicons name="search-outline" size={15} color="#868e96" />
        <TextInput style={s.searchInput} value={search} onChangeText={onSearch}
          placeholder="Search name / EPIC / mobile..." placeholderTextColor="#adb5bd" />
        {!!search && (
          <TouchableOpacity onPress={() => onSearch('')}>
            <Ionicons name="close-circle" size={15} color="#adb5bd" />
          </TouchableOpacity>
        )}
      </View>
      {loading
        ? <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>
        : (
          <FlatList
            data={items}
            keyExtractor={v => v.id.toString()}
            contentContainerStyle={{ padding: 12 }}
            refreshControl={<RefreshControl refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(1, search, true); }} />}
            onEndReached={() => {
              if (!moreLoad && page < Math.ceil(total / 30)) {
                setMoreLoad(true); load(page + 1, search);
              }
            }}
            onEndReachedThreshold={0.3}
            ListFooterComponent={moreLoad
              ? <ActivityIndicator color="#3b5bdb" style={{ padding: 12 }} />
              : <Text style={s.footerTxt}>Showing {items.length} of {total} completed</Text>
            }
            ListEmptyComponent={
              <View style={s.center}>
                <Ionicons name="checkmark-done-circle-outline" size={48} color="#2f9e44" />
                <Text style={{ color: '#868e96', marginTop: 8 }}>No completed voters found.</Text>
              </View>
            }
            renderItem={({ item: v }) => (
              <View style={[s.voterRow, { borderLeftColor: '#2f9e44' }]}>
                <View style={{ flex: 1 }}>
                  <Text style={s.voterName}>{v.name}</Text>
                  <Text style={s.voterMeta}>
                    {v.voterEpic} � Booth {v.boothNumber}
                    {v.wardNumber ? ` � Ward ${v.wardNumber}` : ''}
                  </Text>
                  {v.mobileNumber
                    ? <Text style={s.voterPhone}>{v.mobileNumber}</Text>
                    : null}
                  <Text style={{ fontSize: 11, color: '#2f9e44', marginTop: 2 }}>
                    ? {new Date(v.completedAt).toLocaleDateString('en-IN')}
                  </Text>
                </View>
                <View style={{ alignItems: 'flex-end', gap: 6 }}>
                  {v.hasCoupon && v.couponCode
                    ? <View style={s.couponBadge}>
                        <Text style={s.couponTxt}>{v.couponCode}</Text>
                      </View>
                    : v.hasCoupon
                      ? <View style={[s.couponBadge, { backgroundColor: '#e9ecef' }]}>
                          <Text style={[s.couponTxt, { color: '#868e96' }]}>Issued</Text>
                        </View>
                      : null}
                  <TouchableOpacity style={s.editBtn}
                    onPress={() => nav.navigate('EditVoterSurvey', { voterId: v.id })}>
                    <Ionicons name="create-outline" size={14} color="#3b5bdb" />
                    <Text style={s.editBtnTxt}>Edit</Text>
                  </TouchableOpacity>
                  {v.mobileNumber && (
                    <TouchableOpacity style={s.waBtn}
                      onPress={() => {
                        const msg = encodeURIComponent(
                          `Dear ${v.name}, thank you for completing the voter survey! Your response has been recorded.`
                        );
                        Linking.openURL(`https://wa.me/91${v.mobileNumber}?text=${msg}`);
                      }}>
                      <Ionicons name="logo-whatsapp" size={16} color="#fff" />
                    </TouchableOpacity>
                  )}
                </View>
              </View>
            )}
          />
        )}
    </View>
  );
}

// ?? Pending voters tab ????????????????????????????????????????????
function PendingTab({ filterBooth, filterWard }: { filterBooth?: number; filterWard?: string }) {
  const [items,      setItems]      = useState<SurveyPendingVoter[]>([]);
  const [search,     setSearch]     = useState('');
  const [page,       setPage]       = useState(1);
  const [total,      setTotal]      = useState(0);
  const [loading,    setLoading]    = useState(true);
  const [moreLoad,   setMoreLoad]   = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const surveyUrl = `${API_BASE_URL}/Survey`;

  const load = useCallback(async (p = 1, q = '', reset = false) => {
    try {
      const res = await getPendingVoters({
        booth: filterBooth, ward: filterWard,
        search: q || undefined, page: p, pageSize: 30,
      });
      setTotal(res.total);
      setItems(prev => reset || p === 1 ? res.items : [...prev, ...res.items]);
      setPage(p);
    } finally {
      setLoading(false); setMoreLoad(false); setRefreshing(false);
    }
  }, [filterBooth, filterWard]);

  useEffect(() => { load(1, '', true); }, [load]);

  const onSearch = (t: string) => { setSearch(t); setLoading(true); load(1, t, true); };

  return (
    <View style={{ flex: 1 }}>
      <View style={s.searchRow}>
        <Ionicons name="search-outline" size={15} color="#868e96" />
        <TextInput style={s.searchInput} value={search} onChangeText={onSearch}
          placeholder="Search name / EPIC / mobile..." placeholderTextColor="#adb5bd" />
        {!!search && (
          <TouchableOpacity onPress={() => onSearch('')}>
            <Ionicons name="close-circle" size={15} color="#adb5bd" />
          </TouchableOpacity>
        )}
      </View>
      {loading
        ? <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>
        : (
          <FlatList
            data={items}
            keyExtractor={v => v.id.toString()}
            contentContainerStyle={{ padding: 12 }}
            refreshControl={<RefreshControl refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(1, search, true); }} />}
            onEndReached={() => {
              if (!moreLoad && page < Math.ceil(total / 30)) {
                setMoreLoad(true); load(page + 1, search);
              }
            }}
            onEndReachedThreshold={0.3}
            ListFooterComponent={moreLoad
              ? <ActivityIndicator color="#3b5bdb" style={{ padding: 12 }} />
              : <Text style={s.footerTxt}>Showing {items.length} of {total} pending</Text>
            }
            ListEmptyComponent={
              <View style={s.center}>
                <Ionicons name="checkmark-done-circle-outline" size={48} color="#2f9e44" />
                <Text style={{ color: '#2f9e44', fontWeight: '700', marginTop: 8 }}>
                  All voters have completed the survey! ??
                </Text>
              </View>
            }
            renderItem={({ item: v }) => {
              const waMsg = encodeURIComponent(
                `Dear ${v.name}, please fill in this quick voter survey and claim your reward: ${surveyUrl}`
              );
              return (
                <View style={[s.voterRow, { borderLeftColor: '#e03131' }]}>
                  <View style={{ flex: 1 }}>
                    <Text style={s.voterName}>{v.name}</Text>
                    <Text style={s.voterMeta}>
                      {v.voterEpic} � Booth {v.boothNumber}
                      {v.wardNumber ? ` � Ward ${v.wardNumber}` : ''}
                    </Text>
                    {v.mobileNumber
                      ? <Text style={s.voterPhone}>{v.mobileNumber}</Text>
                      : <Text style={{ fontSize: 11, color: '#adb5bd', marginTop: 2 }}>No mobile</Text>}
                  </View>
                  <View style={{ flexDirection: 'row', gap: 8, alignItems: 'center' }}>
                    {v.mobileNumber && (
                      <>
                        <TouchableOpacity style={s.waBtn}
                          onPress={() => Linking.openURL(
                            `https://wa.me/91${v.mobileNumber}?text=${waMsg}`
                          )}>
                          <Ionicons name="logo-whatsapp" size={16} color="#fff" />
                        </TouchableOpacity>
                        <TouchableOpacity style={s.callBtn}
                          onPress={() => Linking.openURL(`tel:${v.mobileNumber}`)}>
                          <Ionicons name="call-outline" size={16} color="#3b5bdb" />
                        </TouchableOpacity>
                      </>
                    )}
                    <TouchableOpacity style={s.linkBtn}
                      onPress={() => Linking.openURL(surveyUrl)}>
                      <Ionicons name="link-outline" size={16} color="#868e96" />
                    </TouchableOpacity>
                  </View>
                </View>
              );
            }}
          />
        )}
    </View>
  );
}

// ?? Main Screen ???????????????????????????????????????????????????
export default function VoterConsentScreen() {
  const [activeTab,    setActiveTab]    = useState<Tab>('stats');
  const [stats,        setStats]        = useState<VoterConsentStats | null>(null);
  const [statsLoading, setStatsLoading] = useState(true);
  const [refreshing,   setRefreshing]   = useState(false);
  const [filterBooth,  setFilterBooth]  = useState<number | undefined>();
  const [filterWard,   setFilterWard]   = useState<string | undefined>();
  const [showFilters,  setShowFilters]  = useState(false);

  const loadStats = useCallback(async () => {
    try { setStats(await getConsentStats(filterBooth, filterWard)); }
    finally { setStatsLoading(false); setRefreshing(false); }
  }, [filterBooth, filterWard]);

  useEffect(() => { loadStats(); }, [loadStats]);

  const TABS: { key: Tab; label: string; icon: string; badge?: number; badgeColor?: string }[] = [
    { key: 'stats',     label: 'Overview',  icon: 'stats-chart-outline' },
    { key: 'completed', label: 'Completed', icon: 'checkmark-circle-outline',
      badge: stats?.completedCount, badgeColor: '#2f9e44' },
    { key: 'pending',   label: 'Pending',   icon: 'hourglass-outline',
      badge: stats?.pendingCount,   badgeColor: '#e03131' },
  ];

  const hasFilter = !!filterBooth || !!filterWard;

  return (
    <View style={{ flex: 1, backgroundColor: '#f0f2f5' }}>

      {/* Header */}
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Voter Consent Analytics</Text>
          <Text style={s.sub}>Survey completions &amp; outreach</Text>
        </View>
        <TouchableOpacity style={s.filterIconBtn}
          onPress={() => setShowFilters(v => !v)}>
          <Ionicons name="funnel-outline" size={20} color="#fff" />
          {hasFilter && <View style={s.filterActiveDot} />}
        </TouchableOpacity>
      </View>

      {/* Filter pills */}
      {showFilters && stats && (
        <View style={s.filterBar}>
          <ScrollView horizontal showsHorizontalScrollIndicator={false}>
            <TouchableOpacity
              style={[s.filterChip, !hasFilter && s.filterChipActive]}
              onPress={() => { setFilterBooth(undefined); setFilterWard(undefined); }}>
              <Text style={[s.filterChipTxt, !hasFilter && { color: '#fff' }]}>All</Text>
            </TouchableOpacity>
            {stats.availableBooths.map(b => (
              <TouchableOpacity key={b}
                style={[s.filterChip, filterBooth === b && s.filterChipActive]}
                onPress={() => setFilterBooth(filterBooth === b ? undefined : b)}>
                <Text style={[s.filterChipTxt, filterBooth === b && { color: '#fff' }]}>
                  Booth {b}
                </Text>
              </TouchableOpacity>
            ))}
            {stats.availableWards.map(w => (
              <TouchableOpacity key={w}
                style={[s.filterChip, filterWard === w && s.filterChipActive]}
                onPress={() => setFilterWard(filterWard === w ? undefined : w)}>
                <Text style={[s.filterChipTxt, filterWard === w && { color: '#fff' }]}>
                  Ward {w}
                </Text>
              </TouchableOpacity>
            ))}
          </ScrollView>
        </View>
      )}

      {/* Tab bar */}
      <View style={s.tabBar}>
        {TABS.map(t => (
          <TouchableOpacity key={t.key}
            style={[s.tab, activeTab === t.key && s.tabActive]}
            onPress={() => setActiveTab(t.key)}>
            <Ionicons name={t.icon as any} size={15}
              color={activeTab === t.key ? '#3b5bdb' : '#868e96'} />
            <Text style={[s.tabTxt, activeTab === t.key && s.tabTxtActive]}>
              {t.label}
            </Text>
            {t.badge != null && t.badge > 0 && (
              <View style={[s.tabBadge, { backgroundColor: t.badgeColor }]}>
                <Text style={s.tabBadgeTxt}>{t.badge}</Text>
              </View>
            )}
          </TouchableOpacity>
        ))}
      </View>

      {/* Content */}
      {statsLoading && activeTab === 'stats'
        ? <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>
        : activeTab === 'stats' && stats
          ? <StatsTab stats={stats} />
          : activeTab === 'completed'
            ? <CompletedTab filterBooth={filterBooth} filterWard={filterWard} />
            : <PendingTab   filterBooth={filterBooth} filterWard={filterWard} />
      }
    </View>
  );
}

// ?? Styles ????????????????????????????????????????????????????????
const s = StyleSheet.create({
  header:          { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16,
                     paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:           { color: '#fff', fontSize: 20, fontWeight: '700' },
  sub:             { color: '#868e96', fontSize: 11, marginTop: 2 },
  filterIconBtn:   { padding: 6, position: 'relative' },
  filterActiveDot: { position: 'absolute', top: 4, right: 4, width: 8, height: 8,
                     borderRadius: 4, backgroundColor: '#f59f00' },
  filterBar:       { backgroundColor: '#fff', paddingVertical: 10, paddingHorizontal: 12,
                     borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  filterChip:      { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 20,
                     paddingHorizontal: 12, paddingVertical: 6, marginRight: 8 },
  filterChipActive:{ backgroundColor: '#3b5bdb', borderColor: '#3b5bdb' },
  filterChipTxt:   { fontSize: 12, fontWeight: '600', color: '#495057' },
  tabBar:          { flexDirection: 'row', backgroundColor: '#fff',
                     borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  tab:             { flex: 1, flexDirection: 'row', alignItems: 'center',
                     justifyContent: 'center', gap: 5, paddingVertical: 12 },
  tabActive:       { borderBottomWidth: 2, borderBottomColor: '#3b5bdb' },
  tabTxt:          { fontSize: 12, fontWeight: '600', color: '#868e96' },
  tabTxtActive:    { color: '#3b5bdb' },
  tabBadge:        { borderRadius: 10, minWidth: 18, height: 18,
                     justifyContent: 'center', alignItems: 'center', paddingHorizontal: 4 },
  tabBadgeTxt:     { color: '#fff', fontSize: 10, fontWeight: '800' },
  center:          { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 32 },
  card:            { backgroundColor: '#fff', borderRadius: 12, padding: 16,
                     marginBottom: 12, elevation: 1 },
  cardTitle:       { fontSize: 14, fontWeight: '700', color: '#212529', marginBottom: 12 },
  cardRow:         { flexDirection: 'row', gap: 8, marginBottom: 12 },
  miniCard:        { flex: 1, backgroundColor: '#fff', borderRadius: 10, padding: 12,
                     alignItems: 'center', borderTopWidth: 3, elevation: 1 },
  miniVal:         { fontSize: 20, fontWeight: '800' },
  miniLbl:         { fontSize: 10, color: '#868e96', marginTop: 2 },
  boothChip:       { backgroundColor: '#f1f3f5', borderRadius: 8,
                     paddingHorizontal: 10, paddingVertical: 6, alignItems: 'center' },
  boothChipNum:    { fontSize: 11, color: '#868e96' },
  boothChipVal:    { fontSize: 16, fontWeight: '800', color: '#3b5bdb' },
  shareBtn:        { flexDirection: 'row', alignItems: 'center', backgroundColor: '#3b5bdb',
                     borderRadius: 8, paddingHorizontal: 12, paddingVertical: 8,
                     alignSelf: 'flex-start', marginTop: 10 },
  searchRow:       { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff',
                     margin: 12, borderRadius: 10, paddingHorizontal: 12,
                     paddingVertical: 10, elevation: 1, gap: 8 },
  searchInput:     { flex: 1, fontSize: 13, color: '#212529' },
  voterRow:        { backgroundColor: '#fff', borderRadius: 10, padding: 14,
                     marginBottom: 8, flexDirection: 'row', alignItems: 'center',
                     borderLeftWidth: 4, elevation: 1, gap: 10 },
  voterName:       { fontSize: 14, fontWeight: '700', color: '#212529' },
  voterMeta:       { fontSize: 11, color: '#868e96', marginTop: 2 },
  voterPhone:      { fontSize: 11, color: '#4dabf7', marginTop: 2 },
  couponBadge:     { backgroundColor: '#d3f9d8', borderRadius: 6,
                     paddingHorizontal: 8, paddingVertical: 4 },
  couponTxt:       { fontSize: 11, fontWeight: '700', color: '#2f9e44' },
  editBtn:         { flexDirection: 'row', alignItems: 'center', gap: 4, borderWidth: 1,
                     borderColor: '#3b5bdb', borderRadius: 8, paddingHorizontal: 8, paddingVertical: 7 },
  editBtnTxt:      { fontSize: 11, fontWeight: '700', color: '#3b5bdb' },
  waBtn:           { backgroundColor: '#25d366', borderRadius: 8, padding: 8 },
  callBtn:         { borderWidth: 1, borderColor: '#3b5bdb', borderRadius: 8, padding: 8 },
  linkBtn:         { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 8, padding: 8 },
  footerTxt:       { textAlign: 'center', color: '#adb5bd', fontSize: 12, padding: 12 },
});
