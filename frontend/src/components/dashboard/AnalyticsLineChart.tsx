import React from 'react';
import { TrendingUp } from 'lucide-react';

interface ChartDataPoint {
  label: string;
  value: number;
}

interface AnalyticsLineChartProps {
  data?: ChartDataPoint[];
  title?: string;
}

const AnalyticsLineChart: React.FC<AnalyticsLineChartProps> = ({ data = [], title = "Analytics Overview" }) => {
  // Generate sample data if none provided
  const chartData: ChartDataPoint[] = data.length > 0 ? data : Array.from({ length: 7 }, (_, i) => ({
    label: `Day ${i + 1}`,
    value: Math.floor(Math.random() * 100) + 20
  }));

  const maxValue = Math.max(...chartData.map(d => d.value), 1);
  const minValue = Math.min(...chartData.map(d => d.value), 0);

  return (
    <div className="rounded-xl bg-layout-card shadow-card p-5">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-sm font-semibold text-slate-900">{title}</h3>
          <p className="text-xs text-slate-600 mt-0.5">Last 7 days</p>
        </div>
        <TrendingUp className="h-4 w-4 text-primary" />
      </div>

      <div className="h-48 relative">
        <svg className="w-full h-full" viewBox="0 0 400 200" preserveAspectRatio="none">
          {/* Grid lines */}
          {[0, 1, 2, 3, 4].map((i) => (
            <line
              key={i}
              x1="0"
              y1={40 + i * 40}
              x2="400"
              y2={40 + i * 40}
              stroke="#e2e8f0"
              strokeWidth="1"
            />
          ))}

          {/* Line chart */}
          <polyline
            points={chartData.map((d, i) => {
              const x = (i / (chartData.length - 1)) * 380 + 20;
              const y = 180 - ((d.value - minValue) / (maxValue - minValue || 1)) * 140 - 20;
              return `${x},${y}`;
            }).join(' ')}
            fill="none"
            stroke="#9874D3"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* Area under line */}
          <polygon
            points={`20,180 ${chartData.map((d, i) => {
              const x = (i / (chartData.length - 1)) * 380 + 20;
              const y = 180 - ((d.value - minValue) / (maxValue - minValue || 1)) * 140 - 20;
              return `${x},${y}`;
            }).join(' ')} 380,180`}
            fill="url(#gradient)"
            opacity="0.1"
          />

          <defs>
            <linearGradient id="gradient" x1="0%" y1="0%" x2="0%" y2="100%">
              <stop offset="0%" stopColor="#9874D3" stopOpacity="0.3" />
              <stop offset="100%" stopColor="#9874D3" stopOpacity="0" />
            </linearGradient>
          </defs>

          {/* Data points */}
          {chartData.map((d, i) => {
            const x = (i / (chartData.length - 1)) * 380 + 20;
            const y = 180 - ((d.value - minValue) / (maxValue - minValue || 1)) * 140 - 20;
            return (
              <circle
                key={i}
                cx={x}
                cy={y}
                r="4"
                fill="#9874D3"
                stroke="#ffffff"
                strokeWidth="2"
              />
            );
          })}
        </svg>

        {/* X-axis labels */}
        <div className="absolute bottom-0 left-0 right-0 flex justify-between px-2">
          {chartData.map((d, i) => (
            <span key={i} className="text-xs text-slate-500">
              {d.label}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
};

export default AnalyticsLineChart;

