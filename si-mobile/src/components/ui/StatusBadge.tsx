import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors } from '../../theme';

/**
 * Maps backend status to SI doc UI labels and status color.
 */
export function getStatusLabel(status: string): string {
  const s = (status || '').trim();
  const map: Record<string, string> = {
    Assigned: 'Assigned',
    OnTheWay: 'On the Way',
    ON_THE_WAY: 'On the Way',
    MetCustomer: 'Met Customer',
    MET_CUSTOMER: 'Met Customer',
    StartWork: 'Start Work',
    START_WORK: 'Start Work',
    Installing: 'Installing',
    OrderCompleted: 'Complete',
    Completed: 'Complete',
    COMPLETE: 'Complete',
    Problem: 'Report Problem',
    PROBLEM: 'Report Problem',
    Reschedule: 'Request Reschedule',
    RESCHEDULE: 'Request Reschedule',
    Pending: 'Pending',
  };
  return map[s] ?? s;
}

export function getStatusColor(status: string): string {
  const s = (status || '').trim();
  if (/assigned|pending/i.test(s)) return colors.statusAssigned;
  if (/ontheway|on_the_way/i.test(s)) return colors.statusOnTheWay;
  if (/metcustomer|met_customer|installing|startwork|start_work/i.test(s)) return colors.statusMetCustomer;
  if (/complete|completed|ordercompleted/i.test(s)) return colors.statusComplete;
  if (/problem|reschedule/i.test(s)) return colors.statusProblem;
  return colors.muted;
}

interface StatusBadgeProps {
  status: string;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const label = getStatusLabel(status);
  const bg = getStatusColor(status);
  return (
    <View style={[styles.badge, { backgroundColor: bg }]}>
      <Text style={styles.text} numberOfLines={1}>
        {label}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 999,
  },
  text: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.foreground,
  },
});
