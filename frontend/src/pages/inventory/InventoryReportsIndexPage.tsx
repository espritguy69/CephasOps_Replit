import React from 'react';
import { useNavigate } from 'react-router-dom';
import { BarChart3, Activity, TrendingUp, FileBarChart } from 'lucide-react';
import { Card, EmptyState } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useDepartment } from '../../contexts/DepartmentContext';

const REPORTS = [
  { path: '/inventory/reports/usage', label: 'Usage by period', description: 'Received, transferred, issued, and returned totals in a date range. Group by material, location, or department.', icon: BarChart3 },
  { path: '/inventory/reports/serial-lifecycle', label: 'Serial lifecycle', description: 'Look up one or more serial numbers to see received → allocated → issued → returned events with timestamps and order refs.', icon: Activity },
  { path: '/inventory/reports/stock-trend', label: 'Stock trend by location', description: 'Daily, weekly, or monthly snapshots of quantity on hand by material and location.', icon: TrendingUp }
];

const InventoryReportsIndexPage: React.FC = () => {
  const navigate = useNavigate();
  const { departmentId } = useDepartment();

  if (!departmentId) {
    return (
      <PageShell title="Inventory Reports" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports' }]}>
        <EmptyState title="Department required" description="Please select a department from the header to access inventory reports." />
      </PageShell>
    );
  }

  return (
    <PageShell title="Inventory Reports" breadcrumbs={[{ label: 'Inventory', path: '/inventory/stock-summary' }, { label: 'Reports' }]}>
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">Ledger-based usage, serial lifecycle, and stock trend reports.</p>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {REPORTS.map(({ path, label, description, icon: Icon }) => (
          <Card
            key={path}
            className="p-4 cursor-pointer hover:border-primary/50 transition-colors"
            onClick={() => navigate(path)}
          >
            <div className="flex items-start gap-3">
              <div className="rounded-lg bg-primary/10 p-2">
                <Icon className="h-5 w-5 text-primary" />
              </div>
              <div className="flex-1 min-w-0">
                <h2 className="font-semibold">{label}</h2>
                <p className="text-sm text-muted-foreground mt-1">{description}</p>
              </div>
            </div>
          </Card>
        ))}
      </div>
      </div>
    </PageShell>
  );
};

export default InventoryReportsIndexPage;
