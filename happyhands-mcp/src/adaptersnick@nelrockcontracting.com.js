export async function sendEmail({ to, subject, body, attachments }) {
  return {
    provider: 'mock-email',
    to,
    subject,
    body,
    attachments: attachments || [],
    status: 'queued'
  };
}
