import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  ActivityIndicator, RefreshControl, Modal, TextInput, Alert,
  ScrollView, Linking, KeyboardAvoidingView, Platform,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { getVehicles, createVehicle, getTransportRequests, updateTransportStatus, TransportVehicleItem, TransportRequestItem, VEHICLE_TYPES, TRANSPORT_STATUSES } from '../api/transport';

const BRAND = '#f59f00';

export default function TransportScreen() {
  const [tab,       setTab]       = useState<'requests' | 'vehicles'>('requests');
  const [requests,  setRequests]  = useState<TransportRequestItem[]>([]);
  const [vehicles,  setVehicles]  = useState<TransportVehicleItem[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [refreshing,setRefreshing]= useState(false);
  const [showVehicleModal, setShowVehicleModal] = useState(false);
  // vehicle form
  const [driverName,  setDriverName]  = useState('');
  const [driverPhone, setDriverPhone] = useState('');
  const [vehNum,      setVehNum]      = useState('');
  const [vehType,     setVehType]     = useState('Car');
  const [capacity,    setCapacity]    = useState('');
  const [booth,       setBooth]       = useState('');
  const [notes,       setNotes]       = useState('');
  const [saving,      setSaving]      = useState(false);

  const load = useCallback(async () => {
    try {
      const [r, v] = await Promise.all([getTransportRequests(), getVehicles()]);
      setRequests(r); setVehicles(v);
    } finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAddVehicle = async () => {
    if (!driverName.trim() || !driverPhone.trim() || !capacity || !booth) {
      Alert.alert('Required', 'Driver name, phone, capacity and booth are required.'); return;
    }
    setSaving(true);
    try {
      await createVehicle({ driverName: driverName.trim(), driverPhone: driverPhone.trim(), vehicleNumber: vehNum || undefined, vehicleType: vehType, capacity: parseInt(capacity, 10), boothNumber: parseInt(booth, 10), notes: notes || undefined });
      setShowVehicleModal(false);
      setDriverName(''); setDriverPhone(''); setVehNum(''); setVehType('Car'); setCapacity(''); setBooth(''); setNotes('');
      load();
    } catch { Alert.alert('Error', 'Failed to add vehicle.');
    } finally { setSaving(false); }
  };

  const handleStatusChange = (req: TransportRequestItem, status: string) => {
    Alert.alert('Update Status', `Mark as "${status}"?`, [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Update', onPress: async () => {
          try { await updateTransportStatus(req.id, status); load(); } catch { Alert.alert('Error', 'Failed to update.'); }
        }},
    ]);
  };

  const pending = requests.filter(r => r.status === 'Pending').length;

  if (loading) return <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>;

  return (
    <View style={s.container}>
      <View style={s.header}>
        <View style={{ flex: 1 }}>
          <Text style={s.title}>Voter Transport</Text>
          <Text style={s.sub}>{requests.length} requests � {pending} pending � {vehicles.length} vehicles</Text>
        </View>
        {tab === 'vehicles' && (
          <TouchableOpacity style={s.addBtn} onPress={() => setShowVehicleModal(true)}>
            <Ionicons name="add" size={22} color="#fff" />
          </TouchableOpacity>
        )}
      </View>

      <View style={s.tabBar}>
        {(['requests', 'vehicles'] as const).map(t => (
          <TouchableOpacity key={t} style={[s.tab, tab === t && s.tabActive]} onPress={() => setTab(t)}>
            <Text style={[s.tabTxt, tab === t && s.tabTxtActive]}>
              {t === 'requests' ? `Requests (${requests.length})` : `Vehicles (${vehicles.length})`}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {tab === 'requests' ? (
        <FlatList
          data={requests}
          keyExtractor={r => r.id.toString()}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
          ListEmptyComponent={<View style={s.empty}><Ionicons name="car-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No transport requests.</Text></View>}
          renderItem={({ item: req }) => {
            const st = TRANSPORT_STATUSES.find(x => x.key === req.status);
            const color = st?.color ?? '#868e96';
            const nextStatus = TRANSPORT_STATUSES.find(x => TRANSPORT_STATUSES.indexOf(x) === TRANSPORT_STATUSES.findIndex(y => y.key === req.status) + 1);
            return (
              <View style={s.card}>
                <View style={[s.statusDot, { backgroundColor: color }]} />
                <View style={{ flex: 1, marginLeft: 12 }}>
                  <Text style={s.name}>{req.voterName}</Text>
                  {req.voterPhone && (
                    <TouchableOpacity onPress={() => Linking.openURL(`tel:${req.voterPhone}`)}>
                      <Text style={s.phone}>{req.voterPhone}</Text>
                    </TouchableOpacity>
                  )}
                  {req.pickupAddress && <Text style={s.addr}><Ionicons name="location-outline" size={11} /> {req.pickupAddress}</Text>}
                  <View style={s.metaRow}>
                    <View style={[s.badge, { backgroundColor: color + '20' }]}>
                      <Text style={[s.badgeTxt, { color }]}>{st?.label ?? req.status}</Text>
                    </View>
                    {req.driverName && <Text style={s.meta}>{req.driverName} {req.vehicleNumber ? `� ${req.vehicleNumber}` : ''}</Text>}
                  </View>
                </View>
                {nextStatus && req.status !== 'Voted' && req.status !== 'Cancelled' && (
                  <TouchableOpacity style={[s.nextBtn, { backgroundColor: nextStatus.color + '20' }]}
                    onPress={() => handleStatusChange(req, nextStatus.key)}>
                    <Text style={[s.nextTxt, { color: nextStatus.color }]}>{nextStatus.label}</Text>
                  </TouchableOpacity>
                )}
              </View>
            );
          }}
        />
      ) : (
        <FlatList
          data={vehicles}
          keyExtractor={v => v.id.toString()}
          contentContainerStyle={{ padding: 12, paddingBottom: 40 }}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); load(); }} />}
          ListEmptyComponent={<View style={s.empty}><Ionicons name="car-outline" size={48} color="#dee2e6" /><Text style={s.emptyTxt}>No vehicles registered.</Text></View>}
          renderItem={({ item: v }) => (
            <View style={s.card}>
              <View style={[s.statusDot, { backgroundColor: v.isAvailable ? '#2f9e44' : '#868e96' }]} />
              <View style={{ flex: 1, marginLeft: 12 }}>
                <Text style={s.name}>{v.driverName}</Text>
                <TouchableOpacity onPress={() => Linking.openURL(`tel:${v.driverPhone}`)}>
                  <Text style={s.phone}>{v.driverPhone}</Text>
                </TouchableOpacity>
                <View style={s.metaRow}>
                  <Text style={s.meta}>{v.vehicleType ?? 'Vehicle'} � {v.vehicleNumber ?? 'No plate'} � {v.capacity} seats</Text>
                </View>
                <Text style={s.meta}>Booth {v.boothNumber} � {v.isAvailable ? 'Available' : 'Busy'}</Text>
              </View>
            </View>
          )}
        />
      )}

      {/* Add Vehicle Modal */}
      <Modal visible={showVehicleModal} animationType="slide" presentationStyle="pageSheet" onRequestClose={() => setShowVehicleModal(false)}>
        <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
          <View style={fm.container}>
            <View style={fm.header}>
              <Text style={fm.title}>Add Vehicle</Text>
              <TouchableOpacity onPress={() => setShowVehicleModal(false)}><Ionicons name="close" size={24} color="#212529" /></TouchableOpacity>
            </View>
            <ScrollView contentContainerStyle={{ padding: 16 }}>
              {[['Driver Name *', driverName, setDriverName, 'default'], ['Driver Phone *', driverPhone, setDriverPhone, 'phone-pad'],
                ['Vehicle Number', vehNum, setVehNum, 'default'], ['Capacity *', capacity, setCapacity, 'numeric'], ['Booth Number *', booth, setBooth, 'numeric']].map(([label, val, setter, kb]: any) => (
                <View key={label}>
                  <Text style={fm.label}>{label}</Text>
                  <TextInput style={fm.input} value={val} onChangeText={setter} keyboardType={kb} placeholder={label.replace(' *', '')} />
                </View>
              ))}
              <Text style={fm.label}>Vehicle Type</Text>
              <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 16 }}>
                {VEHICLE_TYPES.map(t => (
                  <TouchableOpacity key={t} style={[fm.chip, vehType === t && fm.chipActive]} onPress={() => setVehType(t)}>
                    <Text style={[fm.chipTxt, vehType === t && { color: '#fff' }]}>{t}</Text>
                  </TouchableOpacity>
                ))}
              </View>
              <Text style={fm.label}>Notes</Text>
              <TextInput style={fm.input} value={notes} onChangeText={setNotes} placeholder="Optional" />
              <TouchableOpacity style={[fm.saveBtn, saving && { opacity: 0.6 }]} onPress={handleAddVehicle} disabled={saving}>
                {saving ? <ActivityIndicator color="#fff" /> : <Text style={fm.saveTxt}>Add Vehicle</Text>}
              </TouchableOpacity>
            </ScrollView>
          </View>
        </KeyboardAvoidingView>
      </Modal>
    </View>
  );
}

const s = StyleSheet.create({
  container:   { flex: 1, backgroundColor: '#f0f2f5' },
  center:      { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header:      { backgroundColor: '#1a1f2e', paddingTop: 52, paddingBottom: 16, paddingHorizontal: 16, flexDirection: 'row', alignItems: 'flex-end' },
  title:       { color: '#fff', fontSize: 22, fontWeight: '700' },
  sub:         { color: '#868e96', fontSize: 12, marginTop: 2 },
  addBtn:      { backgroundColor: BRAND, borderRadius: 10, padding: 8 },
  tabBar:      { flexDirection: 'row', margin: 12, backgroundColor: '#fff', borderRadius: 10, padding: 4, elevation: 1 },
  tab:         { flex: 1, paddingVertical: 8, borderRadius: 8, alignItems: 'center' },
  tabActive:   { backgroundColor: BRAND },
  tabTxt:      { fontSize: 13, fontWeight: '600', color: '#868e96' },
  tabTxtActive:{ color: '#fff' },
  card:        { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'flex-start', elevation: 1 },
  statusDot:   { width: 4, borderRadius: 2, alignSelf: 'stretch' },
  name:        { fontSize: 14, fontWeight: '700', color: '#212529', marginBottom: 2 },
  phone:       { fontSize: 12, color: '#4dabf7', marginBottom: 4 },
  addr:        { fontSize: 12, color: '#495057', marginBottom: 4 },
  metaRow:     { flexDirection: 'row', alignItems: 'center', gap: 8 },
  badge:       { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  badgeTxt:    { fontSize: 11, fontWeight: '700' },
  meta:        { fontSize: 11, color: '#868e96' },
  nextBtn:     { borderRadius: 8, paddingHorizontal: 10, paddingVertical: 6, marginLeft: 8 },
  nextTxt:     { fontSize: 12, fontWeight: '700' },
  empty:       { alignItems: 'center', paddingVertical: 60 },
  emptyTxt:    { color: '#adb5bd', marginTop: 12, fontSize: 14 },
});
const fm = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff' },
  header:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#f1f3f5' },
  title:     { fontSize: 18, fontWeight: '700', color: '#212529' },
  label:     { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 6 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 10, paddingHorizontal: 14, paddingVertical: 10, fontSize: 14, color: '#212529', marginBottom: 16 },
  chip:      { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20, borderWidth: 1, borderColor: '#dee2e6' },
  chipActive:{ backgroundColor: BRAND, borderColor: BRAND },
  chipTxt:   { fontSize: 12, fontWeight: '600', color: '#495057' },
  saveBtn:   { backgroundColor: BRAND, borderRadius: 12, alignItems: 'center', paddingVertical: 14, marginBottom: 8 },
  saveTxt:   { color: '#fff', fontSize: 15, fontWeight: '700' },
});
