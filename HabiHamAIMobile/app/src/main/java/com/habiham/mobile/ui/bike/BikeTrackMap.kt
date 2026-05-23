package com.habiham.mobile.ui.bike

import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import com.habiham.mobile.data.model.BikeTrackPointDto
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Polyline

@Composable
fun BikeTrackMap(
    trackpoints: List<BikeTrackPointDto>,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val points = trackpoints.mapNotNull { tp ->
        val lat = tp.latitude
        val lon = tp.longitude
        if (lat != null && lon != null) GeoPoint(lat, lon) else null
    }

    if (points.isEmpty()) return

    AndroidView(
        modifier = modifier
            .fillMaxWidth()
            .height(240.dp),
        factory = {
            MapView(context).apply {
                setMultiTouchControls(true)
                controller.setZoom(14.0)
            }
        },
        update = { mapView ->
            mapView.overlays.clear()
            val polyline = Polyline(mapView).apply {
                setPoints(points)
                outlinePaint.strokeWidth = 8f
            }
            mapView.overlays.add(polyline)
            val center = points[points.size / 2]
            mapView.controller.animateTo(center)
            mapView.invalidate()
        },
    )

    DisposableEffect(Unit) {
        onDispose { }
    }
}
