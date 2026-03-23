import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { 
  Calculator, TrendingUp, TrendingDown, DollarSign, FileText, 
  AlertTriangle, ArrowUpRight, ArrowDownRight, CreditCard
} from 'lucide-react';
import { getAccountingDashboard } from '../../api/accounting';
import { LoadingSpinner, EmptyState, useToast, Card } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { AccountingDashboard, Payment, SupplierInvoice } from '../../types/accounting';

interface PaymentSummary {
  totalIncome: number;
  incomeThisMonth: number;
  totalExpenses: number;
  expensesThisMonth: number;
  netCashFlow: number;
  totalPayments: number;
  unreconciledPayments: number;
  monthlyTrend?: Array<{
    period: string;
    income: number;
    expenses: number;
    net: number;
  }>;
}

interface SupplierInvoiceSummary {
  pendingApproval: number;
  overdueInvoices: number;
}

interface DashboardData {
  paymentSummary: PaymentSummary;
  supplierInvoiceSummary: SupplierInvoiceSummary;
  totalReceivables: number;
  totalPayables: number;
  recentPayments: Payment[];
  overdueInvoices: SupplierInvoice[];
}

const AccountingDashboardPage: React.FC = () => {
  const { showError } = useToast();
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async (): Promise<void> => {
    try {
      setLoading(true);
      const dashboardData = await getAccountingDashboard();
      // Transform the data to match our expected structure
      setData({
        paymentSummary: {
          totalIncome: (dashboardData as any).paymentSummary?.totalIncome || 0,
          incomeThisMonth: (dashboardData as any).paymentSummary?.incomeThisMonth || 0,
          totalExpenses: (dashboardData as any).paymentSummary?.totalExpenses || 0,
          expensesThisMonth: (dashboardData as any).paymentSummary?.expensesThisMonth || 0,
          netCashFlow: (dashboardData as any).paymentSummary?.netAmount || 0,
          totalPayments: (dashboardData as any).paymentSummary?.totalPayments || 0,
          unreconciledPayments: (dashboardData as any).paymentSummary?.unreconciledPayments || 0,
          monthlyTrend: (dashboardData as any).paymentSummary?.monthlyTrend || []
        },
        supplierInvoiceSummary: {
          pendingApproval: dashboardData.supplierInvoices?.pending || 0,
          overdueInvoices: dashboardData.supplierInvoices?.overdue || 0
        },
        totalReceivables: (dashboardData as any).totalReceivables || 0,
        totalPayables: (dashboardData as any).totalPayables || 0,
        recentPayments: dashboardData.recentPayments || [],
        overdueInvoices: dashboardData.overdueInvoices || []
      });
    } catch (err) {
      console.error('Error loading dashboard:', err);
      showError('Failed to load accounting dashboard');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', {
      style: 'currency',
      currency: 'MYR'
    }).format(amount || 0);
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-MY', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  if (loading) {
    return (
      <PageShell title="Accounting Dashboard" breadcrumbs={[{ label: 'Accounting' }]}>
        <div data-testid="accounting-dashboard-root">
          <LoadingSpinner fullPage />
        </div>
      </PageShell>
    );
  }
  if (!data) {
    return (
      <PageShell title="Accounting Dashboard" breadcrumbs={[{ label: 'Accounting' }]}>
        <div data-testid="accounting-dashboard-root">
          <EmptyState title="No data available" message="Accounting dashboard could not be loaded." />
        </div>
      </PageShell>
    );
  }

  const { paymentSummary, supplierInvoiceSummary, totalReceivables, totalPayables, recentPayments, overdueInvoices } = data;

  return (
    <PageShell title="Accounting Dashboard" breadcrumbs={[{ label: 'Accounting' }]}>
      <div data-testid="accounting-dashboard-root" className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-slate-400 text-sm">Total Income</p>
              <p className="text-2xl font-bold text-green-500">{formatCurrency(paymentSummary?.totalIncome)}</p>
              <p className="text-xs text-slate-500">This month: {formatCurrency(paymentSummary?.incomeThisMonth)}</p>
            </div>
            <div className="p-3 bg-green-500/10 rounded-lg">
              <ArrowUpRight className="h-6 w-6 text-green-500" />
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-slate-400 text-sm">Total Expenses</p>
              <p className="text-2xl font-bold text-red-500">{formatCurrency(paymentSummary?.totalExpenses)}</p>
              <p className="text-xs text-slate-500">This month: {formatCurrency(paymentSummary?.expensesThisMonth)}</p>
            </div>
            <div className="p-3 bg-red-500/10 rounded-lg">
              <ArrowDownRight className="h-6 w-6 text-red-500" />
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-slate-400 text-sm">Net Cash Flow</p>
              <p className={`text-2xl font-bold ${paymentSummary?.netCashFlow >= 0 ? 'text-green-500' : 'text-red-500'}`}>
                {formatCurrency(paymentSummary?.netCashFlow)}
              </p>
              <p className="text-xs text-slate-500">{paymentSummary?.totalPayments || 0} transactions</p>
            </div>
            <div className={`p-3 ${paymentSummary?.netCashFlow >= 0 ? 'bg-green-500/10' : 'bg-red-500/10'} rounded-lg`}>
              <DollarSign className={`h-6 w-6 ${paymentSummary?.netCashFlow >= 0 ? 'text-green-500' : 'text-red-500'}`} />
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-slate-400 text-sm">Unreconciled</p>
              <p className="text-2xl font-bold text-yellow-500">{paymentSummary?.unreconciledPayments || 0}</p>
              <p className="text-xs text-slate-500">Payments pending</p>
            </div>
            <div className="p-3 bg-yellow-500/10 rounded-lg">
              <CreditCard className="h-6 w-6 text-yellow-500" />
            </div>
          </div>
        </Card>
      </div>

      {/* Payables & Receivables */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Card className="p-4">
          <h3 className="text-lg font-semibold text-white mb-4 flex items-center gap-2">
            <TrendingUp className="h-5 w-5 text-green-500" />
            Receivables
          </h3>
          <p className="text-3xl font-bold text-white">{formatCurrency(totalReceivables)}</p>
          <p className="text-sm text-slate-400 mt-2">Outstanding customer invoices</p>
        </Card>

        <Card className="p-4">
          <h3 className="text-lg font-semibold text-white mb-4 flex items-center gap-2">
            <TrendingDown className="h-5 w-5 text-red-500" />
            Payables
          </h3>
          <p className="text-3xl font-bold text-white">{formatCurrency(totalPayables)}</p>
          <div className="flex items-center gap-4 mt-2">
            <span className="text-sm text-slate-400">{supplierInvoiceSummary?.pendingApproval || 0} pending approval</span>
            <span className="text-sm text-red-400">{supplierInvoiceSummary?.overdueInvoices || 0} overdue</span>
          </div>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Payments */}
        <Card className="p-4">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-semibold text-white">Recent Payments</h3>
            <Link to="/accounting/payments" className="text-brand-400 text-sm hover:text-brand-300">
              View All
            </Link>
          </div>
          <div className="space-y-3">
            {recentPayments?.length === 0 ? (
              <p className="text-slate-400 text-sm">No recent payments</p>
            ) : (
              recentPayments?.slice(0, 5).map((payment: Payment) => (
                <div key={payment.id} className="flex justify-between items-center py-2 border-b border-slate-700 last:border-0">
                  <div>
                    <p className="text-white font-medium">{(payment as any).payerPayeeName || 'N/A'}</p>
                    <p className="text-xs text-slate-400">{(payment as any).paymentNumber || payment.id} • {formatDate(payment.paymentDate)}</p>
                  </div>
                  <span className={`font-semibold ${payment.paymentType === 'Income' ? 'text-green-500' : 'text-red-500'}`}>
                    {payment.paymentType === 'Income' ? '+' : '-'}{formatCurrency(payment.amount)}
                  </span>
                </div>
              ))
            )}
          </div>
        </Card>

        {/* Overdue Invoices */}
        <Card className="p-4">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-semibold text-white flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-red-500" />
              Overdue Invoices
            </h3>
            <Link to="/accounting/supplier-invoices?status=Overdue" className="text-brand-400 text-sm hover:text-brand-300">
              View All
            </Link>
          </div>
          <div className="space-y-3">
            {overdueInvoices?.length === 0 ? (
              <p className="text-green-400 text-sm">No overdue invoices - Great job!</p>
            ) : (
              overdueInvoices?.slice(0, 5).map((invoice: SupplierInvoice) => (
                <div key={invoice.id} className="flex justify-between items-center py-2 border-b border-slate-700 last:border-0">
                  <div>
                    <p className="text-white font-medium">{invoice.supplierName || 'N/A'}</p>
                    <p className="text-xs text-slate-400">{invoice.invoiceNumber} • Due: {formatDate(invoice.dueDate)}</p>
                  </div>
                  <span className="text-red-500 font-semibold">{formatCurrency((invoice as any).outstandingAmount || invoice.totalAmount)}</span>
                </div>
              ))
            )}
          </div>
        </Card>
      </div>

      {/* Monthly Trend Chart Placeholder */}
      {paymentSummary?.monthlyTrend && paymentSummary.monthlyTrend.length > 0 && (
        <Card className="p-4">
          <h3 className="text-lg font-semibold text-white mb-4">Monthly Cash Flow Trend</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-slate-400 border-b border-slate-700">
                  <th className="text-left py-2">Period</th>
                  <th className="text-right py-2">Income</th>
                  <th className="text-right py-2">Expenses</th>
                  <th className="text-right py-2">Net</th>
                </tr>
              </thead>
              <tbody>
                {paymentSummary.monthlyTrend.map((month) => (
                  <tr key={month.period} className="border-b border-slate-700/50">
                    <td className="py-2 text-white">{month.period}</td>
                    <td className="py-2 text-right text-green-500">{formatCurrency(month.income)}</td>
                    <td className="py-2 text-right text-red-500">{formatCurrency(month.expenses)}</td>
                    <td className={`py-2 text-right font-medium ${month.net >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                      {formatCurrency(month.net)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
      </div>
    </PageShell>
  );
};

export default AccountingDashboardPage;

