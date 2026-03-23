import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getDocumentTemplates,
  getDocumentTemplate,
  createDocumentTemplate,
  updateDocumentTemplate,
  deleteDocumentTemplate,
  activateDocumentTemplate,
  duplicateDocumentTemplate,
  getPlaceholderDefinitions,
  getCarboneStatus,
  type DocumentTemplateFilters,
  type CreateDocumentTemplateRequest,
  type UpdateDocumentTemplateRequest,
} from '../api/documentTemplates';
import { useToast } from '../components/ui';

/**
 * Hook to fetch document templates list
 */
export const useDocumentTemplates = (filters: DocumentTemplateFilters = {}) => {
  return useQuery({
    queryKey: ['documentTemplates', filters],
    queryFn: () => getDocumentTemplates(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single document template
 */
export const useDocumentTemplate = (id: string) => {
  return useQuery({
    queryKey: ['documentTemplate', id],
    queryFn: () => getDocumentTemplate(id),
    enabled: !!id,
  });
};

/**
 * Hook to fetch placeholder definitions for a document type
 */
export const usePlaceholderDefinitions = (documentType: string) => {
  return useQuery({
    queryKey: ['placeholderDefinitions', documentType],
    queryFn: () => getPlaceholderDefinitions(documentType),
    enabled: !!documentType,
    staleTime: 10 * 60 * 1000, // 10 minutes - placeholders don't change often
  });
};

/**
 * Hook to fetch Carbone engine status
 */
export const useCarboneStatus = () => {
  return useQuery({
    queryKey: ['carboneStatus'],
    queryFn: () => getCarboneStatus(),
    staleTime: 30 * 60 * 1000, // 30 minutes
  });
};

/**
 * Hook to create a new document template
 */
export const useCreateDocumentTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateDocumentTemplateRequest) => createDocumentTemplate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documentTemplates'] });
      showSuccess('Document template created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create document template');
    },
  });
};

/**
 * Hook to update a document template
 */
export const useUpdateDocumentTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateDocumentTemplateRequest }) =>
      updateDocumentTemplate(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['documentTemplates'] });
      queryClient.invalidateQueries({ queryKey: ['documentTemplate', variables.id] });
      showSuccess('Document template updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update document template');
    },
  });
};

/**
 * Hook to delete a document template
 */
export const useDeleteDocumentTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteDocumentTemplate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documentTemplates'] });
      showSuccess('Document template deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete document template');
    },
  });
};

/**
 * Hook to activate a document template
 */
export const useActivateDocumentTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => activateDocumentTemplate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documentTemplates'] });
      showSuccess('Document template activated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to activate document template');
    },
  });
};

/**
 * Hook to duplicate a document template
 */
export const useDuplicateDocumentTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => duplicateDocumentTemplate(id),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['documentTemplates'] });
      queryClient.invalidateQueries({ queryKey: ['documentTemplate', data.id] });
      showSuccess('Document template duplicated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to duplicate document template');
    },
  });
};

