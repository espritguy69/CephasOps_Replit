import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { Card } from '../components/ui';
import { colors } from '../theme';

/**
 * Placeholder: Material / serial scanning.
 * Later: Install, Remove, Return, Faulty, Lookup modes and camera-based scan.
 */
export function ScanScreen() {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Scan</Text>
      <Card style={styles.card}>
        <Text style={styles.placeholder}>
          Material & device scanning will be implemented here.
        </Text>
        <Text style={styles.hint}>Modes: Install, Remove, Return, Faulty, Lookup</Text>
      </Card>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: 16 },
  title: { fontSize: 24, fontWeight: '700', color: colors.foreground, marginBottom: 16 },
  card: { padding: 20 },
  placeholder: { fontSize: 16, color: colors.foreground },
  hint: { fontSize: 13, color: colors.mutedForeground, marginTop: 8 },
});
