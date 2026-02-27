import { useState } from 'react';
import { 
  IconBug, 
  IconLayoutDashboard, 
  IconSettings, 
  IconFeather, 
  IconChevronLeft, 
  IconChevronRight,
  IconBooks
} from '@tabler/icons-react';
import { Tooltip } from '../ui/Tooltip';
import './Sidebar.css';

interface SidebarProps {
  readonly currentView: string;
  readonly onViewChange: (view: string) => void;
}

const NavItems = [
  { id: 'dashboard', label: 'Головна', icon: IconLayoutDashboard, desc: 'Загальна статистика' },
  { id: 'debug', label: 'Debug Engine', icon: IconBug, desc: 'Тестування рушія C#' },
  { id: 'morphology', label: 'Морфологія', icon: IconBooks, desc: 'Аналіз слів (у розробці)' },
  { id: 'settings', label: 'Налаштування', icon: IconSettings, desc: 'Конфігурація розширення' }
] as const;

export function Sidebar({ currentView, onViewChange }: SidebarProps) {
  const [isCollapsed, setIsCollapsed] = useState(false);

  const toggleSidebar = () => setIsCollapsed(prev => !prev);

  return (
    <aside className={`sidebar ${isCollapsed ? 'sidebar--collapsed' : ''}`}>
      <header className="sidebar__header">
        {!isCollapsed && (
          <div className="sidebar__brand">
            <IconFeather size={24} />
            <span>Pero</span>
          </div>
        )}
        <Tooltip content={isCollapsed ? "Розгорнути меню" : "Згорнути меню"} position="right">
          <button className="sidebar__toggle" onClick={toggleSidebar}>
            {isCollapsed ? <IconChevronRight size={20} /> : <IconChevronLeft size={20} />}
          </button>
        </Tooltip>
      </header>

      <nav className="sidebar__nav">
        {NavItems.map(item => {
          const Icon = item.icon;
          const isActive = currentView === item.id;
          
          const buttonContent = (
            <button 
              key={item.id}
              className={`nav-item ${isActive ? 'nav-item--active' : ''}`}
              onClick={() => onViewChange(item.id)}
            >
              <span className="nav-item__icon">
                <Icon size={22} stroke={isActive ? 2.5 : 2} />
              </span>
              {!isCollapsed && <span>{item.label}</span>}
            </button>
          );

          return isCollapsed ? (
            <Tooltip key={item.id} content={item.desc} position="right">
              {buttonContent}
            </Tooltip>
          ) : (
            buttonContent
          );
        })}
      </nav>

      {!isCollapsed && (
        <footer className="sidebar__footer">
          <p>Pero Engine v1.0</p>
        </footer>
      )}
    </aside>
  );
}