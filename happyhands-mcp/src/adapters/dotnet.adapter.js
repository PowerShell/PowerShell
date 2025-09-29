export async function callDotnetService(endpoint, method = 'GET', data = null) {
  const baseUrl = process.env.DOTNET_SERVICE_URL || 'http://localhost:5001';
  const url = `${baseUrl}${endpoint}`;
  
  try {
    const options = {
      method,
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      }
    };
    
    if (data && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
      options.body = JSON.stringify(data);
    }
    
    const response = await fetch(url, options);
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }
    
    return await response.json();
  } catch (error) {
    console.error(`Error calling .NET service ${endpoint}:`, error.message);
    throw error;
  }
}

export async function fetchStormSwath(eventId, format = 'json') {
  return await callDotnetService(`/api/storm/fetch_storm_swath?eventId=${encodeURIComponent(eventId)}&format=${format}`);
}

export async function getHailStats(lat, lon, date) {
  return await callDotnetService(`/api/storm/hail_stats_at?lat=${lat}&lon=${lon}&date=${date}`);
}

export async function intersectServiceArea(polygon, eventId) {
  return await callDotnetService('/api/storm/intersect_service_area', 'POST', { polygon, eventId });
}

export async function getEventSummary(eventId) {
  return await callDotnetService(`/api/storm/event_summary?eventId=${encodeURIComponent(eventId)}`);
}

export async function buildScope(propertyId, damages) {
  return await callDotnetService('/api/estimate/build_scope', 'POST', { propertyId, damages });
}

export async function exportXactimate(scopeId) {
  return await callDotnetService(`/api/estimate/export_xactimate?scopeId=${encodeURIComponent(scopeId)}`);
}

export async function calculateMaterialCosts(scopeId) {
  return await callDotnetService(`/api/estimate/calculate_material_costs?scopeId=${encodeURIComponent(scopeId)}`);
}
