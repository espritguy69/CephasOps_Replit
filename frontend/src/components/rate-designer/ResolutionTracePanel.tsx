import React from 'react';
import { Card } from '../ui';

interface ResolutionTracePanelProps {
  steps: string[];
  title?: string;
}

export const ResolutionTracePanel: React.FC<ResolutionTracePanelProps> = ({
  steps,
  title = 'Resolution trace'
}) => {
  return (
    <Card
      title={title}
      subtitle="How the engine derived the result. Use this to see which rule won and which modifiers applied."
    >
      {steps.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          Run the calculator to see the resolution steps.
        </p>
      ) : (
        <ul className="text-sm space-y-1.5 list-none pl-0 font-mono bg-muted/20 rounded p-3 max-h-64 overflow-y-auto">
          {steps.map((step, i) => (
            <li key={i} className="border-l-2 border-primary/30 pl-2 py-0.5">
              {step}
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
};
