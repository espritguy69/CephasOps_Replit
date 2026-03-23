import React from 'react';
import { AccumulationChartComponent, AccumulationSeriesCollectionDirective, AccumulationSeriesDirective, Inject, AccumulationLegend, PieSeries, AccumulationTooltip, AccumulationDataLabel } from '@syncfusion/ej2-react-charts';

interface PartnerDataPoint {
  partner: string;
  count: number;
  color?: string;
}

interface OrdersByPartnerChartProps {
  data: PartnerDataPoint[];
  title?: string;
  height?: string;
}

/**
 * Orders by Partner Pie Chart
 * Shows distribution of orders across partners
 * Uses Syncfusion Accumulation Charts
 */
const OrdersByPartnerChart: React.FC<OrdersByPartnerChartProps> = ({ 
  data, 
  title = 'Orders by Partner',
  height = '350px' 
}) => {
  const legendSettings = {
    visible: true,
    position: 'Bottom' as any,
    textStyle: { color: 'hsl(var(--foreground))' }
  };

  const dataLabel = {
    visible: true,
    position: 'Outside' as any,
    name: 'text',
    font: {
      fontWeight: '600',
      color: 'hsl(var(--foreground))'
    },
    connectorStyle: { length: '20px', color: 'hsl(var(--border))' }
  };

  const tooltip = {
    enable: true,
    format: '<b>${point.x}</b><br/>Orders: <b>${point.y}</b><br/>Percentage: <b>${point.percentage}%</b>',
    fill: 'hsl(var(--popover))',
    textStyle: { color: 'hsl(var(--popover-foreground))' }
  };

  // Color palette for partners
  const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

  const enhancedData = data.map((item, index) => ({
    x: item.partner,
    y: item.count,
    text: `${item.partner}: ${item.count}`,
    fill: item.color || colors[index % colors.length]
  }));

  return (
    <div className="bg-card rounded-xl border border-border shadow-sm p-4 md:p-6">
      <h3 className="text-base md:text-lg font-semibold text-foreground mb-4">{title}</h3>
      <AccumulationChartComponent
        id="orders-by-partner-chart"
        legendSettings={legendSettings}
        tooltip={tooltip}
        background="transparent"
        height={height}
        enableSmartLabels={true}
      >
        <Inject services={[AccumulationLegend, PieSeries, AccumulationTooltip, AccumulationDataLabel]} />
        <AccumulationSeriesCollectionDirective>
          <AccumulationSeriesDirective
            dataSource={enhancedData}
            xName="x"
            yName="y"
            type="Pie"
            dataLabel={dataLabel}
            radius="70%"
            explode={true}
            explodeOffset="10%"
            explodeIndex={0}
          />
        </AccumulationSeriesCollectionDirective>
      </AccumulationChartComponent>
    </div>
  );
};

export default OrdersByPartnerChart;

