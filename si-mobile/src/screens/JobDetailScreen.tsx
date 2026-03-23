import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import { Card, Button, StatusBadge } from '../components/ui';
import { getOrder, getAssignedOrders } from '../api/orders';
import { getAllowedTransitions, executeTransition } from '../api/workflow';
import { colors } from '../theme';

type Params = { orderId: string };

export function JobDetailScreen() {
  const { orderId } = useRoute<RouteProp<{ params: Params }>>().params ?? { orderId: '' };
  const navigation = useNavigation();
  const queryClient = useQueryClient();
  const [transitioning, setTransitioning] = useState<string | null>(null);

  const { data: order, isLoading } = useQuery({
    queryKey: ['order', orderId],
    queryFn: () => getOrder(orderId),
    enabled: !!orderId,
  });

  const { data: allowedTransitions = [] } = useQuery({
    queryKey: ['allowedTransitions', orderId, order?.status],
    queryFn: () => getAllowedTransitions(orderId, order?.status ?? ''),
    enabled: !!orderId && !!order?.status,
  });

  const executeMutation = useMutation({
    mutationFn: async (targetStatus: string) => {
      return executeTransition(orderId, targetStatus);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['order', orderId] });
      queryClient.invalidateQueries({ queryKey: ['assignedOrders'] });
      setTransitioning(null);
    },
    onError: (err: Error) => {
      setTransitioning(null);
      Alert.alert('Error', err.message);
    },
  });

  const handleTransition = (toStatus: string) => {
    setTransitioning(toStatus);
    executeMutation.mutate(toStatus);
  };

  if (isLoading || !order) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Card style={styles.section}>
        <View style={styles.row}>
          <Text style={styles.customer}>{order.customerName}</Text>
          <StatusBadge status={order.status} />
        </View>
        {order.customerPhone ? (
          <Text style={styles.meta}>Phone: {order.customerPhone}</Text>
        ) : null}
        <Text style={styles.address}>
          {order.addressLine1}
          {order.addressLine2 ? `, ${order.addressLine2}` : ''}, {order.city}
          {order.postcode ? ` ${order.postcode}` : ''}
        </Text>
        {order.appointmentDate ? (
          <Text style={styles.meta}>
            Appointment: {new Date(order.appointmentDate).toLocaleString()}
            {order.appointmentWindowFrom || order.appointmentWindowTo
              ? ` (${order.appointmentWindowFrom ?? ''}–${order.appointmentWindowTo ?? ''})`
              : ''}
          </Text>
        ) : null}
        {order.orderType ? (
          <Text style={styles.meta}>Job type: {order.orderType}</Text>
        ) : null}
        {order.partnerName ? (
          <Text style={styles.meta}>Partner: {order.partnerName}</Text>
        ) : null}
      </Card>

      {allowedTransitions.length > 0 ? (
        <Card style={styles.section}>
          <Text style={styles.sectionTitle}>Next action</Text>
          {allowedTransitions.map((t) => (
            <Button
              key={t.toStatus}
              title={
                transitioning === t.toStatus
                  ? 'Updating…'
                  : t.name || t.toStatus
              }
              onPress={() => handleTransition(t.toStatus)}
              loading={transitioning === t.toStatus}
              disabled={!!transitioning}
              style={styles.actionBtn}
            />
          ))}
        </Card>
      ) : null}

      <TouchableOpacity
        style={styles.back}
        onPress={() => navigation.goBack()}
      >
        <Text style={styles.backText}>← Back to Jobs</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  content: { padding: 16, paddingBottom: 32 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  section: { marginBottom: 16 },
  sectionTitle: { fontSize: 14, fontWeight: '600', color: colors.mutedForeground, marginBottom: 12 },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 8 },
  customer: { fontSize: 20, fontWeight: '700', color: colors.foreground, flex: 1 },
  address: { fontSize: 15, color: colors.foreground, marginTop: 4 },
  meta: { fontSize: 13, color: colors.mutedForeground, marginTop: 4 },
  actionBtn: { marginBottom: 8 },
  back: { paddingVertical: 12, alignItems: 'center' },
  backText: { fontSize: 16, color: colors.primary, fontWeight: '500' },
});
