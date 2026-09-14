import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, Alert, TouchableOpacity,
  ActivityIndicator, TextInput, Linking, Share,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaView } from 'react-native-safe-area-context';
import { get2FA, enable2FA, disable2FA, TwoFactorStatus } from '../api/account';

const BRAND = '#3b5bdb';

export default function TwoFactorScreen() {
  const [status, setStatus] = useState<TwoFactorStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try { setStatus(await get2FA()); }
    catch { /* ignore */ }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { setLoading(true); load(); }, [load]);

  const openAuthenticator = async () => {
    if (!status?.authenticatorUri) return;
    try {
      const supported = await Linking.canOpenURL(status.authenticatorUri);
      if (supported) await Linking.openURL(status.authenticatorUri);
      else Alert.alert('No authenticator app', 'Install Google Authenticator, Microsoft Authenticator, or Authy first, then tap "Copy secret key".');
    } catch { Alert.alert('Error', 'Could not open authenticator app'); }
  };

  const shareKey = () => {
    if (!status?.sharedKey) return;
    Share.share({ message: status.sharedKey.replace(/\s/g, '').toUpperCase() });
  };

  const doEnable = async () => {
    const stripped = code.replace(/\s|-/g, '');
    if (stripped.length !== 6) { Alert.alert('Invalid', 'Enter the 6-digit code from your authenticator app.'); return; }
    setBusy(true);
    try {
      await enable2FA(stripped);
      Alert.alert('Enabled', 'Two-factor authentication is now active.');
      setCode('');
      await load();
    } catch (e: any) {
      const err = e?.response?.data?.error ?? 'Verification failed';
      Alert.alert('Error', err);
    } finally { setBusy(false); }
  };

  const doDisable = () => {
    Alert.alert(
      'Disable 2FA?',
      'You will only need your password to log in. Continue?',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Disable', style: 'destructive',
          onPress: async () => {
            setBusy(true);
            try { await disable2FA(); Alert.alert('Disabled', '2FA is now off.'); await load(); }
            catch { Alert.alert('Error', 'Could not disable 2FA'); }
            finally { setBusy(false); }
          }
        }
      ]);
  };

  if (loading || !status) {
    return <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.center}><ActivityIndicator color={BRAND} size="large" /></View>
    </SafeAreaView>;
  }

  return (
    <SafeAreaView style={s.container} edges={['top']}>
      <View style={s.header}>
        <Text style={s.hTitle}>Two-Factor Authentication</Text>
        <Text style={s.hSub}>Add an extra layer of security to your account</Text>
      </View>

      <ScrollView contentContainerStyle={{ padding: 12, paddingBottom: 40 }}>
        <View style={[s.card, { flexDirection: 'row', alignItems: 'center' }]}>
          <Ionicons
            name={status.isEnabled ? 'shield-checkmark' : 'shield-outline'}
            size={36}
            color={status.isEnabled ? '#2f9e44' : '#adb5bd'}
          />
          <View style={{ flex: 1, marginLeft: 12 }}>
            <Text style={s.statusTitle}>{status.isEnabled ? '2FA is ON' : '2FA is OFF'}</Text>
            <Text style={s.statusSub}>
              {status.isEnabled
                ? 'You will be asked for a code from your authenticator app after each login.'
                : 'Enable 2FA to protect your account against password theft.'}
            </Text>
          </View>
        </View>

        {status.isEnabled ? (
          <TouchableOpacity style={[s.dangerBtn, busy && { opacity: 0.6 }]} onPress={doDisable} disabled={busy}>
            <Ionicons name="lock-open-outline" size={16} color="#fff" />
            <Text style={s.dangerTxt}>Disable 2FA</Text>
          </TouchableOpacity>
        ) : (
          <>
            <View style={s.card}>
              <Text style={s.cardTitle}>1. Add secret to your authenticator app</Text>
              <Text style={s.stepTxt}>Scan the QR (via app) or paste the key below.</Text>

              <View style={s.keyBox}>
                <Text selectable style={s.keyTxt}>{status.sharedKey}</Text>
              </View>

              <View style={{ flexDirection: 'row', gap: 8, marginTop: 10 }}>
                <TouchableOpacity style={s.secBtn} onPress={openAuthenticator}>
                  <Ionicons name="open-outline" size={14} color={BRAND} />
                  <Text style={s.secTxt}>Open authenticator</Text>
                </TouchableOpacity>
                <TouchableOpacity style={s.secBtn} onPress={shareKey}>
                  <Ionicons name="copy-outline" size={14} color={BRAND} />
                  <Text style={s.secTxt}>Copy / share key</Text>
                </TouchableOpacity>
              </View>
            </View>

            <View style={s.card}>
              <Text style={s.cardTitle}>2. Verify code from your app</Text>
              <TextInput
                style={s.input}
                value={code}
                onChangeText={setCode}
                placeholder="6-digit code"
                placeholderTextColor="#adb5bd"
                keyboardType="number-pad"
                maxLength={7}
              />
              <TouchableOpacity style={[s.saveBtn, busy && { opacity: 0.6 }]} onPress={doEnable} disabled={busy}>
                {busy ? <ActivityIndicator color="#fff" /> :
                  <><Ionicons name="checkmark-circle" size={16} color="#fff" /><Text style={s.saveTxt}>Verify & enable</Text></>}
              </TouchableOpacity>
            </View>
          </>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f0f2f5' },
  header:    { backgroundColor: '#1a1f2e', paddingHorizontal: 16, paddingBottom: 12, paddingTop: 8 },
  hTitle:    { color: '#fff', fontSize: 20, fontWeight: '800' },
  hSub:      { color: '#adb5bd', fontSize: 11, marginTop: 2 },
  center:    { flex: 1, justifyContent: 'center', alignItems: 'center', padding: 40 },
  card:      { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 10, elevation: 1 },
  cardTitle: { fontSize: 12, fontWeight: '800', color: '#495057', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 8 },
  statusTitle:{ fontSize: 16, fontWeight: '800', color: '#212529' },
  statusSub: { fontSize: 12, color: '#495057', marginTop: 2 },
  stepTxt:   { fontSize: 12, color: '#495057' },
  keyBox:    { backgroundColor: '#f8f9fa', borderRadius: 8, padding: 12, marginTop: 8, borderWidth: 1, borderColor: '#dee2e6' },
  keyTxt:    { fontSize: 14, fontFamily: 'monospace', letterSpacing: 1, color: '#212529' },
  secBtn:    { flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 4,
               borderWidth: 1, borderColor: BRAND, paddingVertical: 8, borderRadius: 8 },
  secTxt:    { color: BRAND, fontWeight: '700', fontSize: 12 },
  input:     { borderWidth: 1, borderColor: '#dee2e6', backgroundColor: '#fff', borderRadius: 8, padding: 12,
               color: '#212529', fontSize: 18, letterSpacing: 4, textAlign: 'center' },
  saveBtn:   { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 6,
               backgroundColor: BRAND, paddingVertical: 12, borderRadius: 10, marginTop: 12 },
  saveTxt:   { color: '#fff', fontWeight: '800' },
  dangerBtn: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 6,
               backgroundColor: '#e03131', paddingVertical: 12, borderRadius: 10, marginTop: 4 },
  dangerTxt: { color: '#fff', fontWeight: '800' },
});
