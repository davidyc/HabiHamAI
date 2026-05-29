import { useEffect, useId, useRef, useState } from 'react';
import './FilterSelect.css';

export const STANDARD_DATE_RANGE_PRESETS = [
  { value: '1', label: 'День' },
  { value: '7', label: 'Неделя' },
  { value: '10', label: '10 дней' },
  { value: '15', label: '15 дней' },
  { value: '30', label: 'Месяц' },
];

export const HABIT_ANALYTICS_PRESETS = [
  { value: '7', label: '7 дней' },
  { value: '14', label: '14 дней' },
  { value: '30', label: '30 дней' },
];

export const TODO_PERIOD_PRESETS = [
  { value: 'all', label: 'Все' },
  { value: 'month', label: 'Месяц' },
  { value: 'week', label: 'Неделя' },
  { value: '3days', label: '3 дня' },
  { value: 'weekend', label: 'Выходные' },
];

export const CUSTOM_PERIOD_VALUE = 'custom';

export const CUSTOM_PERIOD_OPTION = {
  value: CUSTOM_PERIOD_VALUE,
  label: 'Указать период',
};

export const STANDARD_DATE_PERIOD_OPTIONS = [
  ...STANDARD_DATE_RANGE_PRESETS,
  CUSTOM_PERIOD_OPTION,
];

export const HABIT_DATE_PERIOD_OPTIONS = [
  ...HABIT_ANALYTICS_PRESETS,
  CUSTOM_PERIOD_OPTION,
];

export const TODO_DATE_PERIOD_OPTIONS = [
  ...TODO_PERIOD_PRESETS,
  CUSTOM_PERIOD_OPTION,
];

function StyledDropdown({
  id,
  value,
  onChange,
  options,
  placeholder,
}) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef(null);
  const menuId = useId();

  const selected = options.find((opt) => opt.value === value);
  const showPlaceholder = !selected && placeholder;
  const displayLabel = selected?.label ?? placeholder ?? 'Выберите…';

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

  const pick = (next) => {
    onChange(next);
    setOpen(false);
  };

  return (
    <div className="filter-dropdown" ref={rootRef}>
      <button
        type="button"
        id={id}
        className={`filter-dropdown__trigger${open ? ' is-open' : ''}${showPlaceholder ? ' is-placeholder' : ''}`}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? menuId : undefined}
        onClick={() => setOpen((prev) => !prev)}
      >
        <span className="filter-dropdown__value">{displayLabel}</span>
        <span className="filter-dropdown__chevron" aria-hidden="true" />
      </button>
      {open ? (
        <ul
          id={menuId}
          className="filter-dropdown__menu"
          role="listbox"
          aria-labelledby={id}
        >
          {placeholder ? (
            <li role="presentation">
              <button
                type="button"
                role="option"
                aria-selected={!value}
                className={`filter-dropdown__option is-placeholder-option${!value ? ' is-selected' : ''}`}
                onClick={() => pick('')}
              >
                <span className="filter-dropdown__check" aria-hidden="true">
                  ✓
                </span>
                <span className="filter-dropdown__option-label">
                  {placeholder}
                </span>
              </button>
            </li>
          ) : null}
          {options.map((opt) => {
            const isSelected = opt.value === value;
            return (
              <li key={opt.value} role="presentation">
                <button
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  className={`filter-dropdown__option${isSelected ? ' is-selected' : ''}`}
                  onClick={() => pick(opt.value)}
                >
                  <span className="filter-dropdown__check" aria-hidden="true">
                    ✓
                  </span>
                  <span className="filter-dropdown__option-label">
                    {opt.label}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}

export function FilterSelect({
  label,
  value,
  onChange,
  options,
  placeholder,
  className = '',
  id,
}) {
  const reactId = useId();
  const fieldId =
    id || (label ? `filter-${label.replace(/\s+/g, '-').toLowerCase()}` : reactId);

  return (
    <div className={`filter-field${className ? ` ${className}` : ''}`}>
      {label ? (
        <span className="filter-field__label" id={`${fieldId}-label`}>
          {label}
        </span>
      ) : null}
      <StyledDropdown
        id={fieldId}
        value={value}
        onChange={onChange}
        options={options}
        placeholder={placeholder}
      />
    </div>
  );
}

/** Период: пресет в dropdown; поля дат только при «Указать период». */
export function DatePeriodFilter({
  label = 'Период',
  preset,
  onPresetChange,
  from,
  to,
  onFromChange,
  onToChange,
  options,
  onApplyPreset,
  className = '',
  children = null,
}) {
  const showCustomDates = preset === CUSTOM_PERIOD_VALUE;

  const handlePresetChange = (next) => {
    onPresetChange(next);
    if (next === CUSTOM_PERIOD_VALUE || !next) return;
    onApplyPreset(next);
  };

  return (
    <div className={`row filter-toolbar date-period-filter${className ? ` ${className}` : ''}`}>
      <FilterSelect
        label={label}
        value={preset}
        onChange={handlePresetChange}
        options={options}
      />
      {showCustomDates ? (
        <>
          <div className="date-period-filter__date">
            <input
              type="date"
              aria-label="Дата начала периода"
              value={from}
              onChange={(e) => onFromChange(e.target.value)}
            />
          </div>
          <div className="date-period-filter__date">
            <input
              type="date"
              aria-label="Дата окончания периода"
              value={to}
              onChange={(e) => onToChange(e.target.value)}
            />
          </div>
        </>
      ) : null}
      {children}
    </div>
  );
}

export default FilterSelect;
