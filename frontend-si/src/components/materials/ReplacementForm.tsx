import React, { useState, useRef } from 'react';
import { RefreshCw, X, Loader2, AlertCircle, QrCode, Camera, CheckCircle, XCircle } from 'lucide-react';
import { Button, TextInput, Textarea, useToast } from '../ui';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { recordMaterialReplacement, type RecordReplacementRequest } from '../../api/si-app';
import { uploadOrderPhoto } from '../../api/photos';
import { useAuth } from '../../contexts/AuthContext';
import type { Location } from '../../types/api';

interface ReplacementFormProps {
  orderId: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

const REPLACEMENT_REASONS = [
  'Faulty ONU',
  'Faulty Router',
  'LOSi (Loss of Signal)',
  'LOBi (Loss of Beacon)',
  'Customer Request',
  'Other',
];

export function ReplacementForm({
  orderId,
  isOpen,
  onClose,
  onSuccess,
}: ReplacementFormProps) {
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();

  const { user, serviceInstaller } = useAuth();
  const [oldSerialNumber, setOldSerialNumber] = useState('');
  const [newSerialNumber, setNewSerialNumber] = useState('');
  const [replacementReason, setReplacementReason] = useState('');
  const [customReason, setCustomReason] = useState('');
  const [notes, setNotes] = useState('');
  const [photos, setPhotos] = useState<File[]>([]);
  const [photoPreviews, setPhotoPreviews] = useState<string[]>([]);
  const [oldSerialValidated, setOldSerialValidated] = useState(false);
  const [newSerialValidated, setNewSerialValidated] = useState(false);
  const [scanningOldSerial, setScanningOldSerial] = useState(false);
  const [scanningNewSerial, setScanningNewSerial] = useState(false);
  const cameraInputRef = useRef<HTMLInputElement>(null);
  const photoInputRef = useRef<HTMLInputElement>(null);

  const replacementMutation = useMutation({
    mutationFn: async (data: RecordReplacementRequest) => {
      return recordMaterialReplacement(orderId, data);
    },
    onSuccess: (data) => {
      showSuccess(data.message || 'Replacement recorded successfully!');
      setOldSerialNumber('');
      setNewSerialNumber('');
      setReplacementReason('');
      setCustomReason('');
      setNotes('');
      setPhotos([]);
      setPhotoPreviews([]);
      setOldSerialValidated(false);
      setNewSerialValidated(false);
      queryClient.invalidateQueries({ queryKey: ['jobDetails', orderId] });
      queryClient.invalidateQueries({ queryKey: ['materialUsage', orderId] });
      queryClient.invalidateQueries({ queryKey: ['orderMaterials', orderId] });
      onSuccess?.();
      onClose();
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to record replacement.');
    },
  });

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

  const handleStartScan = async (type: 'old' | 'new') => {
    try {
      if (type === 'old') {
        setScanningOldSerial(true);
      } else {
        setScanningNewSerial(true);
      }

      // Dynamically import html5-qrcode
      const { Html5Qrcode } = await import('html5-qrcode');
      
      const scannerId = type === 'old' ? 'old-serial-scanner' : 'new-serial-scanner';
      const html5QrCode = new Html5Qrcode(scannerId);

      await html5QrCode.start(
        { facingMode: 'environment' },
        {
          fps: 10,
          qrbox: { width: 250, height: 250 }
        },
        (decodedText: string) => {
          // Successfully scanned
          if (type === 'old') {
            setOldSerialNumber(decodedText);
            setOldSerialValidated(false);
          } else {
            setNewSerialNumber(decodedText);
            setNewSerialValidated(false);
          }
          html5QrCode.stop().then(() => {
            html5QrCode.clear();
            if (type === 'old') {
              setScanningOldSerial(false);
            } else {
              setScanningNewSerial(false);
            }
          }).catch(() => {});
        },
        (errorMessage: string) => {
          // Ignore scanning errors
        }
      );
    } catch (err: any) {
      console.error('Error starting scanner:', err);
      showError(err.message || 'Failed to start camera. Please check permissions.');
      if (type === 'old') {
        setScanningOldSerial(false);
      } else {
        setScanningNewSerial(false);
      }
    }
  };

  const handleStopScan = async (type: 'old' | 'new') => {
    try {
      const scannerId = type === 'old' ? 'old-serial-scanner' : 'new-serial-scanner';
      const { Html5Qrcode } = await import('html5-qrcode');
      const html5QrCode = new Html5Qrcode(scannerId);
      await html5QrCode.stop();
      await html5QrCode.clear();
    } catch (err) {
      // Ignore errors when stopping
    }
    if (type === 'old') {
      setScanningOldSerial(false);
    } else {
      setScanningNewSerial(false);
    }
  };

  const handlePhotoSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    if (files.length === 0) return;

    const newPhotos = [...photos, ...files.slice(0, 5 - photos.length)];
    setPhotos(newPhotos);

    // Create previews
    const newPreviews = newPhotos.map(file => URL.createObjectURL(file));
    setPhotoPreviews(newPreviews);

    // Reset input
    if (photoInputRef.current) {
      photoInputRef.current.value = '';
    }
  };

  const handleRemovePhoto = (index: number) => {
    const newPhotos = photos.filter((_, i) => i !== index);
    const newPreviews = photoPreviews.filter((_, i) => i !== index);
    
    // Revoke object URLs to prevent memory leaks
    URL.revokeObjectURL(photoPreviews[index]);
    
    setPhotos(newPhotos);
    setPhotoPreviews(newPreviews);
  };

  const handleUploadPhotos = async (): Promise<string[]> => {
    if (photos.length === 0) return [];

    const location = await getCurrentLocation();
    const uploadedPhotoIds: string[] = [];

    for (const photo of photos) {
      try {
        const result = await uploadOrderPhoto(orderId, photo, {
          location,
          notes: `Replacement evidence - ${replacementReason || 'N/A'}`,
          eventType: 'Replacement'
        });
        uploadedPhotoIds.push(result.id || result.photoId || '');
      } catch (err: any) {
        console.error('Error uploading photo:', err);
        showError(`Failed to upload photo: ${err.message}`);
      }
    }

    return uploadedPhotoIds;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!oldSerialNumber.trim()) {
      showError('Please enter the old (faulty) device serial number.');
      return;
    }

    if (!newSerialNumber.trim()) {
      showError('Please enter the new (replacement) device serial number.');
      return;
    }

    if (oldSerialNumber.trim() === newSerialNumber.trim()) {
      showError('Old and new serial numbers must be different.');
      return;
    }

    if (!replacementReason) {
      showError('Please select a replacement reason.');
      return;
    }

    const finalReason = replacementReason === 'Other' 
      ? customReason.trim() 
      : replacementReason;

    if (!finalReason) {
      showError('Please provide a replacement reason.');
      return;
    }

    // Upload photos first (optional)
    let photoIds: string[] = [];
    if (photos.length > 0) {
      try {
        photoIds = await handleUploadPhotos();
      } catch (err: any) {
        showError(`Photo upload failed: ${err.message}`);
        return;
      }
    }

    replacementMutation.mutate({
      oldSerialNumber: oldSerialNumber.trim(),
      newSerialNumber: newSerialNumber.trim(),
      replacementReason: finalReason,
      notes: notes.trim() || undefined,
    });
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-background rounded-lg shadow-lg w-full max-w-md mx-4 p-6 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold flex items-center gap-2">
            <RefreshCw className="h-5 w-5 text-primary" />
            Record Material Replacement
          </h2>
          <button
            onClick={onClose}
            className="text-muted-foreground hover:text-foreground"
            disabled={replacementMutation.isPending}
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-md">
          <div className="flex items-start gap-2">
            <AlertCircle className="h-4 w-4 text-blue-600 mt-0.5 flex-shrink-0" />
            <p className="text-sm text-blue-800">
              This form is for Assurance orders only. Record the faulty device and its replacement.
            </p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Old Serial Number */}
          <div>
            <label className="block text-sm font-medium mb-1">
              Old (Faulty) Device Serial Number <span className="text-red-500">*</span>
            </label>
            <div className="space-y-2">
              <div className="flex gap-2">
                <TextInput
                  placeholder="Enter or scan old device serial"
                  value={oldSerialNumber}
                  onChange={(e) => {
                    setOldSerialNumber(e.target.value);
                    setOldSerialValidated(false);
                  }}
                  disabled={replacementMutation.isPending || scanningOldSerial}
                  required
                  className="flex-1"
                />
                {!scanningOldSerial ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => handleStartScan('old')}
                    disabled={replacementMutation.isPending}
                    className="shrink-0"
                  >
                    <QrCode className="h-4 w-4" />
                  </Button>
                ) : (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => handleStopScan('old')}
                    disabled={replacementMutation.isPending}
                    className="shrink-0"
                  >
                    <X className="h-4 w-4" />
                  </Button>
                )}
              </div>
              {scanningOldSerial && (
                <div
                  id="old-serial-scanner"
                  className="w-full rounded-lg border border-border overflow-hidden bg-black"
                  style={{ minHeight: '200px' }}
                />
              )}
              {oldSerialNumber && oldSerialValidated && (
                <div className="flex items-center gap-1 text-xs text-green-600">
                  <CheckCircle className="h-3 w-3" />
                  Serial number validated
                </div>
              )}
            </div>
          </div>

          {/* New Serial Number */}
          <div>
            <label className="block text-sm font-medium mb-1">
              New (Replacement) Device Serial Number <span className="text-red-500">*</span>
            </label>
            <div className="space-y-2">
              <div className="flex gap-2">
                <TextInput
                  placeholder="Enter or scan new device serial"
                  value={newSerialNumber}
                  onChange={(e) => {
                    setNewSerialNumber(e.target.value);
                    setNewSerialValidated(false);
                  }}
                  disabled={replacementMutation.isPending || scanningNewSerial}
                  required
                  className="flex-1"
                />
                {!scanningNewSerial ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => handleStartScan('new')}
                    disabled={replacementMutation.isPending}
                    className="shrink-0"
                  >
                    <QrCode className="h-4 w-4" />
                  </Button>
                ) : (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => handleStopScan('new')}
                    disabled={replacementMutation.isPending}
                    className="shrink-0"
                  >
                    <X className="h-4 w-4" />
                  </Button>
                )}
              </div>
              {scanningNewSerial && (
                <div
                  id="new-serial-scanner"
                  className="w-full rounded-lg border border-border overflow-hidden bg-black"
                  style={{ minHeight: '200px' }}
                />
              )}
              {newSerialNumber && newSerialValidated && (
                <div className="flex items-center gap-1 text-xs text-green-600">
                  <CheckCircle className="h-3 w-3" />
                  Serial number validated
                </div>
              )}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">
              Replacement Reason <span className="text-red-500">*</span>
            </label>
            <select
              value={replacementReason}
              onChange={(e) => setReplacementReason(e.target.value)}
              disabled={replacementMutation.isPending}
              className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
              required
            >
              <option value="">Select reason...</option>
              {REPLACEMENT_REASONS.map((reason) => (
                <option key={reason} value={reason}>
                  {reason}
                </option>
              ))}
            </select>
          </div>

          {replacementReason === 'Other' && (
            <div>
              <label className="block text-sm font-medium mb-1">
                Custom Reason <span className="text-red-500">*</span>
              </label>
              <TextInput
                placeholder="Please specify the reason"
                value={customReason}
                onChange={(e) => setCustomReason(e.target.value)}
                disabled={replacementMutation.isPending}
                required
              />
            </div>
          )}

          <div>
            <label className="block text-sm font-medium mb-1">Notes (Optional)</label>
            <Textarea
              placeholder="Additional details about the replacement..."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={replacementMutation.isPending}
              rows={3}
            />
          </div>

          {/* Photo Upload */}
          <div>
            <label className="block text-sm font-medium mb-1">
              Evidence Photos (Optional)
            </label>
            <div className="space-y-2">
              <input
                ref={photoInputRef}
                type="file"
                accept="image/*"
                capture="environment"
                multiple
                onChange={handlePhotoSelect}
                disabled={replacementMutation.isPending || photos.length >= 5}
                className="hidden"
              />
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => photoInputRef.current?.click()}
                  disabled={replacementMutation.isPending || photos.length >= 5}
                  className="flex items-center gap-2"
                >
                  <Camera className="h-4 w-4" />
                  {photos.length >= 5 ? 'Max 5 photos' : 'Add Photo'}
                </Button>
                {photos.length > 0 && (
                  <span className="text-xs text-muted-foreground self-center">
                    {photos.length} photo{photos.length !== 1 ? 's' : ''} selected
                  </span>
                )}
              </div>
              
              {/* Photo Previews */}
              {photoPreviews.length > 0 && (
                <div className="grid grid-cols-3 gap-2">
                  {photoPreviews.map((preview, index) => (
                    <div key={index} className="relative group">
                      <img
                        src={preview}
                        alt={`Preview ${index + 1}`}
                        className="w-full h-24 object-cover rounded border border-border"
                      />
                      <button
                        type="button"
                        onClick={() => handleRemovePhoto(index)}
                        disabled={replacementMutation.isPending}
                        className="absolute top-1 right-1 bg-red-500 text-white rounded-full p-1 opacity-0 group-hover:opacity-100 transition-opacity"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="flex gap-2 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={replacementMutation.isPending}
              className="flex-1"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={
                replacementMutation.isPending ||
                !oldSerialNumber.trim() ||
                !newSerialNumber.trim() ||
                !replacementReason ||
                (replacementReason === 'Other' && !customReason.trim())
              }
              className="flex-1"
            >
              {replacementMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  Recording...
                </>
              ) : (
                <>
                  <RefreshCw className="h-4 w-4 mr-2" />
                  Record Replacement
                </>
              )}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

