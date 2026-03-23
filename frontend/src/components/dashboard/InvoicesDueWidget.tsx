import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Receipt, AlertCircle, Calendar } from 'lucide-react';
import { getInvoices } from '../../api/billing';
import { Card } from '../ui';
import type { Invoice } from '../../types/billing';

const InvoicesDueWidget: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState<boolean>(true);
  const [dueInvoices, setDueInvoices] = useState<Invoice[]>([]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const invoices = await getInvoices({
        status: 'Invoiced'
      });

      const today = new Date();
      today.setHours(0, 0, 0, 0);

      // Filter invoices that are due soon (within 7 days) or overdue
      const due = invoices.filter((invoice) => {
        if (!invoice.dueDate) return false;
        const dueDate = new Date(invoice.dueDate);
        dueDate.setHours(0, 0, 0, 0);
        const daysUntilDue = Math.ceil((dueDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
        return daysUntilDue <= 7; // Due within 7 days or overdue
      })
      .sort((a, b) => {
        const dateA = new Date(a.dueDate).getTime();
        const dateB = new Date(b.dueDate).getTime();
        return dateA - dateB;
      })
      .slice(0, 5); // Show top 5

      setDueInvoices(due);
    } catch (err) {
      console.error('Failed to load invoices due:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <Card className="p-3">
        <div className="animate-pulse text-xs text-muted-foreground">Loading...</div>
      </Card>
    );
  }

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-MY', {
      style: 'currency',
      currency: 'MYR',
      minimumFractionDigits: 0
    }).format(amount);
  };

  const formatDate = (dateString: string | undefined): string => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric',
      year: 'numeric'
    });
  };

  const getDaysUntilDue = (dueDate: string | undefined): number | null => {
    if (!dueDate) return null;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const due = new Date(dueDate);
    due.setHours(0, 0, 0, 0);
    const diff = Math.ceil((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return diff;
  };

  return (
    <Card className="p-3">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <Receipt className="h-4 w-4 text-red-500" />
          <h3 className="text-xs font-semibold text-foreground">Invoices Nearing Due</h3>
        </div>
        <button
          onClick={() => navigate('/billing/invoices')}
          className="text-xs text-primary hover:underline"
        >
          View All
        </button>
      </div>

      <div className="space-y-1.5">
        {dueInvoices.length > 0 ? (
          dueInvoices.map((invoice) => {
            const daysUntilDue = getDaysUntilDue(invoice.dueDate);
            const isOverdue = daysUntilDue !== null && daysUntilDue < 0;
            const isDueSoon = daysUntilDue !== null && daysUntilDue >= 0 && daysUntilDue <= 3;

            return (
              <div
                key={invoice.id}
                className="flex items-center justify-between text-xs p-1.5 rounded hover:bg-muted cursor-pointer"
                onClick={() => navigate(`/billing/invoices/${invoice.id}`)}
              >
                <div className="flex-1 min-w-0">
                  <div className="font-medium text-foreground truncate">
                    {invoice.invoiceNumber || 'N/A'}
                  </div>
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <span>{invoice.partnerName || 'Unknown Partner'}</span>
                    <span className="font-semibold text-foreground">
                      {formatCurrency(invoice.totalAmount || 0)}
                    </span>
                  </div>
                </div>
                <div className="flex items-center gap-1 ml-2">
                  <Calendar className="h-3 w-3" />
                  <span className={`text-xs ${
                    isOverdue ? 'text-red-600 font-semibold' :
                    isDueSoon ? 'text-orange-600 font-semibold' :
                    'text-muted-foreground'
                  }`}>
                    {daysUntilDue === null 
                      ? 'N/A'
                      : isOverdue 
                      ? `${Math.abs(daysUntilDue)}d overdue`
                      : daysUntilDue === 0
                      ? 'Due today'
                      : `${daysUntilDue}d left`
                    }
                  </span>
                </div>
              </div>
            );
          })
        ) : (
          <div className="text-xs text-muted-foreground text-center py-2">
            No invoices due soon
          </div>
        )}
      </div>
    </Card>
  );
};

export default InvoicesDueWidget;

