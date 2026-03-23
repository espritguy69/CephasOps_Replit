/**
 * Address Parser Utility
 * Extracts building name, street, city, state, and postcode from full address text
 * 
 * This is a simplified frontend parser. For more complex parsing, consider using
 * the backend AddressParser service via API.
 */

export interface ParsedAddress {
  buildingName?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  postcode?: string;
  unitNo?: string;
}

/**
 * Malaysian states for detection
 */
const MALAYSIAN_STATES = [
  'Johor', 'Kedah', 'Kelantan', 'Melaka', 'Negeri Sembilan',
  'Pahang', 'Perak', 'Perlis', 'Pulau Pinang', 'Penang',
  'Sabah', 'Sarawak', 'Selangor', 'Terengganu',
  'Wilayah Persekutuan', 'WP', 'Kuala Lumpur', 'Labuan', 'Putrajaya'
];

/**
 * Extract postcode (5-digit Malaysian postcode)
 */
function extractPostcode(text: string): string | undefined {
  const match = text.match(/\b(\d{5})\b/);
  return match ? match[1] : undefined;
}

/**
 * Extract state from address text
 */
function extractState(text: string): string | undefined {
  const textLower = text.toLowerCase();
  
  for (const state of MALAYSIAN_STATES) {
    if (textLower.includes(state.toLowerCase())) {
      // Handle special cases
      if (state === 'Kuala Lumpur' || textLower.includes('kuala lumpur')) {
        return 'Wilayah Persekutuan';
      }
      if (state === 'Putrajaya' || textLower.includes('putrajaya')) {
        return 'Wilayah Persekutuan';
      }
      if (state === 'WP' || state === 'Wilayah Persekutuan') {
        return 'Wilayah Persekutuan';
      }
      return state;
    }
  }
  
  return undefined;
}

/**
 * Extract city from address text (usually appears before state/postcode)
 */
function extractCity(text: string, postcode?: string, state?: string): string | undefined {
  // Try to find city after postcode
  if (postcode) {
    const postcodeIndex = text.indexOf(postcode);
    if (postcodeIndex !== -1) {
      const afterPostcode = text.substring(postcodeIndex + postcode.length).trim();
      // City is usually the first word/phrase after postcode, before state
      const parts = afterPostcode.split(',').map(p => p.trim()).filter(p => p);
      if (parts.length > 0 && parts[0] !== state) {
        return parts[0];
      }
    }
  }
  
  // Common city patterns
  const cityPatterns = [
    /Kuala Lumpur/i,
    /Petaling Jaya/i,
    /Subang Jaya/i,
    /Shah Alam/i,
    /Klang/i,
    /Ampang/i,
    /Cheras/i,
    /Puchong/i,
    /Putrajaya/i,
    /Cyberjaya/i,
    /Johor Bahru/i,
    /George Town/i,
    /Ipoh/i,
    /Malacca/i,
    /Seremban/i
  ];
  
  for (const pattern of cityPatterns) {
    const match = text.match(pattern);
    if (match) {
      return match[0];
    }
  }
  
  return undefined;
}

/**
 * Extract building name from address text
 * Building names are usually:
 * - All caps words
 * - Appear before street address
 * - Common patterns: "TOWER", "RESIDENCE", "PLAZA", "CONDO", etc.
 */
function extractBuildingName(text: string): string | undefined {
  // Remove unit numbers and levels first
  let cleaned = text
    .replace(/Level\s+\d+[A-Z]?/gi, '')
    .replace(/Unit\s+\d+[A-Z]?/gi, '')
    .replace(/Block\s+[A-Z]/gi, '')
    .replace(/No\.?\s*\d+/gi, '');
  
  // Look for all-caps words (common building name pattern)
  const allCapsMatch = cleaned.match(/\b([A-Z]{2,}(?:\s+[A-Z]{2,})*)\b/);
  if (allCapsMatch) {
    const candidate = allCapsMatch[1];
    // Filter out common non-building words
    const skipWords = ['JALAN', 'JLN', 'LORONG', 'LOT', 'TAMAN', 'TMN'];
    const words = candidate.split(/\s+/);
    const filtered = words.filter(w => !skipWords.includes(w));
    if (filtered.length > 0) {
      return filtered.join(' ');
    }
  }
  
  // Look for common building name patterns
  const buildingPatterns = [
    /([A-Z][A-Z\s]+(?:RESIDENCE|TOWER|PLAZA|CONDO|APARTMENT|SUITE|MENARA|COMPLEX))/i,
    /([A-Z][A-Z\s]+(?:HEIGHTS|VIEW|GARDEN|PARK|VILLA|ESTATE))/i
  ];
  
  for (const pattern of buildingPatterns) {
    const match = cleaned.match(pattern);
    if (match && match[1]) {
      return match[1].trim();
    }
  }
  
  return undefined;
}

/**
 * Extract street address (address line 1)
 * This is usually the street name, appearing after building name
 */
function extractStreetAddress(text: string, buildingName?: string): string | undefined {
  let cleaned = text;
  
  // Remove building name if found
  if (buildingName) {
    cleaned = cleaned.replace(new RegExp(buildingName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi'), '');
  }
  
  // Remove unit/level info
  cleaned = cleaned
    .replace(/Level\s+\d+[A-Z]?/gi, '')
    .replace(/Unit\s+\d+[A-Z]?/gi, '')
    .replace(/Block\s+[A-Z]/gi, '')
    .replace(/No\.?\s*\d+/gi, '');
  
  // Look for street patterns
  const streetPatterns = [
    /(Jalan|Jln|Lorong|Lrg|Persiaran|Lebuh|Lebuhraya)\s+[A-Za-z0-9\s]+/i,
    /([A-Za-z\s]+(?:Street|Road|Avenue|Boulevard))/i
  ];
  
  for (const pattern of streetPatterns) {
    const match = cleaned.match(pattern);
    if (match) {
      return match[0].trim();
    }
  }
  
  // If no pattern found, take first meaningful line (excluding building name)
  const lines = cleaned.split(',').map(l => l.trim()).filter(l => l);
  if (lines.length > 0) {
    return lines[0];
  }
  
  return undefined;
}

/**
 * Extract unit number
 */
function extractUnitNo(text: string): string | undefined {
  const unitPatterns = [
    /Unit\s+(\d+[A-Z]?)/i,
    /No\.?\s*(\d+[A-Z]?)/i,
    /Level\s+\d+[A-Z]?,\s*Unit\s+(\d+[A-Z]?)/i
  ];
  
  for (const pattern of unitPatterns) {
    const match = text.match(pattern);
    if (match && match[1]) {
      return match[1];
    }
  }
  
  return undefined;
}

/**
 * Parse full address text into components
 * 
 * @param addressText - Full address string from parser
 * @returns Parsed address components
 */
export function parseAddress(addressText?: string): ParsedAddress {
  if (!addressText || !addressText.trim()) {
    return {};
  }

  const text = addressText.trim();
  
  // Extract components in order
  const postcode = extractPostcode(text);
  const state = extractState(text);
  const city = extractCity(text, postcode, state);
  const buildingName = extractBuildingName(text);
  const addressLine1 = extractStreetAddress(text, buildingName);
  const unitNo = extractUnitNo(text);
  
  return {
    buildingName,
    addressLine1,
    city,
    state,
    postcode,
    unitNo
  };
}

/**
 * Parse address and return data suitable for QuickBuildingModal
 */
export function parseAddressForBuilding(addressText?: string): {
  buildingName?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  postcode?: string;
} {
  const parsed = parseAddress(addressText);
  
  return {
    buildingName: parsed.buildingName,
    addressLine1: parsed.addressLine1,
    city: parsed.city,
    state: parsed.state,
    postcode: parsed.postcode
  };
}

