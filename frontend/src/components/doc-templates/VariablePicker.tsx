import React, { useMemo, useState } from 'react';
import { Input, Badge, Card } from '../ui';
import { AlertTriangle } from 'lucide-react';

interface VariablePickerProps {
  variables: string[];
  onInsert: (value: string) => void;
  loading?: boolean;
  error?: string | null;
}

const VariablePicker: React.FC<VariablePickerProps> = ({ variables, onInsert, loading, error }) => {
  const [query, setQuery] = useState('');

  const filtered = useMemo(() => {
    if (!query.trim()) return variables;
    const lower = query.toLowerCase();
    return variables.filter((variable) => variable.toLowerCase().includes(lower));
  }, [query, variables]);

  return (
    <Card className="p-4 space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-xs md:text-sm font-semibold">Placeholders</h3>
        {loading && <span className="text-xs text-muted-foreground">Loading...</span>}
      </div>
      {error && (
        <div className="flex items-center gap-2 text-xs text-destructive">
          <AlertTriangle className="h-4 w-4" />
          {error}
        </div>
      )}
      <Input
        placeholder="Search placeholders"
        value={query}
        onChange={(event) => setQuery(event.target.value)}
      />
      <div className="flex flex-wrap gap-2">
        {filtered.length === 0 && (
          <span className="text-xs text-muted-foreground">No placeholders found.</span>
        )}
        {filtered.map((variable) => (
          <button
            key={variable}
            type="button"
            onClick={() => onInsert(variable)}
            className="hover:opacity-80"
          >
            <Badge variant="secondary">{variable}</Badge>
          </button>
        ))}
      </div>
    </Card>
  );
};

export default VariablePicker;
