import React, { useMemo, useState } from 'react';
import { Button, Card, Modal, Textarea } from '../ui';
import { FlaskConical, AlertTriangle } from 'lucide-react';
import { testRenderTemplate } from '../../api/docTemplates';
import { DEFAULT_TEST_RENDER_DATA } from '../../lib/docTemplates/constants';

interface TestRenderDialogProps {
  open: boolean;
  onClose: () => void;
  templateContent: string;
  outputType: string;
}

const TestRenderDialog: React.FC<TestRenderDialogProps> = ({
  open,
  onClose,
  templateContent,
  outputType,
}) => {
  const [sampleJson, setSampleJson] = useState<string>(
    JSON.stringify(DEFAULT_TEST_RENDER_DATA, null, 2)
  );
  const [renderedHtml, setRenderedHtml] = useState<string>('');
  const [warnings, setWarnings] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const parsedJson = useMemo(() => {
    try {
      return JSON.parse(sampleJson) as Record<string, unknown>;
    } catch {
      return null;
    }
  }, [sampleJson]);

  const handleRender = async () => {
    if (!parsedJson) {
      setError('Sample data must be valid JSON.');
      return;
    }
    try {
      setLoading(true);
      setError(null);
      const response = await testRenderTemplate({
        templateContent,
        outputType,
        dataJson: parsedJson,
      });
      setRenderedHtml(response.renderedHtml);
      setWarnings(response.warnings || []);
    } catch (err: any) {
      setError(err?.message || 'Failed to render template.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={open} onClose={onClose} size="xl" title="Test Render">
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <Card className="p-4 space-y-3">
          <div className="flex items-center gap-2 text-sm font-semibold">
            <FlaskConical className="h-4 w-4" />
            Sample JSON Data
          </div>
          <Textarea
            rows={14}
            value={sampleJson}
            onChange={(event) => setSampleJson(event.target.value)}
            className="font-mono text-xs md:text-sm"
          />
          <Button onClick={handleRender} disabled={loading}>
            {loading ? 'Rendering...' : 'Render'}
          </Button>
          {error && (
            <div className="text-xs text-destructive flex items-center gap-2">
              <AlertTriangle className="h-4 w-4" />
              {error}
            </div>
          )}
          {warnings.length > 0 && (
            <div className="text-xs text-muted-foreground space-y-1">
              <div>Warnings:</div>
              {warnings.map((warning) => (
                <div key={warning}>- {warning}</div>
              ))}
            </div>
          )}
        </Card>
        <Card className="p-4 space-y-3">
          <div className="text-sm font-semibold">Rendered Output</div>
          {renderedHtml ? (
            <div
              className="prose max-w-none text-sm border border-border rounded-lg p-3 bg-background/60"
              dangerouslySetInnerHTML={{ __html: renderedHtml }}
            />
          ) : (
            <div className="text-xs text-muted-foreground">Run a test render to see output.</div>
          )}
        </Card>
      </div>
    </Modal>
  );
};

export default TestRenderDialog;
