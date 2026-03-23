import React from 'react';
import { BarChart3 } from 'lucide-react';

interface ChartDataPoint {
  label: string;
  value: number;
}

interface SalesBarChartProps {
  data?: ChartDataPoint[];
  title?: string;
}

const SalesBarChart: React.FC<SalesBarChartProps> = ({ data = [], title = "Sales Overview" }) => {
  // Generate sample data if none provided
  const chartData: ChartDataPoint[] = data.length > 0 ? data : [
    { label: 'Mon', value: 45 },
    { label: 'Tue', value: 52 },
    { label: 'Wed', value: 48 },
    { label: 'Thu', value: 61 },
    { label: 'Fri', value: 55 },
    { label: 'Sat', value: 67 },
    { label: 'Sun', value: 58 }
  ];

  const maxValue = Math.max(...chartData.map(d => d.value), 1);

  return (
    <div className="rounded-xl bg-layout-card shadow-card p-5">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-sm font-semibold text-slate-900">{title}</h3>
          <p className="text-xs text-slate-600 mt-0.5">Weekly sales</p>
        </div>
        <BarChart3 className="h-4 w-4 text-primary" />
      </div>

      <div className="h-48 flex items-end justify-between gap-2">
        {chartData.map((d, i) => (
          <div key={i} className="flex-1 flex flex-col items-center gap-2">
            <div className="w-full flex flex-col items-center justify-end" style={{ height: '100%' }}>
              <div
                className="w-full rounded-t bg-primary hover:bg-primary/80 transition-colors relative group"
                style={{ height: `${(d.value / maxValue) * 100}%` }}
              >
                <div className="absolute -top-6 left-1/2 transform -translate-x-1/2 opacity-0 group-hover:opacity-100 transition-opacity">
                  <div className="bg-slate-900 text-white text-xs px-2 py-1 rounded whitespace-nowrap">
                    {d.value}
                  </div>
                </div>
              </div>
            </div>
            <span className="text-xs text-slate-500">{d.label}</span>
          </div>
        ))}
      </div>
    </div>
  );
};

export default SalesBarChart;

