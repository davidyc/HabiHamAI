import { useEffect, useId, useRef, useState } from 'react';
import './HabitStatusDropdown.css';

export const HABIT_STATUS_OPTIONS = [
  { value: '', label: 'без отметки', statusClass: 'none' },
  { value: 'partial', label: 'частично', statusClass: 'partial' },
  { value: 'done', label: 'выполнено', statusClass: 'done' },
  { value: 'failed', label: 'провалено', statusClass: 'failed' },
];

function normalizeStatus(status) {
  return status || '';
}

export default function HabitStatusDropdown({
  status,
  onChange,
  ariaLabel,
  title,
}) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef(null);
  const menuId = useId();
  const triggerId = useId();

  const currentValue = normalizeStatus(status);
  const currentOption =
    HABIT_STATUS_OPTIONS.find((opt) => opt.value === currentValue) ??
    HABIT_STATUS_OPTIONS[0];

  useEffect(() => {
    if (!open) return undefined;
    const onPointerDown = (event) => {
      if (!rootRef.current?.contains(event.target)) {
        setOpen(false);
      }
    };
    const onKeyDown = (event) => {
      if (event.key === 'Escape') setOpen(false);
    };
    document.addEventListener('mousedown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [open]);

  const pick = (nextValue) => {
    const nextStatus = nextValue || null;
    onChange(nextStatus);
    setOpen(false);
  };

  return (
    <div
      className={`habit-status-dropdown${open ? ' is-open' : ''}`}
      ref={rootRef}
    >
      <button
        type="button"
        id={triggerId}
        className={`habit-status-dropdown__trigger habit-status-dropdown__trigger--${currentOption.statusClass}`}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? menuId : undefined}
        aria-label={ariaLabel}
        title={title}
        onClick={() => setOpen((prev) => !prev)}
      >
        <span
          className={`habit-status-cell habit-status-cell--${currentOption.statusClass}`}
          aria-hidden="true"
        />
        <span className="habit-status-dropdown__chevron" aria-hidden="true" />
      </button>
      {open ? (
        <ul
          id={menuId}
          className="habit-status-dropdown__menu"
          role="listbox"
          aria-labelledby={triggerId}
        >
          {HABIT_STATUS_OPTIONS.map((opt) => {
            const isSelected = opt.value === currentValue;
            return (
              <li key={opt.value || 'none'} role="presentation">
                <button
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  className={`habit-status-dropdown__option${isSelected ? ' is-selected' : ''}`}
                  onClick={() => pick(opt.value)}
                >
                  <span
                    className={`habit-status-cell habit-status-cell--${opt.statusClass}`}
                    aria-hidden="true"
                  />
                  <span className="habit-status-dropdown__option-label">
                    {opt.label}
                  </span>
                  {isSelected ? (
                    <span className="habit-status-dropdown__check" aria-hidden="true">
                      ✓
                    </span>
                  ) : null}
                </button>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}
