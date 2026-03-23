import React from 'react';
import { Card } from '../ui';

export const ModifierPanel: React.FC = () => {
  return (
    <Card
      title="Modifiers"
      subtitle="Adjustments applied after the base rate (Installation Method → SI Tier → Partner)"
    >
      <p className="text-sm text-muted-foreground">
        Modifiers are applied during payout resolution in this order: Installation Method, then SI Tier, then Partner.
        Each can add a fixed amount or multiply the current amount. Which modifiers applied for your scenario appears in
        the <strong>Resolution trace</strong> below after you run the calculator.
      </p>
      <p className="text-sm text-muted-foreground mt-2">
        Modifier configuration is managed in the rate engine. This view is informational.
      </p>
    </Card>
  );
};
