import React, { useState } from 'react';
import { X } from 'lucide-react';
import { Modal, Button, TextInput, useToast } from '../ui';
import { createOrder } from '../../api/orders';
import type { Order } from '../../types/orders';

interface CreateOrderModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: (order: Order) => void;
}

interface OrderFormData {
  partnerId: string;
  orderTypeId: string;
  serviceId: string;
  ticketId: string;
  externalRef: string;
  priority: string;
  buildingId: string;
  unitNo: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  postcode: string;
  latitude: string;
  longitude: string;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  orderNotesInternal: string;
  partnerNotes: string;
  requestedAppointmentAt: string;
  appointmentDate: string;
  appointmentWindowFrom: string;
  appointmentWindowTo: string;
}

const CreateOrderModal: React.FC<CreateOrderModalProps> = ({ isOpen, onClose, onSuccess }) => {
  const { showSuccess, showError } = useToast();
  const [loading, setLoading] = useState<boolean>(false);
  const [formData, setFormData] = useState<OrderFormData>({
    partnerId: '',
    orderTypeId: '',
    serviceId: '',
    ticketId: '',
    externalRef: '',
    priority: 'Normal',
    buildingId: '',
    unitNo: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    postcode: '',
    latitude: '',
    longitude: '',
    customerName: '',
    customerPhone: '',
    customerEmail: '',
    orderNotesInternal: '',
    partnerNotes: '',
    requestedAppointmentAt: '',
    appointmentDate: '',
    appointmentWindowFrom: '',
    appointmentWindowTo: ''
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>): void => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();

    try {
      setLoading(true);
      
      // Convert time strings to TimeSpan format (HH:mm:ss)
      const convertTimeToTimeSpan = (timeString: string): string | undefined => {
        if (!timeString) return undefined;
        // timeString is in format "HH:mm", convert to "HH:mm:ss"
        return timeString + ':00';
      };

      // Prepare data for API
      const orderData: Partial<Order> = {
        partnerId: formData.partnerId || '00000000-0000-0000-0000-000000000000', // Required, use empty GUID if not provided
        orderTypeId: formData.orderTypeId || '00000000-0000-0000-0000-000000000000', // Required, use empty GUID if not provided
        serviceId: formData.serviceId || '',
        ticketId: formData.ticketId || null,
        externalRef: formData.externalRef || null,
        priority: formData.priority || null,
        buildingId: formData.buildingId || '00000000-0000-0000-0000-000000000000', // Required, use empty GUID if not provided
        unitNo: formData.unitNo || null,
        addressLine1: formData.addressLine1 || '',
        addressLine2: formData.addressLine2 || null,
        city: formData.city || '',
        state: formData.state || '',
        postcode: formData.postcode || '',
        latitude: formData.latitude ? parseFloat(formData.latitude) : null,
        longitude: formData.longitude ? parseFloat(formData.longitude) : null,
        customerName: formData.customerName || '',
        customerPhone: formData.customerPhone || '',
        customerEmail: formData.customerEmail || null,
        orderNotesInternal: formData.orderNotesInternal || null,
        partnerNotes: formData.partnerNotes || null,
        requestedAppointmentAt: formData.requestedAppointmentAt ? new Date(formData.requestedAppointmentAt).toISOString() : null,
        appointmentDate: formData.appointmentDate || new Date().toISOString().split('T')[0],
        appointmentWindowFrom: convertTimeToTimeSpan(formData.appointmentWindowFrom) || '00:00:00',
        appointmentWindowTo: convertTimeToTimeSpan(formData.appointmentWindowTo) || '23:59:59'
      };

      const createdOrder = await createOrder(orderData);
      showSuccess('Order created successfully!');
      onSuccess?.(createdOrder);
      onClose();
      
      // Reset form
      setFormData({
        partnerId: '',
        orderTypeId: '',
        serviceId: '',
        ticketId: '',
        externalRef: '',
        priority: 'Normal',
        buildingId: '',
        unitNo: '',
        addressLine1: '',
        addressLine2: '',
        city: '',
        state: '',
        postcode: '',
        latitude: '',
        longitude: '',
        customerName: '',
        customerPhone: '',
        customerEmail: '',
        orderNotesInternal: '',
        partnerNotes: '',
        requestedAppointmentAt: '',
        appointmentDate: '',
        appointmentWindowFrom: '',
        appointmentWindowTo: ''
      });
    } catch (error) {
      console.error('Error creating order:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to create order';
      showError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose}>
      <div className="bg-white rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-white border-b px-6 py-4 flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900">Create Order</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
            disabled={loading}
          >
            <X className="h-6 w-6" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Basic Information */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-900 border-b pb-2">Basic Information</h3>
            
            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Service ID *"
                name="serviceId"
                value={formData.serviceId}
                onChange={handleChange}
                required
              />
              <div className="space-y-2">
                <label className="text-sm font-medium leading-none">
                  Priority
                </label>
                <select
                  name="priority"
                  value={formData.priority}
                  onChange={handleChange}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                >
                  <option value="Low">Low</option>
                  <option value="Normal">Normal</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Partner ID"
                name="partnerId"
                value={formData.partnerId}
                onChange={handleChange}
                type="text"
              />
              <TextInput
                label="Order Type ID"
                name="orderTypeId"
                value={formData.orderTypeId}
                onChange={handleChange}
                type="text"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Ticket ID"
                name="ticketId"
                value={formData.ticketId}
                onChange={handleChange}
              />
              <TextInput
                label="External Ref"
                name="externalRef"
                value={formData.externalRef}
                onChange={handleChange}
              />
            </div>
          </div>

          {/* Address Information */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-900 border-b pb-2">Address Information</h3>
            
            <TextInput
              label="Address Line 1 *"
              name="addressLine1"
              value={formData.addressLine1}
              onChange={handleChange}
              required
            />
            
            <TextInput
              label="Address Line 2"
              name="addressLine2"
              value={formData.addressLine2}
              onChange={handleChange}
            />

            <div className="grid grid-cols-3 gap-4">
              <TextInput
                label="City *"
                name="city"
                value={formData.city}
                onChange={handleChange}
                required
              />
              <TextInput
                label="State *"
                name="state"
                value={formData.state}
                onChange={handleChange}
                required
              />
              <TextInput
                label="Postcode *"
                name="postcode"
                value={formData.postcode}
                onChange={handleChange}
                required
              />
            </div>

            <div className="grid grid-cols-3 gap-4">
              <TextInput
                label="Building ID"
                name="buildingId"
                value={formData.buildingId}
                onChange={handleChange}
              />
              <TextInput
                label="Unit No"
                name="unitNo"
                value={formData.unitNo}
                onChange={handleChange}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Latitude"
                name="latitude"
                value={formData.latitude}
                onChange={handleChange}
                type="number"
                step="any"
              />
              <TextInput
                label="Longitude"
                name="longitude"
                value={formData.longitude}
                onChange={handleChange}
                type="number"
                step="any"
              />
            </div>
          </div>

          {/* Customer Information */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-900 border-b pb-2">Customer Information</h3>
            
            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Customer Name *"
                name="customerName"
                value={formData.customerName}
                onChange={handleChange}
                required
              />
              <TextInput
                label="Customer Phone *"
                name="customerPhone"
                value={formData.customerPhone}
                onChange={handleChange}
                required
              />
            </div>

            <TextInput
              label="Customer Email"
              name="customerEmail"
              value={formData.customerEmail}
              onChange={handleChange}
              type="email"
            />
          </div>

          {/* Appointment Information */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-900 border-b pb-2">Appointment Information</h3>
            
            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Appointment Date *"
                name="appointmentDate"
                value={formData.appointmentDate}
                onChange={handleChange}
                type="date"
                required
              />
              <TextInput
                label="Requested Appointment At"
                name="requestedAppointmentAt"
                value={formData.requestedAppointmentAt}
                onChange={handleChange}
                type="datetime-local"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Window From"
                name="appointmentWindowFrom"
                value={formData.appointmentWindowFrom}
                onChange={handleChange}
                type="time"
              />
              <TextInput
                label="Window To"
                name="appointmentWindowTo"
                value={formData.appointmentWindowTo}
                onChange={handleChange}
                type="time"
              />
            </div>
          </div>

          {/* Notes */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-900 border-b pb-2">Notes</h3>
            
            <div className="space-y-2">
              <label className="text-sm font-medium leading-none">
                Internal Notes
              </label>
              <textarea
                name="orderNotesInternal"
                value={formData.orderNotesInternal}
                onChange={handleChange}
                rows={3}
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium leading-none">
                Partner Notes
              </label>
              <textarea
                name="partnerNotes"
                value={formData.partnerNotes}
                onChange={handleChange}
                rows={3}
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              />
            </div>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={loading}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={loading}
            >
              {loading ? 'Creating...' : 'Create Order'}
            </Button>
          </div>
        </form>
      </div>
    </Modal>
  );
};

export default CreateOrderModal;

