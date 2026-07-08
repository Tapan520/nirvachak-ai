import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Alert,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getAdminUsers, toggleUser, AdminUserItem } from '../api/admin';
import { useAuth } from '../context/AuthContext';

const BRAND = '#3b5bdb';
const ROLE_COLOR: Record<string, string> = {
  SuperAdmin: '#212529', Admin: '#e03131', CampaignManager: '#3b5bdb',
  Candidate: '#f59f00', BoothAgent: '#0c8599', FieldWorker: '#868e96',
};

export default function AdminScreen() {
  const { user } = useAuth();
  const [users,      setUsers]      = useState<AdminUserItem[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try { setUsers(await getAdminUsers()); }
    catch { Alert.alert('Error', 'Could not load users.'); }
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleToggle = (u: AdminUserItem) => {
    if (u.role === 'SuperAdmin') return;
    Alert.alert(
      u.isActive ? 'Disable User' : 'Enable User',
      `${u.isActive ? 'Disable' : 'Enable'} ${u.fullName}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Confirm', onPress: async () => {
            try { await toggleUser(u.id); load(); } catch { Alert.alert('Error', 'Failed to update user.'); }
          }},
      ]
    );
  };

  const active   = users.filter(u => u.isActive).length;
  const inactive = users.filter(u => !u.isActive).length;

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View>
          <Text style={s.title}>User Management</Text>
          <Text style={s.sub}>{users.length} users · {active} active · {inactive} inactive</Text>
        </View>
      </View>

      <FlatList
        data={users}
        keyExtractor={u => u.id}
        contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
        ListEmptyComponent={
          <View style={s.empty}>
            <Ionicons name="people-outline" size={48} color="#dee2e6" />
            <Text style={s.emptyTxt}>No users found.</Text>
          </View>
        }
        renderItem={({ item: u }) => {
          const roleColor = ROLE_COLOR[u.role] ?? '#868e96';
          const isSelf = u.id === user?.userId;
          return (
            <View style={[s.card, !u.isActive && s.inactiveCard]}>
              <View style={[s.avatar, { backgroundColor: roleColor + '20' }]}>
                <Text style={[s.avatarTxt, { color: roleColor }]}>{u.fullName[0]}</Text>
              </View>
              <View style={{ flex: 1, marginLeft: 12 }}>
                <View style={s.nameRow}>
                  <Text style={s.name}>{u.fullName} {isSelf ? '(You)' : ''}</Text>
                  <View style={[s.roleBadge, { backgroundColor: roleColor + '20' }]}>
                    <Text style={[s.roleTxt, { color: roleColor }]}>{u.role}</Text>
                  </View>
                </View>
                <Text style={s.email}>{u.email}</Text>
                <View style={s.metaRow}>
                  {u.constituencyName && <Text style={s.meta}>{u.constituencyName}</Text>}
                  {u.assignedWard && <Text style={s.meta}>Ward: {u.assignedWard}</Text>}
                </View>
              </View>
              {!isSelf && u.role !== 'SuperAdmin' && (
                <TouchableOpacity
                  style={[s.toggleBtn, { backgroundColor: u.isActive ? '#fff3f3' : '#d3f9d8' }]}
                  onPress={() => handleToggle(u)}>
                  <Ionicons
                    name={u.isActive ? 'pause-circle-outline' : 'play-circle-outline'}
                    size={22}
                    color={u.isActive ? '#e03131' : '#2f9e44'}
                  />
                </TouchableOpacity>
              )}
            </View>
          );
        }}
      />
    </View>
  );
}

const s = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#f0f2f5' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:       { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16 },
  title:        { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:          { color: '#868e96', fontSize: 12, marginTop: 2 },
  card:         { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', elevation: 1 },
  inactiveCard: { opacity: 0.6 },
  avatar:       { width: 44, height: 44, borderRadius: 10, justifyContent: 'center', alignItems: 'center' },
  avatarTxt:    { fontSize: 20, fontWeight: '800' },
  nameRow:      { flexDirection: 'row', alignItems: 'center', gap: 8, flexWrap: 'wrap', marginBottom: 2 },
  name:         { fontSize: 14, fontWeight: '700', color: '#212529' },
  roleBadge:    { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  roleTxt:      { fontSize: 11, fontWeight: '700' },
  email:        { fontSize: 12, color: '#868e96', marginBottom: 4 },
  metaRow:      { flexDirection: 'row', gap: 10 },
  meta:         { fontSize: 11, color: '#adb5bd' },
  toggleBtn:    { borderRadius: 8, padding: 8 },
  empty:        { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:     { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
