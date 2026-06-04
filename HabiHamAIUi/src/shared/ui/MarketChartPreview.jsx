import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import './MarketChartPreview.css';

const PRESET_TICKERS = [
  { id: 'SBER.RU', label: 'SBER.RU', name: 'Сбербанк' },
  { id: 'HSBK.KZ', label: 'HSBK.KZ', name: 'Halyk Bank (KASE)' },
  { id: 'HSBK.AIX.KZ', label: 'HSBK.AIX.KZ', name: 'Halyk GDR (AIX)' },
  { id: 'KCEL.Y.AIX.KZ', label: 'KCEL.Y.AIX.KZ', name: 'Kcell GDR (AIX)' },
  { id: 'FRHC.AIX.KZ', label: 'FRHC.AIX.KZ', name: 'Freedom (AIX)' },
  { id: 'FRHC.US', label: 'FRHC.US', name: 'Freedom Holding (US)' },
  { id: 'AAPL.US', label: 'AAPL.US', name: 'Apple' },
];

const INTERVALS = [
  { id: '1d', label: '1 день' },
  { id: '1h', label: '1 час' },
];

const PERIODS = [
  { id: '1m', label: 'Месяц', days: 30 },
  { id: '3m', label: '3 мес.', days: 90 },
  { id: '6m', label: '6 мес.', days: 180 },
];

const SEARCH_EXCHANGES = [
  { id: '', label: 'Все рынки' },
  { id: 'KASE', label: 'KASE (KZ)' },
  { id: 'AIX', label: 'AIX' },
  { id: 'USA', label: 'USA' },
  { id: 'MCX', label: 'MOEX' },
];

function seededRandom(seed) {
  let s = seed;
  return () => {
    s = (s * 16807) % 2147483647;
    return (s - 1) / 2147483646;
  };
}

function hashTicker(ticker) {
  let h = 0;
  for (let i = 0; i < ticker.length; i += 1) {
    h = (h * 31 + ticker.charCodeAt(i)) | 0;
  }
  return Math.abs(h) + 1;
}

export function buildDemoCandles(ticker, barCount, basePrice = 280) {
  const rand = seededRandom(hashTicker(ticker) + barCount);
  const candles = [];
  let close = basePrice * (0.85 + rand() * 0.3);
  const start = new Date();
  start.setDate(start.getDate() - barCount);

  for (let i = 0; i < barCount; i += 1) {
    const date = new Date(start);
    date.setDate(start.getDate() + i);
    const drift = (rand() - 0.48) * basePrice * 0.018;
    const open = close;
    close = Math.max(basePrice * 0.5, open + drift);
    const wick = basePrice * (0.004 + rand() * 0.012);
    const high = Math.max(open, close) + wick * rand();
    const low = Math.min(open, close) - wick * rand();
    candles.push({
      date: date.toISOString().slice(0, 10),
      open: Number(open.toFixed(2)),
      high: Number(high.toFixed(2)),
      low: Number(low.toFixed(2)),
      close: Number(close.toFixed(2)),
      volume: Math.round(500000 + rand() * 4000000),
    });
  }
  return candles;
}

const CHART_W = 720;
const CHART_H = 320;
const PAD = { left: 56, right: 16, top: 20, bottom: 36 };

function formatPrice(value, currency) {
  try {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: currency || 'USD',
      maximumFractionDigits: 2,
    }).format(value);
  } catch {
    return `${Number(value).toFixed(2)}`;
  }
}

function tickerCurrency(ticker) {
  if (ticker.endsWith('.RU')) return 'RUB';
  if (ticker.endsWith('.KZ')) return 'KZT';
  return 'USD';
}

function demoBasePrice(ticker) {
  if (ticker.includes('SBER')) return 280;
  if (ticker.includes('HSBK')) return 2800;
  if (ticker.includes('KCEL')) return 1200;
  if (ticker.includes('AAPL')) return 195;
  return 145;
}

export default function MarketChartPreview({ accessToken, request }) {
  const [ticker, setTicker] = useState('HSBK.KZ');
  const [interval, setInterval] = useState('1d');
  const [period, setPeriod] = useState('3m');
  const [candles, setCandles] = useState([]);
  const [dataSource, setDataSource] = useState('demo');
  const [statusMessage, setStatusMessage] = useState('');
  const [loading, setLoading] = useState(false);
  const [marketStatus, setMarketStatus] = useState(null);
  const [reloadNonce, setReloadNonce] = useState(0);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchExchange, setSearchExchange] = useState('KASE');
  const [searchResults, setSearchResults] = useState([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [searchMessage, setSearchMessage] = useState('');

  const requestRef = useRef(request);
  const accessTokenRef = useRef(accessToken);
  requestRef.current = request;
  accessTokenRef.current = accessToken;

  const preset =
    PRESET_TICKERS.find((t) => t.id === ticker) ?? {
      id: ticker,
      label: ticker,
      name: '',
    };
  const intervalMeta = INTERVALS.find((i) => i.id === interval) ?? INTERVALS[0];
  const periodMeta = PERIODS.find((p) => p.id === period) ?? PERIODS[1];

  const applyDemoCandles = useCallback(() => {
    const count = Math.max(
      20,
      Math.round(periodMeta.days * (interval === '1h' ? 6 : 0.7)),
    );
    setCandles(buildDemoCandles(ticker, count, demoBasePrice(ticker)));
    setDataSource('demo');
  }, [ticker, interval, period, periodMeta.days]);

  useEffect(() => {
    if (!accessToken) {
      applyDemoCandles();
      return;
    }
    let cancelled = false;
    const req = requestRef.current;
    if (typeof req !== 'function') {
      return;
    }
    req('GET', '/users/me/market/status', null, accessToken).then((result) => {
      if (!cancelled && result.ok) {
        setMarketStatus(result.data);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [accessToken, applyDemoCandles]);

  useEffect(() => {
    let cancelled = false;
    const q = searchQuery.trim();
    if (q.length < 2) {
      setSearchResults([]);
      setSearchMessage('');
      setSearchLoading(false);
      return () => {
        cancelled = true;
      };
    }

    const timer = window.setTimeout(async () => {
      const token = accessTokenRef.current;
      const req = requestRef.current;
      if (!token || typeof req !== 'function') {
        if (!cancelled) {
          setSearchMessage('Войдите в аккаунт для поиска тикеров.');
        }
        return;
      }

      if (!cancelled) {
        setSearchLoading(true);
        setSearchMessage('');
      }

      const params = new URLSearchParams({ q });
      if (searchExchange) {
        params.set('exchange', searchExchange);
      }

      const result = await req(
        'GET',
        `/users/me/market/search?${params.toString()}`,
        null,
        token,
      );

      if (cancelled) return;

      setSearchLoading(false);
      if (result.ok && Array.isArray(result.data?.results)) {
        setSearchResults(result.data.results);
        if (result.data.results.length === 0) {
          setSearchMessage('Ничего не найдено. Попробуйте другой запрос или рынок.');
        }
        return;
      }

      setSearchResults([]);
      setSearchMessage(result.data?.message || 'Ошибка поиска тикеров.');
    }, 400);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [searchQuery, searchExchange, accessToken]);

  useEffect(() => {
    let cancelled = false;
    const token = accessTokenRef.current;
    const req = requestRef.current;

    async function loadCandles() {
      if (!token || typeof req !== 'function') {
        if (!cancelled) {
          applyDemoCandles();
          setStatusMessage('Войдите в аккаунт для загрузки котировок с сервера.');
        }
        return;
      }

      if (!cancelled) {
        setLoading(true);
        setStatusMessage('');
      }

      const qs = new URLSearchParams({ ticker, interval, period });
      const result = await req(
        'GET',
        `/users/me/market/candles?${qs.toString()}`,
        null,
        token,
      );

      if (cancelled) return;

      if (
        result.ok &&
        Array.isArray(result.data?.candles) &&
        result.data.candles.length > 0
      ) {
        setCandles(
          result.data.candles.map((c) => ({
            date: c.date || '',
            open: Number(c.open),
            high: Number(c.high),
            low: Number(c.low),
            close: Number(c.close),
            volume: c.volume,
          })),
        );
        setDataSource(result.data.source || 'tradernet');
        setStatusMessage('');
        setLoading(false);
        return;
      }

      if (result.status === 503) {
        applyDemoCandles();
        setStatusMessage(
          result.data?.message ||
            'Ключи Tradernet не заданы на сервере — показаны демо-данные.',
        );
        setLoading(false);
        return;
      }

      applyDemoCandles();
      setStatusMessage(
        result.data?.message ||
          'Не удалось загрузить свечи. Проверьте тикер или ключи API.',
      );
      setLoading(false);
    }

    loadCandles();
    return () => {
      cancelled = true;
    };
  }, [ticker, interval, period, accessToken, reloadNonce, applyDemoCandles]);

  const layout = useMemo(() => {
    if (candles.length === 0) return null;
    const min = Math.min(...candles.map((c) => c.low));
    const max = Math.max(...candles.map((c) => c.high));
    const padY = (max - min) * 0.08 || 1;
    const yMin = min - padY;
    const yMax = max + padY;
    const innerW = CHART_W - PAD.left - PAD.right;
    const innerH = CHART_H - PAD.top - PAD.bottom;
    const n = candles.length;
    const slot = innerW / n;
    const bodyW = Math.max(3, Math.min(14, slot * 0.55));

    const yScale = (v) =>
      PAD.top + innerH - ((v - yMin) / (yMax - yMin)) * innerH;

    const bars = candles.map((c, i) => {
      const cx = PAD.left + slot * i + slot / 2;
      const up = c.close >= c.open;
      const color = up ? '#3fb950' : '#f85149';
      const openY = yScale(c.open);
      const closeY = yScale(c.close);
      const highY = yScale(c.high);
      const lowY = yScale(c.low);
      const bodyTop = Math.min(openY, closeY);
      const bodyH = Math.max(2, Math.abs(closeY - openY));

      return {
        ...c,
        key: `${c.date}-${i}`,
        cx,
        color,
        wick: { x1: cx, y1: highY, x2: cx, y2: lowY },
        body: {
          x: cx - bodyW / 2,
          y: bodyTop,
          w: bodyW,
          h: bodyH,
        },
      };
    });

    const last = candles[candles.length - 1];
    const first = candles[0];
    const change = last.close - first.open;
    const changePct = first.open ? (change / first.open) * 100 : 0;

    return {
      bars,
      yMin,
      yMax,
      last,
      first,
      change,
      changePct,
      periodHigh: max,
      periodLow: min,
    };
  }, [candles]);

  const currency = tickerCurrency(ticker);
  const badgeLabel =
    dataSource === 'tradernet'
      ? 'Tradernet'
      : loading
        ? 'Загрузка…'
        : 'Демо';

  return (
    <div className="market-chart-preview">
      <div className="market-chart-preview__head">
        <div>
          <h4 className="market-chart-preview__title">
            {preset.label}
            <span className="market-chart-preview__name">{preset.name}</span>
          </h4>
          <p className="subtitle market-chart-preview__hint">
            Свечи для анализа через Freedom / Tradernet API. Ключи хранятся только
            на сервере.
          </p>
          {marketStatus?.connected ? (
            <p className="subtitle market-chart-preview__hint" style={{ marginTop: 4 }}>
              API: подключено ({marketStatus.domain})
            </p>
          ) : marketStatus?.configured === false ? (
            <p className="subtitle market-chart-preview__hint" style={{ marginTop: 4 }}>
              API: ключи не заданы в .env — демо-график.
            </p>
          ) : null}
        </div>
        <span
          className={`market-chart-preview__badge${
            dataSource === 'tradernet' ? ' market-chart-preview__badge--live' : ''
          }`}
        >
          {badgeLabel}
        </span>
      </div>

      <div className="market-chart-preview__search-block">
        <div className="row market-chart-preview__controls">
          <label className="market-chart-preview__field market-chart-preview__field--grow">
            <span>Поиск тикера</span>
            <input
              type="search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="HSBK, Apple, SBER…"
              aria-label="Поиск тикера"
              autoComplete="off"
            />
          </label>
          <label className="market-chart-preview__field">
            <span>Рынок поиска</span>
            <select
              value={searchExchange}
              onChange={(e) => setSearchExchange(e.target.value)}
              aria-label="Рынок для поиска"
            >
              {SEARCH_EXCHANGES.map((x) => (
                <option key={x.id || 'all'} value={x.id}>
                  {x.label}
                </option>
              ))}
            </select>
          </label>
        </div>
        {searchLoading ? (
          <p className="subtitle market-chart-preview__search-hint">Поиск…</p>
        ) : null}
        {searchMessage && !searchLoading ? (
          <p className="subtitle market-chart-preview__search-hint">{searchMessage}</p>
        ) : null}
        {searchExchange === 'AIX' && accessToken ? (
          <p className="subtitle market-chart-preview__search-hint">
            AIX: тикеры вида <code>HSBK.AIX.KZ</code>, не <code>HSBK.KZ</code>.
            {' '}
            <a
              href="#"
              onClick={(e) => {
                e.preventDefault();
                const token = accessTokenRef.current;
                const req = requestRef.current;
                if (!token || typeof req !== 'function') return;
                const qs = new URLSearchParams();
                if (document.getElementById('aix-tradeable-only')?.checked) {
                  qs.set('tradeableOnly', 'true');
                }
                req(
                  'GET',
                  `/users/me/market/refbook/AIX?${qs.toString()}`,
                  null,
                  token,
                ).then((result) => {
                  if (!result.ok) return;
                  const blob = new Blob([JSON.stringify(result.data, null, 2)], {
                    type: 'application/json',
                  });
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement('a');
                  a.href = url;
                  a.download = 'aix-instruments.json';
                  a.click();
                  URL.revokeObjectURL(url);
                });
              }}
            >
              Скачать список AIX (JSON)
            </a>
            {' · '}
            <label style={{ display: 'inline', fontSize: 12 }}>
              <input type="checkbox" id="aix-tradeable-only" defaultChecked /> только
              торгуемые
            </label>
          </p>
        ) : null}
        {searchResults.length > 0 ? (
          <ul className="market-chart-preview__search-results" role="listbox">
            {searchResults.map((item) => (
              <li key={item.ticker}>
                <button
                  type="button"
                  className="market-chart-preview__search-item"
                  onClick={() => {
                    setTicker(item.ticker);
                    setSearchQuery('');
                    setSearchResults([]);
                  }}
                >
                  <span className="market-chart-preview__search-ticker">
                    {item.ticker}
                  </span>
                  {item.name ? (
                    <span className="market-chart-preview__search-name">
                      {item.name}
                    </span>
                  ) : null}
                  {item.exchange ? (
                    <span className="market-chart-preview__search-exchange">
                      {item.exchange}
                    </span>
                  ) : null}
                </button>
              </li>
            ))}
          </ul>
        ) : null}
      </div>

      <div className="row market-chart-preview__controls">
        <label className="market-chart-preview__field">
          <span>Быстрый выбор</span>
          <select
            value={ticker}
            onChange={(e) => setTicker(e.target.value)}
            aria-label="Тикер"
          >
            {PRESET_TICKERS.map((t) => (
              <option key={t.id} value={t.id}>
                {t.label}
              </option>
            ))}
          </select>
        </label>
        <label className="market-chart-preview__field">
          <span>Интервал</span>
          <select
            value={interval}
            onChange={(e) => setInterval(e.target.value)}
            aria-label="Интервал свечей"
          >
            {INTERVALS.map((i) => (
              <option key={i.id} value={i.id}>
                {i.label}
              </option>
            ))}
          </select>
        </label>
        <label className="market-chart-preview__field">
          <span>Период</span>
          <select
            value={period}
            onChange={(e) => setPeriod(e.target.value)}
            aria-label="Период графика"
          >
            {PERIODS.map((p) => (
              <option key={p.id} value={p.id}>
                {p.label}
              </option>
            ))}
          </select>
        </label>
        <button
          type="button"
          className="ghost-btn"
          onClick={() => setReloadNonce((n) => n + 1)}
          disabled={loading}
        >
          {loading ? 'Загрузка…' : 'Обновить'}
        </button>
      </div>

      {statusMessage ? (
        <p
          className="subtitle"
          style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}
        >
          {statusMessage}
        </p>
      ) : null}

      {layout ? (
        <>
          <div className="tracking-analytics-stats market-chart-preview__stats">
            <div className="tracking-analytics-stat">
              <span className="tracking-analytics-stat__label">Закрытие</span>
              <span className="tracking-analytics-stat__value">
                {formatPrice(layout.last.close, currency)}
              </span>
            </div>
            <div className="tracking-analytics-stat">
              <span className="tracking-analytics-stat__label">Изм. за период</span>
              <span
                className="tracking-analytics-stat__value"
                style={{
                  color: layout.change >= 0 ? '#3fb950' : '#f85149',
                }}
              >
                {layout.change >= 0 ? '+' : ''}
                {layout.changePct.toFixed(2)}%
              </span>
            </div>
            <div className="tracking-analytics-stat">
              <span className="tracking-analytics-stat__label">Макс.</span>
              <span className="tracking-analytics-stat__value">
                {formatPrice(layout.periodHigh, currency)}
              </span>
            </div>
            <div className="tracking-analytics-stat">
              <span className="tracking-analytics-stat__label">Мин.</span>
              <span className="tracking-analytics-stat__value">
                {formatPrice(layout.periodLow, currency)}
              </span>
            </div>
          </div>

          <div className="market-chart-preview__chart-wrap">
            <svg
              viewBox={`0 0 ${CHART_W} ${CHART_H}`}
              width="100%"
              height={CHART_H}
              role="img"
              aria-label={`Свечной график ${preset.label}`}
              className="market-chart-preview__svg"
            >
              <defs>
                <linearGradient
                  id="market-chart-grid-fade"
                  x1="0"
                  y1="0"
                  x2="0"
                  y2="1"
                >
                  <stop offset="0%" stopColor="rgba(157,194,169,0.08)" />
                  <stop offset="100%" stopColor="rgba(157,194,169,0.02)" />
                </linearGradient>
              </defs>
              <rect
                x={PAD.left}
                y={PAD.top}
                width={CHART_W - PAD.left - PAD.right}
                height={CHART_H - PAD.top - PAD.bottom}
                fill="url(#market-chart-grid-fade)"
                rx="8"
              />
              {[0.25, 0.5, 0.75].map((f) => {
                const y = PAD.top + (CHART_H - PAD.top - PAD.bottom) * (1 - f);
                return (
                  <line
                    key={f}
                    x1={PAD.left}
                    y1={y}
                    x2={CHART_W - PAD.right}
                    y2={y}
                    stroke="rgba(157,194,169,0.12)"
                  />
                );
              })}
              <line
                x1={PAD.left}
                y1={PAD.top}
                x2={PAD.left}
                y2={CHART_H - PAD.bottom}
                stroke="rgba(157,194,169,0.35)"
              />
              <line
                x1={PAD.left}
                y1={CHART_H - PAD.bottom}
                x2={CHART_W - PAD.right}
                y2={CHART_H - PAD.bottom}
                stroke="rgba(157,194,169,0.35)"
              />
              <text x="8" y={PAD.top + 4} fill="var(--muted)" fontSize="11">
                {layout.yMax.toFixed(2)}
              </text>
              <text
                x="8"
                y={CHART_H - PAD.bottom}
                fill="var(--muted)"
                fontSize="11"
              >
                {layout.yMin.toFixed(2)}
              </text>
              {layout.bars.map((b) => (
                <g key={b.key}>
                  <line
                    x1={b.wick.x1}
                    y1={b.wick.y1}
                    x2={b.wick.x2}
                    y2={b.wick.y2}
                    stroke={b.color}
                    strokeWidth="1"
                  />
                  <rect
                    x={b.body.x}
                    y={b.body.y}
                    width={b.body.w}
                    height={b.body.h}
                    fill={b.color}
                    rx="1"
                  >
                    <title>{`${b.date}\nO ${b.open} H ${b.high} L ${b.low} C ${b.close}`}</title>
                  </rect>
                </g>
              ))}
              <text
                x={PAD.left}
                y={CHART_H - 8}
                fill="var(--muted)"
                fontSize="11"
              >
                {layout.first.date}
              </text>
              <text
                x={CHART_W - PAD.right}
                y={CHART_H - 8}
                textAnchor="end"
                fill="var(--muted)"
                fontSize="11"
              >
                {layout.last.date}
              </text>
            </svg>
          </div>

          <p className="subtitle market-chart-preview__foot">
            {intervalMeta.label} · {periodMeta.label} · {candles.length} свечей
            · источник: {dataSource === 'tradernet' ? 'Tradernet (live)' : 'демо'}
          </p>
        </>
      ) : (
        <div className="workout-empty">
          {loading ? 'Загрузка свечей…' : 'Нет данных для графика.'}
        </div>
      )}
    </div>
  );
}
