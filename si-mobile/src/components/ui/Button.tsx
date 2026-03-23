import React from 'react';
import { TouchableOpacity, Text, StyleSheet, ActivityIndicator, ViewStyle, TextStyle } from 'react-native';
import { colors } from '../../theme';

type Variant = 'primary' | 'secondary' | 'outline' | 'destructive';

interface ButtonProps {
  title: string;
  onPress: () => void;
  variant?: Variant;
  disabled?: boolean;
  loading?: boolean;
  style?: ViewStyle;
  textStyle?: TextStyle;
}

export function Button({
  title,
  onPress,
  variant = 'primary',
  disabled = false,
  loading = false,
  style,
  textStyle,
}: ButtonProps) {
  const isDisabled = disabled || loading;
  const isPrimary = variant === 'primary';
  const isDestructive = variant === 'destructive';
  const isOutline = variant === 'outline';
  const bg = isPrimary ? colors.primary : isDestructive ? colors.destructive : isOutline ? 'transparent' : colors.secondary;
  const fg = isPrimary || isDestructive ? colors.primaryForeground : isOutline ? colors.primary : colors.secondaryForeground;
  const border = isOutline ? { borderWidth: 1, borderColor: colors.border } : {};

  return (
    <TouchableOpacity
      onPress={onPress}
      disabled={isDisabled}
      activeOpacity={0.8}
      style={[
        styles.btn,
        { backgroundColor: bg as string },
        border,
        isDisabled && styles.disabled,
        style,
      ]}
    >
      {loading ? (
        <ActivityIndicator color={fg as string} />
      ) : (
        <Text style={[styles.text, { color: fg as string }, textStyle]}>{title}</Text>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  btn: {
    paddingVertical: 14,
    paddingHorizontal: 20,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 48,
  },
  text: {
    fontSize: 16,
    fontWeight: '600',
  },
  disabled: { opacity: 0.6 },
});
