import { ReactNode } from 'react';
import { Sidebar } from './Sidebar';
import './AppLayout.css';

interface AppLayoutProps {
  readonly currentView: string;
  readonly onViewChange: (view: string) => void;
  readonly children: ReactNode;
  readonly title: string;
}

export function AppLayout({ currentView, onViewChange, children, title }: AppLayoutProps) {
  return (
    <div className="app-layout">
      <Sidebar currentView={currentView} onViewChange={onViewChange} />
      <main className="app-main">
        <header className="app-main__header">
          <h1 className="app-main__title">{title}</h1>
        </header>
        <section className="app-main__content">
          {children}
        </section>
      </main>
    </div>
  );
}