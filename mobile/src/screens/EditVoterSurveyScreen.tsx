import React, { useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity,
  ActivityIndicator, Alert, Switch,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation, useRoute } from '@react-navigation/native';
import {
  getVoterSurveyProfile, updateVoterSurveyProfile,
  VoterSurveyProfile,
} from '../api/voterConsent';

// ?? Static option lists (mirror the Razor Page) ???????????????????
const AGE_BRACKETS     = ['18–25', '26–35', '36–50', '51–65', '65+'];
const CASTE_CATEGORIES = ['General', 'OBC', 'SC', 'ST', 'NT'];
const RELIGIONS        = ['Hindu', 'Muslim', 'Christian', 'Sikh', 'Buddhist', 'Jain', 'Other'];
const EDUCATIONS       = ['Below 10th', '10th', '12th', 'Graduate', 'PG+'];
const OCCUPATIONS      = ['Farmer', 'Service', 'Business', 'Student', 'Homemaker', 'Other'];
const INCOME_BRACKETS  = ['<10K', '10-25K', '25-50K', '50K+'];
const LANGUAGES        = ['Marathi', 'Hindi', 'English', 'Urdu', 'Other'];
const ISSUE_LIST       = [
  'Roads & Infrastructure', 'Water Supply', 'Employment', 'Education',
  'Healthcare', 'Electricity', 'Agriculture / MSP', 'Women Safety',
  'GST / Business', 'Housing / Ration', 'Law & Order', 'Youth Development',
];

// ?? Reusable pill-select ??????????????????????????????????????????
function PillSelect({ label, options, value, onChange }: {
  label: string; options: string[];
  value: string | undefined; onChange: (v: string) => void;
}) {
  return (
    <View style={{ marginBottom: 18 }}>
      <Text style={f.label}>{label}</Text>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        {options.map(o => (
          <TouchableOpacity key={o}
            style={[f.pill, value === o && f.pillActive]}
            onPress={() => onChange(o)}>
            <Text style={[f.pillTxt, value === o && f.pillTxtActive]}>{o}</Text>
          </TouchableOpacity>
        ))}
      </View>
    </View>
  );
}

// ?? Concern chip selector (max 3) ?????????????????????????????????
function ConcernChips({ selected, onChange }: {
  selected: string[]; onChange: (v: string[]) => void;
}) {
  const toggle = (issue: string) => {
    if (selected.includes(issue)) {
      onChange(selected.filter(i => i !== issue));
    } else {
      if (selected.length >= 3) {
        Alert.alert('Limit reached', 'You can select up to 3 concerns.');
        return;
      }
      onChange([...selected, issue]);
    }
  };
  return (
    <View style={{ marginBottom: 18 }}>
      <Text style={f.label}>Top Concerns <Text style={{ color: '#868e96', fontWeight: '400' }}>(up to 3)</Text></Text>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
        {ISSUE_LIST.map(issue => {
          const active = selected.includes(issue);
          return (
            <TouchableOpacity key={issue}
              style={[f.pill, active && f.pillActive]}
              onPress={() => toggle(issue)}>
              <Text style={[f.pillTxt, active && f.pillTxtActive]}>{issue}</Text>
            </TouchableOpacity>
          );
        })}
      </View>
    </View>
  );
}

// ?? Consent toggle row ????????????????????????????????????????????
function ConsentRow({ label, desc, value, onChange, mandatory = false }: {
  label: string; desc: string; value: boolean;
  onChange: (v: boolean) => void; mandatory?: boolean;
}) {
  return (
    <View style={f.consentRow}>
      <View style={{ flex: 1, marginRight: 12 }}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
          <Text style={f.consentLabel}>{label}</Text>
          {mandatory && (
            <View style={f.mandatoryBadge}>
              <Text style={f.mandatoryTxt}>Mandatory</Text>
            </View>
          )}
        </View>
        <Text style={f.consentDesc}>{desc}</Text>
      </View>
      <Switch
        value={value}
        onValueChange={onChange}
        trackColor={{ false: '#dee2e6', true: '#3b5bdb' }}
        thumbColor={value ? '#fff' : '#f1f3f5'}
      />
    </View>
  );
}

// ?? Section heading ???????????????????????????????????????????????
function Section({ icon, title }: { icon: string; title: string }) {
  return (
    <View style={f.sectionHead}>
      <Ionicons name={icon as any} size={16} color="#3b5bdb" />
      <Text style={f.sectionTitle}>{title}</Text>
    </View>
  );
}

// ?? Main Screen ???????????????????????????????????????????????????
export default function EditVoterSurveyScreen() {
  const nav   = useNavigation<any>();
  const route = useRoute<any>();
  const { voterId } = route.params as { voterId: number };

  const [profile,  setProfile]  = useState<VoterSurveyProfile | null>(null);
  const [loading,  setLoading]  = useState(true);
  const [saving,   setSaving]   = useState(false);

  // Form state
  const [ageBracket,          setAgeBracket]          = useState<string | undefined>();
  const [casteCategory,       setCasteCategory]       = useState<string | undefined>();
  const [religion,            setReligion]            = useState<string | undefined>();
  const [education,           setEducation]           = useState<string | undefined>();
  const [occupation,          setOccupation]          = useState<string | undefined>();
  const [incomeBracket,       setIncomeBracket]       = useState<string | undefined>();
  const [preferredLanguage,   setPreferredLanguage]   = useState<string | undefined>();
  const [primaryConcerns,     setPrimaryConcerns]     = useState<string[]>([]);
  const [consentThirdParty,   setConsentThirdParty]   = useState(false);
  const [consentCampaign,     setConsentCampaign]     = useState(false);
  const [consentWhatsApp,     setConsentWhatsApp]     = useState(false);
  const [consentScheme,       setConsentScheme]       = useState(false);
  const [consentAnalytics,    setConsentAnalytics]    = useState(false);

  useEffect(() => {
    getVoterSurveyProfile(voterId)
      .then(p => {
        setProfile(p);
        // Pre-fill all fields from existing profile
        setAgeBracket(p.ageBracket);
        setCasteCategory(p.casteCategory);
        setReligion(p.religion);
        setEducation(p.education);
        setOccupation(p.occupation);
        setIncomeBracket(p.monthlyIncomeBracket);
        setPreferredLanguage(p.preferredLanguage);
        setPrimaryConcerns(p.primaryConcerns ?? []);
        setConsentThirdParty(p.consentThirdParty);
        setConsentCampaign(p.consentCampaign);
        setConsentWhatsApp(p.consentWhatsApp);
        setConsentScheme(p.consentScheme);
        setConsentAnalytics(p.consentAnalytics);
      })
      .catch(() => Alert.alert('Error', 'Failed to load survey profile.'))
      .finally(() => setLoading(false));
  }, [voterId]);

  const onSave = async () => {
    setSaving(true);
    try {
      await updateVoterSurveyProfile(voterId, {
        ageBracket,
        casteCategory,
        religion,
        education,
        occupation,
        monthlyIncomeBracket: incomeBracket,
        preferredLanguage,
        primaryConcerns,
        consentThirdParty,
        consentCampaign,
        consentWhatsApp,
        consentScheme,
        consentAnalytics,
      });
      Alert.alert('Saved', 'Survey profile updated successfully.', [
        { text: 'OK', onPress: () => nav.goBack() },
      ]);
    } catch {
      Alert.alert('Error', 'Failed to save. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: '#f0f2f5' }}>
        <ActivityIndicator color="#3b5bdb" size="large" />
      </View>
    );
  }

  if (!profile) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', padding: 32 }}>
        <Text style={{ color: '#868e96' }}>Profile not found.</Text>
      </View>
    );
  }

  const lastUpdated = profile.profileUpdatedAt
    ? new Date(profile.profileUpdatedAt).toLocaleDateString('en-IN', {
        day: '2-digit', month: 'short', year: 'numeric',
      })
    : null;

  return (
    <View style={{ flex: 1, backgroundColor: '#f0f2f5' }}>
      {/* Voter identity banner */}
      <View style={f.identityBanner}>
        <View style={f.avatarCircle}>
          <Text style={f.avatarTxt}>{profile.voterName[0]}</Text>
        </View>
        <View style={{ flex: 1, marginLeft: 12 }}>
          <Text style={f.voterName}>{profile.voterName}</Text>
          <Text style={f.voterMeta}>
            {profile.voterEpic} · Booth {profile.boothNumber}
            {profile.wardNumber ? ` · Ward ${profile.wardNumber}` : ''}
          </Text>
          {lastUpdated && (
            <Text style={{ fontSize: 11, color: '#4dabf7', marginTop: 2 }}>
              Last updated {lastUpdated}
            </Text>
          )}
        </View>
        <View style={f.editBadge}>
          <Ionicons name="create-outline" size={13} color="#3b5bdb" />
          <Text style={f.editBadgeTxt}>Editing</Text>
        </View>
      </View>

      <ScrollView contentContainerStyle={{ padding: 16 }}>

        {/* ?? About You ?? */}
        <Section icon="person-outline" title="About Voter" />

        <PillSelect label="Age Bracket"      options={AGE_BRACKETS}     value={ageBracket}        onChange={setAgeBracket} />
        <PillSelect label="Caste Category"   options={CASTE_CATEGORIES} value={casteCategory}     onChange={setCasteCategory} />
        <PillSelect label="Religion"         options={RELIGIONS}        value={religion}           onChange={setReligion} />
        <PillSelect label="Education"        options={EDUCATIONS}       value={education}          onChange={setEducation} />
        <PillSelect label="Occupation"       options={OCCUPATIONS}      value={occupation}         onChange={setOccupation} />
        <PillSelect label="Monthly Income"   options={INCOME_BRACKETS}  value={incomeBracket}      onChange={setIncomeBracket} />
        <PillSelect label="Preferred Language" options={LANGUAGES}      value={preferredLanguage}  onChange={setPreferredLanguage} />

        {/* ?? Concerns ?? */}
        <Section icon="alert-circle-outline" title="Top Concerns" />
        <ConcernChips selected={primaryConcerns} onChange={setPrimaryConcerns} />

        {/* ?? Consents ?? */}
        <Section icon="shield-checkmark-outline" title="Consent & Data Use" />
        <View style={f.consentCard}>
          <ConsentRow
            mandatory
            label="3rd-Party Advertising"
            desc="Allow anonymised demographic profile to be shared with partner brands for coupon delivery and targeted ads."
            value={consentThirdParty}
            onChange={setConsentThirdParty}
          />
          <View style={f.consentDivider} />
          <ConsentRow
            label="Campaign Outreach"
            desc="Allow the campaign team to contact this voter (calls / visits)."
            value={consentCampaign}
            onChange={setConsentCampaign}
          />
          <View style={f.consentDivider} />
          <ConsentRow
            label="WhatsApp Messages"
            desc="Allow campaign WhatsApp messages."
            value={consentWhatsApp}
            onChange={setConsentWhatsApp}
          />
          <View style={f.consentDivider} />
          <ConsentRow
            label="Scheme Notifications"
            desc="Allow government scheme and benefit notifications."
            value={consentScheme}
            onChange={setConsentScheme}
          />
          <View style={f.consentDivider} />
          <ConsentRow
            label="Data for Analytics"
            desc="Allow anonymous use of voter data for campaign analytics."
            value={consentAnalytics}
            onChange={setConsentAnalytics}
          />
        </View>

        {/* ?? Save button ?? */}
        <TouchableOpacity
          style={[f.saveBtn, saving && { opacity: 0.6 }]}
          onPress={onSave}
          disabled={saving}>
          {saving
            ? <ActivityIndicator color="#fff" />
            : <>
                <Ionicons name="checkmark-circle-outline" size={20} color="#fff" />
                <Text style={f.saveTxt}>Save Changes</Text>
              </>
          }
        </TouchableOpacity>

        <TouchableOpacity style={f.cancelBtn} onPress={() => nav.goBack()}>
          <Text style={{ color: '#868e96', fontWeight: '600', textAlign: 'center' }}>Cancel</Text>
        </TouchableOpacity>

        <View style={{ height: 32 }} />
      </ScrollView>
    </View>
  );
}

// ?? Styles ????????????????????????????????????????????????????????
const f = StyleSheet.create({
  identityBanner: { backgroundColor: '#1a1f2e', paddingTop: 16, paddingBottom: 16,
                    paddingHorizontal: 16, flexDirection: 'row', alignItems: 'center' },
  avatarCircle:   { width: 46, height: 46, borderRadius: 23, backgroundColor: '#3b5bdb',
                    justifyContent: 'center', alignItems: 'center' },
  avatarTxt:      { color: '#fff', fontSize: 20, fontWeight: '800' },
  voterName:      { color: '#fff', fontSize: 15, fontWeight: '700' },
  voterMeta:      { color: '#868e96', fontSize: 11, marginTop: 2 },
  editBadge:      { flexDirection: 'row', alignItems: 'center', gap: 4,
                    backgroundColor: '#e8eeff', borderRadius: 8,
                    paddingHorizontal: 8, paddingVertical: 4 },
  editBadgeTxt:   { fontSize: 11, color: '#3b5bdb', fontWeight: '700' },
  sectionHead:    { flexDirection: 'row', alignItems: 'center', gap: 8,
                    marginBottom: 12, marginTop: 8 },
  sectionTitle:   { fontSize: 14, fontWeight: '700', color: '#3b5bdb' },
  label:          { fontSize: 13, fontWeight: '600', color: '#495057', marginBottom: 8 },
  pill:           { borderWidth: 1, borderColor: '#dee2e6', borderRadius: 20,
                    paddingHorizontal: 12, paddingVertical: 7 },
  pillActive:     { backgroundColor: '#3b5bdb', borderColor: '#3b5bdb' },
  pillTxt:        { fontSize: 12, fontWeight: '600', color: '#495057' },
  pillTxtActive:  { color: '#fff' },
  consentCard:    { backgroundColor: '#fff', borderRadius: 12, overflow: 'hidden',
                    marginBottom: 18, elevation: 1 },
  consentRow:     { flexDirection: 'row', alignItems: 'center',
                    paddingHorizontal: 16, paddingVertical: 14 },
  consentLabel:   { fontSize: 13, fontWeight: '700', color: '#212529' },
  consentDesc:    { fontSize: 11, color: '#868e96', marginTop: 2, lineHeight: 15 },
  consentDivider: { height: 1, backgroundColor: '#f1f3f5', marginHorizontal: 16 },
  mandatoryBadge: { backgroundColor: '#fff0f0', borderRadius: 6,
                    paddingHorizontal: 6, paddingVertical: 2 },
  mandatoryTxt:   { fontSize: 9, fontWeight: '800', color: '#e03131', textTransform: 'uppercase' },
  saveBtn:        { backgroundColor: '#3b5bdb', borderRadius: 12, flexDirection: 'row',
                    alignItems: 'center', justifyContent: 'center', gap: 8,
                    paddingVertical: 14, marginTop: 8, marginBottom: 10 },
  saveTxt:        { color: '#fff', fontSize: 15, fontWeight: '700' },
  cancelBtn:      { paddingVertical: 12 },
});
