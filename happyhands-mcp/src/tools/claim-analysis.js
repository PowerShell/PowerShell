import Ajv from 'ajv';
import addFormats from 'ajv-formats';
import { v4 as uuid } from 'uuid';
import { db } from '../store.js';
import * as superEliteAdapter from '../adapters/superelite.adapter.js';

const ajv = new Ajv({ allErrors: true });
addFormats(ajv);

// Schema definitions for advanced claim analysis
const estimateAnalysisSchema = {
  type: 'object',
  properties: {
    estimate_file_path: { type: 'string' },
    scope_file_path: { type: 'string' },
    carrier_name: { type: 'string' },
    jurisdiction: { type: 'string' },
    photo_paths: {
      type: 'array',
      items: { type: 'string' }
    },
    claim_number: { type: 'string' },
    policy_number: { type: 'string' },
    property_address: { type: 'string' }
  },
  required: ['estimate_file_path', 'scope_file_path', 'carrier_name']
};

const blueprintSupplementSchema = {
  type: 'object',
  properties: {
    blueprint_file_path: { type: 'string' },
    photo_library_paths: {
      type: 'array',
      items: { type: 'string' }
    },
    scope_items: {
      type: 'array',
      items: { type: 'string' }
    },
    property_id: { type: 'string' },
    supplement_type: { 
      type: 'string', 
      enum: ['missing_items', 'code_upgrade', 'hidden_damage', 'pricing_dispute'] 
    }
  },
  required: ['blueprint_file_path', 'photo_library_paths', 'scope_items']
};

async function validateAndRun(schema, data, fn) {
  const validate = ajv.compile(schema);
  if (!validate(data)) {
    throw new Error(`Validation failed: ${ajv.errorsText(validate.errors)}`);
  }
  return await fn(data);
}

// Helper function to load file content
async function loadFileContent(filePath) {
  try {
    const fs = await import('fs/promises');
    return await fs.readFile(filePath);
  } catch (error) {
    throw new Error(`Failed to load file ${filePath}: ${error.message}`);
  }
}

export const claimAnalysisTools = {
  analyze_carrier_estimate: {
    description: 'Analyze carrier estimate against contractor scope and generate comprehensive audit report with legal memo',
    schema: estimateAnalysisSchema,
    async run(args) {
      return await validateAndRun(estimateAnalysisSchema, args, async (data) => {
        try {
          // Load file contents
          const estimateFile = await loadFileContent(data.estimate_file_path);
          const scopeFile = await loadFileContent(data.scope_file_path);
          
          // Load photos if provided
          const photos = [];
          if (data.photo_paths && data.photo_paths.length > 0) {
            for (const photoPath of data.photo_paths) {
              try {
                const photoContent = await loadFileContent(photoPath);
                photos.push(photoContent);
              } catch (error) {
                console.warn(`Failed to load photo ${photoPath}:`, error.message);
              }
            }
          }
          
          const result = await superEliteAdapter.analyzeEstimate(
            estimateFile,
            scopeFile,
            data.carrier_name,
            data.jurisdiction || 'General',
            photos
          );
          
          // Store analysis record
          const record = {
            id: uuid(),
            type: 'estimate_analysis',
            timestamp: new Date().toISOString(),
            carrier_name: data.carrier_name,
            jurisdiction: data.jurisdiction,
            claim_number: data.claim_number,
            policy_number: data.policy_number,
            property_address: data.property_address,
            file_paths: {
              estimate: data.estimate_file_path,
              scope: data.scope_file_path,
              photos: data.photo_paths || []
            },
            analysis_results: {
              audit_report_url: result.auditReport,
              comparison_chart_url: result.comparisonChart,
              legal_memo_url: result.legalMemo
            }
          };
          await db.add('claim_analysis', record);
          
          return {
            success: true,
            analysis_id: record.id,
            results: {
              audit_report: result.auditReport,
              comparison_chart: result.comparisonChart,
              legal_memo: result.legalMemo,
              summary: 'Advanced AI analysis completed comparing carrier estimate to contractor scope with legal recommendations'
            },
            metadata: {
              carrier: data.carrier_name,
              jurisdiction: data.jurisdiction,
              photos_analyzed: photos.length,
              timestamp: record.timestamp
            }
          };
        } catch (error) {
          throw new Error(`Estimate analysis failed: ${error.message}`);
        }
      });
    }
  },

  generate_blueprint_supplement: {
    description: 'Match contractor scope to property blueprint and generate annotated supplement package with photo evidence',
    schema: blueprintSupplementSchema,
    async run(args) {
      return await validateAndRun(blueprintSupplementSchema, args, async (data) => {
        try {
          // Load blueprint file
          const blueprintFile = await loadFileContent(data.blueprint_file_path);
          
          // Load photo library
          const photoLibrary = [];
          for (const photoPath of data.photo_library_paths) {
            try {
              const photoContent = await loadFileContent(photoPath);
              photoLibrary.push(photoContent);
            } catch (error) {
              console.warn(`Failed to load photo ${photoPath}:`, error.message);
            }
          }
          
          const result = await superEliteAdapter.supplementBlueprint(
            blueprintFile,
            photoLibrary,
            data.scope_items
          );
          
          // Store supplement record
          const record = {
            id: uuid(),
            type: 'blueprint_supplement',
            timestamp: new Date().toISOString(),
            property_id: data.property_id,
            supplement_type: data.supplement_type,
            file_paths: {
              blueprint: data.blueprint_file_path,
              photo_library: data.photo_library_paths
            },
            scope_items: data.scope_items,
            supplement_results: {
              annotated_photos_url: result.annotatedPhotos,
              supplement_package_url: result.supplementPackage,
              rebuttal_memo_url: result.rebuttalMemo
            }
          };
          await db.add('blueprint_supplements', record);
          
          return {
            success: true,
            supplement_id: record.id,
            results: {
              annotated_photos: result.annotatedPhotos,
              supplement_package: result.supplementPackage,
              rebuttal_memo: result.rebuttalMemo,
              summary: 'Blueprint-matched supplement package generated with annotated photo evidence and legal rebuttal memo'
            },
            metadata: {
              property_id: data.property_id,
              supplement_type: data.supplement_type,
              scope_items_count: data.scope_items.length,
              photos_analyzed: photoLibrary.length,
              timestamp: record.timestamp
            }
          };
        } catch (error) {
          throw new Error(`Blueprint supplement generation failed: ${error.message}`);
        }
      });
    }
  },

  get_claim_analysis_history: {
    description: 'Retrieve history of claim analyses and supplement packages for a property or claim',
    schema: {
      type: 'object',
      properties: {
        property_id: { type: 'string' },
        claim_number: { type: 'string' },
        carrier_name: { type: 'string' },
        limit: { type: 'integer', minimum: 1, maximum: 100, default: 10 }
      }
    },
    async run(args) {
      try {
        const filters = {};
        if (args.property_id) filters.property_id = args.property_id;
        if (args.claim_number) filters.claim_number = args.claim_number;
        if (args.carrier_name) filters.carrier_name = args.carrier_name;
        
        const analysisRecords = await db.find('claim_analysis', filters, args.limit || 10);
        const supplementRecords = await db.find('blueprint_supplements', 
          args.property_id ? { property_id: args.property_id } : {}, 
          args.limit || 10
        );
        
        return {
          success: true,
          history: {
            estimate_analyses: analysisRecords.map(record => ({
              id: record.id,
              timestamp: record.timestamp,
              carrier: record.carrier_name,
              claim_number: record.claim_number,
              results: record.analysis_results
            })),
            blueprint_supplements: supplementRecords.map(record => ({
              id: record.id,
              timestamp: record.timestamp,
              property_id: record.property_id,
              supplement_type: record.supplement_type,
              scope_items_count: record.scope_items.length,
              results: record.supplement_results
            }))
          },
          metadata: {
            total_analyses: analysisRecords.length,
            total_supplements: supplementRecords.length,
            filters_applied: filters
          }
        };
      } catch (error) {
        throw new Error(`Failed to retrieve claim analysis history: ${error.message}`);
      }
    }
  },

  compare_multiple_estimates: {
    description: 'Compare multiple carrier estimates and generate consolidated analysis report',
    schema: {
      type: 'object',
      properties: {
        estimates: {
          type: 'array',
          items: {
            type: 'object',
            properties: {
              carrier_name: { type: 'string' },
              estimate_file_path: { type: 'string' },
              estimate_date: { type: 'string', format: 'date' }
            },
            required: ['carrier_name', 'estimate_file_path']
          },
          minItems: 2
        },
        contractor_scope_path: { type: 'string' },
        property_address: { type: 'string' }
      },
      required: ['estimates', 'contractor_scope_path']
    },
    async run(args) {
      try {
        const comparisons = [];
        const contractorScope = await loadFileContent(args.contractor_scope_path);
        
        for (const estimate of args.estimates) {
          const estimateFile = await loadFileContent(estimate.estimate_file_path);
          
          const analysis = await superEliteAdapter.analyzeEstimate(
            estimateFile,
            contractorScope,
            estimate.carrier_name,
            'Comparative Analysis'
          );
          
          comparisons.push({
            carrier: estimate.carrier_name,
            estimate_date: estimate.estimate_date,
            analysis_results: analysis
          });
        }
        
        // Store comparison record
        const record = {
          id: uuid(),
          type: 'multi_estimate_comparison',
          timestamp: new Date().toISOString(),
          property_address: args.property_address,
          carriers_compared: args.estimates.map(e => e.carrier_name),
          comparison_results: comparisons
        };
        await db.add('claim_analysis', record);
        
        return {
          success: true,
          comparison_id: record.id,
          results: {
            comparisons: comparisons,
            summary: `Comparative analysis completed for ${args.estimates.length} carrier estimates`,
            consolidated_recommendations: 'Review individual analysis reports for carrier-specific recommendations'
          },
          metadata: {
            carriers_compared: args.estimates.length,
            property_address: args.property_address,
            timestamp: record.timestamp
          }
        };
      } catch (error) {
        throw new Error(`Multi-estimate comparison failed: ${error.message}`);
      }
    }
  }
};