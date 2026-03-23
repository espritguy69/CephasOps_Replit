import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getWhatsAppTemplates,
  getWhatsAppTemplate,
  createWhatsAppTemplate,
  updateWhatsAppTemplate,
  updateWhatsAppTemplateApprovalStatus,
  deleteWhatsAppTemplate,
  type WhatsAppTemplate,
  type CreateWhatsAppTemplateDto,
  type UpdateWhatsAppTemplateDto,
  type WhatsAppTemplateFilters,
} from '../api/whatsappTemplates';
import { useToast } from '../components/ui';

/**
 * Hook to fetch WhatsApp templates list
 */
export const useWhatsAppTemplates = (filters: WhatsAppTemplateFilters) => {
  return useQuery({
    queryKey: ['whatsappTemplates', filters],
    queryFn: () => getWhatsAppTemplates(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single WhatsApp template
 */
export const useWhatsAppTemplate = (id: string) => {
  return useQuery({
    queryKey: ['whatsappTemplate', id],
    queryFn: () => getWhatsAppTemplate(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new WhatsApp template
 */
export const useCreateWhatsAppTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ companyId, data }: { companyId: string; data: CreateWhatsAppTemplateDto }) =>
      createWhatsAppTemplate(companyId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['whatsappTemplates'] });
      showSuccess('WhatsApp template created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create WhatsApp template');
    },
  });
};

/**
 * Hook to update a WhatsApp template
 */
export const useUpdateWhatsAppTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateWhatsAppTemplateDto }) =>
      updateWhatsAppTemplate(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['whatsappTemplates'] });
      queryClient.invalidateQueries({ queryKey: ['whatsappTemplate', variables.id] });
      showSuccess('WhatsApp template updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update WhatsApp template');
    },
  });
};

/**
 * Hook to update WhatsApp template approval status
 */
export const useUpdateWhatsAppTemplateApprovalStatus = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, approvalStatus }: { id: string; approvalStatus: string }) =>
      updateWhatsAppTemplateApprovalStatus(id, approvalStatus),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['whatsappTemplates'] });
      queryClient.invalidateQueries({ queryKey: ['whatsappTemplate', variables.id] });
      showSuccess('Approval status updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update approval status');
    },
  });
};

/**
 * Hook to delete a WhatsApp template
 */
export const useDeleteWhatsAppTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteWhatsAppTemplate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['whatsappTemplates'] });
      showSuccess('WhatsApp template deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete WhatsApp template');
    },
  });
};

