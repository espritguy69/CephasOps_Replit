import React from 'react';
import { ChartComponent, SeriesCollectionDirective, SeriesDirective, Inject, LineSeries, Category, Tooltip, Legend, DataLabel, DateTime, AreaSeries, Crosshair, ColumnSeries } from '@syncfusion/ej2-react-charts';

interface PnlTrendDataPoint {
  month: string;
  revenue: number;
  costs: number;
  profit: number;
}

interface PnlTrendChartProps {
  data: PnlTrendDataPoint[];
  title?: string;
  height?: string;
}

const PnlTrendChart: React.FC<PnlTrendChartProps> = ({ 
  data, 
  title = 'P&L Trend',
  height = '400px' 
}) => {
  if (!data || data.length === 0) {
    return (
      <div className="bg-card rounded-xl border border-border shadow-sm p-4 md:p-6">
        <h3 className="text-base md:text-lg font-semibold text-foreground mb-4">{title}</h3>
        <div className="flex items-center justify-center" style={{ height }}>
          <p className="text-sm text-muted-foreground">No trend data available</p>
        </div>
      </div>
    );
  }

  const useColumnChart = data.length <= 2;
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
    width: 8,
    height: 8,
    border: { width: 2, color: '#ffffff' }
  };

  const tooltip = {
    enable: true,
    shared: true,
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
        id="pnl-trend-chart"
        primaryXAxis={primaryXAxis}
        primaryYAxis={primaryYAxis}
        tooltip={tooltip}
        legendSettings={legendSettings}
        background="transparent"
        height={height}
        crosshair={{ enable: true, lineType: 'Vertical' }}
      >
        <Inject services={[LineSeries, AreaSeries, ColumnSeries, Category, Tooltip, Legend, DataLabel, DateTime, Crosshair]} />
        <SeriesCollectionDirective>
          <SeriesDirective
            dataSource={data}
            xName="month"
            yName="revenue"
            name="Revenue"
            type={useColumnChart ? 'Column' : 'Line'}
            width={3}
            fill="#10b981"
            marker={useColumnChart ? undefined : marker}
          />
          <SeriesDirective
            dataSource={data}
            xName="month"
            yName="costs"
            name="Costs"
            type={useColumnChart ? 'Column' : 'Line'}
            width={3}
            fill="#ef4444"
            marker={useColumnChart ? undefined : marker}
            dashArray={useColumnChart ? undefined : "5,5"}
          />
          <SeriesDirective
            dataSource={data}
            xName="month"
            yName="profit"
            name="Net Profit"
            type={useColumnChart ? 'Column' : 'Area'}
            fill="#9874D3"
            opacity={useColumnChart ? 1 : 0.3}
            border={useColumnChart ? undefined : { width: 2, color: '#9874D3' }}
            marker={useColumnChart ? undefined : marker}
          />
        </SeriesCollectionDirective>
      </ChartComponent>
    </div>
  );
};

export default PnlTrendChart;

