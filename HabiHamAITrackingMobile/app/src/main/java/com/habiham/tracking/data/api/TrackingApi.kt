package com.habiham.tracking.data.api

import com.habiham.tracking.data.model.CreateHabitRequest
import com.habiham.tracking.data.model.UpdateHabitRequest
import com.habiham.tracking.data.model.CreateTodoRequest
import com.habiham.tracking.data.model.HabitCheckinDto
import com.habiham.tracking.data.model.HabitOverviewDto
import com.habiham.tracking.data.model.LoginRequest
import com.habiham.tracking.data.model.LoginResponse
import com.habiham.tracking.data.model.RegisterRequest
import com.habiham.tracking.data.model.RegisterResponse
import com.habiham.tracking.data.model.TodoItemDto
import com.habiham.tracking.data.model.UpsertHabitCheckinRequest
import com.habiham.tracking.data.model.UpsertTodoDoneRequest
import com.habiham.tracking.data.model.UserCategoryDto
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface TrackingApi {
    @POST("auth/login")
    suspend fun login(@Body body: LoginRequest): LoginResponse

    @POST("auth/register")
    suspend fun register(@Body body: RegisterRequest): RegisterResponse

    @GET("users/me/categories")
    suspend fun getCategories(): List<UserCategoryDto>

    @GET("users/me/habits/overview")
    suspend fun getHabitsOverview(): List<HabitOverviewDto>

    @POST("users/me/habits")
    suspend fun createHabit(@Body body: CreateHabitRequest): List<HabitOverviewDto>

    @PUT("users/me/habits/{habitId}")
    suspend fun updateHabit(
        @Path("habitId") habitId: String,
        @Body body: UpdateHabitRequest,
    ): List<HabitOverviewDto>

    @DELETE("users/me/habits/{habitId}")
    suspend fun deleteHabit(@Path("habitId") habitId: String): Response<Unit>

    @GET("users/me/habits/{habitId}/checkins")
    suspend fun getHabitCheckins(
        @Path("habitId") habitId: String,
        @Query("from") from: String? = null,
        @Query("to") to: String? = null,
    ): List<HabitCheckinDto>

    @POST("users/me/habits/{habitId}/checkins")
    suspend fun upsertHabitCheckin(
        @Path("habitId") habitId: String,
        @Body body: UpsertHabitCheckinRequest,
    ): HabitCheckinDto

    @DELETE("users/me/habits/{habitId}/checkins")
    suspend fun deleteHabitCheckin(
        @Path("habitId") habitId: String,
        @Query("date") date: String,
    ): Response<Unit>

    @GET("users/me/todos")
    suspend fun getTodos(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null,
    ): List<TodoItemDto>

    @POST("users/me/todos")
    suspend fun createTodo(@Body body: CreateTodoRequest): TodoItemDto

    @DELETE("users/me/todos/{todoId}")
    suspend fun deleteTodo(@Path("todoId") todoId: String): Response<Unit>

    @PUT("users/me/todos/{todoId}/done")
    suspend fun upsertTodoDone(
        @Path("todoId") todoId: String,
        @Body body: UpsertTodoDoneRequest,
    ): TodoItemDto
}
