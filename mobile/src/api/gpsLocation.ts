import apiClient from './client';

export interface GpsPosition {
  latitude: number;
  longitude: number;
  accuracyMeters?: number;
}

/**
 * Request location permission and return the current GPS position.
 * Uses a dynamic import of expo-location so the app still builds without it.
 * Returns null if permission denied, unavailable, or package not installed.
 */
export async function getCurrentPosition(): Promise<GpsPosition | null> {
  try {
    const Location = await import('expo-location' as string).catch(() => null) as any;
    if (!Location) return null;

    const { status } = await Location.requestForegroundPermissionsAsync();
    if (status !== 'granted') return null;

    const loc = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy?.High ?? 4 });
    return {
      latitude:       loc.coords.latitude,
      longitude:      loc.coords.longitude,
      accuracyMeters: loc.coords.accuracy ?? undefined,
    };
  } catch {
    return null;
  }
}

/** Log a door-to-door visit with GPS coordinates to the backend. */
export async function logVisitWithGps(params: {
  voterId: number;
  status: string;
  sentiment: string;
  notes?: string;
  issuesRaised?: string;
  latitude?: number;
  longitude?: number;
  accuracyMeters?: number;
}) {
  const { data } = await apiClient.post('/mobile/visit', params);
  return data;
}

/** Push the user's current location to the backend for the volunteer tracking map. */
export async function updateVolunteerLocation(pos: GpsPosition) {
  await apiClient.post('/mobile/location', {
    latitude:       pos.latitude,
    longitude:      pos.longitude,
    accuracyMeters: pos.accuracyMeters,
  });
}

/** Fetch all active volunteer locations (updated in last 8h) for the map view. */
export async function getVolunteerLocations(): Promise<{
  userId: string;
  userName: string;
  latitude: number;
  longitude: number;
  accuracyMeters?: number;
  updatedAt: string;
}[]> {
  const { data } = await apiClient.get('/mobile/volunteer-locations');
  return data;
}
