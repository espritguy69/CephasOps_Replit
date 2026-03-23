import tailwindcss from '@tailwindcss/postcss';
import autoprefixer from 'autoprefixer';
import type { ProcessOptions } from 'postcss';

// PostCSS config that properly handles the 'from' option
// Note: The warning about 'from' option is a known issue with Tailwind CSS v4's PostCSS plugin
// It's harmless and doesn't affect functionality - the plugin author needs to fix it
export default (opts?: ProcessOptions) => {
  return {
    plugins: [
      // Tailwind CSS v4 PostCSS plugin
      // The 'from' option warning is a known issue with this plugin version
      tailwindcss(),
      // Autoprefixer
      autoprefixer({
        overrideBrowserslist: ['> 1%', 'last 2 versions'],
      }),
    ],
    from: opts?.from, // Explicitly pass 'from' option if provided
  };
};
