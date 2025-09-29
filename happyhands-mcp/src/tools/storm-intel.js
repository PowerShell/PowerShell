import Ajv from 'ajv';
import addFormats from 'ajv-formats';
import { v4 as uuid } from 'uuid';
import { db } from '../store.js';
import * as dotnetAdapter from '../adapters/dotnet.adapter.js';

const ajv = new Ajv({ allErrors: true });
addFormats(ajv);

// Schema definitions
const stormSwathSchema = {
  type: 'object',
  properties: {
    bbox: {
      type: 'object',
      properties: {
        north: { type: 'number' },
        south: { type: 'number' },
        east: { type: 'number' },
        west: { type: 'number' }
      },
      required: ['north', 'south', 'east', 'west']
    },
    start_utc: { type: 'string', format: 'date-time' },
    end_utc: { type: 'string', format: 'date-time' },
    hazards: {
      type: 'array',
      items: { type: 'string', enum: ['hail', 'wind', 'tornado'] }
    }
  },
  required: ['bbox', 'start_utc', 'end_utc']
};

const hailStatsSchema = {
  type: 'object',
  properties: {
    lat: { type: 'number', minimum: -90, maximum: 90 },
    lon: { type: 'number', minimum: -180, maximum: 180 },
    date: { type: 'string', format: 'date' }
  },
  required: ['lat', 'lon', 'date']
};

const serviceAreaIntersectionSchema = {
  type: 'object',
  properties: {
    service_area_geojson: { type: 'object' },
    swath_geojson: { type: 'object' }
  },
  required: ['service_area_geojson', 'swath_geojson']
};

const buildScopeSchema = {
  type: 'object',
  properties: {
    case_id: { type: 'string' },
    measurements: {
      type: 'object',
      properties: {
        facets: {
          type: 'array',
          items: {
            type: 'object',
            properties: {
              facetId: { type: 'string' },
              squareFootage: { type: 'number' },
              pitch: { type: 'number' },
              orientation: { type: 'string' },
              layers: { type: 'integer' },
              material: { type: 'string' }
            },
            required: ['facetId', 'squareFootage', 'pitch']
          }
        },
        totalSquareFootage: { type: 'number' },
        stories: { type: 'integer' },
        primaryMaterial: { type: 'string' },
        accessType: { type: 'string' }
      },
      required: ['facets', 'totalSquareFootage']
    },
    damages: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          damageType: { type: 'string' },
          location: { type: 'string' },
          severity: { type: 'string', enum: ['minor', 'moderate', 'severe'] },
          description: { type: 'string' },
          photoIds: { type: 'array', items: { type: 'string' } }
        },
        required: ['damageType', 'location']
      }
    },
    buildingCodes: { type: 'array', items: { type: 'string' } }
  },
  required: ['case_id', 'measurements']
};

async function validateAndRun(schema, data, fn) {
  const validate = ajv.compile(schema);
  if (!validate(data)) {
    throw new Error(`Validation failed: ${ajv.errorsText(validate.errors)}`);
  }
  return await fn(data);
}

export const stormIntelTools = {
  fetch_storm_swath: {
    description: 'Get detailed storm path data for a specific event',
    schema: {
      type: 'object',
      properties: {
        event_id: { type: 'string' },
        format: { type: 'string', enum: ['geojson', 'json'] }
      },
      required: ['event_id']
    },
    async run(args) {
      return await validateAndRun({ type: 'object', properties: { event_id: { type: 'string' }, format: { type: 'string' } }, required: ['event_id'] }, args, async (data) => {
        try {
          const result = await dotnetAdapter.fetchStormSwath(data.event_id, data.format || 'json');
          
          const record = {
            id: uuid(),
            type: 'storm_swath_fetch',
            timestamp: new Date().toISOString(),
            event_id: data.event_id,
            format: data.format || 'json'
          };
          await db.add('storm_intel', record);
          
          return result;
        } catch (error) {
          throw new Error(`Storm swath fetch failed: ${error.message}`);
        }
      });
    }
  },

  hail_stats_at: {
    description: 'Analyze hail damage potential at a specific location and date',
    schema: hailStatsSchema,
    async run(args) {
      return await validateAndRun(hailStatsSchema, args, async (data) => {
        try {
          const result = await dotnetAdapter.getHailStats(data.lat, data.lon, data.date);
          
          const record = {
            id: uuid(),
            type: 'hail_stats',
            timestamp: new Date().toISOString(),
            location: { lat: data.lat, lon: data.lon },
            date: data.date,
            stats: result
          };
          await db.add('storm_intel', record);
          
          return result;
        } catch (error) {
          throw new Error(`Hail stats fetch failed: ${error.message}`);
        }
      });
    }
  },

  intersect_service_area: {
    description: 'Find affected properties within a service area polygon',
    schema: {
      type: 'object',
      properties: {
        polygon: { type: 'object' },
        event_id: { type: 'string' }
      },
      required: ['polygon', 'event_id']
    },
    async run(args) {
      return await validateAndRun({ type: 'object', properties: { polygon: { type: 'object' }, event_id: { type: 'string' } }, required: ['polygon', 'event_id'] }, args, async (data) => {
        try {
          const result = await dotnetAdapter.intersectServiceArea(data.polygon, data.event_id);
          
          const record = {
            id: uuid(),
            type: 'service_area_intersection',
            timestamp: new Date().toISOString(),
            event_id: data.event_id,
            affected_properties_count: result.affectedProperties?.length || 0
          };
          await db.add('storm_intel', record);
          
          return result;
        } catch (error) {
          throw new Error(`Service area intersection failed: ${error.message}`);
        }
      });
    }
  },

  event_summary: {
    description: 'Generate a comprehensive storm event report with damage assessment',
    schema: {
      type: 'object',
      properties: {
        event_id: { type: 'string' }
      },
      required: ['event_id']
    },
    async run(args) {
      return await validateAndRun({ type: 'object', properties: { event_id: { type: 'string' } }, required: ['event_id'] }, args, async (data) => {
        try {
          const result = await dotnetAdapter.getEventSummary(data.event_id);
          
          const record = {
            id: uuid(),
            type: 'event_summary',
            timestamp: new Date().toISOString(),
            event_id: data.event_id
          };
          await db.add('storm_intel', record);
          
          return result;
        } catch (error) {
          throw new Error(`Event summary generation failed: ${error.message}`);
        }
      });
    }
  }
};

export const estimatingTools = {
  build_scope: {
    description: 'Create a repair estimate with IRC compliance based on property damage',
    schema: {
      type: 'object',
      properties: {
        property_id: { type: 'string' },
        damages: {
          type: 'array',
          items: {
            type: 'object',
            properties: {
              type: { type: 'string' },
              location: { type: 'string' },
              severity: { type: 'string', enum: ['minor', 'moderate', 'severe'] },
              description: { type: 'string' }
            },
            required: ['type', 'location']
          }
        }
      },
      required: ['property_id', 'damages']
    },
    async run(args) {
      const schema = {
        type: 'object',
        properties: {
          property_id: { type: 'string' },
          damages: { type: 'array' }
        },
        required: ['property_id', 'damages']
      };
      
      return await validateAndRun(schema, args, async (data) => {
        try {
          const result = await dotnetAdapter.buildScope(data.property_id, data.damages);
          
          const record = {
            id: uuid(),
            type: 'scope_build',
            timestamp: new Date().toISOString(),
            property_id: data.property_id,
            damages_count: data.damages.length,
            scope_id: result.scopeId
          };
          await db.add('estimates', record);
          
          return result;
        } catch (error) {
          throw new Error(`Scope building failed: ${error.message}`);
        }
      });
    }
  },

  export_xactimate: {
    description: 'Export repair scope to Xactimate format for insurance processing',
    schema: {
      type: 'object',
      properties: {
        scope_id: { type: 'string' }
      },
      required: ['scope_id']
    },
    async run(args) {
      return await validateAndRun({ type: 'object', properties: { scope_id: { type: 'string' } }, required: ['scope_id'] }, args, async (data) => {
        try {
          const result = await dotnetAdapter.exportXactimate(data.scope_id);
          
          const record = {
            id: uuid(),
            type: 'xactimate_export',
            timestamp: new Date().toISOString(),
            scope_id: data.scope_id,
            file_url: result.fileUrl
          };
          await db.add('estimates', record);
          
          return result;
        } catch (error) {
          throw new Error(`Xactimate export failed: ${error.message}`);
        }
      });
    }
  },

  calculate_material_costs: {
    description: 'Calculate current material costs for a repair scope',
    schema: {
      type: 'object',
      properties: {
        scope_id: { type: 'string' }
      },
      required: ['scope_id']
    },
    async run(args) {
      return await validateAndRun({ type: 'object', properties: { scope_id: { type: 'string' } }, required: ['scope_id'] }, args, async (data) => {
        try {
          const result = await dotnetAdapter.calculateMaterialCosts(data.scope_id);
          
          const record = {
            id: uuid(),
            type: 'material_costs',
            timestamp: new Date().toISOString(),
            scope_id: data.scope_id,
            total_cost: result.costs?.totalCost || 0
          };
          await db.add('estimates', record);
          
          return result;
        } catch (error) {
          throw new Error(`Material cost calculation failed: ${error.message}`);
        }
      });
    }
  }
};
