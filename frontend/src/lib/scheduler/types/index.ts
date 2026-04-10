import type { ServiceInstaller } from '../../../types/serviceInstallers';
import type { CalendarSlot } from '../../../types/scheduler';

export type { ServiceInstaller, CalendarSlot };

export interface JobContext {
  orderId: string;
  orderType?: string;
  serviceType?: string;
  buildingName?: string;
  address?: string;
  appointmentDate?: string;
  appointmentTime?: string;
  windowFrom?: string;
  windowTo?: string;
  customerName?: string;
  requiredSkills?: string[];
}

export interface InstallerWorkload {
  installerId: string;
  jobCount: number;
  totalMinutes: number;
  utilizationPct: number;
  level: 'free' | 'medium' | 'overloaded';
}

export interface ScoringResult {
  installerId: string;
  score: number;
  reasons: string[];
  breakdown: {
    availability: number;
    workload: number;
    distance: number;
    skillMatch: number;
    jobHistory: number;
  };
  blocked: boolean;
  blockReason?: string;
}

export interface WindowScore {
  windowFrom: string;
  windowTo: string;
  score: number;
  reasons: string[];
  isPreferred: boolean;
}

export type WindowStatus = 'PREFERRED' | 'BEST_FIT' | 'NO_SLOT';

export interface BestWindowResult {
  status: 'PREFERRED' | 'BEST_FIT';
  windowFrom: string;
  windowTo: string;
  score: number;
  reason: string;
}

export interface NoSlotResult {
  status: 'NO_SLOT';
  reason: string;
}

export interface AutoAssignment {
  orderId: string;
  installerId: string;
  installerName: string;
  score: number;
  reasons: string[];
  date: string;
  windowFrom: string;
  windowTo: string;
  windowReason: string;
}

export interface AutoAssignResult {
  assignments: AutoAssignment[];
  unassignable: Array<{
    orderId: string;
    reason: string;
  }>;
}

export interface ConflictCheckResult {
  hasConflict: boolean;
  conflictSlot?: CalendarSlot;
  message?: string;
}

export interface OverrideRecord {
  overridden: boolean;
  reason: string;
  userId: string;
  timestamp: string;
  originalValue?: unknown;
  overriddenValue?: unknown;
}
