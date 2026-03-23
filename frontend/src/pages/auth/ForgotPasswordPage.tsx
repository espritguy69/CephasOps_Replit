import React, { useState, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { forgotPassword } from '../../api/auth';
import { LoadingSpinner, Button, TextInput } from '../../components/ui';

/**
 * Request a password reset email. Shows generic success message regardless of whether account exists.
 */
const ForgotPasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    setSuccess(false);
    if (!email?.trim()) {
      setError('Please enter your email address.');
      return;
    }
    setLoading(true);
    try {
      await forgotPassword(email.trim());
      setSuccess(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 p-4">
      <div className="w-full max-w-md">
        <div className="bg-card rounded-xl shadow-xl border border-border p-8">
          <div className="mb-6 text-center">
            <h1 className="text-xl font-bold text-foreground mb-1">Forgot password?</h1>
            <p className="text-sm text-muted-foreground">
              Enter your email and we’ll send you a link to reset your password.
            </p>
          </div>
          {success ? (
            <div className="space-y-4">
              <div className="rounded-lg border border-green-200 bg-green-50 dark:bg-green-900/20 dark:border-green-800 p-4 text-sm text-green-800 dark:text-green-200" role="alert">
                If an account exists for that email, you will receive a password reset link shortly.
              </div>
              <Button type="button" className="w-full" onClick={() => navigate('/login')}>
                Back to sign in
              </Button>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              {error && (
                <div className="rounded-lg border border-red-200 bg-red-50 dark:bg-red-900/20 dark:border-red-800 p-3 text-sm text-red-800 dark:text-red-200" role="alert">
                  {error}
                </div>
              )}
              <TextInput
                id="email"
                name="email"
                label="Email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
                disabled={loading}
                placeholder="Enter your email"
              />
              <Button type="submit" className="w-full" disabled={loading || !email.trim()}>
                {loading ? <><LoadingSpinner size="sm" className="mr-2" /> Sending…</> : 'Send reset link'}
              </Button>
            </form>
          )}
          <p className="mt-6 text-center text-sm text-muted-foreground">
            <button type="button" onClick={() => navigate('/login')} className="underline hover:no-underline">
              Back to sign in
            </button>
          </p>
        </div>
      </div>
    </div>
  );
};

export default ForgotPasswordPage;
