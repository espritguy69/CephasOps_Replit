import React, { useEffect, useRef, useState } from 'react';
import { Camera, X, Check } from 'lucide-react';
import { Button } from '../ui';
import { cn } from '../../lib/utils';

interface BarcodeScannerProps {
  onScan: (barcode: string) => void;
  onClose?: () => void;
  isOpen: boolean;
  className?: string;
}

/**
 * Barcode Scanner Component
 * Uses html5-qrcode library for barcode scanning
 * Supports both camera scanning and manual input
 */
export const BarcodeScanner: React.FC<BarcodeScannerProps> = ({
  onScan,
  onClose,
  isOpen,
  className
}) => {
  const [isScanning, setIsScanning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [manualBarcode, setManualBarcode] = useState('');
  const scannerRef = useRef<HTMLDivElement>(null);
  const html5QrCodeRef = useRef<any>(null);

  useEffect(() => {
    if (!isOpen) {
      stopScanning();
      return;
    }

    return () => {
      stopScanning();
    };
  }, [isOpen]);

  const startScanning = async () => {
    try {
      setError(null);
      setIsScanning(true);

      // Dynamically import html5-qrcode to avoid loading it if not needed
      const { Html5Qrcode } = await import('html5-qrcode');
      
      if (!scannerRef.current) {
        throw new Error('Scanner container not found');
      }

      const html5QrCode = new Html5Qrcode(scannerRef.current.id);
      html5QrCodeRef.current = html5QrCode;

      await html5QrCode.start(
        { facingMode: 'environment' }, // Use back camera on mobile
        {
          fps: 10,
          qrbox: { width: 250, height: 250 }
        },
        (decodedText: string) => {
          // Successfully scanned
          handleScanSuccess(decodedText);
        },
        (errorMessage: string) => {
          // Ignore scanning errors (they're expected while scanning)
        }
      );
    } catch (err: any) {
      console.error('Error starting scanner:', err);
      setError(err.message || 'Failed to start camera. Please check permissions.');
      setIsScanning(false);
    }
  };

  const stopScanning = async () => {
    if (html5QrCodeRef.current) {
      try {
        await html5QrCodeRef.current.stop();
        await html5QrCodeRef.current.clear();
      } catch (err) {
        // Ignore errors when stopping
      }
      html5QrCodeRef.current = null;
    }
    setIsScanning(false);
  };

  const handleScanSuccess = (barcode: string) => {
    stopScanning();
    onScan(barcode);
    if (onClose) {
      onClose();
    }
  };

  const handleManualSubmit = () => {
    if (manualBarcode.trim()) {
      onScan(manualBarcode.trim());
      setManualBarcode('');
      if (onClose) {
        onClose();
      }
    }
  };

  if (!isOpen) return null;

  return (
    <div className={cn('space-y-3', className)}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Camera className="h-4 w-4 text-primary" />
          <span className="text-sm font-medium">Scan Barcode</span>
        </div>
        {onClose && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              stopScanning();
              onClose();
            }}
            className="h-6 w-6 p-0"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      {/* Camera Scanner */}
      <div className="space-y-2">
        {!isScanning ? (
          <Button
            variant="outline"
            size="sm"
            onClick={startScanning}
            className="w-full"
          >
            <Camera className="h-4 w-4 mr-2" />
            Start Camera Scanner
          </Button>
        ) : (
          <div className="space-y-2">
            <div
              id="barcode-scanner"
              ref={scannerRef}
              className="w-full rounded-lg border border-border overflow-hidden bg-black"
              style={{ minHeight: '250px' }}
            />
            <Button
              variant="outline"
              size="sm"
              onClick={stopScanning}
              className="w-full"
            >
              Stop Scanning
            </Button>
          </div>
        )}

        {error && (
          <div className="text-xs text-destructive bg-destructive/10 p-2 rounded border border-destructive/20">
            {error}
          </div>
        )}
      </div>

      {/* Manual Input */}
      <div className="space-y-2 border-t border-border pt-3">
        <label className="text-xs font-medium">Or Enter Barcode Manually</label>
        <div className="flex gap-2">
          <input
            type="text"
            value={manualBarcode}
            onChange={(e) => setManualBarcode(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                handleManualSubmit();
              }
            }}
            placeholder="Enter barcode..."
            className="flex-1 h-8 rounded-md border border-input bg-background px-3 py-1 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          />
          <Button
            size="sm"
            onClick={handleManualSubmit}
            disabled={!manualBarcode.trim()}
          >
            <Check className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
};

