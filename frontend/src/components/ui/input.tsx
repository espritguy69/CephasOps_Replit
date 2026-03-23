import * as React from "react";

import { cn } from "../../lib/utils";

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  size?: "compact" | "default" | "large";
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type = "text", size = "default", ...props }, ref) => {
    const sizeClasses: Record<string, string> = {
      compact: "h-8 text-xs px-2.5 py-1.5",
      default: "h-9 text-sm px-3 py-2",
      large: "h-10 text-sm px-3 py-2",
    };
    
    return (
      <input
        type={type}
        className={cn(
          "flex w-full rounded-lg border border-input bg-background",
          "text-foreground shadow-sm transition-fast",
          "file:border-0 file:bg-transparent file:text-sm file:font-medium",
          "placeholder:text-muted-foreground/60",
          "focus-ring disabled:cursor-not-allowed disabled:opacity-50",
          size === "compact" ? "min-h-[36px]" : "min-h-[44px]",
          sizeClasses[size],
          className
        )}
        ref={ref}
        {...props}
      />
    );
  }
);
Input.displayName = "Input";

export { Input };

