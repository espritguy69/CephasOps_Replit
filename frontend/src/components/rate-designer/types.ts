/**
 * Shared types for the GPON Rate Designer.
 * Context is the selected "job scenario" used to filter rates and run the calculator.
 */
export interface RateDesignerContext {
  orderTypeId: string;
  orderSubtypeId: string;
  orderCategoryId: string;
  installationMethodId: string;
  rateGroupId: string;
  serviceProfileId: string;
  siLevel: string;
  partnerGroupId: string;
  serviceInstallerId: string;
}

export const emptyRateDesignerContext: RateDesignerContext = {
  orderTypeId: '',
  orderSubtypeId: '',
  orderCategoryId: '',
  installationMethodId: '',
  rateGroupId: '',
  serviceProfileId: '',
  siLevel: '',
  partnerGroupId: '',
  serviceInstallerId: ''
};

export type RateDesignerContextKey = keyof RateDesignerContext;
