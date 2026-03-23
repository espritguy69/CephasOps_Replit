import React, { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getMaterials } from '../../api/inventory';
import { Button, Label, Modal, Select } from '../ui';
import type { Material } from '../../types/inventory';

/**
 * Modal to map an unmatched parsed material name to a Material (creates alias).
 * Used in Parser Review/Edit and parser-origin Order Detail.
 */
export const MatchMaterialModal: React.FC<{
  open: boolean;
  parsedName: string;
  selectedMaterialId: string;
  onSelectedMaterialIdChange: (id: string) => void;
  onClose: () => void;
  onSave: () => void | Promise<void>;
  saving: boolean;
}> = ({ open, parsedName, selectedMaterialId, onSelectedMaterialIdChange, onClose, onSave, saving }) => {
  const { data: materials = [], isLoading } = useQuery<Material[]>({
    queryKey: ['materials', 'active'],
    queryFn: () => getMaterials({ isActive: true }),
    enabled: open
  });
  const options = useMemo(
    () =>
      materials.map((m) => ({
        value: m.id,
        label: [m.itemCode || m.code, m.description || m.name].filter(Boolean).join(' — ') || m.id
      })),
    [materials]
  );
  return (
    <Modal open={open} onClose={onClose} title={`Match material: "${parsedName}"`} size="sm">
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Select the Material master to map this parsed name to. Future drafts will resolve automatically.
        </p>
        <div>
          <Label>Material</Label>
          {isLoading ? (
            <div className="text-sm text-muted-foreground">Loading materials...</div>
          ) : (
            <Select
              value={selectedMaterialId}
              onChange={(e) => onSelectedMaterialIdChange(e.target.value)}
              options={options}
              placeholder="Select material..."
            />
          )}
        </div>
        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button
            type="button"
            onClick={() => onSave()}
            disabled={saving || !selectedMaterialId}
          >
            {saving ? 'Saving...' : 'Save alias'}
          </Button>
        </div>
      </div>
    </Modal>
  );
};
