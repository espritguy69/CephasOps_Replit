import apiClient from './client';
import type { Photo, Location } from '../types/api';

/**
 * Photos API
 * Handles photo uploads for orders
 */

export interface PhotoMetadata {
  location?: Location;
  notes?: string;
  eventType?: string;
}

/**
 * Upload photo for an order
 */
export const uploadOrderPhoto = async (
  orderId: string, 
  photo: File, 
  metadata: PhotoMetadata = {}
): Promise<any> => {
  const formData = new FormData();
  formData.append('photo', photo);
  if (metadata.location) {
    formData.append('latitude', metadata.location.latitude.toString());
    formData.append('longitude', metadata.location.longitude.toString());
    if (metadata.location.accuracy) {
      formData.append('accuracy', metadata.location.accuracy.toString());
    }
  }
  if (metadata.notes) formData.append('notes', metadata.notes);
  if (metadata.eventType) formData.append('eventType', metadata.eventType);

  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';
  const token = localStorage.getItem('authToken');
  
  const response = await fetch(`${API_BASE_URL}/orders/${orderId}/photos`, {
    method: 'POST',
    headers: {
      'Authorization': token ? `Bearer ${token}` : '',
      // Don't set Content-Type for FormData - browser will set it with boundary
    },
    body: formData
  });

  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = `API Error: ${response.status} ${response.statusText}`;
    try {
      const errorData = JSON.parse(errorText);
      errorMessage = errorData.message || errorData.error || errorMessage;
    } catch {
      if (errorText) errorMessage = errorText;
    }
    throw new Error(errorMessage);
  }

  return response.json();
};

/**
 * Get photos for an order
 */
export const getOrderPhotos = async (orderId: string): Promise<Photo[]> => {
  const response = await apiClient.get<Photo[] | { data: Photo[] }>(`/orders/${orderId}/photos`);
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: Photo[] }).data;
  }
  return [];
};

