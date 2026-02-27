import { ReactNode } from 'react';

interface TooltipProps {
  readonly content: string;
  readonly children: ReactNode;
  readonly position?: 'top' | 'right';
}

export function Tooltip({ content, children, position = 'top' }: TooltipProps) {
  return (
    <div 
      data-tooltip={content} 
      data-tooltip-pos={position}
      style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}
    >
      {children}
    </div>
  );
}