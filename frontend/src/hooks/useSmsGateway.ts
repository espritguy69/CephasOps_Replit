import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  registerSmsGateway,
  getActiveSmsGateway,
  getAllSmsGateways,
  deactivateSmsGateway,
  type RegisterSmsGatewayRequest,
} from '../api/smsGateway';
import { useToast } from '../components/ui';

/**
 * Hook to fetch the active SMS Gateway
 */
export const useActiveSmsGateway = () => {
  return useQuery({
    queryKey: ['smsGateway', 'active'],
    queryFn: () => getActiveSmsGateway(),
    staleTime: 30 * 1000, // 30 seconds
  });
};

/**
 * Hook to fetch all SMS Gateways
 */
export const useAllSmsGateways = () => {
  return useQuery({
    queryKey: ['smsGateway', 'all'],
    queryFn: () => getAllSmsGateways(),
    staleTime: 60 * 1000, // 1 minute
  });
};

/**
 * Hook to register an SMS Gateway
 */
export const useRegisterSmsGateway = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: RegisterSmsGatewayRequest) => registerSmsGateway(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smsGateway'] });
      showSuccess('SMS Gateway registered successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to register SMS Gateway');
    },
  });
};

/**
 * Hook to deactivate an SMS Gateway
 */
export const useDeactivateSmsGateway = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deactivateSmsGateway(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['smsGateway'] });
      showSuccess('SMS Gateway deactivated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to deactivate SMS Gateway');
    },
  });
};

