// API response types

export interface ApiResponse<T = any> {
  success?: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}

export interface ApiError extends Error {
  status?: number;
  data?: any;
}

// Auth types
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

// Order types
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
  partnerName?: string;
  orderType?: string;
  parsedMaterials?: ParsedMaterial[];
}

export interface ParsedMaterial {
  id?: string;
  materialId?: string;
  materialName?: string;
  quantity?: number;
  unit?: string;
  serialNumber?: string;
  category?: string;
}

// Checklist types
export interface ChecklistItem {
  id: string;
  description: string;
  text?: string; // Alternative field name
  isRequired: boolean;
  parentChecklistItemId?: string;
  subSteps?: ChecklistItem[];
  answer?: ChecklistAnswer;
}

export interface ChecklistAnswer {
  checklistItemId: string;
  answer: boolean;
  remarks?: string;
}

export interface ChecklistData {
  items?: ChecklistItem[];
  answers?: ChecklistAnswer[];
}

// Workflow types
export interface WorkflowTransition {
  fromStatus: string;
  toStatus: string;
  name: string;
}

// Earnings types
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

export interface EarningsSummary {
  totalEarnings: number;
  totalJobs: number;
  totalBasePay: number;
  totalAdjustments: number;
  averagePerJob: number;
  period: string;
  fromDate: string;
  toDate: string;
}

export interface KpiMetrics {
  totalJobs: number;
  onTimeJobs: number;
  lateJobs: number;
  exceededSlaJobs: number;
  excusedJobs: number;
  onTimeRate: number;
  averageEarnings: number;
  totalEarnings: number;
}

// Photo types
export interface Photo {
  id: string;
  url: string;
  photoUrl?: string;
  preview?: string;
}

// Location types
export interface Location {
  latitude: number;
  longitude: number;
  accuracy?: number;
  altitude?: number;
  altitudeAccuracy?: number;
  heading?: number;
  speed?: number;
  timestamp?: number;
}

// Stock movement types
export interface StockMovement {
  id: string;
  orderId: string;
  material?: {
    id: string;
    name: string;
    unit?: string;
    categoryName?: string;
  };
  quantity: number;
  serialNumber?: string;
  movementType?: string;
}

