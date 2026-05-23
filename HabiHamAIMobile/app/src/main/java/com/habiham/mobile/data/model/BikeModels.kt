package com.habiham.mobile.data.model

import com.squareup.moshi.JsonClass

@JsonClass(generateAdapter = true)
data class BikeActivitySummaryDto(
    val id: String,
    val sport: String? = null,
    val notes: String? = null,
    val startTimeUtc: String? = null,
    val totalSeconds: Double? = null,
    val distanceMeters: Double? = null,
    val calories: Double? = null,
    val averageHeartRateBpm: Int? = null,
    val maxHeartRateBpm: Int? = null,
    val trackpointCount: Int = 0,
    val importedAtUtc: String? = null,
)

@JsonClass(generateAdapter = true)
data class BikeActivityDetailDto(
    val id: String,
    val sport: String? = null,
    val notes: String? = null,
    val startTimeUtc: String? = null,
    val totalSeconds: Double? = null,
    val distanceMeters: Double? = null,
    val calories: Double? = null,
    val averageHeartRateBpm: Int? = null,
    val maxHeartRateBpm: Int? = null,
    val trackpointCount: Int = 0,
    val importedAtUtc: String? = null,
    val intensity: String? = null,
    val triggerMethod: String? = null,
    val trackpoints: List<BikeTrackPointDto> = emptyList(),
)

@JsonClass(generateAdapter = true)
data class BikeTrackPointDto(
    val timeUtc: String? = null,
    val latitude: Double? = null,
    val longitude: Double? = null,
    val altitudeMeters: Double? = null,
    val heartRateBpm: Int? = null,
    val cadence: Int? = null,
    val speedMetersPerSecond: Double? = null,
)
