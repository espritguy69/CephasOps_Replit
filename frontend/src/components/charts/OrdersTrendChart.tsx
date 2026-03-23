import React from 'react';
import { ChartComponent, SeriesCollectionDirective, SeriesDirective, Inject, LineSeries, Category, Tooltip, Legend, DataLabel, DateTime, SplineSeries } from '@syncfusion/ej2-react-charts';

interface TrendDataPoint {
  date: string;
  count: number;
}

interface OrdersTrendChartProps {
  data: TrendDataPoint[];
  title?: string;
  height?: string;
}

/**
 * Orders Trend Chart
 * Shows order count trend over the last 30 days
 * Uses Syncfusion Charts for professional visualization
 */
const OrdersTrendChart: React.FC<OrdersTrendChartProps> = ({ 
  data, 
  title = 'Orders Trend - Last 30 Days',
  height = '350px' 
}) => {
  const primaryXAxis = {
    valueType: 'Category' as any,
    labelStyle: { color: 'hsl(var(--muted-foreground))' },
    majorGridLines: { width: 0 },
    minorGridLines: { width: 0 },
    majorTickLines: { width: 0 },
    minorTickLines: { width: 0 },
    lineStyle: { width: 0 }
  };

  const primaryYAxis = {
    labelStyle: { color: 'hsl(var(--muted-foreground))' },
    lineStyle: { width: 0 },
    majorTickLines: { width: 0 },
    majorGridLines: { color: 'hsl(var(--border))', width: 1 },
    minorGridLines: { width: 0 },
    minorTickLines: { width: 0 }
  };

  const marker = {
    visible: true,
    width: 8,
    height: 8,
    fill: '#9874D3',
    border: { width: 2, color: '#ffffff' }
  };

  const chartArea = {
    border: { width: 0 }
  };

  const tooltip = {
    enable: true,
    format: '${point.x}: <b>${point.y} orders</b>',
    fill: 'hsl(var(--popover))',
    textStyle: { color: 'hsl(var(--popover-foreground))' }
  };

  return (
    <div className="bg-card rounded-xl border border-border shadow-sm p-4 md:p-6">
      <h3 className="text-base md:text-lg font-semibold text-foreground mb-4">{title}</h3>
      <ChartComponent
        id="orders-trend-chart"
        primaryXAxis={primaryXAxis}
        primaryYAxis={primaryYAxis}
        tooltip={tooltip}
        chartArea={chartArea}
        background="transparent"
        height={height}
      >
        <Inject services={[SplineSeries, Category, Tooltip, Legend, DataLabel, DateTime]} />
        <SeriesCollectionDirective>
          <SeriesDirective
            dataSource={data}
            xName="date"
            yName="count"
            name="Orders"
            type="Spline"
            width={3}
            fill="#9874D3"
            marker={marker}
          />
        </SeriesCollectionDirective>
      </ChartComponent>
    </div>
  );
};

export default OrdersTrendChart;

