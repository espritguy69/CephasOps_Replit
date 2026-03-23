import React, { useState, FormEvent } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { changePasswordRequired } from '../../api/auth';
import { LoadingSpinner, Button, TextInput } from '../../components/ui';

/**
 * Shown when user must change password (e.g. after admin reset).
 * Email comes from location.state (from login redirect) or auth context (pendingPasswordChangeEmail).
 */
const ChangePasswordPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { pendingPasswordChangeEmail, clearPendingPasswordChange } = useAuth();
  const emailFromState = (location.state as { email?: string } | null)?.email;
  const email = emailFromState ?? pendingPasswordChangeEmail ?? '';

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    if (!email?.trim()) {
      setError('Email is required. Please go back to the login page.');
      return;
    }
    if (!currentPassword) {
      setError('Current password is required.');
      return;
    }
    if (!newPassword || newPassword.length < 6) {
      setError('New password must be at least 6 characters.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setError('New password and confirmation do not match.');
      return;
    }
    setLoading(true);
    try {
      await changePasswordRequired(email.trim().toLowerCase(), currentPassword, newPassword);
      clearPendingPasswordChange();
      navigate('/login', { state: null, replace: true });
      // Show a brief message - login page could read state and show "Password changed. You can sign in."
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to change password.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  if (!email && !pendingPasswordChangeEmail) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 p-4">
        <div className="bg-card rounded-xl border border-border p-8 max-w-md text-center">
          <p className="text-muted-foreground mb-4">No email context. Please sign in first; you will be directed here if you must change your password.</p>
          <Button onClick={() => navigate('/login')}>Go to sign in</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 p-4">
      <div className="w-full max-w-md">
        <div className="bg-card rounded-xl shadow-xl border border-border p-8">
          <div className="mb-6 text-center">
            <h1 className="text-xl font-bold text-foreground mb-1">Change your password</h1>
            <p className="text-sm text-muted-foreground">You must set a new password before continuing.</p>
          </div>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 dark:bg-red-900/20 dark:border-red-800 p-3 text-sm text-red-800 dark:text-red-200" role="alert">
                {error}
              </div>
            )}
            <TextInput
              label="Email"
              type="email"
              value={email}
              disabled
              className="bg-muted"
            />
            <TextInput
              label="Current password"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
              autoComplete="current-password"
              disabled={loading}
              placeholder="Enter your current password"
            />
            <TextInput
              label="New password"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              autoComplete="new-password"
              disabled={loading}
              placeholder="At least 6 characters"
            />
            <TextInput
              label="Confirm new password"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
              autoComplete="new-password"
              disabled={loading}
              placeholder="Confirm new password"
            />
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? <><LoadingSpinner size="sm" className="mr-2" /> Updating…</> : 'Change password'}
            </Button>
          </form>
          <p className="mt-4 text-center text-sm text-muted-foreground">
            <button type="button" onClick={() => navigate('/login')} className="underline hover:no-underline">Back to sign in</button>
          </p>
        </div>
      </div>
    </div>
  );
};

export default ChangePasswordPage;
