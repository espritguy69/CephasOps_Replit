import React from 'react';
import { Tabs, TabPanel } from '../ui';
import { Textarea } from '../ui';
import type { ContentFormat } from '../../lib/docTemplates/constants';

interface TemplateContentEditorProps {
  content: string;
  contentFormat: ContentFormat;
  onContentChange: (value: string) => void;
  onFormatChange: (value: ContentFormat) => void;
  editorRef: React.RefObject<HTMLTextAreaElement>;
  error?: string;
  disabled?: boolean;
}

const TemplateContentEditor: React.FC<TemplateContentEditorProps> = ({
  content,
  contentFormat,
  onContentChange,
  onFormatChange,
  editorRef,
  error,
  disabled = false,
}) => {
  const tabIndex = contentFormat === 'Markdown' ? 0 : 1;

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <div className="text-xs md:text-sm font-medium">Template Content</div>
        {error && <span className="text-xs text-destructive">{error}</span>}
      </div>

      <Tabs
        key={contentFormat}
        defaultActiveTab={tabIndex}
        onTabChange={(index) => onFormatChange(index === 0 ? 'Markdown' : 'HTML')}
      >
        <TabPanel label="Markdown">
          <Textarea
            ref={editorRef}
            value={content}
            rows={16}
            disabled={disabled}
            onChange={(event) => onContentChange(event.target.value)}
            className="font-mono text-xs md:text-sm"
            placeholder="Write markdown content with {{VariableName}} placeholders..."
          />
        </TabPanel>
        <TabPanel label="HTML">
          <Textarea
            ref={editorRef}
            value={content}
            rows={16}
            disabled={disabled}
            onChange={(event) => onContentChange(event.target.value)}
            className="font-mono text-xs md:text-sm"
            placeholder="<div>Write HTML content with {{VariableName}} placeholders...</div>"
          />
        </TabPanel>
      </Tabs>
      <p className="text-xs text-muted-foreground">
        Use placeholders in the format <code>{'{{VariableName}}'}</code>
      </p>
    </div>
  );
};

export default TemplateContentEditor;
