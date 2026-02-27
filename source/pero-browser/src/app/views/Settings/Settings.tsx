import { useState, useEffect } from 'react';
import { StorageKeys } from '@shared/constants';
import { IconBug } from '@tabler/icons-react';
import './Settings.css';

export function Settings() {
  const [isDebugEnabled, setIsDebugEnabled] = useState(false);

  useEffect(() => {
    chrome.storage.local.get(StorageKeys.DebugMode, (result) => {
      setIsDebugEnabled(Boolean(result[StorageKeys.DebugMode]));
    });
  }, []);

  const handleToggle = (e: React.ChangeEvent<HTMLInputElement>) => {
    const isChecked = e.target.checked;
    setIsDebugEnabled(isChecked);
    chrome.storage.local.set({ [StorageKeys.DebugMode]: isChecked });
  };

  return (
    <div className="settings-view">
      <section className="settings-group">
        <h2 className="settings-group__title">Розробка</h2>
        
        <article className="settings-card">
          <div className="settings-card__icon">
            <IconBug size={24} />
          </div>
          <div className="settings-card__info">
            <label htmlFor="debugToggle" className="settings-card__name">Режим розробника (Debug Engine)</label>
            <span className="settings-card__desc">Дозволяє виводити сирі запити та відповіді від .NET рушія у консоль Offscreen документа.</span>
          </div>
          <label className="settings-toggle">
            <input 
              type="checkbox" 
              id="debugToggle" 
              className="settings-toggle__input" 
              checked={isDebugEnabled}
              onChange={handleToggle}
            />
            <span className="settings-toggle__slider"></span>
          </label>
        </article>
      </section>
    </div>
  );
}