import { habitStatusSharePct } from '../tracking/analytics';

function StatCard({ label, value, hint }) {
  return (
    <div className="tracking-analytics-stat">
      <span className="tracking-analytics-stat__label">{label}</span>
      <span className="tracking-analytics-stat__value">{value}</span>
      {hint ? (
        <span className="tracking-analytics-stat__hint">{hint}</span>
      ) : null}
    </div>
  );
}

function StatusDistributionBar({ none, partial, done, failed, total }) {
  if (!total) return null;
  const segments = [
    { key: 'done', count: done, className: 'habit-status-cell--done', label: 'выполнено' },
    {
      key: 'partial',
      count: partial,
      className: 'habit-status-cell--partial',
      label: 'частично',
    },
    {
      key: 'failed',
      count: failed,
      className: 'habit-status-cell--failed',
      label: 'провалено',
    },
    { key: 'none', count: none, className: 'habit-status-cell--none', label: 'без отметки' },
  ].filter((s) => s.count > 0);

  return (
    <div className="tracking-analytics-distribution">
      <div
        className="tracking-analytics-distribution__bar"
        role="img"
        aria-label="Распределение отметок за период"
      >
        {segments.map((seg) => (
          <span
            key={seg.key}
            className={`tracking-analytics-distribution__segment habit-status-cell ${seg.className}`}
            style={{ flexGrow: seg.count }}
            title={`${seg.label}: ${seg.count} (${habitStatusSharePct(seg.count, total)}%)`}
          />
        ))}
      </div>
      <div className="tracking-analytics-distribution__legend">
        {segments.map((seg) => (
          <span key={seg.key} className="tracking-analytics-distribution__legend-item">
            <span className={`habit-status-cell ${seg.className}`} aria-hidden="true" />
            {seg.label}: {seg.count}
          </span>
        ))}
      </div>
    </div>
  );
}

export function HabitPeriodAnalyticsPanel({ summary }) {
  if (!summary) return null;

  return (
    <section className="tracking-analytics-panel" aria-label="Аналитика привычек за период">
      <h4 className="tracking-analytics-panel__title">Аналитика за период</h4>
      {summary.periodLabel ? (
        <p className="subtitle tracking-analytics-panel__period">{summary.periodLabel}</p>
      ) : null}
      <div className="tracking-analytics-stats">
        <StatCard label="Привычек" value={summary.habitCount} />
        <StatCard label="Отметок" value={summary.total} hint="слотов за период" />
        <StatCard label="Выполнено" value={`${summary.completionPct}%`} />
        <StatCard
          label="Лучшая серия"
          value={`${summary.bestStreak} дн.`}
          hint="среди выбранных"
        />
      </div>
      <StatusDistributionBar
        none={summary.none}
        partial={summary.partial}
        done={summary.done}
        failed={summary.failed}
        total={summary.total}
      />
      {summary.dailyRates.length > 0 ? (
        <div className="tracking-analytics-chart">
          <p className="tracking-analytics-chart__caption">
            Доля выполненных привычек по дням (%)
          </p>
          <div
            className="tracking-analytics-chart__bars"
            role="img"
            aria-label="График выполнения привычек по дням"
          >
            {summary.dailyRates.map((day) => (
              <div
                key={day.date}
                className="tracking-analytics-chart__bar-wrap"
                title={`${day.date}: ${day.done}/${day.total} (${day.pct}%)`}
              >
                <div
                  className="tracking-analytics-chart__bar tracking-analytics-chart__bar--habit"
                  style={{ height: `${Math.max(4, day.pct)}%` }}
                />
                <span className="tracking-analytics-chart__bar-label">
                  {day.date.slice(5)}
                </span>
              </div>
            ))}
          </div>
        </div>
      ) : null}
    </section>
  );
}

export function TodoPeriodAnalyticsPanel({ summary }) {
  if (!summary) return null;

  return (
    <section className="tracking-analytics-panel" aria-label="Аналитика задач за период">
      <h4 className="tracking-analytics-panel__title">Аналитика за период</h4>
      {summary.periodLabel ? (
        <p className="subtitle tracking-analytics-panel__period">{summary.periodLabel}</p>
      ) : null}
      <div className="tracking-analytics-stats">
        <StatCard label="Всего" value={summary.total} />
        <StatCard label="Выполнено" value={summary.done} />
        <StatCard label="Открыто" value={summary.open} />
        <StatCard label="Просрочено" value={summary.overdue} />
        <StatCard label="Готово" value={`${summary.completionPct}%`} />
      </div>
      {summary.completedByDay.some((d) => d.count > 0) ? (
        <div className="tracking-analytics-chart">
          <p className="tracking-analytics-chart__caption">
            Задач отмечено выполненными по дням
          </p>
          <div
            className="tracking-analytics-chart__bars"
            role="img"
            aria-label="График выполненных задач по дням"
          >
            {summary.completedByDay.map((day) => (
              <div
                key={day.date}
                className="tracking-analytics-chart__bar-wrap"
                title={`${day.date}: ${day.count}`}
              >
                <div
                  className="tracking-analytics-chart__bar tracking-analytics-chart__bar--todo"
                  style={{
                    height: `${Math.max(4, (day.count / summary.maxDayCount) * 100)}%`,
                  }}
                />
                <span className="tracking-analytics-chart__bar-label">
                  {day.date.slice(5)}
                </span>
              </div>
            ))}
          </div>
        </div>
      ) : (
        <p className="subtitle tracking-analytics-panel__empty-chart">
          За период нет отмеченных выполненных задач.
        </p>
      )}
    </section>
  );
}
