import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ActivityIndicator, FlatList,
  TouchableOpacity, Alert, RefreshControl,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getVolunteerLocations, updateVolunteerLocation, getCurrentPosition } from '../api/gpsLocation';
import { useAuth } from '../context/AuthContext';

interface VolunteerLoc {
  userId: string;
  userName: string;
  latitude: number;
  longitude: number;
  accuracyMeters?: number;
  updatedAt: string;
}

export default function VolunteerMapScreen() {
  const { user }  = useAuth();
  const [locs,    setLocs]    = useState<VolunteerLoc[]>([]);
  const [loading, setLoading] = useState(true);
  const [sharing, setSharing] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      const data = await getVolunteerLocations();
      setLocs(data);
    } catch { /* offline */ }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const shareMyLocation = async () => {
    setSharing(true);
    try {
      const pos = await getCurrentPosition();
      if (!pos) {
        Alert.alert('Location unavailable', 'Enable location permission in Settings.');
        return;
      }
      await updateVolunteerLocation(pos);
      Alert.alert('? Location shared', `Lat: ${pos.latitude.toFixed(5)}, Lon: ${pos.longitude.toFixed(5)}`);
      load();
    } catch {
      Alert.alert('Error', 'Could not share location. Check your connection.');
    } finally {
      setSharing(false);
    }
  };

  const formatAge = (iso: string) => {
    const diff = (Date.now() - new Date(iso).getTime()) / 60000;
    if (diff < 1) return 'just now';
    if (diff < 60) return `${Math.floor(diff)}m ago`;
    return `${Math.floor(diff / 60)}h ago`;
  };

  const openMaps = (loc: VolunteerLoc) => {
    const url = `https://maps.google.com/?q=${loc.latitude},${loc.longitude}`;
    import('react-native').then(({ Linking }) => Linking.openURL(url));
  };

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View>
          <Text style={s.title}>Volunteer Map</Text>
          <Text style={s.sub}>{locs.length} active field workers (last 8h)</Text>
        </View>
        <TouchableOpacity
          style={[s.shareBtn, sharing && s.shareBtnDisabled]}
          onPress={shareMyLocation}
          disabled={sharing}>
          {sharing
            ? <ActivityIndicator color="#fff" size="small" />
            : <Ionicons name="location" size={18} color="#fff" />
          }
          <Text style={s.shareBtnTxt}>{sharing ? 'Sharing…' : 'Share My Location'}</Text>
        </TouchableOpacity>
      </View>

      {/* Info banner */}
      <View style={s.infoBanner}>
        <Ionicons name="information-circle-outline" size={16} color="#1971c2" />
        <Text style={s.infoTxt}>
          Tap "Share My Location" to update your position on the team map.
          Locations auto-expire after 8 hours.
        </Text>
      </View>

      {loading ? (
        <View style={s.center}><ActivityIndicator color="#3b5bdb" size="large" /></View>
      ) : (
        <FlatList
          data={locs}
          keyExtractor={l => l.userId}
          refreshControl={
            <RefreshControl refreshing={refreshing}
              onRefresh={() => { setRefreshing(true); load(); }} />
          }
          ListEmptyComponent={
            <View style={s.center}>
              <Ionicons name="map-outline" size={48} color="#dee2e6" />
              <Text style={s.emptyTxt}>No active field workers in the last 8 hours.</Text>
              <Text style={[s.emptyTxt, { color: '#adb5bd', fontSize: 12 }]}>
                Workers appear here when they share their location.
              </Text>
            </View>
          }
          renderItem={({ item: loc }) => {
            const isMe = loc.userId === user?.userId;
            return (
              <TouchableOpacity style={[s.card, isMe && s.myCard]} onPress={() => openMaps(loc)}>
                <View style={[s.avatar, { backgroundColor: isMe ? '#3b5bdb' : '#868e96' }]}>
                  <Ionicons name="person" size={18} color="#fff" />
                </View>
                <View style={{ flex: 1, marginLeft: 12 }}>
                  <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
                    <Text style={s.name}>{loc.userName}</Text>
                    {isMe && <View style={s.mePill}><Text style={s.mePillTxt}>You</Text></View>}
                  </View>
                  <Text style={s.coords}>
                    {loc.latitude.toFixed(5)}, {loc.longitude.toFixed(5)}
                    {loc.accuracyMeters ? ` ±${Math.round(loc.accuracyMeters)}m` : ''}
                  </Text>
                  <Text style={s.age}>{formatAge(loc.updatedAt)}</Text>
                </View>
                <Ionicons name="open-outline" size={18} color="#adb5bd" />
              </TouchableOpacity>
            );
          }}
        />
      )}
    </View>
  );
}

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16,
                  paddingHorizontal: 16, flexDirection: 'row',
                  justifyContent: 'space-between', alignItems: 'flex-end' },
  title:        { color: '#fff', fontSize: 20, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  shareBtn:     { flexDirection: 'row', alignItems: 'center', gap: 6,
                  backgroundColor: '#3b5bdb', borderRadius: 10,
                  paddingHorizontal: 14, paddingVertical: 9 },
  shareBtnDisabled: { opacity: 0.6 },
  shareBtnTxt:  { color: '#fff', fontWeight: '700', fontSize: 13 },
  infoBanner:   { flexDirection: 'row', alignItems: 'flex-start', gap: 8,
                  backgroundColor: '#e7f5ff', margin: 12, borderRadius: 10,
                  padding: 12 },
  infoTxt:      { flex: 1, fontSize: 12, color: '#1971c2', lineHeight: 17 },
  card:         { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff',
                  marginHorizontal: 12, marginBottom: 8, borderRadius: 12,
                  padding: 14, elevation: 1 },
  myCard:       { borderWidth: 1.5, borderColor: '#3b5bdb' },
  avatar:       { width: 40, height: 40, borderRadius: 20,
                  justifyContent: 'center', alignItems: 'center' },
  name:         { fontSize: 15, fontWeight: '700', color: '#212529' },
  coords:       { fontSize: 11, color: '#868e96', fontFamily: 'monospace', marginTop: 2 },
  age:          { fontSize: 11, color: '#adb5bd', marginTop: 2 },
  mePill:       { backgroundColor: '#d0ebff', borderRadius: 6,
                  paddingHorizontal: 7, paddingVertical: 2 },
  mePillTxt:    { fontSize: 10, fontWeight: '700', color: '#1971c2' },
  emptyTxt:     { color: '#868e96', fontSize: 14, textAlign: 'center',
                  marginTop: 12, lineHeight: 20 },
});
