import React, { useState } from 'react';
import { Badge, Button, Card, Input, Select, Textarea } from '../ui';
import type { FieldErrors, UseFormRegister, UseFormSetValue } from 'react-hook-form';
import type { ContentFormat, OutputType } from '../../lib/docTemplates/constants';

export interface TemplateFormValues {
  name: string;
  category: string;
  outputType: OutputType;
  status: 'Draft' | 'Active';
  tags: string[];
  description: string;
  content: string;
  contentFormat: ContentFormat;
}

interface TemplateMetaFormProps {
  register: UseFormRegister<TemplateFormValues>;
  setValue: UseFormSetValue<TemplateFormValues>;
  errors: FieldErrors<TemplateFormValues>;
  categories: string[];
  outputTypes: OutputType[];
  statusOptions: Array<'Draft' | 'Active'>;
  tags: string[];
  category: string;
  outputType: OutputType;
  status: 'Draft' | 'Active';
  disabled?: boolean;
}

const TemplateMetaForm: React.FC<TemplateMetaFormProps> = ({
  register,
  setValue,
  errors,
  categories,
  outputTypes,
  statusOptions,
  tags,
  category,
  outputType,
  status,
  disabled = false,
}) => {
  const [tagInput, setTagInput] = useState('');

  const addTag = (value: string) => {
    const trimmed = value.trim();
    if (!trimmed) return;
    if (tags.includes(trimmed)) return;
    setValue('tags', [...tags, trimmed], { shouldDirty: true, shouldValidate: true });
    setTagInput('');
  };

  const removeTag = (value: string) => {
    setValue('tags', tags.filter((tag) => tag !== value), { shouldDirty: true, shouldValidate: true });
  };

  return (
    <Card className="p-4 space-y-4">
      <div className="space-y-2">
        <label className="text-xs md:text-sm font-medium">
          Template Name <span className="text-destructive">*</span>
        </label>
        <Input
          placeholder="e.g., Standard Invoice Template"
          disabled={disabled}
          {...register('name')}
        />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <Select
          label="Category / Module"
          required
          disabled={disabled}
          placeholder="Select category"
          options={categories}
          error={errors.category?.message}
          value={category}
          onChange={(event) =>
            setValue('category', event.target.value, { shouldDirty: true, shouldValidate: true })
          }
        />
        <Select
          label="Output Type"
          required
          disabled={disabled}
          placeholder="Select output"
          options={outputTypes as string[]}
          error={errors.outputType?.message}
          value={outputType}
          onChange={(event) =>
            setValue('outputType', event.target.value as OutputType, { shouldDirty: true, shouldValidate: true })
          }
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <Select
          label="Status"
          disabled={disabled}
          options={statusOptions as string[]}
          error={errors.status?.message}
          value={status}
          onChange={(event) =>
            setValue('status', event.target.value as 'Draft' | 'Active', { shouldDirty: true, shouldValidate: true })
          }
        />
        <div className="space-y-2">
          <label className="text-xs md:text-sm font-medium">Tags</label>
          <div className="flex gap-2">
            <Input
              placeholder="Add tag"
              value={tagInput}
              disabled={disabled}
              onChange={(event) => setTagInput(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  event.preventDefault();
                  addTag(tagInput);
                }
              }}
            />
            <Button type="button" variant="outline" onClick={() => addTag(tagInput)} disabled={disabled}>
              Add
            </Button>
          </div>
          <div className="flex flex-wrap gap-2">
            {tags.length === 0 && (
              <span className="text-xs text-muted-foreground">No tags added</span>
            )}
            {tags.map((tag) => (
              <Badge key={tag} variant="secondary" className="flex items-center gap-1">
                {tag}
                <button
                  type="button"
                  onClick={() => removeTag(tag)}
                  className="text-muted-foreground hover:text-foreground"
                  aria-label={`Remove ${tag}`}
                >
                  ×
                </button>
              </Badge>
            ))}
          </div>
        </div>
      </div>

      <div className="space-y-2">
        <label className="text-xs md:text-sm font-medium">Description</label>
        <Textarea
          placeholder="Describe this template and its intended usage"
          rows={3}
          disabled={disabled}
          {...register('description')}
        />
      </div>
    </Card>
  );
};

export default TemplateMetaForm;
