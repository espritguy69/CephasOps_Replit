import type { NativeStackScreenProps } from '@react-navigation/native-stack';

export type AuthStackParamList = {
  Login: undefined;
};

export type MainStackParamList = {
  MainTabs: undefined;
  JobDetail: { orderId: string };
};

export type MainTabsParamList = {
  Home: undefined;
  Jobs: undefined;
  Scan: undefined;
  Earnings: undefined;
  Profile: undefined;
};

export type AuthStackScreenProps<T extends keyof AuthStackParamList> = NativeStackScreenProps<
  AuthStackParamList,
  T
>;
export type MainStackScreenProps<T extends keyof MainStackParamList> = NativeStackScreenProps<
  MainStackParamList,
  T
>;
