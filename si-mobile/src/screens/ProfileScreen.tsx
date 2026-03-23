import React from 'react';
import { View, Text, StyleSheet, ScrollView } from 'react-native';
import { Button } from '../components/ui';
import { useAuth } from '../contexts/AuthContext';
import { colors } from '../theme';

export function ProfileScreen() {
  const { user, serviceInstaller, logout } = useAuth();

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.title}>Profile</Text>
      <View style={styles.card}>
        <Text style={styles.label}>Name</Text>
        <Text style={styles.value}>{user?.name ?? '–'}</Text>
        <Text style={styles.label}>Email</Text>
        <Text style={styles.value}>{user?.email ?? '–'}</Text>
        {user?.phone ? (
          <>
            <Text style={styles.label}>Phone</Text>
            <Text style={styles.value}>{user.phone}</Text>
          </>
        ) : null}
        {serviceInstaller ? (
          <>
            <Text style={styles.label}>Partner / Team</Text>
            <Text style={styles.value}>{serviceInstaller.departmentName ?? '–'}</Text>
          </>
        ) : null}
      </View>
      <Text style={styles.help}>Help Center & support – coming soon</Text>
      <Button title="Logout" onPress={() => logout()} variant="outline" style={styles.logout} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background },
  content: { padding: 16, paddingBottom: 32 },
  title: { fontSize: 24, fontWeight: '700', color: colors.foreground, marginBottom: 16 },
  card: { backgroundColor: colors.card, borderRadius: 12, padding: 16, borderWidth: 1, borderColor: colors.border, marginBottom: 24 },
  label: { fontSize: 12, color: colors.mutedForeground, marginTop: 12 },
  value: { fontSize: 16, color: colors.foreground, marginTop: 2 },
  help: { fontSize: 14, color: colors.mutedForeground, marginBottom: 24 },
  logout: { alignSelf: 'stretch' },
});
