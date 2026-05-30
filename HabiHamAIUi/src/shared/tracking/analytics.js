/** @typedef {'none'|'partial'|'done'|'failed'} HabitStatusKey */

/**
 * @param {object} params
 * @param {Array<object>} params.habits
 * @param {Record<string, Record<string, string>>} params.checkinsByHabitId
 * @param {{ from: string, to: string }} params.filterRange
 * @param {(habit: object) => string[]} params.getDisplayDates
 * @param {(statusMap: Record<string, string>, habit: object, date: string) => string|null} params.resolveStatus
 */
export function computeHabitPeriodAnalytics({
  habits,
  checkinsByHabitId,
  filterRange,
  getDisplayDates,
  resolveStatus,
}) {
  if (!habits?.length) return null;

  const counts = { none: 0, partial: 0, done: 0, failed: 0 };
  let bestStreak = 0;
  const dailyMap = new Map();

  for (const habit of habits) {
    bestStreak = Math.max(bestStreak, habit.currentStreakDays ?? 0);
    const statusMap = checkinsByHabitId[String(habit.id)] ?? {};
    const dates = getDisplayDates(habit);
    for (const date of dates) {
      const status = resolveStatus(statusMap, habit, date);
      if (status === 'partial') counts.partial += 1;
      else if (status === 'done') counts.done += 1;
      else if (status === 'failed') counts.failed += 1;
      else counts.none += 1;

      if (!dailyMap.has(date)) {
        dailyMap.set(date, { date, done: 0, total: 0 });
      }
      const row = dailyMap.get(date);
      row.total += 1;
      if (status === 'done') row.done += 1;
    }
  }

  const total = counts.none + counts.partial + counts.done + counts.failed;
  const completionPct = total > 0 ? Math.round((counts.done / total) * 100) : 0;

  const dailyRates = Array.from(dailyMap.values())
    .sort((a, b) => a.date.localeCompare(b.date))
    .map((row) => ({
      ...row,
      pct: row.total > 0 ? Math.round((row.done / row.total) * 100) : 0,
    }));

  const periodLabel =
    filterRange.from && filterRange.to
      ? `${filterRange.from} — ${filterRange.to}`
      : '';

  return {
    habitCount: habits.length,
    total,
    ...counts,
    completionPct,
    bestStreak,
    dailyRates,
    periodLabel,
  };
}

/**
 * @param {object} params
 * @param {Array<object>} params.todos
 * @param {string} params.filterFrom
 * @param {string} params.filterTo
 * @param {() => string} params.getToday
 * @param {(from: string, to: string) => string[]} params.getDateRange
 */
export function computeTodoPeriodAnalytics({
  todos,
  filterFrom,
  filterTo,
  getToday,
  getDateRange,
}) {
  if (!todos?.length) return null;

  const today = getToday();
  let open = 0;
  let done = 0;
  let overdue = 0;

  for (const todo of todos) {
    if (todo.doneDate) {
      done += 1;
    } else {
      open += 1;
      if (isTodoOverdue(todo, getToday)) overdue += 1;
    }
  }

  const total = todos.length;
  const completionPct = total > 0 ? Math.round((done / total) * 100) : 0;

  let from = filterFrom;
  let to = filterTo || today;
  if (!from) {
    const doneDates = todos
      .map((t) => String(t.doneDate ?? '').slice(0, 10))
      .filter(Boolean);
    from =
      doneDates.length > 0
        ? doneDates.reduce((a, b) => (a < b ? a : b))
        : today;
  }
  if (!to) to = today;
  if (from > to) [from, to] = [to, from];

  const dates = getDateRange(from, to);
  const completedByDay = dates.map((date) => ({
    date,
    count: todos.filter(
      (t) => String(t.doneDate ?? '').slice(0, 10) === date,
    ).length,
  }));
  const maxDayCount = Math.max(1, ...completedByDay.map((d) => d.count));

  const periodLabel = filterFrom && filterTo ? `${filterFrom} — ${filterTo}` : '';

  return {
    total,
    open,
    done,
    overdue,
    completionPct,
    completedByDay,
    maxDayCount,
    periodLabel,
  };
}

export function habitStatusSharePct(count, total) {
  if (!total) return 0;
  return Math.round((count / total) * 100);
}

/** Открытая задача с дедлайном раньше сегодня. */
export function isTodoOverdue(todo, getToday) {
  if (!todo || todo.doneDate) return false;
  const due = String(todo.dueDate ?? '').slice(0, 10);
  const today = getToday();
  if (!due || !today) return false;
  return due < today;
}
