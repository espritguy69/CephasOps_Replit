import React from 'react';
import { ChartComponent, SeriesCollectionDirective, SeriesDirective, Inject, Category, Tooltip, Legend, DataLabel, WaterfallSeries } from '@syncfusion/ej2-react-charts';

interface WaterfallDataPoint {
  x: string;
  y: number;
  intermediateSum?: boolean;
  sum?: boolean;
}

interface PnlWaterfallChartProps {
  revenue: number;
  siCosts: number;
  materialCosts: number;
  overheads: number;
  title?: string;
  height?: string;
}

/**
 * PnL Waterfall Chart
 * Shows how revenue flows to net profit
 * Revenue → SI Costs → Material Costs → Overheads → Net Profit
 * Management LOVES this visualization!
 */
const PnlWaterfallChart: React.FC<PnlWaterfallChartProps> = ({ 
  revenue,
  siCosts,
  materialCosts,
  overheads,
  title = 'P&L Waterfall - Revenue to Net Profit',
  height = '400px' 
}) => {
  const netProfit = revenue - siCosts - materialCosts - overheads;

  const data: WaterfallDataPoint[] = [
    { x: 'Revenue', y: revenue },
    { x: 'SI Costs', y: -siCosts },
    { x: 'Material Costs', y: -materialCosts },
    { x: 'Overheads', y: -overheads },
    { x: 'Net Profit', y: netProfit, sum: true }
  ];

  const primaryXAxis = {
    valueType: 'Category' as any,
    majorGridLines: { width: 0 },
    labelStyle: { color: 'hsl(var(--muted-foreground))', size: '12px' }
  };

  const primaryYAxis = {
    labelFormat: 'RM {value}',
    majorGridLines: { color: 'hsl(var(--border))', width: 1 },
    labelStyle: { color: 'hsl(var(--muted-foreground))' }
  };

  const dataLabel = {
    visible: true,
    font: { fontWeight: '600', color: 'hsl(var(--foreground))' },
    template: '<div>${point.y >= 0 ? "RM" : "-RM"} ${Math.abs(point.y).toLocaleString()}</div>'
  };

  const tooltip = {
    enable: true,
    format: '<b>${point.x}</b><br/>Amount: <b>RM ${point.y.toLocaleString()}</b>',
    fill: 'hsl(var(--popover))',
    textStyle: { color: 'hsl(var(--popover-foreground))' }
  };

  const marker = {
    dataLabel: dataLabel
  };

  const connector = {
    color: 'hsl(var(--border))',
    width: 2
  };

  return (
    <div className="bg-card rounded-xl border border-border shadow-sm p-4 md:p-6">
      <h3 className="text-base md:text-lg font-semibold text-foreground mb-4">{title}</h3>
      <ChartComponent
        id="pnl-waterfall-chart"
        primaryXAxis={primaryXAxis}
        primaryYAxis={primaryYAxis}
        tooltip={tooltip}
        background="transparent"
        height={height}
      >
        <Inject services={[WaterfallSeries, Category, Tooltip, Legend, DataLabel]} />
        <SeriesCollectionDirective>
          <SeriesDirective
            dataSource={data}
            xName="x"
            yName="y"
            name="Amount"
            type="Waterfall"
            intermediateSumIndexes={[]}
            sumIndexes={[4]}
            marker={marker}
            connector={connector}
            columnWidth={0.6}
            negativeFillColor="#ef4444"
            summaryFillColor="#3b82f6"
          />
        </SeriesCollectionDirective>
      </ChartComponent>
    </div>
  );
};

export default PnlWaterfallChart;

