import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getRoles,
  getRole,
  createRole,
  updateRole,
  deleteRole,
  getPermissions,
  assignPermissionsToRole,
  type CreateRoleRequest,
  type UpdateRoleRequest,
  type AssignPermissionsRequest,
} from '../api/rbac';
import { useToast } from '../components/ui';

/**
 * Hook to fetch roles list
 */
export const useRoles = () => {
  return useQuery({
    queryKey: ['roles'],
    queryFn: () => getRoles(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single role
 */
export const useRole = (id: string) => {
  return useQuery({
    queryKey: ['role', id],
    queryFn: () => getRole(id),
    enabled: !!id,
  });
};

/**
 * Hook to fetch permissions list
 */
export const usePermissions = () => {
  return useQuery({
    queryKey: ['permissions'],
    queryFn: () => getPermissions(),
    staleTime: 10 * 60 * 1000, // 10 minutes - permissions don't change often
  });
};

/**
 * Hook to create a new role
 */
export const useCreateRole = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateRoleRequest) => createRole(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      showSuccess('Role created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create role');
    },
  });
};

/**
 * Hook to update a role
 */
export const useUpdateRole = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateRoleRequest }) =>
      updateRole(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      queryClient.invalidateQueries({ queryKey: ['role', variables.id] });
      showSuccess('Role updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update role');
    },
  });
};

/**
 * Hook to delete a role
 */
export const useDeleteRole = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteRole(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      showSuccess('Role deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete role');
    },
  });
};

/**
 * Hook to assign permissions to role
 */
export const useAssignPermissionsToRole = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ roleId, data }: { roleId: string; data: AssignPermissionsRequest }) =>
      assignPermissionsToRole(roleId, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      queryClient.invalidateQueries({ queryKey: ['role', variables.roleId] });
      showSuccess('Permissions assigned successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to assign permissions');
    },
  });
};

