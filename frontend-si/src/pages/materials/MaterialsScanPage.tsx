import React, { useState, useEffect, useRef } from 'react';
import { QrCode, Package, MapPin, History, Loader2, Camera, X, CheckCircle, AlertCircle } from 'lucide-react';
import { Card, Button, TextInput, Skeleton, EmptyState, useToast } from '../../components/ui';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { recordMaterialScan, getScanHistory, lookupMaterialBySerial } from '../../api/inventory';
import { useAuth } from '../../contexts/AuthContext';
import type { Location } from '../../types/api';
import { Html5Qrcode } from 'html5-qrcode';

const getCurrentLocation = (): Promise<Location | null> => {
  return new Promise((resolve) => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          resolve({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
          });
        },
        (error) => {
          console.warn('Error getting location:', error);
          resolve(null);
        },
        { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
      );
    } else {
      resolve(null);
    }
  });
};

export function MaterialsScanPage() {
  const { user } = useAuth();
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const [isScanning, setIsScanning] = useState(false);
  const [cameraError, setCameraError] = useState<string | null>(null);

  const [serialNumber, setSerialNumber] = useState('');
  const [deviceType, setDeviceType] = useState('');
  const [location, setLocation] = useState<Location | null>(null);
  const [materialInfo, setMaterialInfo] = useState<any>(null);
  const [isLookingUp, setIsLookingUp] = useState(false);

  // Fetch scan history
  const { data: scanHistory, isLoading: isLoadingHistory } = useQuery({
    queryKey: ['scanHistory'],
    queryFn: () => getScanHistory({ fromDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString() }),
    enabled: !!user?.id,
  });

  // Lookup material when serial number changes
  useEffect(() => {
    const lookupMaterial = async () => {
      if (!serialNumber.trim() || serialNumber.length < 3) {
        setMaterialInfo(null);
        return;
      }

      setIsLookingUp(true);
      try {
        const material = await lookupMaterialBySerial(serialNumber.trim());
        if (material) {
          // Only allow scanning for serialised materials
          if (!material.isSerialised) {
            setMaterialInfo(null);
            showError('This material is not serialised. Serial scanning is only available for serialised materials.');
            return;
          }
          
          setMaterialInfo(material);
          // Auto-populate device type from material name/description
          if (!deviceType && material.name) {
            setDeviceType(material.name);
          }
        } else {
          setMaterialInfo(null);
        }
      } catch (error) {
        setMaterialInfo(null);
      } finally {
        setIsLookingUp(false);
      }
    };

    // Debounce lookup
    const timeoutId = setTimeout(lookupMaterial, 500);
    return () => clearTimeout(timeoutId);
  }, [serialNumber, deviceType, showError]);

  // Start camera scanner
  const startScanner = async () => {
    try {
      setCameraError(null);
      const scanner = new Html5Qrcode('qr-reader');
      
      // Try back camera first, fallback to any camera
      let cameraId: string | { facingMode: string } = { facingMode: 'environment' };
      
      try {
        // Try to get available cameras
        const devices = await Html5Qrcode.getCameras();
        if (devices && devices.length > 0) {
          // Prefer back camera if available
          const backCamera = devices.find(d => d.label.toLowerCase().includes('back') || d.label.toLowerCase().includes('rear'));
          cameraId = backCamera?.id || devices[0].id;
        }
      } catch (e) {
        // Use facingMode if camera enumeration fails
        cameraId = { facingMode: 'environment' };
      }
      
      await scanner.start(
        cameraId,
        {
          fps: 10,
          qrbox: { width: 250, height: 250 },
          aspectRatio: 1.0,
        },
        (decodedText) => {
          // Successfully scanned
          handleScannedCode(decodedText);
          stopScanner();
        },
        (errorMessage) => {
          // Scan error (ignore, keep scanning)
          // Only log if it's not a "not found" error
          if (!errorMessage.includes('NotFoundException')) {
            console.debug('Scan error:', errorMessage);
          }
        }
      );

      scannerRef.current = scanner;
      setIsScanning(true);
    } catch (error: any) {
      const errorMsg = error.message || 'Failed to start camera';
      const isPermissionDenied = errorMsg.toLowerCase().includes('permission') || 
                                  errorMsg.toLowerCase().includes('denied') ||
                                  errorMsg.toLowerCase().includes('not allowed');
      setCameraError(
        isPermissionDenied 
          ? 'Camera permission denied. Please allow camera access in your browser settings to use the scanner.'
          : errorMsg
      );
      showError(isPermissionDenied ? 'Camera permission denied. Please enable camera access.' : `Camera error: ${errorMsg}`);
      setIsScanning(false);
    }
  };

  // Stop camera scanner
  const stopScanner = () => {
    if (scannerRef.current) {
      scannerRef.current.stop().catch(() => {});
      scannerRef.current.clear();
      scannerRef.current = null;
    }
    setIsScanning(false);
    setCameraError(null);
  };

  // Handle scanned code
  const handleScannedCode = (code: string) => {
    setSerialNumber(code);
    // Material lookup will happen automatically via useEffect
  };

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      stopScanner();
    };
  }, []);

  // Record scan mutation
  const recordScanMutation = useMutation({
    mutationFn: async (scanData: { serialNumber: string; deviceType: string; location: Location | null; materialId?: string }) => {
      if (!user?.id) {
        throw new Error('Authentication required');
      }
      return recordMaterialScan({
        serialNumber: scanData.serialNumber,
        materialId: scanData.materialId,
        deviceType: scanData.deviceType || undefined,
        location: scanData.location || undefined,
      });
    },
    onSuccess: () => {
      showSuccess('Serial number scanned successfully');
      setSerialNumber('');
      setDeviceType('');
      setMaterialInfo(null);
      queryClient.invalidateQueries({ queryKey: ['scanHistory'] });
      stopScanner();
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to record scan');
    },
  });

  const handleScan = async () => {
    if (!serialNumber.trim()) {
      showError('Please enter a serial number');
      return;
    }

    // Validate that material is serialised
    if (materialInfo && !materialInfo.isSerialised) {
      showError('This material is not serialised. Serial scanning is only available for serialised materials.');
      return;
    }

    // Get current location
    const currentLocation = await getCurrentLocation();
    setLocation(currentLocation);

    recordScanMutation.mutate({
      serialNumber: serialNumber.trim(),
      deviceType: deviceType.trim() || materialInfo?.name || undefined,
      location: currentLocation,
      materialId: materialInfo?.id,
    });
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleScan();
    }
  };

  return (
    <div className="p-4 space-y-4">
      <h2 className="text-2xl font-bold text-foreground flex items-center gap-2">
        <QrCode className="h-6 w-6" />
        Material Scanning
      </h2>

      {/* Camera Scanner */}
      <Card className="p-4">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-semibold text-lg flex items-center gap-2">
            <Camera className="h-5 w-5" />
            Camera Scanner
          </h3>
          {isScanning ? (
            <Button onClick={stopScanner} variant="outline" size="sm">
              <X className="h-4 w-4 mr-2" />
              Stop Scanner
            </Button>
          ) : (
            <Button onClick={startScanner} size="sm">
              <Camera className="h-4 w-4 mr-2" />
              Start Scanner
            </Button>
          )}
        </div>
        {isScanning && (
          <div className="relative">
            <div id="qr-reader" className="w-full rounded-lg overflow-hidden bg-black" style={{ minHeight: '300px' }} />
            {cameraError && (
              <div className="absolute inset-0 flex items-center justify-center bg-black/50 rounded-lg">
                <p className="text-white text-sm">{cameraError}</p>
              </div>
            )}
          </div>
        )}
        {!isScanning && !cameraError && (
          <p className="text-sm text-muted-foreground text-center py-8">
            Tap "Start Scanner" to activate camera for barcode/QR code scanning
          </p>
        )}
        {!isScanning && cameraError && (
          <div className="py-6 px-4 text-center">
            <AlertCircle className="h-10 w-10 text-yellow-500 mx-auto mb-3" />
            <p className="text-sm font-medium text-foreground mb-1">Camera Unavailable</p>
            <p className="text-sm text-muted-foreground mb-3">{cameraError}</p>
            <p className="text-xs text-muted-foreground">You can still enter serial numbers manually below.</p>
          </div>
        )}
      </Card>

      {/* Scan Input Card */}
      <Card className="p-4">
        <h3 className="font-semibold text-lg mb-4">Scan Serial Number</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Serial Number *</label>
            <div className="relative">
              <TextInput
                value={serialNumber}
                onChange={(e) => setSerialNumber(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder="Enter or scan serial number"
                className="w-full"
                autoFocus
              />
              {isLookingUp && (
                <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                  <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
                </div>
              )}
            </div>
            {materialInfo && (
              <div className="mt-2 p-3 bg-green-50 border border-green-200 rounded-md">
                <div className="flex items-start gap-2">
                  <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-green-900">Material Found (Serialised)</p>
                    <p className="text-sm text-green-700 mt-1">
                      <strong>Name:</strong> {materialInfo.name}
                    </p>
                    {materialInfo.description && (
                      <p className="text-sm text-green-700">
                        <strong>Description:</strong> {materialInfo.description}
                      </p>
                    )}
                    {materialInfo.categoryName && (
                      <p className="text-sm text-green-700">
                        <strong>Category:</strong> {materialInfo.categoryName}
                      </p>
                    )}
                    {materialInfo.code && (
                      <p className="text-sm text-green-700">
                        <strong>Code:</strong> {materialInfo.code}
                      </p>
                    )}
                    {materialInfo.isSerialised && (
                      <p className="text-xs text-green-600 mt-1 font-medium">
                        ✓ Serialised material - scanning enabled
                      </p>
                    )}
                  </div>
                </div>
              </div>
            )}
            {serialNumber.trim().length >= 3 && !isLookingUp && !materialInfo && (
              <div className="mt-2 p-3 bg-yellow-50 border border-yellow-200 rounded-md">
                <div className="flex items-start gap-2">
                  <AlertCircle className="h-5 w-5 text-yellow-600 mt-0.5" />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-yellow-900">Material Not Found</p>
                    <p className="text-sm text-yellow-700 mt-1">
                      Serial number not found in database. You can still record the scan manually.
                    </p>
                  </div>
                </div>
              </div>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">
              Device Type {materialInfo ? '(Auto-filled)' : '(Optional)'}
            </label>
            <TextInput
              value={deviceType}
              onChange={(e) => setDeviceType(e.target.value)}
              onKeyPress={handleKeyPress}
              placeholder={materialInfo ? materialInfo.name : "e.g., ONU, Router, Fiber"}
              className="w-full"
              disabled={!!materialInfo?.name}
            />
            {materialInfo && (
              <p className="text-xs text-muted-foreground mt-1">
                Auto-populated from material database
              </p>
            )}
          </div>
          {location && (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <MapPin className="h-4 w-4" />
              <span>
                Location: {location.latitude.toFixed(6)}, {location.longitude.toFixed(6)}
              </span>
            </div>
          )}
          <Button
            onClick={handleScan}
            disabled={recordScanMutation.isPending || !serialNumber.trim()}
            className="w-full"
          >
            {recordScanMutation.isPending ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Recording...
              </>
            ) : (
              <>
                <QrCode className="h-4 w-4 mr-2" />
                Record Scan
              </>
            )}
          </Button>
        </div>
      </Card>

      {/* Recent Scans */}
      <Card className="p-4">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-semibold text-lg flex items-center gap-2">
            <History className="h-5 w-5" />
            Recent Scans (Last 7 Days)
          </h3>
        </div>
        {isLoadingHistory ? (
          <div className="space-y-2">
            {[1, 2, 3].map((i) => (
              <div key={i} className="border-b border-border pb-2 last:border-b-0 last:pb-0">
                <div className="flex justify-between items-start">
                  <div className="flex-1 space-y-1">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-3 w-24" />
                  </div>
                  <Skeleton className="h-5 w-16 rounded-md" />
                </div>
              </div>
            ))}
          </div>
        ) : !scanHistory || scanHistory.length === 0 ? (
          <EmptyState
            title="No scans yet"
            description="Scanned serial numbers will appear here"
          />
        ) : (
          <div className="space-y-2">
            {scanHistory.map((scan: any) => (
              <div
                key={scan.id}
                className="border-b border-border pb-2 last:border-b-0 last:pb-0"
              >
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <p className="font-medium">{scan.serialNumber}</p>
                    {scan.deviceType && (
                      <p className="text-sm text-muted-foreground">{scan.deviceType}</p>
                    )}
                    {scan.materialName && (
                      <p className="text-sm text-muted-foreground">Material: {scan.materialName}</p>
                    )}
                    {scan.orderNumber && (
                      <p className="text-sm text-muted-foreground">Order: {scan.orderNumber}</p>
                    )}
                    {scan.scannedAt && (
                      <p className="text-xs text-muted-foreground mt-1">
                        {new Date(scan.scannedAt).toLocaleString()}
                      </p>
                    )}
                  </div>
                  {scan.location && (
                    <MapPin className="h-4 w-4 text-muted-foreground" />
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}
