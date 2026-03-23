import React from 'react';
import { ChartComponent, SeriesCollectionDirective, SeriesDirective, Inject, AreaSeries, Category, Tooltip, Legend, DataLabel, DateTime, StepLineSeries } from '@syncfusion/ej2-react-charts';

interface StockTrendDataPoint {
  date: string;
  value: number;
  safetyStock?: number;
}

interface StockValueTrendChartProps {
  data: StockTrendDataPoint[];
  title?: string;
  height?: string;
}

/**
 * Stock Value Trend Chart
 * Shows inventory value over time with safety stock level indicator
 * Area chart for professional financial visualization
 */
const StockValueTrendChart: React.FC<StockValueTrendChartProps> = ({ 
  data, 
  title = 'Stock Value Trend - Last 90 Days',
  height = '350px' 
}) => {
  const primaryXAxis = {
    valueType: 'Category' as any,
    labelStyle: { color: 'hsl(var(--muted-foreground))' },
    majorGridLines: { width: 0 }
  };

  const primaryYAxis = {
    labelFormat: 'RM {value}',
    labelStyle: { color: 'hsl(var(--muted-foreground))' },
    majorGridLines: { color: 'hsl(var(--border))', width: 1 }
  };

  const marker = {
    visible: true,
    width: 6,
    height: 6,
    fill: '#10b981',
    border: { width: 2, color: '#ffffff' }
  };

  const tooltip = {
    enable: true,
    shared: true,
    format: '<b>${point.x}</b><br/>Value: <b>RM ${point.y.toLocaleString()}</b>',
    fill: 'hsl(var(--popover))',
    textStyle: { color: 'hsl(var(--popover-foreground))' }
  };

  const legendSettings = {
    visible: true,
    position: 'Bottom' as any,
    textStyle: { color: 'hsl(var(--foreground))' }
  };

  return (
    <div className="bg-card rounded-xl border border-border shadow-sm p-4 md:p-6">
      <h3 className="text-base md:text-lg font-semibold text-foreground mb-4">{title}</h3>
      <ChartComponent
        id="stock-value-trend-chart"
        primaryXAxis={primaryXAxis}
        primaryYAxis={primaryYAxis}
        tooltip={tooltip}
        legendSettings={legendSettings}
        background="transparent"
        height={height}
      >
        <Inject services={[AreaSeries, StepLineSeries, Category, Tooltip, Legend, DataLabel, DateTime]} />
        <SeriesCollectionDirective>
          <SeriesDirective
            dataSource={data}
            xName="date"
            yName="value"
            name="Stock Value"
            type="Area"
            fill="#10b981"
            opacity={0.4}
            border={{ width: 2, color: '#10b981' }}
            marker={marker}
          />
          {data.some(d => d.safetyStock) && (
            <SeriesDirective
              dataSource={data}
              xName="date"
              yName="safetyStock"
              name="Safety Stock Level"
              type="StepLine"
              width={2}
              fill="#f59e0b"
              dashArray="5,5"
            />
          )}
        </SeriesCollectionDirective>
      </ChartComponent>
    </div>
  );
};

export default StockValueTrendChart;

