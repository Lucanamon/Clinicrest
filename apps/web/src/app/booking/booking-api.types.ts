/** ISO-8601 instant from the API, always interpreted as UTC by `Date`. */
export type ApiUtcIsoString = string;

export interface SlotApiDto {
  id: string;
  start_time: ApiUtcIsoString;
  end_time: ApiUtcIsoString;
  capacity: number;
  booked_count: number;
  available_slots: number;
}

export interface BookingApiDto {
  id: string;
  user_id: string;
  slot_id: string;
  status: string;
  created_at: ApiUtcIsoString;
}
