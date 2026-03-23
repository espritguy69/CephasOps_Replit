import React from 'react';
import { SparklineComponent, Inject, SparklineTooltip } from '@syncfusion/ej2-react-charts';

interface SparklineProps {
  data: number[];
  type?: 'Line' | 'Area' | 'Column' | 'WinLoss';
  color?: string;
  height?: string;
  width?: string;
}

/**
 * Sparkline Component
 * Tiny charts for KPI cards showing mini trends
 * Uses Syncfusion Sparkline
 */
const Sparkline: React.FC<SparklineProps> = ({ 
  data, 
  type = 'Line',
  color = '#3b82f6',
  height = '40px',
  width = '100%'
}) => {
  return (
    <SparklineComponent
      id={`sparkline-${Math.random().toString(36).substr(2, 9)}`}
      height={height}
      width={width}
      lineWidth={2}
      fill={color}
      dataSource={data.map((value, index) => ({ x: index, y: value }))}
      xName="x"
      yName="y"
      type={type}
      tooltipSettings={{
        visible: true,
        format: '${y}',
        trackLineSettings: {
          visible: true,
          color: color,
          width: 1
        }
      }}
    >
      <Inject services={[SparklineTooltip]} />
    </SparklineComponent>
  );
};

export default Sparkline;

