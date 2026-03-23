import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getServiceInstallers,
  getServiceInstaller,
  createServiceInstaller,
  updateServiceInstaller,
  deleteServiceInstaller,
  getServiceInstallerContacts,
  createServiceInstallerContact,
  updateServiceInstallerContact,
  deleteServiceInstallerContact,
  type ServiceInstallerFilters,
  type CreateServiceInstallerRequest,
  type UpdateServiceInstallerRequest,
  type CreateServiceInstallerContactRequest,
  type UpdateServiceInstallerContactRequest,
} from '../api/serviceInstallers';
import { useToast } from '../components/ui';

/**
 * Hook to fetch service installers list
 */
export const useServiceInstallers = (filters: ServiceInstallerFilters = {}) => {
  return useQuery({
    queryKey: ['serviceInstallers', filters],
    queryFn: () => getServiceInstallers(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single service installer
 */
export const useServiceInstaller = (id: string) => {
  return useQuery({
    queryKey: ['serviceInstaller', id],
    queryFn: () => getServiceInstaller(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new service installer
 */
export const useCreateServiceInstaller = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateServiceInstallerRequest) => createServiceInstaller(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['serviceInstallers'] });
      showSuccess('Service installer created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create service installer');
    },
  });
};

/**
 * Hook to update a service installer
 */
export const useUpdateServiceInstaller = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateServiceInstallerRequest }) =>
      updateServiceInstaller(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['serviceInstallers'] });
      queryClient.invalidateQueries({ queryKey: ['serviceInstaller', variables.id] });
      showSuccess('Service installer updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update service installer');
    },
  });
};

/**
 * Hook to delete a service installer
 */
export const useDeleteServiceInstaller = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteServiceInstaller(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['serviceInstallers'] });
      showSuccess('Service installer deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete service installer');
    },
  });
};

/**
 * Hook to fetch contacts for a service installer
 */
export const useServiceInstallerContacts = (siId: string) => {
  return useQuery({
    queryKey: ['serviceInstallerContacts', siId],
    queryFn: () => getServiceInstallerContacts(siId),
    enabled: !!siId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to create a contact for a service installer
 */
export const useCreateServiceInstallerContact = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ siId, data }: { siId: string; data: CreateServiceInstallerContactRequest }) =>
      createServiceInstallerContact(siId, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['serviceInstallerContacts', variables.siId] });
      showSuccess('Contact created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create contact');
    },
  });
};

/**
 * Hook to update a service installer contact
 */
export const useUpdateServiceInstallerContact = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ contactId, data }: { contactId: string; data: UpdateServiceInstallerContactRequest }) =>
      updateServiceInstallerContact(contactId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['serviceInstallerContacts'] });
      showSuccess('Contact updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update contact');
    },
  });
};

/**
 * Hook to delete a service installer contact
 */
export const useDeleteServiceInstallerContact = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (contactId: string) => deleteServiceInstallerContact(contactId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['serviceInstallerContacts'] });
      showSuccess('Contact deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete contact');
    },
  });
};

