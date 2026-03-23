import React, { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Card } from '../components/ui';
import { useAuth } from '../contexts/AuthContext';
import { getMyEarnings } from '../api';
import { colors } from '../theme';

type Period = 'today' | 'week' | 'month';

export function EarningsScreen() {
  const { siId } = useAuth();
  const [period, setPeriod] = useState<Period>('month');

  const now = new Date();
  let from: Date;
  let to: Date;
  if (period === 'today') {
    from = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    to = new Date(from);
    to.setDate(to.getDate() + 1);
  } else if (period === 'week') {
    const day = now.getDay();
    const diff = now.getDate() - day + (day === 0 ? -6 : 1);
    from = new Date(now.getFullYear(), now.getMonth(), diff);
    to = new Date(from);
    to.setDate(to.getDate() + 7);
  } else {
    from = new Date(now.getFullYear(), now.getMonth(), 1);
    to = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59);
  }

  const { data: records = [] } = useQuery({
    queryKey: ['earnings', period, from.toISOString(), to.toISOString()],
    queryFn: () => getMyEarnings({ fromDate: from.toISOString(), toDate: to.toISOString() }),
    enabled: !!siId,
  });

  const total = records.reduce((sum, r) => sum + (r.finalPay ?? 0), 0);
  const label = period === 'today' ? 'Today' : period === 'week' ? 'This week' : 'This month';

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.tabs}>
        {(['today', 'week', 'month'] as const).map((p) => (
          <TouchableOpacity
            key={p}
            style={[styles.tab, period === p && styles.tabActive]}
            onPress={() => setPeriod(p)}
          >
            <Text style={[styles.tabText, period === p && styles.tabTextActive]}>
              {p === 'today' ? 'Today' : p === 'week' ? 'Week' : 'Month'}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
      <Card style={styles.summary}>
        <Text style={styles.summaryLabel}>{label} earnings</Text>
        <Text style={styles.summaryValue}>RM {total.toFixed(2)}</Text>
        <Text style={styles.summaryJobs}>{records.length} jobs</Text>
      </Card>
      <Text style={styles.listTitle}>Breakdown</Text>
      {records.length === 0 ? (
        <Text style={styles.placeholder}>No earnings in this period</Text>
      ) : (
        records.map((r) => (
          <Card key={r.id} style={styles.rowCard}>
            <Text style={styles.rowOrder}>{r.orderId}</Text>
            <Text style={styles.rowType}>{r.jobType}</Text>
            <Text style={styles.rowPay}>RM {(r.finalPay ?? 0).toFixed(2)}</Text>
          </Card>
        ))
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  content: { padding: 16, paddingBottom: 32 },
  tabs: { flexDirection: 'row', gap: 8, marginBottom: 16 },
  tab: { flex: 1, paddingVertical: 10, alignItems: 'center', borderRadius: 8, backgroundColor: colors.card, borderWidth: 1, borderColor: colors.border },
  tabActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  tabText: { fontSize: 14, color: colors.mutedForeground },
  tabTextActive: { color: colors.primaryForeground, fontWeight: '600' },
  summary: { marginBottom: 16 },
  summaryLabel: { fontSize: 14, color: colors.mutedForeground },
  summaryValue: { fontSize: 28, fontWeight: '700', color: colors.primary, marginTop: 4 },
  summaryJobs: { fontSize: 13, color: colors.muted, marginTop: 4 },
  listTitle: { fontSize: 16, fontWeight: '600', color: colors.foreground, marginBottom: 12 },
  rowCard: { marginBottom: 8 },
  rowOrder: { fontSize: 14, fontWeight: '500', color: colors.foreground },
  rowType: { fontSize: 12, color: colors.mutedForeground, marginTop: 2 },
  rowPay: { fontSize: 16, fontWeight: '600', color: colors.primary, marginTop: 4 },
  placeholder: { padding: 24, textAlign: 'center', color: colors.mutedForeground },
});
