import { IconSettings, IconBug } from '@tabler/icons-react';

export function App() {
  return (
    <div className="pero-app-layout">
      <aside className="pero-sidebar">
        <h2>Pero 🪶</h2>
        <nav>
          <button className="pero-nav-btn">
            <IconBug size={20} />
            <span>Debug Engine</span>
          </button>
          <button className="pero-nav-btn">
            <IconSettings size={20} />
            <span>Settings</span>
          </button>
        </nav>
      </aside>
      <main className="pero-main-content">
        <h1>Dashboard Initialization</h1>
        <p>React architecture is ready for standalone tools.</p>
      </main>
    </div>
  );
}