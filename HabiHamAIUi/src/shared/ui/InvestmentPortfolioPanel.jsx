import { useEffect, useRef, useState } from 'react';

function formatMoney(value, currency) {
  try {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: currency || 'USD',
      maximumFractionDigits: 2,
    }).format(value ?? 0);
  } catch {
    return `${Number(value ?? 0).toFixed(2)}`;
  }
}

export default function InvestmentPortfolioPanel({ accessToken, request }) {
  const [portfolio, setPortfolio] = useState(null);
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
          setMessage('Войдите в аккаунт, чтобы загрузить портфель с брокера.');
          setPortfolio(null);
        }
        return;
      }

      if (!cancelled) {
        setLoading(true);
        setMessage('');
      }

      const result = await req(
        'GET',
        '/users/me/market/portfolio',
        null,
        token,
      );

      if (cancelled) return;

      if (result.ok && result.data) {
        setPortfolio(result.data);
        if (result.data.isStub) {
          setMessage('Позиций на счёте не найдено или пустой ответ брокера.');
        } else {
          setMessage('');
        }
        setLoading(false);
        return;
      }

      setPortfolio(null);
      setMessage(
        result.data?.message ||
          'Не удалось загрузить портфель. Проверьте ключи API и домен TRADERNET_DOMAIN.',
      );
      setLoading(false);
    }

    load();
    return () => {
      cancelled = true;
    };
  }, [accessToken, reloadNonce]);

  const currency = portfolio?.currency || 'USD';
  const positions = portfolio?.positions ?? [];

  return (
    <div style={{ marginTop: 12 }}>
      <p className="subtitle">
        Портфель с брокерского счёта (Tradernet). Только чтение, без торговли.
      </p>
      <div className="row" style={{ marginTop: 8 }}>
        <button
          type="button"
          className="ghost-btn"
          onClick={() => setReloadNonce((n) => n + 1)}
          disabled={loading}
        >
          {loading ? 'Загрузка…' : 'Обновить с брокера'}
        </button>
        {portfolio?.source === 'tradernet' && !portfolio?.isStub ? (
          <span
            className="market-chart-preview__badge market-chart-preview__badge--live"
            style={{ alignSelf: 'center' }}
          >
            Tradernet
          </span>
        ) : null}
      </div>
      {message ? (
        <p
          className="subtitle"
          style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}
        >
          {message}
        </p>
      ) : null}
      {portfolio && !portfolio.isStub ? (
        <>
          <div
            className="tracking-analytics-stats"
            style={{ marginTop: 12 }}
          >
            <div className="tracking-analytics-stat">
              <span className="tracking-analytics-stat__label">Вложено</span>
              <span className="tracking-analytics-stat__value">
                {portfolio.totalInvested != null
                  ? formatMoney(portfolio.totalInvested, currency)
                  : '—'}
              </span>
            </div>
            <div className="tracking-analytics-stat">
              <span className="tracking-analytics-stat__label">
                Текущая стоимость
              </span>
              <span className="tracking-analytics-stat__value">
                {formatMoney(portfolio.totalMarketValue, currency)}
              </span>
            </div>
            <div className="tracking-analytics-stat">
              <span className="tracking-analytics-stat__label">P/L</span>
              <span
                className="tracking-analytics-stat__value"
                style={{
                  color:
                    Number(portfolio.totalProfitLoss) >= 0
                      ? '#3fb950'
                      : '#f85149',
                }}
              >
                {formatMoney(portfolio.totalProfitLoss, currency)}
              </span>
              {Number.isFinite(Number(portfolio.totalProfitLossPercent)) ? (
                <span className="tracking-analytics-stat__hint">
                  {Number(portfolio.totalProfitLossPercent).toFixed(2)}%
                </span>
              ) : null}
            </div>
            <div className="tracking-analytics-stat">
              <span className="tracking-analytics-stat__label">Позиций</span>
              <span className="tracking-analytics-stat__value">
                {portfolio.positionsCount ?? positions.length}
              </span>
            </div>
          </div>
          <div className="users-table-wrap" style={{ marginTop: 12 }}>
            <table className="users-table">
              <thead>
                <tr>
                  <th>Тикер</th>
                  <th>Название</th>
                  <th>Кол-во</th>
                  <th>Ср. цена</th>
                  <th>Текущая</th>
                  <th>Стоимость</th>
                  <th>P/L</th>
                  <th>Валюта</th>
                </tr>
              </thead>
              <tbody>
                {positions.length === 0 && (
                  <tr>
                    <td colSpan="8">Нет открытых позиций.</td>
                  </tr>
                )}
                {positions.map((row) => (
                  <tr key={row.ticker}>
                    <td>{row.ticker || '—'}</td>
                    <td>{row.name || '—'}</td>
                    <td>{row.quantity ?? '—'}</td>
                    <td>
                      {row.averagePrice != null
                        ? formatMoney(row.averagePrice, row.currency || currency)
                        : '—'}
                    </td>
                    <td>
                      {row.currentPrice != null
                        ? formatMoney(row.currentPrice, row.currency || currency)
                        : '—'}
                    </td>
                    <td>
                      {row.marketValue != null
                        ? formatMoney(row.marketValue, row.currency || currency)
                        : '—'}
                    </td>
                    <td>
                      {row.profitLoss != null ? (
                        <span
                          style={{
                            color:
                              Number(row.profitLoss) >= 0 ? '#3fb950' : '#f85149',
                          }}
                        >
                          {formatMoney(row.profitLoss, row.currency || currency)}
                        </span>
                      ) : (
                        '—'
                      )}
                    </td>
                    <td>{row.currency || currency}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      ) : null}
    </div>
  );
}
