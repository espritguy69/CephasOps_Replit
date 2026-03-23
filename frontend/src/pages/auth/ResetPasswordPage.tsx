import React, { useState, FormEvent, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { resetPasswordWithToken } from '../../api/auth';
import { LoadingSpinner, Button, TextInput } from '../../components/ui';

/**
 * Reset password using the token from the email link. Token is read from URL query (?token=...).
 */
const ResetPasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const tokenFromUrl = searchParams.get('token') ?? '';

  const [token, setToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (tokenFromUrl) setToken(tokenFromUrl);
  }, [tokenFromUrl]);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    if (!token?.trim()) {
      setError('Reset link is invalid or missing. Please request a new password reset.');
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
      await resetPasswordWithToken(token.trim(), newPassword, confirmPassword);
      setSuccess(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid or expired reset link. Please request a new one.');
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 p-4">
        <div className="w-full max-w-md">
          <div className="bg-card rounded-xl shadow-xl border border-border p-8">
            <div className="rounded-lg border border-green-200 bg-green-50 dark:bg-green-900/20 dark:border-green-800 p-4 text-sm text-green-800 dark:text-green-200 mb-6" role="alert">
              Your password has been reset. You can now sign in.
            </div>
            <Button type="button" className="w-full" onClick={() => navigate('/login', { replace: true })}>
              Sign in
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 p-4">
      <div className="w-full max-w-md">
        <div className="bg-card rounded-xl shadow-xl border border-border p-8">
          <div className="mb-6 text-center">
            <h1 className="text-xl font-bold text-foreground mb-1">Set new password</h1>
            <p className="text-sm text-muted-foreground">
              Enter your new password below. Use the link from your email; if it has expired, request a new one.
            </p>
          </div>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 dark:bg-red-900/20 dark:border-red-800 p-3 text-sm text-red-800 dark:text-red-200" role="alert">
                {error}
              </div>
            )}
            {!tokenFromUrl && (
              <TextInput
                label="Reset token"
                type="text"
                value={token}
                onChange={(e) => setToken(e.target.value)}
                disabled={loading}
                placeholder="Paste the token from your email (or use the link)"
              />
            )}
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
            <Button type="submit" className="w-full" disabled={loading || !token.trim() || !newPassword || newPassword !== confirmPassword}>
              {loading ? <><LoadingSpinner size="sm" className="mr-2" /> Resetting…</> : 'Reset password'}
            </Button>
          </form>
          <p className="mt-6 text-center text-sm text-muted-foreground">
            <button type="button" onClick={() => navigate('/login')} className="underline hover:no-underline">
              Back to sign in
            </button>
            {' · '}
            <button type="button" onClick={() => navigate('/forgot-password')} className="underline hover:no-underline">
              Request new link
            </button>
          </p>
        </div>
      </div>
    </div>
  );
};

export default ResetPasswordPage;
