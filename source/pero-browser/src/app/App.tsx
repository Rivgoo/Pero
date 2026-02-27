import { useState } from 'react';
import { AppLayout } from './components/layout/AppLayout';
import { Dashboard } from './views/Dashboard/Dashboard';
import { DebugView } from './views/Debug/DebugView';
import { Settings } from './views/Settings/Settings';
import './styles/theme.css';
import './styles/tooltip.css';

export function App() {
  const [currentView, setCurrentView] = useState('dashboard');

  const renderView = () => {
    switch (currentView) {
      case 'dashboard':
        return <Dashboard onNavigate={setCurrentView} />;
      case 'debug':
        return <DebugView />;
      case 'morphology':
        return <div>Морфологічний аналізатор (у розробці).</div>;
      case 'settings':
        return <Settings />;
      default:
        return <div>Невідоме вікно.</div>;
    }
  };

  const getTitle = () => {
    switch (currentView) {
      case 'dashboard': return 'Головна Панель 📊';
      case 'debug': return 'Debug Engine 🛠️';
      case 'morphology': return 'Синтаксис та Морфологія 📚';
      case 'settings': return 'Налаштування ⚙️';
      default: return 'Pero';
    }
  };

  return (
    <AppLayout 
      currentView={currentView} 
      onViewChange={setCurrentView}
      title={getTitle()}
    >
      {renderView()}
    </AppLayout>
  );
}