import React, { useState, useEffect } from 'react';
import { MapPin, RefreshCw, ExternalLink, Loader2, AlertCircle } from 'lucide-react';
import { Button, Card, useToast } from '../ui';
import type { Location } from '../../types/api';

interface LocationDisplayProps {
  onLocationChange?: (location: Location) => void;
  initialLocation?: Location | null;
}

export function LocationDisplay({ onLocationChange, initialLocation }: LocationDisplayProps) {
  const [location, setLocation] = useState<Location | null>(initialLocation || null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { showError } = useToast();

  useEffect(() => {
    if (initialLocation) {
      setLocation(initialLocation);
    }
  }, [initialLocation]);

  const getCurrentLocation = () => {
    setLoading(true);
    setError(null);
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          const newLocation: Location = {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
          };
          setLocation(newLocation);
          if (onLocationChange) {
            onLocationChange(newLocation);
          }
          setLoading(false);
        },
        (err) => {
          console.error('Geolocation error:', err);
          const errorMessage =
            err.code === 1
              ? 'Location permission denied. Please enable location services for this app.'
              : 'Failed to get current location. Please try again.';
          setError(errorMessage);
          showError(errorMessage);
          setLoading(false);
        },
        { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
      );
    } else {
      const noGeoError = 'Geolocation is not supported by this browser.';
      setError(noGeoError);
      showError(noGeoError);
      setLoading(false);
    }
  };

  const openInMaps = () => {
    if (location) {
      window.open(`https://www.google.com/maps/search/?api=1&query=${location.latitude},${location.longitude}`, '_blank');
    }
  };

  return (
    <Card className="p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-semibold text-lg flex items-center gap-2">
          <MapPin className="h-5 w-5 text-primary" />
          Current Location
        </h3>
        <Button
          variant="outline"
          size="sm"
          onClick={getCurrentLocation}
          disabled={loading}
          className="flex items-center gap-1"
        >
          {loading ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <RefreshCw className="h-4 w-4" />
          )}
          {loading ? 'Getting Location...' : 'Refresh'}
        </Button>
      </div>

      {error && (
        <div className="mb-3 p-3 bg-destructive/10 text-destructive rounded-lg text-sm flex items-center gap-2">
          <AlertCircle className="h-4 w-4" />
          <span>{error}</span>
        </div>
      )}

      {location ? (
        <div className="space-y-1 text-sm">
          <p>
            Latitude: <span className="font-medium">{location.latitude?.toFixed(6)}</span>
          </p>
          <p>
            Longitude: <span className="font-medium">{location.longitude?.toFixed(6)}</span>
          </p>
          {location.accuracy && (
            <p>
              Accuracy: <span className="font-medium">±{location.accuracy?.toFixed(2)} m</span>
            </p>
          )}
          <Button
            variant="link"
            size="sm"
            onClick={openInMaps}
            className="p-0 h-auto text-primary flex items-center gap-1"
          >
            Open in Maps <ExternalLink className="h-3 w-3" />
          </Button>
        </div>
      ) : (
        !loading && <p className="text-muted-foreground text-sm">No location data available. Click refresh to get current location.</p>
      )}
    </Card>
  );
}

