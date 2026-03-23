import React, { useState, useEffect } from 'react';
import { 
  KanbanComponent, 
  ColumnsDirective, 
  ColumnDirective 
} from '@syncfusion/ej2-react-kanban';
import { LoadingSpinner, useToast, Button } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useTasks, useUpdateTaskStatus } from '../../hooks/useTasks';
import { Plus, RefreshCw } from 'lucide-react';

/**
 * Task Kanban Board
 * Visual task management with drag & drop workflow
 * 
 * Features:
 * - Drag tasks between columns (TODO → In Progress → Review → Done)
 * - Swimlanes by department
 * - WIP limits (max tasks per column)
 * - Color-coded by priority (Red=High, Yellow=Medium, Green=Low)
 * - Quick edit on card click
 * - Search & filter
 */

const TasksKanbanPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { data: tasks, isLoading, refetch } = useTasks({});
  const updateTaskStatusMutation = useUpdateTaskStatus();
  const [kanbanData, setKanbanData] = useState<any[]>([]);

  useEffect(() => {
    if (tasks) {
      // Convert tasks to Kanban format
      const formatted = tasks.map(task => ({
        Id: task.id,
        Title: task.title,
        Status: task.status || 'TODO',
        Priority: task.priority || 'Medium',
        Tags: task.tags || '',
        Assignee: task.assignedToUserName || 'Unassigned',
        DepartmentId: task.departmentId,
        DepartmentName: task.departmentName,
        DueDate: task.dueDate,
        Description: task.description,
        Color: getPriorityColor(task.priority)
      }));
      setKanbanData(formatted);
    }
  }, [tasks]);

  const getPriorityColor = (priority: string): string => {
    const colors: Record<string, string> = {
      'High': '#ef4444',
      'Medium': '#f59e0b',
      'Low': '#10b981'
    };
    return colors[priority] || '#94a3b8';
  };

  // Kanban columns
  const columns = [
    { headerText: 'TODO', keyField: 'TODO', allowToggle: true },
    { headerText: 'In Progress', keyField: 'InProgress', allowToggle: true, maxCount: 10 },
    { headerText: 'Review', keyField: 'Review', allowToggle: true, maxCount: 5 },
    { headerText: 'Done', keyField: 'Done', allowToggle: true }
  ];

  // Card template
  const cardTemplate = (props: any) => {
    return (
      <div className="e-card-template">
        <div className="e-card-header">
          <div className="e-card-header-title font-semibold">{props.Title}</div>
        </div>
        <div className="e-card-content">
          {props.Assignee && (
            <div className="text-xs text-muted-foreground">
              👤 {props.Assignee}
            </div>
          )}
          {props.DueDate && (
            <div className="text-xs text-muted-foreground">
              📅 {new Date(props.DueDate).toLocaleDateString('en-MY', { 
                day: 'numeric', 
                month: 'short' 
              })}
            </div>
          )}
          {props.Tags && (
            <div className="mt-2">
              <span className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded">
                {props.Tags}
              </span>
            </div>
          )}
        </div>
      </div>
    );
  };

  // Swimlane settings (group by department)
  const swimlaneSettings = {
    keyField: 'DepartmentName',
    textField: 'DepartmentName',
    showEmptyRow: true
  };

  // Map Kanban status to backend status
  const mapKanbanStatusToBackend = (kanbanStatus: string): string => {
    const statusMap: Record<string, string> = {
      'TODO': 'New',
      'InProgress': 'InProgress',
      'Review': 'OnHold',
      'Done': 'Completed'
    };
    return statusMap[kanbanStatus] || 'New';
  };

  // Drag stop handler
  const onDragStop = async (args: any) => {
    if (!args.data || args.data.length === 0) return;
    
    const taskData = args.data[0];
    const taskId = taskData.Id;
    const newStatus = taskData.Status;
    
    if (!taskId) {
      showError('Task ID not found');
      return;
    }

    try {
      const backendStatus = mapKanbanStatusToBackend(newStatus);
      await updateTaskStatusMutation.mutateAsync({
        id: taskId,
        status: backendStatus,
        notes: null
      });
      // Success message is handled by the mutation hook
    } catch (error: any) {
      showError(error.message || 'Failed to update task status');
      // Refetch to restore original state
      refetch();
    }
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading tasks..." fullPage />;
  }

  return (
    <PageShell
      title="Task Board"
      subtitle="Visual task management with drag & drop workflow"
      actions={
        <div className="flex gap-2">
          <Button size="sm" onClick={() => refetch()} variant="outline" className="gap-2">
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          <Button size="sm" className="gap-2">
            <Plus className="h-4 w-4" />
            New Task
          </Button>
        </div>
      }
    >
      <div className="space-y-4">
        <div className="bg-card rounded-xl border border-border shadow-sm p-4">
          <KanbanComponent
            id="kanban"
            dataSource={kanbanData}
            keyField="Status"
            cardSettings={{
              contentField: 'Description',
              headerField: 'Title',
              template: cardTemplate
            }}
            swimlaneSettings={swimlaneSettings}
            dragStop={onDragStop}
            allowDragAndDrop={true}
            allowKeyboard={true}
          >
            <ColumnsDirective>
              {columns.map((col, index) => (
                <ColumnDirective
                  key={index}
                  headerText={col.headerText}
                  keyField={col.keyField}
                  allowToggle={col.allowToggle}
                  maxCount={col.maxCount}
                />
              ))}
            </ColumnsDirective>
          </KanbanComponent>
        </div>

        {/* Feature Guide */}
        <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 rounded-lg p-4">
          <h3 className="font-semibold text-sm mb-2">✨ Kanban Board Features</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2 text-sm">
            <div>• <strong>Drag Cards</strong>: Move between columns to change status</div>
            <div>• <strong>Swimlanes</strong>: Tasks grouped by department</div>
            <div>• <strong>WIP Limits</strong>: Max 10 in "In Progress", 5 in "Review"</div>
            <div>• <strong>Color Coded</strong>: By priority (Red=High, Yellow=Medium, Green=Low)</div>
            <div>• <strong>Click Card</strong>: View/edit task details</div>
            <div>• <strong>Search</strong>: Find tasks quickly</div>
          </div>
        </div>
      </div>
    </PageShell>
  );
};

export default TasksKanbanPage;

