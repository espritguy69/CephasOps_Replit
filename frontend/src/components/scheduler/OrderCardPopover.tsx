import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  MoreVertical, 
  ExternalLink, 
  FileText, 
  AlertTriangle,
  CheckCircle2,
  Clock,
  XCircle
} from 'lucide-react';
import { Dropdown, DropdownItem, Modal, Button, Select, Textarea, Label, useToast } from '../ui';
import { blockOrder } from '../../api/scheduler';
import { updateOrder, addOrderNote } from '../../api/orders';
import type { CalendarSlot } from '../../types/scheduler';

interface OrderCardPopoverProps {
  slot: CalendarSlot;
  onStatusChange?: () => void;
  onBlocked?: () => void;
  onNoteAdded?: () => void;
}

/**
 * OrderCardPopover component
 * Provides action menu for order cards in scheduler
 */
const OrderCardPopover: React.FC<OrderCardPopoverProps> = ({
  slot,
  onStatusChange,
  onBlocked,
  onNoteAdded
}) => {
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const [showBlockModal, setShowBlockModal] = useState<boolean>(false);
  const [showNoteModal, setShowNoteModal] = useState<boolean>(false);
  const [blockerType, setBlockerType] = useState<string>('');
  const [blockerDescription, setBlockerDescription] = useState<string>('');
  const [note, setNote] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  const blockerTypes = [
    { value: 'Customer', label: 'Customer' },
    { value: 'Building', label: 'Building' },
    { value: 'Network', label: 'Network' },
    { value: 'SI', label: 'Service Installer' },
    { value: 'Weather', label: 'Weather' },
    { value: 'Other', label: 'Other' }
  ];

  const orderStatuses = [
    { value: 'Pending', label: 'Pending' },
    { value: 'Assigned', label: 'Assigned' },
    { value: 'OnTheWay', label: 'On The Way' },
    { value: 'MetCustomer', label: 'Met Customer' },
    { value: 'OrderCompleted', label: 'Completed' },
    { value: 'Blocker', label: 'Blocked' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];

  const handleOpenOrderDetail = (): void => {
    navigate(`/orders/${slot.orderId}`);
  };

  const handleChangeStatus = async (newStatus: string): Promise<void> => {
    try {
      setIsSubmitting(true);
      await updateOrder(slot.orderId, { status: newStatus });
      showSuccess('Order status updated');
      onStatusChange?.();
    } catch (err) {
      showError((err as Error).message || 'Failed to update order status');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleBlockOrder = async (): Promise<void> => {
    if (!blockerType || !blockerDescription.trim()) {
      showError('Please select blocker type and provide description');
      return;
    }

    try {
      setIsSubmitting(true);
      await blockOrder(slot.orderId, {
        blockerType,
        description: blockerDescription
      });
      showSuccess('Order blocked successfully');
      setShowBlockModal(false);
      setBlockerType('');
      setBlockerDescription('');
      onBlocked?.();
    } catch (err) {
      showError((err as Error).message || 'Failed to block order');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAddNote = async (): Promise<void> => {
    if (!note.trim()) {
      showError('Please enter a note');
      return;
    }

    try {
      setIsSubmitting(true);
      await addOrderNote(slot.orderId, note);
      showSuccess('Note added successfully');
      setShowNoteModal(false);
      setNote('');
      onNoteAdded?.();
    } catch (err) {
      showError((err as Error).message || 'Failed to add note');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <Dropdown
        trigger={
          <button className="p-1 hover:bg-gray-100 rounded">
            <MoreVertical className="h-4 w-4" />
          </button>
        }
        placement="bottom-right"
      >
        <DropdownItem onClick={handleOpenOrderDetail}>
          <ExternalLink className="h-4 w-4 mr-2" />
          Open Order Detail
        </DropdownItem>
        
        <DropdownItem divider />
        
        <DropdownItem onClick={() => setShowNoteModal(true)}>
          <FileText className="h-4 w-4 mr-2" />
          Add Note
        </DropdownItem>
        
        <DropdownItem divider />
        
        <DropdownItem onClick={() => setShowBlockModal(true)}>
          <AlertTriangle className="h-4 w-4 mr-2" />
          Mark as Blocked
        </DropdownItem>
        
        <DropdownItem divider />
        
        <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">
          Change Status
        </div>
        {orderStatuses.map(status => (
          <DropdownItem
            key={status.value}
            onClick={() => handleChangeStatus(status.value)}
            disabled={isSubmitting || slot.orderStatus === status.value}
          >
            {status.value === 'OrderCompleted' && <CheckCircle2 className="h-4 w-4 mr-2" />}
            {status.value === 'OnTheWay' && <Clock className="h-4 w-4 mr-2" />}
            {status.value === 'Blocker' && <XCircle className="h-4 w-4 mr-2" />}
            {!['OrderCompleted', 'OnTheWay', 'Blocker'].includes(status.value) && (
              <div className="h-4 w-4 mr-2" />
            )}
            {status.label}
          </DropdownItem>
        ))}
      </Dropdown>

      {/* Block Order Modal */}
      <Modal
        isOpen={showBlockModal}
        onClose={() => {
          setShowBlockModal(false);
          setBlockerType('');
          setBlockerDescription('');
        }}
        title="Block Order"
      >
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="blockerType">Blocker Type *</Label>
            <Select
              id="blockerType"
              value={blockerType}
              onChange={(e) => setBlockerType(e.target.value)}
              options={[
                { value: '', label: 'Select blocker type' },
                ...blockerTypes
              ]}
            />
          </div>
          
          <div className="space-y-2">
            <Label htmlFor="blockerDescription">Description *</Label>
            <Textarea
              id="blockerDescription"
              value={blockerDescription}
              onChange={(e) => setBlockerDescription(e.target.value)}
              placeholder="Describe why this order is blocked..."
              rows={4}
            />
          </div>
          
          <div className="flex justify-end gap-2 mt-4">
            <Button
              variant="outline"
              onClick={() => {
                setShowBlockModal(false);
                setBlockerType('');
                setBlockerDescription('');
              }}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button
              onClick={handleBlockOrder}
              disabled={isSubmitting || !blockerType || !blockerDescription.trim()}
            >
              {isSubmitting ? 'Blocking...' : 'Block Order'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Add Note Modal */}
      <Modal
        isOpen={showNoteModal}
        onClose={() => {
          setShowNoteModal(false);
          setNote('');
        }}
        title="Add Note"
      >
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="note">Note</Label>
            <Textarea
              id="note"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Enter note..."
              rows={4}
            />
          </div>
          
          <div className="flex justify-end gap-2 mt-4">
            <Button
              variant="outline"
              onClick={() => {
                setShowNoteModal(false);
                setNote('');
              }}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button
              onClick={handleAddNote}
              disabled={isSubmitting || !note.trim()}
            >
              {isSubmitting ? 'Adding...' : 'Add Note'}
            </Button>
          </div>
        </div>
      </Modal>
    </>
  );
};

export default OrderCardPopover;

