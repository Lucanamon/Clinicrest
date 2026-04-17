/** ISO-8601 instant from the API, always interpreted as UTC by `Date`. */
export type ApiUtcIsoString = string;

export interface SlotApiDto {
  id: number;
  start_time: ApiUtcIsoString;
  end_time: ApiUtcIsoString;
  capacity: number;
  booked_count: number;
  available_slots: number;
}

export interface CreateTimeSlotRequest {
  start_time: ApiUtcIsoString;
  end_time: ApiUtcIsoString;
  capacity: number;
}

export type UpdateTimeSlotCapacityAction = 'increase' | 'decrease';

export interface BookingApiDto {
  id: number;
  slot_id: number;
  patient_name: string;
  phone_number?: string | null;
  patient_id?: string | null;
  doctor_name?: string | null;
  slot_start_time?: ApiUtcIsoString | null;
  status: string;
  created_at: ApiUtcIsoString;
}
