import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { colors } from '../theme';
import type { Order } from '../types/api';

interface CurrentJobBarProps {
  job: Order;
}

/**
 * Persistent bar when user has an active job (not Assigned/Pending/Complete/Problem/Reschedule).
 */
export function CurrentJobBar({ job }: CurrentJobBarProps) {
  const navigation = useNavigation();

  return (
    <TouchableOpacity
      style={styles.bar}
      onPress={() =>
        (navigation as { navigate: (a: string, b: { orderId: string }) => void }).navigate(
          'JobDetail',
          { orderId: job.id }
        )
      }
      activeOpacity={0.8}
    >
      <View style={styles.inner}>
        <Text style={styles.label}>Current Job</Text>
        <Text style={styles.customer} numberOfLines={1}>
          {job.customerName} – {job.city}
        </Text>
      </View>
      <Text style={styles.open}>OPEN</Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: colors.primary,
    paddingVertical: 12,
    paddingHorizontal: 16,
  },
  inner: { flex: 1 },
  label: { fontSize: 11, color: 'rgba(255,255,255,0.8)', marginBottom: 2 },
  customer: { fontSize: 15, fontWeight: '600', color: colors.primaryForeground },
  open: { fontSize: 14, fontWeight: '700', color: colors.primaryForeground },
});
