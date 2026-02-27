import { IconBooks, IconScale, IconBug, IconSettings } from '@tabler/icons-react';
import './Dashboard.css';

interface DashboardProps {
  readonly onNavigate: (view: string) => void;
}

export function Dashboard({ onNavigate }: DashboardProps) {
  return (
    <div className="dashboard">
      <header className="dashboard__hero">
        <h2>Вітаємо у Pero 🪶</h2>
        <p>Ваш персональний асистент для ідеального українського тексту. Керуйте налаштуваннями, перевіряйте роботу рушія та аналізуйте словники з єдиного центру.</p>
      </header>

      <section className="dashboard__grid">
        <article className="stat-card">
          <header className="stat-card__header">
            <IconBooks size={24} />
            <h3 className="stat-card__title">Словникова база</h3>
          </header>
          <p className="stat-card__value">350K+</p>
          <p className="stat-card__desc">Лем та словоформ завантажено в пам'ять.</p>
        </article>

        <article className="stat-card">
          <header className="stat-card__header">
            <IconScale size={24} />
            <h3 className="stat-card__title">Активні правила</h3>
          </header>
          <p className="stat-card__value">12</p>
          <p className="stat-card__desc">Перевіряють орфографію, граматику та стиль.</p>
        </article>
      </section>

      <section className="dashboard__grid">
        <button className="action-card" onClick={() => onNavigate('debug')}>
          <div className="action-card__icon">
            <IconBug size={28} />
          </div>
          <h3 className="action-card__title">Debug Engine</h3>
          <p className="action-card__desc">Тестуйте сирі запити до WASM рушія. Переглядайте продуктивність, токени та внутрішню логіку аналізу.</p>
        </button>

        <button className="action-card" onClick={() => onNavigate('settings')}>
          <div className="action-card__icon">
            <IconSettings size={28} />
          </div>
          <h3 className="action-card__title">Налаштування</h3>
          <p className="action-card__desc">Керуйте винятками, вмикайте додаткові перевірки та налаштовуйте поведінку розширення.</p>
        </button>
      </section>
    </div>
  );
}