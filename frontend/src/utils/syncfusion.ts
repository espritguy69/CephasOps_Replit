/**
 * Syncfusion License Configuration
 * Registers the Syncfusion license key from environment variables
 */

import { registerLicense } from '@syncfusion/ej2-base';

/**
 * Initialize Syncfusion license
 * License key should be set in VITE_SYNCFUSION_LICENSE_KEY environment variable
 * For development, falls back to a default key if env var is not set
 */
export const initializeSyncfusion = () => {
  // Prefer environment variable, fallback to Enterprise Edition Community license key
  const licenseKey = import.meta.env.VITE_SYNCFUSION_LICENSE_KEY || 
    (import.meta.env.DEV ? 'Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH1edHVUR2BcVkVzWEBWYEg=' : null);
  
  if (licenseKey) {
    try {
      registerLicense(licenseKey);
      if (import.meta.env.DEV) {
        console.log('✅ Syncfusion Enterprise Edition license registered successfully');
      }
    } catch (error) {
      console.error('❌ Failed to register Syncfusion license:', error);
    }
  } else {
    // Only show warning in development
    if (import.meta.env.DEV) {
      console.warn('⚠️ Syncfusion license key not found. Some features may be limited.');
    }
  }
};

