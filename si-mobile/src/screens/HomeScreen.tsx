import React from 'react';
import { View, Text, StyleSheet, ScrollView } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Card } from '../components/ui';
import { useAuth } from '../contexts/AuthContext';
import { getAssignedOrders } from '../api';
import { getMyEarnings } from '../api';
import { colors } from '../theme';

function formatGreeting(): string {
  const h = new Date().getHours();
  if (h < 12) return 'Good Morning';
  if (h < 18) return 'Good Afternoon';
  return 'Good Evening';
}

export function HomeScreen() {
  const { user, siId } = useAuth();
  const name = user?.name?.split(' ')[0] ?? 'there';

  const { data: orders = [] } = useQuery({
    queryKey: ['assignedOrders', siId],
    queryFn: () => getAssignedOrders({}),
    enabled: !!siId,
  });

  const todayStart = new Date();
  todayStart.setHours(0, 0, 0, 0);
  const todayEnd = new Date(todayStart);
  todayEnd.setDate(todayEnd.getDate() + 1);
  const todayOrders = orders.filter((o) => {
    const d = o.appointmentDate ? new Date(o.appointmentDate) : null;
    return d && d >= todayStart && d < todayEnd;
  });
  const completed = todayOrders.filter((o) =>
    /OrderCompleted|Completed|Complete/i.test(o.status ?? '')
  );
  const problems = orders.filter((o) => /Problem|Reschedule/i.test(o.status ?? ''));
  const activeJob = orders.find(
    (o) =>
      !/OrderCompleted|Completed|Complete|Problem|Reschedule|Pending|Assigned/i.test(
        o.status ?? ''
      )
  );

  const { data: earningsList = [] } = useQuery({
    queryKey: ['earningsToday'],
    queryFn: () =>
      getMyEarnings({
        fromDate: todayStart.toISOString(),
        toDate: todayEnd.toISOString(),
      }),
    enabled: !!siId,
  });
  const todayEarnings = earningsList.reduce((sum, r) => sum + (r.finalPay ?? 0), 0);

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.greeting}>
        {formatGreeting()}, {name}
      </Text>
      <View style={styles.stats}>
        <Card style={styles.statCard}>
          <Text style={styles.statLabel}>Today's Jobs</Text>
          <Text style={styles.statValue}>{todayOrders.length}</Text>
        </Card>
        <Card style={styles.statCard}>
          <Text style={styles.statLabel}>Completed</Text>
          <Text style={[styles.statValue, { color: colors.success }]}>{completed.length}</Text>
        </Card>
        <Card style={styles.statCard}>
          <Text style={styles.statLabel}>Remaining</Text>
          <Text style={styles.statValue}>{todayOrders.length - completed.length}</Text>
        </Card>
        <Card style={styles.statCard}>
          <Text style={styles.statLabel}>Problems</Text>
          <Text style={[styles.statValue, { color: colors.destructive }]}>{problems.length}</Text>
        </Card>
      </View>
      {activeJob ? (
        <Card style={styles.jobCard}>
          <Text style={styles.jobCardTitle}>Current Job</Text>
          <Text style={styles.jobCustomer}>{activeJob.customerName}</Text>
          <Text style={styles.jobAddress}>
            {activeJob.addressLine1}, {activeJob.city}
          </Text>
          <Text style={styles.jobMeta}>
            {activeJob.appointmentDate
              ? new Date(activeJob.appointmentDate).toLocaleTimeString([], {
                  hour: '2-digit',
                  minute: '2-digit',
                })
              : ''}{' '}
            {activeJob.orderType ?? ''}
          </Text>
        </Card>
      ) : null}
      <Card style={styles.earningsCard}>
        <Text style={styles.earningsLabel}>Today's Earnings</Text>
        <Text style={styles.earningsValue}>RM {todayEarnings.toFixed(2)}</Text>
      </Card>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  content: { padding: 16, paddingBottom: 32 },
  greeting: { fontSize: 22, fontWeight: '700', color: colors.foreground, marginBottom: 16 },
  stats: { flexDirection: 'row', flexWrap: 'wrap', gap: 12, marginBottom: 16 },
  statCard: { flex: 1, minWidth: '45%' },
  statLabel: { fontSize: 12, color: colors.mutedForeground, marginBottom: 4 },
  statValue: { fontSize: 20, fontWeight: '700', color: colors.foreground },
  jobCard: { marginBottom: 16 },
  jobCardTitle: { fontSize: 12, color: colors.mutedForeground, marginBottom: 4 },
  jobCustomer: { fontSize: 18, fontWeight: '600', color: colors.foreground },
  jobAddress: { fontSize: 14, color: colors.mutedForeground, marginTop: 4 },
  jobMeta: { fontSize: 12, color: colors.muted, marginTop: 4 },
  earningsCard: {},
  earningsLabel: { fontSize: 14, color: colors.mutedForeground },
  earningsValue: { fontSize: 24, fontWeight: '700', color: colors.primary, marginTop: 4 },
});
