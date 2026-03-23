/**
 * API and domain types for SI Mobile.
 * Aligned with frontend-si and backend contracts.
 */
export interface User {
  id: string;
  name: string;
  email: string;
  phone?: string;
  roles: string[];
}

export interface ServiceInstaller {
  id: string;
  userId?: string;
  name: string;
  siLevel?: string;
  employeeId?: string;
  phone?: string;
  email?: string;
  departmentId?: string;
  departmentName?: string;
  isSubcontractor?: boolean;
  isActive: boolean;
  companyId?: string;
  createdAt?: string;
}

export interface Order {
  id: string;
  tbbn?: string;
  orderNumber?: string;
  status: string;
  customerName: string;
  customerPhone?: string;
  customerEmail?: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  postcode?: string;
  appointmentDate?: string;
  appointmentWindowFrom?: string;
  appointmentWindowTo?: string;
  partnerName?: string;
  orderType?: string;
}

export interface WorkflowTransition {
  fromStatus: string;
  toStatus: string;
  name: string;
}

export interface Location {
  latitude: number;
  longitude: number;
  accuracy?: number;
  altitude?: number;
  timestamp?: number;
}

export interface JobEarningRecord {
  id: string;
  orderId: string;
  jobType: string;
  kpiResult?: string;
  baseRate: number;
  kpiAdjustment: number;
  finalPay: number;
  period: string;
}

export interface ApiResponse<T = unknown> {
  success?: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}
