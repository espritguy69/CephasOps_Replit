import React from 'react';
import { AlertTriangle } from 'lucide-react';
import { Badge, Card } from '../ui';

interface ValidationWarningsProps {
  unknownPlaceholders: string[];
  missingRecommended: string[];
}

const ValidationWarnings: React.FC<ValidationWarningsProps> = ({
  unknownPlaceholders,
  missingRecommended,
}) => {
  if (unknownPlaceholders.length === 0 && missingRecommended.length === 0) {
    return null;
  }

  return (
    <Card className="p-4 space-y-3 border-amber-200 bg-amber-50">
      <div className="flex items-center gap-2 text-amber-800">
        <AlertTriangle className="h-4 w-4" />
        <span className="text-xs md:text-sm font-semibold">Validation Warnings</span>
      </div>
      {unknownPlaceholders.length > 0 && (
        <div className="space-y-2">
          <p className="text-xs text-amber-900">
            Unknown placeholders (must be fixed before publishing):
          </p>
          <div className="flex flex-wrap gap-2">
            {unknownPlaceholders.map((placeholder) => (
              <Badge key={placeholder} variant="secondary">
                {placeholder}
              </Badge>
            ))}
          </div>
        </div>
      )}
      {missingRecommended.length > 0 && (
        <div className="space-y-2">
          <p className="text-xs text-amber-900">Missing recommended placeholders:</p>
          <div className="flex flex-wrap gap-2">
            {missingRecommended.map((placeholder) => (
              <Badge key={placeholder} variant="outline">
                {placeholder}
              </Badge>
            ))}
          </div>
        </div>
      )}
    </Card>
  );
};

export default ValidationWarnings;
