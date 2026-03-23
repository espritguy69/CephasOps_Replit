export const PLACEHOLDER_REGEX = /{{\s*([A-Za-z0-9_]+)\s*}}/g;

export const extractPlaceholders = (content: string): string[] => {
  if (!content) return [];
  const matches = content.matchAll(PLACEHOLDER_REGEX);
  const values = Array.from(matches, (match) => match[1]?.trim()).filter(Boolean) as string[];
  return Array.from(new Set(values));
};

export const findUnknownPlaceholders = (content: string, allowed: string[]): string[] => {
  const placeholders = extractPlaceholders(content);
  const allowedSet = new Set(allowed);
  return placeholders.filter((placeholder) => !allowedSet.has(placeholder));
};

export const findMissingRecommended = (content: string, recommended: string[]): string[] => {
  if (recommended.length === 0) return [];
  const placeholders = extractPlaceholders(content);
  const placeholdersSet = new Set(placeholders);
  return recommended.filter((placeholder) => !placeholdersSet.has(placeholder));
};
