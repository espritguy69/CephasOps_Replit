import { useQuery, useMutation, useQueryClient, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import {
  getBuildingsSummary,
  getBuildings,
  getBuilding,
  createBuilding,
  updateBuilding,
  deleteBuilding,
  getBuildingContacts,
  createBuildingContact,
  updateBuildingContact,
  deleteBuildingContact,
  saveBuildingRules,
} from '../api/buildings';
import type {
  BuildingsSummary,
  Building,
  BuildingFilters,
  CreateBuildingRequest,
  UpdateBuildingRequest,
  BuildingContact,
  CreateBuildingContactRequest,
  UpdateBuildingContactRequest,
  SaveBuildingRulesRequest,
  BuildingRules
} from '../types/buildings';
import { useToast } from '../components/ui';

/**
 * React Query hook for fetching buildings summary (dashboard)
 * 
 * @param params - Optional parameters (departmentId)
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useBuildingsSummary = <TData = BuildingsSummary>(
  params: Record<string, any> = {},
  options?: Omit<UseQueryOptions<BuildingsSummary, Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<BuildingsSummary, Error, TData>({
    queryKey: ['buildings', 'summary', params],
    queryFn: () => getBuildingsSummary(),
    ...options,
  });
};

/**
 * React Query hook for fetching buildings list
 * 
 * @param filters - Optional filters (propertyType, installationMethodId, state, city, isActive)
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useBuildings = <TData = Building[]>(
  filters: BuildingFilters = {},
  options?: Omit<UseQueryOptions<Building[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<Building[], Error, TData>({
    queryKey: ['buildings', filters],
    queryFn: () => getBuildings(filters),
    ...options,
  });
};

/**
 * React Query hook for fetching a single building
 * 
 * @param buildingId - Building ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useBuilding = <TData = Building>(
  buildingId: string | undefined,
  options?: Omit<UseQueryOptions<Building, Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<Building, Error, TData>({
    queryKey: ['buildings', buildingId],
    queryFn: () => getBuilding(buildingId!),
    enabled: !!buildingId && (options?.enabled !== false),
    ...options,
  });
};

/**
 * React Query mutation hook for creating a building
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useCreateBuilding = (
  options?: Omit<UseMutationOptions<Building, Error, CreateBuildingRequest>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Building, Error, CreateBuildingRequest>({
    mutationFn: (buildingData) => createBuilding(buildingData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
      queryClient.invalidateQueries({ queryKey: ['buildings', 'summary'] });
      showSuccess('Building created successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to create building');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for updating a building
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useUpdateBuilding = (
  options?: Omit<UseMutationOptions<Building, Error, { buildingId: string; buildingData: UpdateBuildingRequest }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Building, Error, { buildingId: string; buildingData: UpdateBuildingRequest }>({
    mutationFn: ({ buildingId, buildingData }) => updateBuilding(buildingId, buildingData),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['buildings', variables.buildingId] });
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
      queryClient.invalidateQueries({ queryKey: ['buildings', 'summary'] });
      showSuccess('Building updated successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to update building');
    },
    ...options,
  });
};

/**
 * React Query hook for fetching building contacts
 * 
 * @param buildingId - Building ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useBuildingContacts = <TData = BuildingContact[]>(
  buildingId: string | undefined,
  options?: Omit<UseQueryOptions<BuildingContact[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<BuildingContact[], Error, TData>({
    queryKey: ['buildings', buildingId, 'contacts'],
    queryFn: () => getBuildingContacts(buildingId!),
    enabled: !!buildingId && (options?.enabled !== false),
    ...options,
  });
};

/**
 * React Query mutation hook for creating a building contact
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useCreateBuildingContact = (
  options?: Omit<UseMutationOptions<BuildingContact, Error, { buildingId: string; contactData: CreateBuildingContactRequest }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<BuildingContact, Error, { buildingId: string; contactData: CreateBuildingContactRequest }>({
    mutationFn: ({ buildingId, contactData }) => createBuildingContact(buildingId, contactData),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['buildings', variables.buildingId, 'contacts'] });
      queryClient.invalidateQueries({ queryKey: ['buildings', variables.buildingId] });
      showSuccess('Contact created successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to create contact');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for updating a building contact
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useUpdateBuildingContact = (
  options?: Omit<UseMutationOptions<BuildingContact, Error, { buildingId: string; contactId: string; contactData: UpdateBuildingContactRequest }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<BuildingContact, Error, { buildingId: string; contactId: string; contactData: UpdateBuildingContactRequest }>({
    mutationFn: ({ buildingId, contactId, contactData }) => 
      updateBuildingContact(buildingId, contactId, contactData),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['buildings', variables.buildingId, 'contacts'] });
      queryClient.invalidateQueries({ queryKey: ['buildings', variables.buildingId] });
      showSuccess('Contact updated successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to update contact');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for deleting a building contact
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useDeleteBuildingContact = (
  options?: Omit<UseMutationOptions<void, Error, { buildingId: string; contactId: string }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<void, Error, { buildingId: string; contactId: string }>({
    mutationFn: ({ buildingId, contactId }) => deleteBuildingContact(buildingId, contactId),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['buildings', variables.buildingId, 'contacts'] });
      queryClient.invalidateQueries({ queryKey: ['buildings', variables.buildingId] });
      showSuccess('Contact deleted successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to delete contact');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for saving building rules
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useSaveBuildingRules = (
  options?: Omit<UseMutationOptions<BuildingRules, Error, { buildingId: string; rulesData: SaveBuildingRulesRequest }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<BuildingRules, Error, { buildingId: string; rulesData: SaveBuildingRulesRequest }>({
    mutationFn: ({ buildingId, rulesData }) => saveBuildingRules(buildingId, rulesData),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['buildings', variables.buildingId] });
      showSuccess('Building rules saved successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to save building rules');
    },
    ...options,
  });
};

