import { useEffect, useRef, useState } from 'react';

function formatMoney(value, currency) {
  try {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: currency || 'RUB',
      maximumFractionDigits: 2,
    }).format(value ?? 0);
  } catch {
    return `${Number(value ?? 0).toFixed(2)}`;
  }
}

export default function InvestmentPortfolioPanel({ accessToken, request }) {
  const [positions, setPositions] = useState([]);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [reloadNonce, setReloadNonce] = useState(0);

  const requestRef = useRef(request);
  const accessTokenRef = useRef(accessToken);
  requestRef.current = request;
  accessTokenRef.current = accessToken;

  useEffect(() => {
    let cancelled = false;
    const token = accessTokenRef.current;
    const req = requestRef.current;

    async function load() {
      if (!token || typeof req !== 'function') {
        if (!cancelled) {
          setMessage('Войдите в аккаунт, чтобы увидеть учёт позиций.');
          setPositions([]);
          setSummary(null);
        }
        return;
      }

      if (!cancelled) {
        setLoading(true);
        setMessage('');
      }

      const [listResult, summaryResult] = await Promise.all([
        req('GET', '/users/me/investments', null, token),
        req('GET', '/users/me/investments/summary', null, token),
      ]);

      if (cancelled) return;

      if (listResult.ok && Array.isArray(listResult.data)) {
        setPositions(listResult.data);
      } else {
        setPositions([]);
      }

      if (summaryResult.ok && summaryResult.data) {
        setSummary(summaryResult.data);
      } else {
        setSummary(null);
      }

      if (!listResult.ok && !summaryResult.ok) {
        setMessage(
          listResult.data?.message ||
            summaryResult.data?.message ||
            'Не удалось загрузить портфель.',
        );
      } else if (summaryResult.data?.isStub && listResult.data?.length === 0) {
        setMessage('Позиции пока не добавлены. Создайте первую через «Новая позиция».');
      } else {
        setMessage('');
      }

      setLoading(false);
    }

    load();
    return () => {
      cancelled = true;
    };
  }, [accessToken, reloadNonce]);

  const currency = positions[0]?.currency || 'RUB';

  return (
    <div style={{ marginTop: 12 }}>
      <p className="subtitle">
        Ручной учёт позиций в приложении. Добавление и редактирование — через кнопку
        «Новая позиция».
      </p>
      <div className="row" style={{ marginTop: 8 }}>
        <button
          type="button"
          className="ghost-btn"
          onClick={() => setReloadNonce((n) => n + 1)}
          disabled={loading}
        >
          {loading ? 'Загрузка…' : 'Обновить'}
        </button>
      </div>
      {message ? (
        <p
          className="subtitle"
          style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}
        >
          {message}
        </p>
      ) : null}
      {summary ? (
        <div
          className="tracking-analytics-stats"
          style={{ marginTop: 12 }}
        >
          <div className="tracking-analytics-stat">
            <span className="tracking-analytics-stat__label">Вложено</span>
            <span className="tracking-analytics-stat__value">
              {formatMoney(summary.totalInvested, currency)}
            </span>
          </div>
          <div className="tracking-analytics-stat">
            <span className="tracking-analytics-stat__label">Текущая стоимость</span>
            <span className="tracking-analytics-stat__value">
              {formatMoney(summary.totalCurrentValue, currency)}
            </span>
          </div>
          <div className="tracking-analytics-stat">
            <span className="tracking-analytics-stat__label">P/L</span>
            <span
              className="tracking-analytics-stat__value"
              style={{
                color:
                  Number(summary.totalProfitLoss) >= 0 ? '#3fb950' : '#f85149',
              }}
            >
              {formatMoney(summary.totalProfitLoss, currency)}
            </span>
            {Number.isFinite(Number(summary.totalProfitLossPercent)) ? (
              <span className="tracking-analytics-stat__hint">
                {Number(summary.totalProfitLossPercent).toFixed(2)}%
              </span>
            ) : null}
          </div>
          <div className="tracking-analytics-stat">
            <span className="tracking-analytics-stat__label">Позиций</span>
            <span className="tracking-analytics-stat__value">
              {summary.positionsCount ?? positions.length}
            </span>
          </div>
        </div>
      ) : null}
      <div className="users-table-wrap" style={{ marginTop: 12 }}>
        <table className="users-table">
          <thead>
            <tr>
              <th>Тикер</th>
              <th>Название</th>
              <th>Тип</th>
              <th>Кол-во</th>
              <th>Ср. цена</th>
              <th>Валюта</th>
            </tr>
          </thead>
          <tbody>
            {positions.length === 0 && (
              <tr>
                <td colSpan="6">Нет позиций.</td>
              </tr>
            )}
            {positions.map((row) => (
              <tr key={row.id || row.ticker}>
                <td>{row.ticker || '—'}</td>
                <td>{row.name || '—'}</td>
                <td>{row.assetType || '—'}</td>
                <td>{row.quantity ?? '—'}</td>
                <td>
                  {row.averagePrice != null
                    ? formatMoney(row.averagePrice, row.currency || currency)
                    : '—'}
                </td>
                <td>{row.currency || currency}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
