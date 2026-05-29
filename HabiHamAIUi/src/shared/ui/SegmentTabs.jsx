import './SegmentTabs.css';

export function segmentTabClass(isActive) {
  return `segment-tab${isActive ? ' active' : ''}`;
}

export function subNavTabClass(level, isActive) {
  return `app-subnav app-subnav--${level}${isActive ? ' is-active' : ''}`;
}

export function SegmentTabs({
  variant = 'primary',
  className = '',
  ariaLabel,
  children,
}) {
  const classes = [
    'segment-tabs',
    variant === 'primary' && 'segment-tabs--primary',
    variant === 'compact' && 'segment-tabs--compact',
    variant === 'sidebar' && 'segment-tabs--sidebar',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={classes} role="tablist" aria-label={ariaLabel}>
      {children}
    </div>
  );
}

export function SegmentTab({ active, className = '', children, ...props }) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      className={`${segmentTabClass(active)}${className ? ` ${className}` : ''}`}
      {...props}
    >
      {children}
    </button>
  );
}

export function SubNavGroup({
  label,
  level = 'secondary',
  nested = false,
  ariaLabel,
  children,
}) {
  return (
    <nav
      className={`app-nav-group${nested ? ' app-nav-group--nested' : ''}`}
      aria-label={ariaLabel || label}
    >
      {label ? <span className="app-nav-group__label">{label}</span> : null}
      <div className={`app-subnav-row app-subnav-row--${level}`} role="tablist">
        {children}
      </div>
    </nav>
  );
}

export function SubNavTab({ level, active, children, ...props }) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      className={subNavTabClass(level, active)}
      {...props}
    >
      {children}
    </button>
  );
}
