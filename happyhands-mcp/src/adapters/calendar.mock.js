import { v4 as uuid } from 'uuid';

const memory = { appts: [] };

export async function availability({ crew_id, start_iso, end_iso }) {
  // Return 60-min slots on the hour between start and end, skip already booked.
  const start = new Date(start_iso);
  const end = new Date(end_iso);
  const slots = [];
  const booked = memory.appts
    .filter(a => a.crew_id === crew_id)
    .map(a => a.start);

  for (let t = new Date(start); t < end; t.setHours(t.getHours() + 1)) {
    const iso = new Date(t).toISOString();
    if (!booked.includes(iso)) {
      slots.push({ start: iso, duration_min: 60 });
    }
  }
  return { crew_id, slots };
}

export async function book({ case_id, crew_id, start_iso, duration_min = 60 }) {
  const appt = { id: uuid(), case_id, crew_id, start: start_iso, duration_min };
  memory.appts.push(appt);
  return appt;
}

export async function reschedule({ appt_id, start_iso }) {
  const idx = memory.appts.findIndex(a => a.id === appt_id);
  if (idx === -1) throw new Error('Appointment not found');
  memory.appts[idx].start = start_iso;
  return memory.appts[idx];
}

export async function cancel({ appt_id }) {
  const idx = memory.appts.findIndex(a => a.id === appt_id);
  if (idx === -1) throw new Error('Appointment not found');
  const [removed] = memory.appts.splice(idx, 1);
  return removed;
}
