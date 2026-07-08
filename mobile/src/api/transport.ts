import apiClient from './client';

export interface TransportVehicleItem {
  id: number;
  driverName: string;
  driverPhone: string;
  vehicleNumber?: string;
  vehicleType?: string;
  capacity: number;
  boothNumber: number;
  isAvailable: boolean;
  notes?: string;
}

export interface TransportRequestItem {
  id: number;
  voterId: number;
  voterName: string;
  voterPhone?: string;
  vehicleId?: number;
  driverName?: string;
  vehicleNumber?: string;
  status: string;
  pickupAddress?: string;
  requestedAt: string;
}

export const VEHICLE_TYPES = ['Auto', 'Car', 'Van', 'Bus', 'Other'];
export const TRANSPORT_STATUSES = [
  { key: 'Pending',  label: 'Pending',    color: '#868e96' },
  { key: 'Assigned', label: 'Assigned',   color: '#3b5bdb' },
  { key: 'PickedUp', label: 'Picked Up',  color: '#f59f00' },
  { key: 'Voted',    label: 'Voted',      color: '#2f9e44' },
  { key: 'Cancelled',label: 'Cancelled',  color: '#e03131' },
];

export const getVehicles = async (): Promise<TransportVehicleItem[]> => {
  const { data } = await apiClient.get<TransportVehicleItem[]>('/transport/vehicles');
  return data;
};

export const createVehicle = async (req: {
  driverName: string; driverPhone: string; vehicleNumber?: string;
  vehicleType?: string; capacity: number; boothNumber: number; notes?: string;
}): Promise<void> => {
  await apiClient.post('/transport/vehicles', req);
};

export const getTransportRequests = async (): Promise<TransportRequestItem[]> => {
  const { data } = await apiClient.get<TransportRequestItem[]>('/transport/requests');
  return data;
};

export const createTransportRequest = async (req: {
  voterId: number; pickupAddress?: string; pickupNotes?: string; vehicleId?: number;
}): Promise<void> => {
  await apiClient.post('/transport/requests', req);
};

export const updateTransportStatus = async (id: number, status: string): Promise<void> => {
  await apiClient.put(`/transport/requests/${id}/status`, null, { params: { status } });
};
