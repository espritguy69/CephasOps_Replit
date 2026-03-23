import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { Card } from '../components/ui';
import { colors } from '../theme';

interface PlaceholderScreenProps {
  title: string;
  description: string;
}

/**
 * Generic placeholder for: Assurance replacement, Extra work, Proof capture, Completion review, etc.
 */
export function PlaceholderScreen({ title, description }: PlaceholderScreenProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>{title}</Text>
      <Card style={styles.card}>
        <Text style={styles.text}>{description}</Text>
      </Card>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.background, padding: 16 },
  title: { fontSize: 24, fontWeight: '700', color: colors.foreground, marginBottom: 16 },
  card: { padding: 20 },
  text: { fontSize: 16, color: colors.foreground },
});
