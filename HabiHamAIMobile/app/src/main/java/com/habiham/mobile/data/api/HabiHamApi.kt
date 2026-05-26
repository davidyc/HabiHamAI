package com.habiham.mobile.data.api

import com.habiham.mobile.data.model.BikeActivityDetailDto
import com.habiham.mobile.data.model.BikeActivitySummaryDto
import com.habiham.mobile.data.model.HistoryProgramOption
import com.habiham.mobile.data.model.LoginRequest
import com.habiham.mobile.data.model.LoginResponse
import com.habiham.mobile.data.model.RegisterRequest
import com.habiham.mobile.data.model.RegisterResponse
import com.habiham.mobile.data.model.UpsertWorkoutSessionRequest
import com.habiham.mobile.data.model.WorkoutSessionDto
import okhttp3.MultipartBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.Part
import retrofit2.http.Path
import retrofit2.http.Query

interface HabiHamApi {
    @POST("auth/login")
    suspend fun login(@Body body: LoginRequest): LoginResponse

    @POST("auth/register")
    suspend fun register(@Body body: RegisterRequest): RegisterResponse

    @GET("users/me/workouts")
    suspend fun getMyWorkouts(
        @Query("includeHistory") includeHistory: Boolean = false,
    ): List<WorkoutSessionDto>

    @POST("users/me/workouts")
    suspend fun upsertWorkout(@Body body: UpsertWorkoutSessionRequest): WorkoutSessionDto

    @GET("users/me/workouts/history")
    suspend fun getWorkoutHistory(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null,
        @Query("program") program: String? = null,
    ): List<WorkoutSessionDto>

    @GET("users/me/workouts/history/options")
    suspend fun getHistoryProgramOptions(): List<HistoryProgramOption>

    @GET("users/me/bike-activities")
    suspend fun listBikeActivities(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null,
        @Query("sport") sport: String? = "Biking",
    ): List<BikeActivitySummaryDto>

    @GET("users/me/bike-activities/{id}")
    suspend fun getBikeActivity(
        @Path("id") id: String,
        @Query("trackpointLimit") trackpointLimit: Int? = 5000,
    ): BikeActivityDetailDto

    @Multipart
    @POST("users/me/bike-activities/import")
    suspend fun importBikeTcx(@Part file: MultipartBody.Part): BikeActivitySummaryDto

    @DELETE("users/me/bike-activities/{id}")
    suspend fun deleteBikeActivity(@Path("id") id: String): Response<Unit>
}
