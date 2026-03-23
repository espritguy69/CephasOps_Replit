import React from 'react';
import { User } from 'lucide-react';
import { useDraggable } from '@dnd-kit/core';
import { Card, Button } from '../ui';
import type { ServiceInstaller } from '../../types/serviceInstallers';

interface InstallerPanelProps {
  installers: ServiceInstaller[];
  bulkMode: boolean;
  selectedOrdersCount: number;
  onBulkAssign: (installerId: string) => void;
  className?: string;
}

interface DraggableInstallerProps {
  installer: ServiceInstaller;
}

/**
 * Draggable installer card for drag-and-drop assignment
 */
const DraggableInstaller: React.FC<DraggableInstallerProps> = ({ installer }) => {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: `installer-${installer.id}`,
    data: {
      type: 'installer',
      installerId: installer.id,
      installerName: installer.name
    }
  });

  const style = transform
    ? {
        transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
        opacity: isDragging ? 0.5 : 1
      }
    : { opacity: isDragging ? 0.5 : 1 };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className={`px-3 py-2 bg-primary text-primary-foreground rounded-md cursor-move text-sm font-medium hover:bg-primary/90 transition-colors ${
        isDragging ? 'opacity-50 shadow-lg' : ''
      }`}
    >
      <User className="h-4 w-4 inline mr-2" />
      {installer.name}
    </div>
  );
};

/**
 * InstallerPanel component
 * Shows available installers that can be dragged onto orders
 */
const InstallerPanel: React.FC<InstallerPanelProps> = ({
  installers,
  bulkMode,
  selectedOrdersCount,
  onBulkAssign,
  className
}) => {
  return (
    <Card className={`w-64 p-4 h-fit sticky top-4 ${className || ''}`}>
      <h2 className="font-semibold mb-4 text-sm">Available Installers</h2>
      <div className="space-y-3">
        {installers.length === 0 ? (
          <div className="text-sm text-muted-foreground text-center py-4">
            No active installers
          </div>
        ) : (
          installers.map((installer) => (
            <div key={installer.id} className="space-y-2">
              <DraggableInstaller installer={installer} />
              {bulkMode && selectedOrdersCount > 0 && (
                <Button
                  size="sm"
                  variant="outline"
                  className="w-full text-xs"
                  onClick={() => onBulkAssign(installer.id)}
                >
                  Assign {selectedOrdersCount} to {installer.name}
                </Button>
              )}
            </div>
          ))
        )}
      </div>
      
      {/* Legend */}
      <div className="mt-6 pt-4 border-t">
        <h3 className="text-xs font-semibold text-muted-foreground mb-2">Card Colors</h3>
        <div className="space-y-1 text-xs">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded bg-gray-100 border border-gray-300" />
            <span>Pending</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded bg-purple-100 border border-purple-300" />
            <span>AWO Orders</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded bg-yellow-100 border border-yellow-300" />
            <span>No WO Number</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded bg-green-50 border border-green-300" />
            <span>Regular WO</span>
          </div>
        </div>
      </div>
    </Card>
  );
};

export default InstallerPanel;

