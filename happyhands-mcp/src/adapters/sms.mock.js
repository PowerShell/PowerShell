export async function sendSms({ to, body, schedule_at }) {
  // Stub; replace with Twilio/MessageBird/etc.
  return {
    provider: 'mock-sms',
    to,
    body,
    schedule_at: schedule_at || null,
    status: schedule_at ? 'scheduled' : 'sent'
  };
}
