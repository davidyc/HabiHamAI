import { useEffect, useMemo } from "react";
import { CircleMarker, MapContainer, Polyline, TileLayer, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";

const MAX_POLYLINE_POINTS = 2500;

function buildTrackPath(trackpoints) {
  const raw = (trackpoints || [])
    .filter((p) => p.latitude != null && p.longitude != null)
    .map((p) => [Number(p.latitude), Number(p.longitude)]);

  if (raw.length <= MAX_POLYLINE_POINTS) return raw;

  const step = Math.ceil(raw.length / MAX_POLYLINE_POINTS);
  const simplified = [];
  for (let i = 0; i < raw.length; i += step) simplified.push(raw[i]);
  const last = raw[raw.length - 1];
  const tail = simplified[simplified.length - 1];
  if (tail[0] !== last[0] || tail[1] !== last[1]) simplified.push(last);
  return simplified;
}

function MapResize({ active }) {
  const map = useMap();
  useEffect(() => {
    if (!active) return undefined;
    const t1 = requestAnimationFrame(() => map.invalidateSize());
    const t2 = setTimeout(() => map.invalidateSize(), 250);
    return () => {
      cancelAnimationFrame(t1);
      clearTimeout(t2);
    };
  }, [map, active]);
  return null;
}

function FitBounds({ positions }) {
  const map = useMap();
  useEffect(() => {
    if (positions.length < 2) return;
    map.fitBounds(positions, { padding: [28, 28] });
  }, [map, positions]);
  return null;
}

function BikeTrackMap({ trackpoints, active = true }) {
  const path = useMemo(() => buildTrackPath(trackpoints), [trackpoints]);
  const pointCount = (trackpoints || []).filter(
    (p) => p.latitude != null && p.longitude != null
  ).length;

  if (path.length < 2) {
    return (
      <p className="subtitle bike-track-map-empty">
        Недостаточно точек с координатами для отображения маршрута.
      </p>
    );
  }

  const center = path[Math.floor(path.length / 2)];

  return (
    <div className="bike-track-map-block">
      <p className="subtitle bike-track-map-caption">
        Маршрут · {pointCount} точек
        {path.length < pointCount ? ` · на карте ${path.length}` : null}
        <span className="bike-track-map-legend">
          <span className="bike-track-map-legend-item bike-track-map-legend-start">старт</span>
          <span className="bike-track-map-legend-item bike-track-map-legend-finish">финиш</span>
        </span>
      </p>
      <div className="bike-track-map" aria-label="Карта маршрута">
        <MapContainer
          center={center}
          zoom={13}
          className="bike-track-map-container"
          scrollWheelZoom
        >
          <MapResize active={active} />
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <Polyline
            positions={path}
            pathOptions={{ color: "#3ecf8e", weight: 4, opacity: 0.92 }}
          />
          <CircleMarker
            center={path[0]}
            radius={7}
            pathOptions={{
              color: "#16a34a",
              fillColor: "#22c55e",
              fillOpacity: 1,
              weight: 2
            }}
          />
          <CircleMarker
            center={path[path.length - 1]}
            radius={7}
            pathOptions={{
              color: "#dc2626",
              fillColor: "#ef4444",
              fillOpacity: 1,
              weight: 2
            }}
          />
          <FitBounds positions={path} />
        </MapContainer>
      </div>
    </div>
  );
}

export default BikeTrackMap;
