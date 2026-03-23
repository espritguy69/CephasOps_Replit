import React from 'react';
import { View, StyleSheet } from 'react-native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { useQuery } from '@tanstack/react-query';
import { HomeScreen } from '../screens/HomeScreen';
import { JobsListScreen } from '../screens/JobsListScreen';
import { ScanScreen } from '../screens/ScanScreen';
import { EarningsScreen } from '../screens/EarningsScreen';
import { ProfileScreen } from '../screens/ProfileScreen';
import { CurrentJobBar } from '../components/CurrentJobBar';
import { useAuth } from '../contexts/AuthContext';
import { getAssignedOrders } from '../api';
import type { MainTabsParamList } from './types';
import { colors } from '../theme';

const Tab = createBottomTabNavigator<MainTabsParamList>();

function TabIcon({ name, focused }: { name: string; focused: boolean }) {
  return (
    <View style={styles.tabIcon}>
      <View style={[styles.dot, focused && styles.dotActive]} />
    </View>
  );
}

export function MainTabs() {
  const { siId } = useAuth();
  const { data: orders = [] } = useQuery({
    queryKey: ['assignedOrders', siId],
    queryFn: () => getAssignedOrders({}),
    enabled: !!siId,
  });
  const activeJob = orders.find(
    (o) =>
      !/OrderCompleted|Completed|Complete|Problem|Reschedule|Pending|Assigned/i.test(o.status ?? '')
  );

  return (
    <View style={styles.container}>
      {activeJob ? <CurrentJobBar job={activeJob} /> : null}
      <Tab.Navigator
        screenOptions={{
          headerStyle: { backgroundColor: colors.card },
          headerTintColor: colors.foreground,
          headerShadowVisible: false,
          tabBarActiveTintColor: colors.primary,
          tabBarInactiveTintColor: colors.mutedForeground,
          tabBarStyle: { backgroundColor: colors.card, borderTopColor: colors.border },
          tabBarLabelStyle: { fontSize: 12 },
        }}
      >
        <Tab.Screen name="Home" component={HomeScreen} options={{ title: 'Home' }} />
        <Tab.Screen name="Jobs" component={JobsListScreen} options={{ title: 'Jobs' }} />
        <Tab.Screen name="Scan" component={ScanScreen} options={{ title: 'Scan' }} />
        <Tab.Screen name="Earnings" component={EarningsScreen} options={{ title: 'Earnings' }} />
        <Tab.Screen name="Profile" component={ProfileScreen} options={{ title: 'Profile' }} />
      </Tab.Navigator>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  tabIcon: { alignItems: 'center', justifyContent: 'center' },
  dot: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.muted },
  dotActive: { backgroundColor: colors.primary },
});
