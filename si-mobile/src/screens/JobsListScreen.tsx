import React, { useState } from 'react';
import { View, Text, StyleSheet, FlatList, TouchableOpacity } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { useNavigation } from '@react-navigation/native';
import { Card, StatusBadge } from '../components/ui';
import { useAuth } from '../contexts/AuthContext';
import { getAssignedOrders } from '../api';
import type { Order } from '../types/api';
import { colors } from '../theme';

type Tab = 'today' | 'upcoming' | 'history';

export function JobsListScreen() {
  const { siId } = useAuth();
  const navigation = useNavigation();
  const [tab, setTab] = useState<Tab>('today');

  const { data: orders = [], isLoading } = useQuery({
    queryKey: ['assignedOrders', siId],
    queryFn: () => getAssignedOrders({}),
    enabled: !!siId,
  });

  const todayStart = new Date();
  todayStart.setHours(0, 0, 0, 0);
  const todayEnd = new Date(todayStart);
  todayEnd.setDate(todayEnd.getDate() + 1);

  const filtered = (() => {
    if (tab === 'today') {
      return orders.filter((o) => {
        const d = o.appointmentDate ? new Date(o.appointmentDate) : null;
        return d && d >= todayStart && d < todayEnd;
      });
    }
    if (tab === 'upcoming') {
      return orders.filter((o) => {
        const d = o.appointmentDate ? new Date(o.appointmentDate) : null;
        return d && d >= todayEnd;
      });
    }
    return orders.filter((o) => {
      const d = o.appointmentDate ? new Date(o.appointmentDate) : null;
      return d && d < todayStart;
    });
  })();

  const renderItem = ({ item }: { item: Order }) => (
    <TouchableOpacity
      onPress={() => (navigation as { navigate: (a: string, b: { orderId: string }) => void }).navigate('JobDetail', { orderId: item.id })}
      activeOpacity={0.8}
    >
      <Card style={styles.card}>
        <View style={styles.row}>
          <Text style={styles.customer}>{item.customerName}</Text>
          <StatusBadge status={item.status} />
        </View>
        <Text style={styles.address}>
          {item.addressLine1}, {item.city}
        </Text>
        {item.appointmentDate ? (
          <Text style={styles.meta}>
            {new Date(item.appointmentDate).toLocaleDateString()} {item.appointmentWindowFrom ?? ''}–{item.appointmentWindowTo ?? ''}
          </Text>
        ) : null}
      </Card>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      <View style={styles.tabs}>
        {(['today', 'upcoming', 'history'] as const).map((t) => (
          <TouchableOpacity
            key={t}
            onPress={() => setTab(t)}
            style={[styles.tab, tab === t && styles.tabActive]}
          >
            <Text style={[styles.tabText, tab === t && styles.tabTextActive]}>
              {t === 'today' ? 'Today' : t === 'upcoming' ? 'Upcoming' : 'History'}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
      {isLoading ? (
        <Text style={styles.placeholder}>Loading…</Text>
      ) : filtered.length === 0 ? (
        <Text style={styles.placeholder}>No jobs</Text>
      ) : (
        <FlatList
          data={filtered}
          keyExtractor={(item) => item.id}
          renderItem={renderItem}
          contentContainerStyle={styles.list}
          style={styles.listInner}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  tabs: { flexDirection: 'row', padding: 16, gap: 8 },
  tab: { flex: 1, paddingVertical: 10, alignItems: 'center', borderRadius: 8 },
  tabActive: { backgroundColor: colors.primary },
  tabText: { fontSize: 14, color: colors.mutedForeground },
  tabTextActive: { color: colors.primaryForeground, fontWeight: '600' },
  list: { padding: 16, paddingTop: 0, paddingBottom: 32 },
  listInner: { flex: 1 },
  card: { marginBottom: 12 },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 },
  customer: { fontSize: 17, fontWeight: '600', color: colors.foreground, flex: 1 },
  address: { fontSize: 14, color: colors.mutedForeground },
  meta: { fontSize: 12, color: colors.muted, marginTop: 4 },
  placeholder: { padding: 24, textAlign: 'center', color: colors.mutedForeground },
});
