import React from 'react';
import { ChartComponent, SeriesCollectionDirective, SeriesDirective, Inject, LineSeries, Category, Tooltip, Legend, DataLabel, AreaSeries } from '@syncfusion/ej2-react-charts';

interface DepreciationDataPoint {
  year: string;
  purchaseValue: number;
  currentValue: number;
  residualValue: number;
}

interface DepreciationCurveChartProps {
  data: DepreciationDataPoint[];
  title?: string;
  height?: string;
}

/**
 * Asset Depreciation Curve Chart
 * Shows asset value decline over time
 * Purchase Value → Current Value → Residual Value
 */
const DepreciationCurveChart: React.FC<DepreciationCurveChartProps> = ({ 
  data, 
  title = 'Asset Depreciation Over Time',
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
        id="depreciation-curve-chart"
        primaryXAxis={primaryXAxis}
        primaryYAxis={primaryYAxis}
        tooltip={tooltip}
        legendSettings={legendSettings}
        background="transparent"
        height={height}
      >
        <Inject services={[LineSeries, AreaSeries, Category, Tooltip, Legend, DataLabel]} />
        <SeriesCollectionDirective>
          <SeriesDirective
            dataSource={data}
            xName="year"
            yName="purchaseValue"
            name="Purchase Value"
            type="Line"
            width={2}
            fill="#3b82f6"
            marker={{ ...marker, fill: '#3b82f6' }}
            dashArray="5,5"
          />
          <SeriesDirective
            dataSource={data}
            xName="year"
            yName="currentValue"
            name="Current Value"
            type="Area"
            fill="#10b981"
            opacity={0.3}
            border={{ width: 2, color: '#10b981' }}
            marker={{ ...marker, fill: '#10b981' }}
          />
          <SeriesDirective
            dataSource={data}
            xName="year"
            yName="residualValue"
            name="Residual Value"
            type="Line"
            width={2}
            fill="#f59e0b"
            marker={{ ...marker, fill: '#f59e0b' }}
            dashArray="10,5"
          />
        </SeriesCollectionDirective>
      </ChartComponent>
    </div>
  );
};

export default DepreciationCurveChart;

