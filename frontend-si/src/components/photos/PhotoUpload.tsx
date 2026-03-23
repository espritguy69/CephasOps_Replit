import React, { useRef, useState } from 'react';
import { Camera, Image, X, Loader2, AlertCircle } from 'lucide-react';
import { Card, Button, useToast } from '../ui';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { uploadOrderPhoto } from '../../api/photos';
import { PhotoGallery } from './PhotoGallery';
import type { Location } from '../../types/api';

interface Photo {
  id: string;
  url?: string;
  photoUrl?: string;
  preview?: string;
  uploading?: boolean;
  failed?: boolean;
}

interface PhotoUploadProps {
  orderId: string;
  existingPhotos?: Photo[];
  maxPhotos?: number;
}

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

export function PhotoUpload({ orderId, existingPhotos = [], maxPhotos = 5 }: PhotoUploadProps) {
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();
  const cameraInputRef = useRef<HTMLInputElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [localPhotos, setLocalPhotos] = useState<Photo[]>([]);
  const [galleryOpen, setGalleryOpen] = useState(false);
  const [currentPhotoIndex, setCurrentPhotoIndex] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const uploadPhotoMutation = useMutation({
    mutationFn: async ({ file, location }: { file: File; location: Location | null }) => {
      return uploadOrderPhoto(orderId, file, { location: location || undefined });
    },
    onSuccess: (data, variables) => {
      showSuccess('Photo uploaded successfully!');
      setLocalPhotos((prev) => prev.filter((p) => p.id !== variables.localId));
      queryClient.invalidateQueries(['jobDetails', orderId]);
      queryClient.invalidateQueries(['jobPhotos', orderId]);
    },
    onError: (err: any, variables) => {
      showError(err.message || 'Failed to upload photo.');
      setLocalPhotos((prev) =>
        prev.map((p) => (p.id === variables.localId ? { ...p, uploading: false, failed: true } : p))
      );
    },
  });

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files || []);
    if (files.length === 0) return;

    const availableSlots = maxPhotos - (existingPhotos.length + localPhotos.length);
    const filesToProcess = files.slice(0, availableSlots);

    if (filesToProcess.length === 0 && files.length > 0) {
      setError(`Maximum ${maxPhotos} photos allowed. Please remove existing photos to upload more.`);
      return;
    }

    setError(null);

    const location = await getCurrentLocation();

    filesToProcess.forEach((file) => {
      const localId = Date.now().toString() + Math.random().toString(36).substring(2, 9);
      const newPhoto: Photo = {
        id: localId,
        file,
        preview: URL.createObjectURL(file),
        uploading: true,
        failed: false,
      };
      setLocalPhotos((prev) => [...prev, newPhoto]);
      uploadPhotoMutation.mutate({ file, location, localId });
    });

    event.target.value = '';
  };

  const removeLocalPhoto = (id: string) => {
    setLocalPhotos((prev) => prev.filter((p) => p.id !== id));
  };

  const allPhotos = [
    ...existingPhotos.map((p) => ({
      id: p.id,
      url: p.url || p.photoUrl,
      uploaded: true,
      uploading: false,
    })),
    ...localPhotos,
  ];

  const openCamera = () => {
    cameraInputRef.current?.click();
  };

  const openFilePicker = () => {
    fileInputRef.current?.click();
  };

  return (
    <Card className="p-4">
      <div className="mb-4">
        <h3 className="font-semibold mb-1">Photos</h3>
        <p className="text-sm text-muted-foreground">
          Upload photos for this job ({allPhotos.length}/{maxPhotos})
        </p>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-destructive/10 text-destructive rounded-lg text-sm flex items-center gap-2">
          <AlertCircle className="h-4 w-4" />
          <span>{error}</span>
        </div>
      )}

      <div className="grid grid-cols-3 gap-3 mb-4">
        {allPhotos.map((photo, index) => (
          <div key={photo.id} className="relative aspect-square rounded-lg overflow-hidden bg-muted">
            <img
              src={photo.url || photo.preview}
              alt={`Job photo ${index + 1}`}
              className="w-full h-full object-cover"
              onClick={() => {
                setCurrentPhotoIndex(index);
                setGalleryOpen(true);
              }}
            />
            {photo.uploading && (
              <div className="absolute inset-0 flex items-center justify-center bg-black/50 text-white">
                <Loader2 className="h-6 w-6 animate-spin" />
              </div>
            )}
            {photo.failed && (
              <div className="absolute inset-0 flex items-center justify-center bg-red-500/70 text-white">
                <AlertCircle className="h-6 w-6" />
              </div>
            )}
            {!photo.uploaded && !photo.uploading && (
              <button
                onClick={() => removeLocalPhoto(photo.id)}
                className="absolute top-1 right-1 p-1 bg-black/50 text-white rounded-full hover:bg-black/70 transition-colors"
                title="Remove photo"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
        ))}

        {allPhotos.length < maxPhotos && (
          <>
            <button
              onClick={openCamera}
              className="flex flex-col items-center justify-center aspect-square border-2 border-dashed border-border rounded-lg text-muted-foreground hover:bg-muted/50 transition-colors"
            >
              <Camera className="h-6 w-6 mb-1" />
              <span className="text-xs">Camera</span>
            </button>
            <input
              type="file"
              accept="image/*"
              capture="environment"
              ref={cameraInputRef}
              onChange={handleFileChange}
              className="hidden"
            />

            <button
              onClick={openFilePicker}
              className="flex flex-col items-center justify-center aspect-square border-2 border-dashed border-border rounded-lg text-muted-foreground hover:bg-muted/50 transition-colors"
            >
              <Image className="h-6 w-6 mb-1" />
              <span className="text-xs">Gallery</span>
            </button>
            <input
              type="file"
              accept="image/*"
              multiple
              ref={fileInputRef}
              onChange={handleFileChange}
              className="hidden"
            />
          </>
        )}
      </div>

      {galleryOpen && (
        <PhotoGallery
          photos={allPhotos.map(p => p.url || p.preview || '').filter(Boolean)}
          initialIndex={currentPhotoIndex}
          onClose={() => setGalleryOpen(false)}
        />
      )}
    </Card>
  );
}

