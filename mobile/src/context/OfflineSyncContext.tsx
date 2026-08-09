import React, { createContext, useContext, useEffect, useRef, useState } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { AppState, AppStateStatus } from 'react-native';
import apiClient from '../api/client';

// ?? Types ????????????????????????????????????????????????????????????????????
export interface QueuedVisit {
  id: string;           // local UUID
  voterId: number;
  status: string;
  sentiment: string;
  notes?: string;
  issuesRaised?: string;
  latitude?: number;
  longitude?: number;
  visitedAt: string;    // ISO string
}

export interface QueuedSentimentUpdate {
  voterId: number;
  sentiment: string;
}

interface OfflineSyncContextType {
  isOnline: boolean;
  pendingCount: number;
  queueVisit: (v: Omit<QueuedVisit, 'id' | 'visitedAt'>) => Promise<void>;
  queueSentiment: (voterId: number, sentiment: string) => Promise<void>;
  syncNow: () => Promise<{ synced: boolean; visitsAdded: number; sentimentUpdates: number }>;
}

const OfflineSyncContext = createContext<OfflineSyncContextType>({} as OfflineSyncContextType);
const VISITS_KEY    = '@offline_visits';
const SENTIMENT_KEY = '@offline_sentiments';

// ?? Provider ?????????????????????????????????????????????????????????????????
export const OfflineSyncProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
const [isOnline, setIsOnline]         = useState(true);
const [pendingCount, setPendingCount] = useState(0);
const syncingRef  = useRef(false);
// Use a ref so the interval/AppState callbacks always call the latest syncNow
const syncNowRef  = useRef<() => Promise<{ synced: boolean; visitsAdded: number; sentimentUpdates: number }>>(() =>
  Promise.resolve({ synced: false, visitsAdded: 0, sentimentUpdates: 0 }));

const refreshPendingCount = async () => {
  const [visits, sentiments] = await Promise.all([
    AsyncStorage.getItem(VISITS_KEY),
    AsyncStorage.getItem(SENTIMENT_KEY),
  ]);
  const v = visits     ? (JSON.parse(visits)     as QueuedVisit[]).length           : 0;
  const s = sentiments ? (JSON.parse(sentiments)  as QueuedSentimentUpdate[]).length : 0;
  setPendingCount(v + s);
};

const syncNow = async () => {
  if (syncingRef.current) return { synced: false, visitsAdded: 0, sentimentUpdates: 0 };
  syncingRef.current = true;
  try {
    const [vRaw, sRaw] = await Promise.all([
      AsyncStorage.getItem(VISITS_KEY),
      AsyncStorage.getItem(SENTIMENT_KEY),
    ]);
    const visits     = vRaw ? (JSON.parse(vRaw)  as QueuedVisit[])           : [];
    const sentiments = sRaw ? (JSON.parse(sRaw) as QueuedSentimentUpdate[]) : [];
    if (!visits.length && !sentiments.length) return { synced: true, visitsAdded: 0, sentimentUpdates: 0 };

    const { data } = await apiClient.post('/mobile/sync', {
      visits: visits.map(v => ({
        voterId:      v.voterId,
        status:       v.status,
        sentiment:    v.sentiment,
        notes:        v.notes,
        issuesRaised: v.issuesRaised,
        latitude:     v.latitude,
        longitude:    v.longitude,
        visitedAt:    v.visitedAt,
      })),
      sentimentUpdates: sentiments,
    });

    await AsyncStorage.multiRemove([VISITS_KEY, SENTIMENT_KEY]);
    await refreshPendingCount();
    return { synced: true, visitsAdded: data.visitsAdded ?? 0, sentimentUpdates: data.sentimentUpdates ?? 0 };
  } catch {
    return { synced: false, visitsAdded: 0, sentimentUpdates: 0 };
  } finally {
    syncingRef.current = false;
  }
};

// Keep ref current so callbacks always use the latest version
syncNowRef.current = syncNow;

// Watch connectivity via AppState + lightweight fetch probe
useEffect(() => {
  const checkOnline = async () => {
    try {
      await fetch('https://nirvachakai-production.up.railway.app/health', {
        method: 'HEAD', cache: 'no-store',
        signal: AbortSignal.timeout ? AbortSignal.timeout(3000) : undefined,
      });
      setIsOnline(true);
      syncNowRef.current();
    } catch {
      setIsOnline(false);
    }
  };

  const sub      = AppState.addEventListener('change', (state: AppStateStatus) => {
    if (state === 'active') checkOnline();
  });
  const interval = setInterval(checkOnline, 30_000);
  checkOnline();
  refreshPendingCount();
  return () => { sub.remove(); clearInterval(interval); };
}, []); // empty deps — intentional, uses ref for syncNow

  const queueVisit = async (v: Omit<QueuedVisit, 'id' | 'visitedAt'>) => {
    const raw   = await AsyncStorage.getItem(VISITS_KEY);
    const queue = raw ? (JSON.parse(raw) as QueuedVisit[]) : [];
    queue.push({ ...v, id: Math.random().toString(36).slice(2), visitedAt: new Date().toISOString() });
    await AsyncStorage.setItem(VISITS_KEY, JSON.stringify(queue));
    await refreshPendingCount();
    if (isOnline) syncNowRef.current();
  };

  const queueSentiment = async (voterId: number, sentiment: string) => {
    const raw      = await AsyncStorage.getItem(SENTIMENT_KEY);
    const queue    = raw ? (JSON.parse(raw) as QueuedSentimentUpdate[]) : [];
    const filtered = queue.filter(s => s.voterId !== voterId);
    filtered.push({ voterId, sentiment });
    await AsyncStorage.setItem(SENTIMENT_KEY, JSON.stringify(filtered));
    await refreshPendingCount();
    if (isOnline) syncNowRef.current();
  };

  return (
    <OfflineSyncContext.Provider value={{ isOnline, pendingCount, queueVisit, queueSentiment, syncNow }}>
      {children}
    </OfflineSyncContext.Provider>
  );
};

export const useOfflineSync = () => useContext(OfflineSyncContext);
