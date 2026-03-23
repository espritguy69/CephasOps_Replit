import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getEmailTemplates,
  getEmailTemplate,
  createEmailTemplate,
  updateEmailTemplate,
  deleteEmailTemplate,
  renderEmailTemplate,
  type EmailTemplate,
  type CreateEmailTemplateDto,
  type UpdateEmailTemplateDto,
} from '../api/emailTemplates';
import { useToast } from '../components/ui';

/**
 * Hook to fetch email templates list, optionally filtered by direction
 */
export const useEmailTemplates = (direction?: string) => {
  return useQuery({
    queryKey: ['emailTemplates', direction],
    queryFn: () => getEmailTemplates(direction),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single email template
 */
export const useEmailTemplate = (id: string) => {
  return useQuery({
    queryKey: ['emailTemplate', id],
    queryFn: () => getEmailTemplate(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new email template
 */
export const useCreateEmailTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateEmailTemplateDto) => createEmailTemplate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emailTemplates'] });
      showSuccess('Email template created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create email template');
    },
  });
};

/**
 * Hook to update an email template
 */
export const useUpdateEmailTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEmailTemplateDto }) =>
      updateEmailTemplate(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['emailTemplates'] });
      queryClient.invalidateQueries({ queryKey: ['emailTemplate', variables.id] });
      showSuccess('Email template updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update email template');
    },
  });
};

/**
 * Hook to delete an email template
 */
export const useDeleteEmailTemplate = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteEmailTemplate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['emailTemplates'] });
      showSuccess('Email template deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete email template');
    },
  });
};

/**
 * Hook to render email template with placeholders
 */
export const useRenderEmailTemplate = () => {
  const { showError } = useToast();

  return useMutation({
    mutationFn: ({ id, placeholders }: { id: string; placeholders: Record<string, string> }) =>
      renderEmailTemplate(id, placeholders),
    onError: (error: any) => {
      showError(error?.message || 'Failed to render email template');
    },
  });
};

