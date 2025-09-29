import Ajv from 'ajv';
import addFormats from 'ajv-formats';
import { v4 as uuid } from 'uuid';
import { db } from '../store.js';
import { sendSms } from '../adapters/sms.mock.js';
import { sendEmail } from '../adapters/email.mock.js';

const ajv = new Ajv({ allErrors: true });
addFormats(ajv);

const smsSchema = {
  type: 'object',
  properties: {
    to: { type: 'string', format: 'uri' },
    body: { type: 'string', maxLength: 1600 },
    schedule_at: { type: 'string', format: 'date-time' }
  },
  required: ['to', 'body']
};

const emailSchema = {
  type: 'object',
  properties: {
    to: { type: 'string', format: 'email' },
    subject: { type: 'string', maxLength: 200 },
    body: { type: 'string' },
    attachments: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          name: { type: 'string' },
          content_type: { type: 'string' },
          url: { type: 'string', format: 'uri' }
        },
        required: ['name', 'content_type', 'url']
      }
    }
  },
  required: ['to', 'subject', 'body']
};

const templateSchema = {
  type: 'object',
  properties: {
    template_id: { type: 'string' },
    variables: { type: 'object' }
  },
  required: ['template_id', 'variables']
};

async function validateAndRun(schema, data, fn) {
  const validate = ajv.compile(schema);
  if (!validate(data)) {
    throw new Error(`Validation failed: ${ajv.errorsText(validate.errors)}`);
  }
  return await fn(data);
}

export const messagingTools = {
  send_sms: {
    description: 'Send SMS message with optional scheduling',
    schema: smsSchema,
    async run(args) {
      return await validateAndRun(smsSchema, args, async (data) => {
        const result = await sendSms(data);
        const record = {
          id: uuid(),
          type: 'sms',
          timestamp: new Date().toISOString(),
          ...data,
          ...result
        };
        await db.add('messages', record);
        return record;
      });
    }
  },

  send_email: {
    description: 'Send email with optional attachments',
    schema: emailSchema,
    async run(args) {
      return await validateAndRun(emailSchema, args, async (data) => {
        const result = await sendEmail(data);
        const record = {
          id: uuid(),
          type: 'email',
          timestamp: new Date().toISOString(),
          ...data,
          ...result
        };
        await db.add('messages', record);
        return record;
      });
    }
  },

  template_preview: {
    description: 'Preview a message template with variables',
    schema: templateSchema,
    async run(args) {
      return await validateAndRun(templateSchema, args, async ({ template_id, variables }) => {
        // Mock template system
        const templates = {
          'storm_initial': 'Hi {{first_name}} — hail impacted {{city}} on {{event_date}} (up to {{hail_max}}" per NOAA). Nelrock Contracting offers a free, code-compliant roof inspection. Reply YES to schedule, STOP to opt out. Msg&data rates may apply.',
          'appointment_confirmation': 'Locked: {{date}} at {{time}} for {{address}}. You\'ll get a 24h reminder and a 30m heads-up. Please e-sign the inspection consent: {{esign_link}}.',
          '24h_reminder': 'Reminder: Your roof inspection is tomorrow {{date}} at {{time}} for {{address}}. Nelrock Contracting will call 30 minutes before arrival.',
          '30m_reminder': 'We\'re on our way! Arriving at {{address}} in 30 minutes for your {{time}} appointment. Please ensure someone 18+ is home.'
        };
        
        const template = templates[template_id];
        if (!template) throw new Error(`Unknown template: ${template_id}`);
        
        let result = template;
        for (const [key, value] of Object.entries(variables)) {
          result = result.replace(new RegExp(`{{${key}}}`, 'g'), value);
        }
        
        return { template_id, preview: result, variables };
      });
    }
  }
};
