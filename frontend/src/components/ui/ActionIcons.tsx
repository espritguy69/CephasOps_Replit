import React from 'react';
import { Eye, Edit, Download, Power, Trash2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import Tooltip from './Tooltip';
import { cn } from '@/lib/utils';

interface ActionIconsProps {
  row: Record<string, any>;
  onView?: (row: Record<string, any>) => void;
  onEdit?: (row: Record<string, any>) => void;
  onDownload?: (row: Record<string, any>) => void;
  onDeactivate?: (row: Record<string, any>) => void;
  onDelete?: (row: Record<string, any>) => void;
  viewPath?: string;
  editPath?: string;
  downloadPath?: string;
  className?: string;
}

/**
 * ActionIcons - Role-based action icons for table rows
 * 
 * Normal users: View, Edit only
 * SuperAdmin/Director/HeadOfDepartment/Supervisor: View, Edit, Deactivate, Delete
 */
const ActionIcons: React.FC<ActionIconsProps> = ({
  row,
  onView,
  onEdit,
  onDownload,
  onDeactivate,
  onDelete,
  viewPath,
  editPath,
  downloadPath,
  className = ''
}) => {
  const navigate = useNavigate();
  const { user } = useAuth();
  
  // Check if user has elevated permissions
  const userRoles = user?.roles || [];
  const isSuperAdmin = userRoles.includes('SuperAdmin');
  const isDirector = userRoles.includes('Director');
  const isHeadOfDepartment = userRoles.includes('HeadOfDepartment');
  const isSupervisor = userRoles.includes('Supervisor');
  const canManage = isSuperAdmin || isDirector || isHeadOfDepartment || isSupervisor;
  
  const handleView = (e: React.MouseEvent): void => {
    e.stopPropagation();
    if (onView) {
      onView(row);
    } else if (viewPath) {
      navigate(viewPath.replace('{id}', row.id));
    }
  };
  
  const handleEdit = (e: React.MouseEvent): void => {
    e.stopPropagation();
    if (onEdit) {
      onEdit(row);
    } else if (editPath) {
      navigate(editPath.replace('{id}', row.id));
    }
  };
  
  const handleDownload = (e: React.MouseEvent): void => {
    e.stopPropagation();
    if (onDownload) {
      onDownload(row);
    } else if (downloadPath) {
      // Handle download logic
      window.open(downloadPath.replace('{id}', row.id), '_blank');
    }
  };
  
  const handleDeactivate = (e: React.MouseEvent): void => {
    e.stopPropagation();
    if (onDeactivate) {
      onDeactivate(row);
    }
  };
  
  const handleDelete = (e: React.MouseEvent): void => {
    e.stopPropagation();
    if (onDelete) {
      onDelete(row);
    }
  };
  
  return (
    <div className={cn("flex items-center gap-1.5", className)}>
      {/* View - Always visible */}
      {(onView || viewPath) && (
        <Tooltip content="View" side="top">
          <button
            onClick={handleView}
            className="p-1 rounded hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
          >
            <Eye className="h-3 w-3" />
          </button>
        </Tooltip>
      )}
      
      {/* Edit - Always visible */}
      {(onEdit || editPath) && (
        <Tooltip content="Edit" side="top">
          <button
            onClick={handleEdit}
            className="p-1 rounded hover:bg-muted text-primary hover:text-primary/80 transition-colors"
          >
            <Edit className="h-3 w-3" />
          </button>
        </Tooltip>
      )}
      
      {/* Download - When provided */}
      {(onDownload || downloadPath) && (
        <Tooltip content="Download" side="top">
          <button
            onClick={handleDownload}
            className="p-1 rounded hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
          >
            <Download className="h-3 w-3" />
          </button>
        </Tooltip>
      )}
      
      {/* Deactivate - Only for HOD/Admin */}
      {canManage && (onDeactivate || row.isActive !== undefined) && (
        <Tooltip content={row.isActive ? 'Deactivate' : 'Activate'} side="top">
          <button
            onClick={handleDeactivate}
            className={cn(
              "p-1 rounded hover:bg-muted transition-colors",
              row.isActive ? "text-yellow-600 hover:text-yellow-700" : "text-green-600 hover:text-green-700"
            )}
          >
            <Power className="h-3 w-3" />
          </button>
        </Tooltip>
      )}
      
      {/* Delete - Only for HOD/Admin */}
      {canManage && onDelete && (
        <Tooltip content="Delete" side="top">
          <button
            onClick={handleDelete}
            className="p-1 rounded hover:bg-red-50 dark:hover:bg-red-900/20 text-red-600 hover:text-red-700 transition-colors"
          >
            <Trash2 className="h-3 w-3" />
          </button>
        </Tooltip>
      )}
    </div>
  );
};

export default ActionIcons;

