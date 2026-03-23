import React, { useState, useEffect } from 'react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Plus, GripVertical, Trash2, Loader2, Clock } from 'lucide-react';
import { PageShell } from '@/components/layout';
import { Card, Button, Input, Label, Modal, LoadingSpinner, EmptyState, useToast } from '@/components/ui';
import Switch from '@/components/ui/Switch';
import AlertDialog from '@/components/ui/AlertDialog';
import {
  getTimeSlots,
  createTimeSlot,
  updateTimeSlot,
  deleteTimeSlot,
  reorderTimeSlots,
  seedDefaultTimeSlots,
} from '@/api/timeSlots';
import type { TimeSlot } from '@/types/timeSlots';

interface DragEndEvent {
  active: { id: string | number };
  over: { id: string | number } | null;
}

interface SortableTimeSlotItemProps {
  timeSlot: TimeSlot;
  onToggle: (id: string, isActive: boolean) => void;
  onDelete: (id: string) => void;
}

// Sortable Time Slot Item Component
const SortableTimeSlotItem: React.FC<SortableTimeSlotItemProps> = ({ timeSlot, onToggle, onDelete }) => {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: timeSlot.id });

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="flex items-center gap-4 p-4 bg-white dark:bg-gray-800 border rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700"
    >
      {/* Drag Handle */}
      <div
        {...attributes}
        {...listeners}
        className="cursor-grab active:cursor-grabbing text-muted-foreground hover:text-foreground"
      >
        <GripVertical className="h-5 w-5" />
      </div>

      {/* Time Display */}
      <div className="flex items-center gap-2 flex-1">
        <Clock className="h-4 w-4 text-muted-foreground" />
        <span className="font-medium text-sm">{timeSlot.time}</span>
      </div>

      {/* Active Toggle */}
      <div className="flex items-center gap-2">
        <Label htmlFor={`active-${timeSlot.id}`} className="text-sm">
          Active
        </Label>
        <Switch
          id={`active-${timeSlot.id}`}
          checked={timeSlot.isActive}
          onCheckedChange={(checked) => onToggle(timeSlot.id, checked)}
        />
      </div>

      {/* Delete Button */}
      <Button
        variant="ghost"
        size="icon"
        onClick={() => onDelete(timeSlot.id)}
        className="text-red-600 hover:text-red-700 hover:bg-red-50"
      >
        <Trash2 className="h-4 w-4" />
      </Button>
    </div>
  );
};

const TimeSlotSettingsPage: React.FC = () => {
  const [isAddDialogOpen, setIsAddDialogOpen] = useState<boolean>(false);
  const [newTimeSlot, setNewTimeSlot] = useState<string>('');
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  const [timeSlots, setTimeSlots] = useState<TimeSlot[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [isCreating, setIsCreating] = useState<boolean>(false);
  const [isUpdating, setIsUpdating] = useState<boolean>(false);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);
  const [isReordering, setIsReordering] = useState<boolean>(false);
  const [isSeeding, setIsSeeding] = useState<boolean>(false);
  const { showSuccess, showError } = useToast();

  // Load time slots
  useEffect(() => {
    loadTimeSlots();
  }, []);

  const loadTimeSlots = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getTimeSlots();
      setTimeSlots(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load time slots');
      console.error('Error loading time slots:', err);
    } finally {
      setLoading(false);
    }
  };

  // Drag and drop sensors
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  // Handle drag end
  const handleDragEnd = async (event: DragEndEvent): Promise<void> => {
    const { active, over } = event;

    if (!over || active.id === over.id || !timeSlots.length) return;

    const oldIndex = timeSlots.findIndex((slot) => slot.id === active.id);
    const newIndex = timeSlots.findIndex((slot) => slot.id === over.id);

    if (oldIndex === -1 || newIndex === -1) return;

    // Optimistically reorder in UI
    const reorderedSlots = arrayMove(timeSlots, oldIndex, newIndex);
    setTimeSlots(reorderedSlots);

    // Send reorder request
    try {
      setIsReordering(true);
      const timeSlotIds = reorderedSlots.map((slot) => slot.id);
      await reorderTimeSlots(timeSlotIds);
      showSuccess('Time slots reordered');
    } catch (err) {
      showError(`Failed to reorder time slots: ${(err as Error).message || 'Unknown error'}`);
      // Revert on error
      await loadTimeSlots();
    } finally {
      setIsReordering(false);
    }
  };

  // Handle toggle active status
  const handleToggleActive = async (id: string, isActive: boolean): Promise<void> => {
    try {
      setIsUpdating(true);
      await updateTimeSlot(id, { isActive });
      showSuccess('Time slot updated');
      await loadTimeSlots();
    } catch (err) {
      showError(`Failed to update time slot: ${(err as Error).message || 'Unknown error'}`);
      await loadTimeSlots(); // Reload to revert
    } finally {
      setIsUpdating(false);
    }
  };

  // Handle delete
  const handleDelete = (id: string): void => {
    setDeleteConfirmId(id);
  };

  const confirmDelete = async (): Promise<void> => {
    if (!deleteConfirmId) return;
    
    try {
      setIsDeleting(true);
      await deleteTimeSlot(deleteConfirmId);
      showSuccess('Time slot deleted');
      setDeleteConfirmId(null);
      await loadTimeSlots();
    } catch (err) {
      showError(`Failed to delete time slot: ${(err as Error).message || 'Unknown error'}`);
    } finally {
      setIsDeleting(false);
    }
  };

  // Handle create
  const handleCreate = async (): Promise<void> => {
    if (!newTimeSlot.trim()) {
      showError('Please enter a time');
      return;
    }

    // Validate time format (basic check)
    const timePattern = /^\d{1,2}:\d{2}\s?(AM|PM)$/i;
    if (!timePattern.test(newTimeSlot.trim())) {
      showError("Invalid time format. Use format like '9:00 AM' or '2:30 PM'");
      return;
    }

    // Check for duplicates
    if (timeSlots.some((slot) => slot.time === newTimeSlot.trim())) {
      showError('This time slot already exists');
      return;
    }

    try {
      setIsCreating(true);
      const nextSortOrder = timeSlots.length
        ? Math.max(...timeSlots.map((s) => s.sortOrder)) + 1
        : 0;

      await createTimeSlot({
        time: newTimeSlot.trim(),
        sortOrder: nextSortOrder,
        isActive: true,
      });
      
      showSuccess('Time slot created');
      setIsAddDialogOpen(false);
      setNewTimeSlot('');
      await loadTimeSlots();
    } catch (err) {
      showError(`Failed to create time slot: ${(err as Error).message || 'Unknown error'}`);
    } finally {
      setIsCreating(false);
    }
  };

  // Handle seed defaults
  const handleSeedDefaults = async (): Promise<void> => {
    if (timeSlots.length > 0) {
      showError('Time slots already exist. Delete existing slots first.');
      return;
    }

    try {
      setIsSeeding(true);
      const result = await seedDefaultTimeSlots();
      const count = result?.count || 0;
      showSuccess(`Created ${count} default time slots`);
      await loadTimeSlots();
    } catch (err) {
      showError(`Failed to seed defaults: ${(err as Error).message || 'Unknown error'}`);
    } finally {
      setIsSeeding(false);
    }
  };

  // Loading state
  if (loading) {
    return (
      <PageShell title="Time Slot Settings" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Time Slots' }]}>
        <LoadingSpinner message="Loading time slots..." />
      </PageShell>
    );
  }

  const sortedTimeSlots = [...timeSlots].sort((a, b) => a.sortOrder - b.sortOrder);

  return (
    <PageShell
      title="Time Slot Settings"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Time Slots' }]}
      actions={
        <div className="flex gap-2">
          {timeSlots.length === 0 && (
            <Button
              variant="outline"
              onClick={handleSeedDefaults}
              disabled={isSeeding}
            >
              {isSeeding ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Seeding...
                </>
              ) : (
                'Seed Defaults'
              )}
            </Button>
          )}
          <Button onClick={() => setIsAddDialogOpen(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            Add Time Slot
          </Button>
        </div>
      }
    >
      <div className="flex-1 p-2 max-w-7xl mx-auto">
      <Card
        title="Time Slot Settings"
        subtitle="Manage available appointment time slots. Drag to reorder."
      >
        {timeSlots.length === 0 ? (
          <div className="text-center py-12">
            <Clock className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
            <h3 className="font-semibold text-lg mb-2">No Time Slots</h3>
            <p className="text-muted-foreground mb-4">
              Get started by adding your first time slot or seeding defaults
            </p>
            <div className="flex gap-2 justify-center">
              <Button variant="outline" onClick={handleSeedDefaults}>
                Seed Defaults
              </Button>
              <Button onClick={() => setIsAddDialogOpen(true)}>
                <Plus className="h-4 w-4 mr-2" />
                Add Time Slot
              </Button>
            </div>
          </div>
        ) : (
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}
          >
            <SortableContext
              items={sortedTimeSlots.map((slot) => slot.id)}
              strategy={verticalListSortingStrategy}
            >
              <div className="space-y-2">
                {sortedTimeSlots.map((timeSlot) => (
                  <SortableTimeSlotItem
                    key={timeSlot.id}
                    timeSlot={timeSlot}
                    onToggle={handleToggleActive}
                    onDelete={handleDelete}
                  />
                ))}
              </div>
            </SortableContext>
          </DndContext>
        )}
      </Card>

      {/* Add Time Slot Dialog */}
      <Modal
        isOpen={isAddDialogOpen}
        onClose={() => setIsAddDialogOpen(false)}
        title="Add Time Slot"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Enter a time in 12-hour format (e.g., "9:00 AM", "2:30 PM")
          </p>
          <div className="space-y-2">
            <Label htmlFor="time">Time</Label>
            <Input
              id="time"
              placeholder="e.g., 9:00 AM"
              value={newTimeSlot}
              onChange={(e) => setNewTimeSlot(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  handleCreate();
                }
              }}
            />
          </div>
          <div className="flex justify-end gap-2 mt-4">
            <Button variant="outline" onClick={() => setIsAddDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={isCreating}>
              {isCreating ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Adding...
                </>
              ) : (
                'Add'
              )}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        open={deleteConfirmId !== null}
        onOpenChange={(open) => !open && setDeleteConfirmId(null)}
        title="Delete Time Slot?"
        description="This action cannot be undone. This will permanently delete the time slot and it will no longer appear in the schedule view."
      >
        <div className="flex justify-end gap-2">
          <AlertDialog.Cancel onClick={() => setDeleteConfirmId(null)}>
            Cancel
          </AlertDialog.Cancel>
          <AlertDialog.Action
            onClick={confirmDelete}
            variant="destructive"
          >
            {isDeleting ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Deleting...
              </>
            ) : (
              'Delete'
            )}
          </AlertDialog.Action>
        </div>
      </AlertDialog>
      </div>
    </PageShell>
  );
};

export default TimeSlotSettingsPage;

