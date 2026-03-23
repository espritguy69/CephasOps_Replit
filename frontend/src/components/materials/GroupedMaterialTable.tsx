import React, { useState, useEffect } from 'react';
import { ChevronsDown, ChevronsUp, Package, Edit, Trash2 } from 'lucide-react';
import { Button, Card, EmptyState, StatusBadge } from '../ui';
import { cn } from '@/lib/utils';
import type { Material } from '../../types/inventory';

interface GroupedMaterialTableProps {
  materials: Material[];
  onEdit: (material: Material) => void;
  onDelete: (material: Material) => void;
  searchQuery?: string;
  emptyMessage?: string;
  canManage?: boolean;
  /** When false, hides the Unit Price / Cost column (RBAC v3 field-level). Default true. */
  showCostColumn?: boolean;
  selectedRows?: string[];
  onSelectionChange?: (rows: string[]) => void;
}

interface GroupedMaterialData {
  categoryKey: string;
  categoryName: string;
  materials: Material[];
}

export const GroupedMaterialTable: React.FC<GroupedMaterialTableProps> = ({
  materials,
  onEdit,
  onDelete,
  searchQuery = '',
  emptyMessage = 'No materials found',
  canManage = false,
  showCostColumn = true,
  selectedRows = [],
  onSelectionChange
}) => {
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set());
  const [allExpanded, setAllExpanded] = useState(false);

  // Group materials by category
  const groupedMaterials = React.useMemo(() => {
    const groups = new Map<string, Material[]>();
    
    materials.forEach((material) => {
      const categoryName = material.category || material.categoryName || material.materialCategoryName || 'Uncategorized';
      if (!groups.has(categoryName)) {
        groups.set(categoryName, []);
      }
      groups.get(categoryName)!.push(material);
    });

    // Convert to array and sort alphabetically
    return Array.from(groups.entries())
      .map(([categoryName, materials]) => ({
        categoryKey: categoryName,
        categoryName,
        materials
      }))
      .sort((a, b) => a.categoryName.localeCompare(b.categoryName));
  }, [materials]);

  // Expand all groups by default if there are few groups
  useEffect(() => {
    if (groupedMaterials.length <= 5 && expandedGroups.size === 0) {
      const allKeys = new Set(groupedMaterials.map(g => g.categoryKey));
      setExpandedGroups(allKeys);
      setAllExpanded(true);
    }
  }, [groupedMaterials.length]);

  const toggleAllGroups = () => {
    if (allExpanded) {
      setExpandedGroups(new Set());
      setAllExpanded(false);
    } else {
      const allKeys = new Set(groupedMaterials.map(g => g.categoryKey));
      setExpandedGroups(allKeys);
      setAllExpanded(true);
    }
  };

  const toggleGroup = (categoryKey: string) => {
    setExpandedGroups(prev => {
      const newSet = new Set(prev);
      if (newSet.has(categoryKey)) {
        newSet.delete(categoryKey);
      } else {
        newSet.add(categoryKey);
      }
      setAllExpanded(newSet.size === groupedMaterials.length);
      return newSet;
    });
  };

  const highlightText = (text: string, query: string): React.ReactNode => {
    if (!query || !text) return text;
    const parts = text.split(new RegExp(`(${query})`, 'gi'));
    return parts.map((part, i) => 
      part.toLowerCase() === query.toLowerCase() ? (
        <mark key={i} className="bg-yellow-200 dark:bg-yellow-800">{part}</mark>
      ) : part
    );
  };

  const getStockLevelColor = (current?: number, min?: number): string => {
    if (!current || !min) return 'text-gray-500';
    const ratio = current / min;
    if (ratio < 1) return 'text-red-600 font-semibold';
    if (ratio >= 1 && ratio <= 1.5) return 'text-amber-600';
    return 'text-green-600';
  };

  const getStockLevelText = (material: Material): string => {
    const current = material.minStockLevel; // Using minStockLevel as current for now
    const min = material.reorderPoint || material.minStockLevel;
    if (!current && !min) return '—';
    return `${current || 0}/${min || 0}`;
  };

  const getSupplierText = (material: Material): string => {
    const partnerNames = (material.partnerNames && material.partnerNames.length > 0)
      ? material.partnerNames
      : (material.partnerName ? [material.partnerName] : []);
    
    if (partnerNames.length === 0) return '—';
    if (partnerNames.length === 1) return partnerNames[0];
    return `${partnerNames[0]} +${partnerNames.length - 1} more`;
  };

  if (groupedMaterials.length === 0) {
    return <EmptyState title={emptyMessage} />;
  }

  return (
    <div className="space-y-3">
      {/* Controls */}
      <div className="flex items-center justify-between">
        <div className="text-xs text-muted-foreground">
          {groupedMaterials.length} categor{groupedMaterials.length !== 1 ? 'ies' : 'y'} • {materials.length} total material{materials.length !== 1 ? 's' : ''}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={toggleAllGroups}
          className="text-xs"
        >
          {allExpanded ? (
            <>
              <ChevronsUp className="h-3 w-3 mr-1" />
              Collapse All
            </>
          ) : (
            <>
              <ChevronsDown className="h-3 w-3 mr-1" />
              Expand All
            </>
          )}
        </Button>
      </div>

      {/* Groups */}
      <div className="space-y-3">
        {groupedMaterials.map((group) => {
          const isExpanded = expandedGroups.has(group.categoryKey);
          const filteredMaterials = searchQuery
            ? group.materials.filter(m => 
                (m.description || m.name || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
                (m.code || m.itemCode || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
                (m.category || m.categoryName || '').toLowerCase().includes(searchQuery.toLowerCase())
              )
            : group.materials;

          if (filteredMaterials.length === 0 && searchQuery) return null;

          return (
            <Card key={group.categoryKey} className="overflow-hidden">
              {/* Category Header */}
              <div
                className="flex items-center justify-between p-3 bg-muted/30 cursor-pointer hover:bg-muted/50 transition-colors"
                onClick={() => toggleGroup(group.categoryKey)}
              >
                <div className="flex items-center gap-2">
                  <Package className="h-4 w-4 text-muted-foreground" />
                  <h3 className="text-sm font-semibold">{group.categoryName}</h3>
                  <span className="text-xs text-muted-foreground">
                    ({filteredMaterials.length} item{filteredMaterials.length !== 1 ? 's' : ''})
                  </span>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-6 w-6 p-0"
                  onClick={(e) => {
                    e.stopPropagation();
                    toggleGroup(group.categoryKey);
                  }}
                >
                  {isExpanded ? (
                    <ChevronsUp className="h-3 w-3" />
                  ) : (
                    <ChevronsDown className="h-3 w-3" />
                  )}
                </Button>
              </div>

              {/* Materials Table */}
              {isExpanded && (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="bg-muted/50 border-b">
                      <tr>
                        {onSelectionChange && (
                          <th className="px-3 py-2 text-left w-10">
                            <input
                              type="checkbox"
                              checked={filteredMaterials.every(m => selectedRows.includes(m.id))}
                              onChange={(e) => {
                                if (e.target.checked) {
                                  const newSelected = [...selectedRows, ...filteredMaterials.map(m => m.id)];
                                  onSelectionChange(newSelected);
                                } else {
                                  const newSelected = selectedRows.filter(id => !filteredMaterials.some(m => m.id === id));
                                  onSelectionChange(newSelected);
                                }
                              }}
                              className="h-3 w-3"
                            />
                          </th>
                        )}
                        <th className="px-3 py-2 text-left font-medium">Material</th>
                        <th className="px-3 py-2 text-left font-medium w-20">Unit</th>
                        {showCostColumn && <th className="px-3 py-2 text-right font-medium w-24">Unit Price</th>}
                        <th className="px-3 py-2 text-left font-medium w-28">Stock Level</th>
                        <th className="px-3 py-2 text-left font-medium w-32">Supplier</th>
                        <th className="px-3 py-2 text-center font-medium w-24">Status</th>
                        {canManage && (
                          <th className="px-3 py-2 text-right font-medium w-20">Actions</th>
                        )}
                      </tr>
                    </thead>
                    <tbody>
                      {filteredMaterials.length === 0 ? (
                        <tr>
                          <td colSpan={onSelectionChange ? 8 : 7} className="px-3 py-8 text-center text-xs text-muted-foreground">
                            No materials found in this category
                          </td>
                        </tr>
                      ) : (
                        filteredMaterials.map((material) => {
                          const isSelected = selectedRows.includes(material.id);
                          const materialName = material.description || material.name || '—';
                          const materialCode = material.code || material.itemCode || '—';
                          
                          return (
                            <tr
                              key={material.id}
                              className={cn(
                                "border-b hover:bg-muted/30 transition-colors",
                                isSelected && "bg-primary/5"
                              )}
                            >
                              {onSelectionChange && (
                                <td className="px-3 py-2">
                                  <input
                                    type="checkbox"
                                    checked={isSelected}
                                    onChange={(e) => {
                                      if (e.target.checked) {
                                        onSelectionChange([...selectedRows, material.id]);
                                      } else {
                                        onSelectionChange(selectedRows.filter(id => id !== material.id));
                                      }
                                    }}
                                    onClick={(e) => e.stopPropagation()}
                                    className="h-3 w-3"
                                  />
                                </td>
                              )}
                              <td className="px-3 py-2">
                                <div className="space-y-0.5">
                                  <div className="text-sm font-medium">
                                    {searchQuery ? highlightText(materialName, searchQuery) : materialName}
                                  </div>
                                  <div className="text-xs text-muted-foreground truncate max-w-md">
                                    {searchQuery ? highlightText(materialCode, searchQuery) : materialCode}
                                  </div>
                                </div>
                              </td>
                              <td className="px-3 py-2 text-xs">
                                {material.unit || material.unitOfMeasure || '—'}
                              </td>
                              {showCostColumn && (
                                <td className="px-3 py-2 text-right text-xs">
                                  {material.unitPrice || material.defaultCost
                                    ? `RM ${((material.unitPrice || material.defaultCost || 0) as number).toFixed(2)}`
                                    : '—'}
                                </td>
                              )}
                              <td className="px-3 py-2">
                                <span className={cn("text-xs", getStockLevelColor(material.minStockLevel, material.reorderPoint))}>
                                  {getStockLevelText(material)}
                                </span>
                              </td>
                              <td className="px-3 py-2 text-xs text-muted-foreground truncate max-w-[120px]">
                                {getSupplierText(material)}
                              </td>
                              <td className="px-3 py-2 text-center">
                                <StatusBadge
                                  variant={material.isActive ? 'success' : 'secondary'}
                                >
                                  {material.isActive ? 'Active' : 'Inactive'}
                                </StatusBadge>
                              </td>
                              {canManage && (
                                <td className="px-3 py-2">
                                  <div className="flex items-center justify-end gap-1">
                                    <button
                                      onClick={(e) => {
                                        e.stopPropagation();
                                        onEdit(material);
                                      }}
                                      className="p-1 hover:bg-primary/20 rounded text-primary"
                                      title="Edit"
                                    >
                                      <Edit className="h-3 w-3" />
                                    </button>
                                    <button
                                      onClick={(e) => {
                                        e.stopPropagation();
                                        if (window.confirm('Are you sure you want to delete this material?')) {
                                          onDelete(material);
                                        }
                                      }}
                                      className="p-1 hover:bg-destructive/20 rounded text-destructive"
                                      title="Delete"
                                    >
                                      <Trash2 className="h-3 w-3" />
                                    </button>
                                  </div>
                                </td>
                              )}
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          );
        })}
      </div>
    </div>
  );
};

