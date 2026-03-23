import React, { useState, useEffect, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { LoadingSpinner, Button, TextInput } from '../../components/ui';

const LoginPage: React.FC = () => {
  const [email, setEmail] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [sessionExpiredMsg, setSessionExpiredMsg] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    const expired = sessionStorage.getItem('sessionExpired');
    if (expired) {
      setSessionExpiredMsg('Your session has expired. Please sign in again.');
      sessionStorage.removeItem('sessionExpired');
    }
  }, []);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const result = await login(email, password);
      if (result.success) {
        navigate('/dashboard');
      } else if (result.requiresPasswordChange) {
        navigate('/change-password', { state: { email: result.email ?? email } });
      } else if (result.accountLocked) {
        setError(result.error || 'Your account is temporarily locked due to repeated failed sign-in attempts. Please try again later.');
      } else {
        setError(result.error || 'Login failed');
      }
    } catch (err) {
      const error = err as Error;
      setError(error.message || 'An error occurred during login');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-900 dark:to-slate-800 p-4">
      <div className="w-full max-w-md">
        <div className="bg-card rounded-xl shadow-xl border border-border p-8">
          {/* Header */}
          <div className="mb-8 text-center">
            <h1 className="text-3xl font-bold text-foreground mb-2">CephasOps</h1>
            <p className="text-base text-muted-foreground">Sign in to your account</p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-5">
            {sessionExpiredMsg && !error && (
              <div className="rounded-lg border border-amber-200 bg-amber-50 dark:bg-amber-900/20 dark:border-amber-800 p-4 text-sm text-amber-800 dark:text-amber-200" role="status">
                {sessionExpiredMsg}
              </div>
            )}
            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 dark:bg-red-900/20 dark:border-red-800 p-4 text-sm text-red-800 dark:text-red-200" role="alert">
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

            <TextInput
              id="password"
              name="password"
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="current-password"
              disabled={loading}
              placeholder="Enter your password"
            />

            <Button
              type="submit"
              className="w-full mt-6 h-11 text-base"
              disabled={loading || !email || !password}
            >
              {loading ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  Signing in...
                </>
              ) : (
                'Sign in'
              )}
            </Button>
          </form>

          {/* Footer */}
          <div className="mt-8 text-center">
            <p className="text-sm text-muted-foreground">
              <button
                type="button"
                onClick={() => navigate('/forgot-password')}
                className="underline hover:no-underline text-primary"
              >
                Forgot your password?
              </button>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;

