import React from 'react';
import { Card } from '../ui';
import VariablePicker from './VariablePicker';
import ValidationWarnings from './ValidationWarnings';
import type { OutputType } from '../../lib/docTemplates/constants';

interface TemplatePreviewPanelProps {
  outputType: OutputType;
  previewHtml: string;
  previewError?: string | null;
  variables: string[];
  onInsertVariable: (value: string) => void;
  variablesLoading?: boolean;
  variablesError?: string | null;
  unknownPlaceholders: string[];
  missingRecommended: string[];
}

const TemplatePreviewPanel: React.FC<TemplatePreviewPanelProps> = ({
  outputType,
  previewHtml,
  previewError,
  variables,
  onInsertVariable,
  variablesLoading,
  variablesError,
  unknownPlaceholders,
  missingRecommended,
}) => {
  return (
    <div className="space-y-4">
      <VariablePicker
        variables={variables}
        onInsert={onInsertVariable}
        loading={variablesLoading}
        error={variablesError}
      />

      <Card className="p-4 space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="text-xs md:text-sm font-semibold">Live Preview</h3>
          {outputType !== 'HTML' && (
            <span className="text-xs text-muted-foreground">Final formatting may differ</span>
          )}
        </div>
        {previewError ? (
          <div className="text-xs text-destructive">{previewError}</div>
        ) : (
          <div
            className="prose max-w-none text-sm border border-border rounded-lg p-3 bg-background/60"
            dangerouslySetInnerHTML={{ __html: previewHtml }}
          />
        )}
      </Card>

      <ValidationWarnings
        unknownPlaceholders={unknownPlaceholders}
        missingRecommended={missingRecommended}
      />
    </div>
  );
};

export default TemplatePreviewPanel;
