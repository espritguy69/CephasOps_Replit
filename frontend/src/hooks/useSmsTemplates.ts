import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getSmsTemplates,
  getSmsTemplate,
  createSmsTemplate,
  updateSmsTemplate,
  deleteSmsTemplate,
  renderSmsMessage,
  type SmsTemplate,
  type CreateSmsTemplateDto,
  type UpdateSmsTemplateDto,
  type SmsTemplateFilters,
} from '../api/smsTemplates';
import { useToast } from '../components/ui';

/**
 * Hook to fetch SMS templates list
 */
export const useSmsTemplates = (filters: SmsTemplateFilters) => {
  return useQuery({
    queryKey: ['smsTemplates', filters],
    queryFn: () => getSmsTemplates(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single SMS template
 */
export const useSmsTemplate = (id: string) => {
  return useQuery({
    queryKey: ['smsTemplate', id],
    queryFn: () => getSmsTemplate(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new SMS template
 */
export const useCreateSmsTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ companyId, data }: { companyId: string; data: CreateSmsTemplateDto }) =>
      createSmsTemplate(companyId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smsTemplates'] });
      showSuccess('SMS template created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create SMS template');
    },
  });
};

/**
 * Hook to update an SMS template
 */
export const useUpdateSmsTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateSmsTemplateDto }) =>
      updateSmsTemplate(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['smsTemplates'] });
      queryClient.invalidateQueries({ queryKey: ['smsTemplate', variables.id] });
      showSuccess('SMS template updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update SMS template');
    },
  });
};

/**
 * Hook to delete an SMS template
 */
export const useDeleteSmsTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteSmsTemplate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smsTemplates'] });
      showSuccess('SMS template deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete SMS template');
    },
  });
};

/**
 * Hook to render SMS message with placeholders
 */
export const useRenderSmsMessage = () => {
  const { showError } = useToast();

  return useMutation({
    mutationFn: ({ id, placeholders }: { id: string; placeholders: Record<string, string> }) =>
      renderSmsMessage(id, placeholders),
    onError: (error: any) => {
      showError(error?.message || 'Failed to render SMS message');
    },
  });
};

