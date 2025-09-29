export async function callSuperEliteAPI(endpoint, method = 'POST', formData = null) {
  const baseUrl = process.env.SUPERELITE_API_URL || 'https://superelite-claimbots.ai/api';
  const url = `${baseUrl}${endpoint}`;
  
  try {
    const options = {
      method,
      headers: {
        'Authorization': `Bearer ${process.env.SUPERELITE_API_KEY}`,
        'Accept': 'application/json'
      }
    };
    
    if (formData && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
      options.body = formData;
    }
    
    const response = await fetch(url, options);
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }
    
    return await response.json();
  } catch (error) {
    console.error(`Error calling Super Elite API ${endpoint}:`, error.message);
    throw error;
  }
}

export async function analyzeEstimate(estimateFile, scopeFile, carrierName, jurisdiction, photos = []) {
  const formData = new FormData();
  
  // Handle file uploads
  if (estimateFile instanceof File || estimateFile instanceof Blob) {
    formData.append('estimateFile', estimateFile);
  } else if (typeof estimateFile === 'string') {
    // Assume it's a file path or base64 encoded content
    const blob = new Blob([estimateFile], { type: 'application/octet-stream' });
    formData.append('estimateFile', blob, 'estimate.pdf');
  }
  
  if (scopeFile instanceof File || scopeFile instanceof Blob) {
    formData.append('scopeFile', scopeFile);
  } else if (typeof scopeFile === 'string') {
    const blob = new Blob([scopeFile], { type: 'application/octet-stream' });
    formData.append('scopeFile', blob, 'scope.pdf');
  }
  
  formData.append('carrierName', carrierName);
  formData.append('jurisdiction', jurisdiction);
  
  // Handle photo uploads
  photos.forEach((photo, index) => {
    if (photo instanceof File || photo instanceof Blob) {
      formData.append('photos', photo);
    } else if (typeof photo === 'string') {
      const blob = new Blob([photo], { type: 'image/jpeg' });
      formData.append('photos', blob, `photo_${index}.jpg`);
    }
  });
  
  return await callSuperEliteAPI('/analyze-estimate', 'POST', formData);
}

export async function supplementBlueprint(blueprintFile, photoLibrary, scopeItems) {
  const formData = new FormData();
  
  // Handle blueprint file upload
  if (blueprintFile instanceof File || blueprintFile instanceof Blob) {
    formData.append('blueprintFile', blueprintFile);
  } else if (typeof blueprintFile === 'string') {
    const blob = new Blob([blueprintFile], { type: 'application/octet-stream' });
    formData.append('blueprintFile', blob, 'blueprint.pdf');
  }
  
  // Handle photo library uploads
  photoLibrary.forEach((photo, index) => {
    if (photo instanceof File || photo instanceof Blob) {
      formData.append('photoLibrary', photo);
    } else if (typeof photo === 'string') {
      const blob = new Blob([photo], { type: 'image/jpeg' });
      formData.append('photoLibrary', blob, `library_photo_${index}.jpg`);
    }
  });
  
  // Handle scope items
  scopeItems.forEach((item, index) => {
    formData.append('scopeItems', item);
  });
  
  return await callSuperEliteAPI('/supplement-blueprint', 'POST', formData);
}