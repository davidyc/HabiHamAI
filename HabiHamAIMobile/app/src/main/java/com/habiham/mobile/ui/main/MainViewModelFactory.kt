package com.habiham.mobile.ui.main

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.habiham.mobile.data.prefs.StoredSession
import com.habiham.mobile.data.repository.BikeRepository
import com.habiham.mobile.data.repository.WorkoutsRepository
import com.habiham.mobile.ui.bike.BikeViewModel
import com.habiham.mobile.ui.workouts.ActiveWorkoutViewModel
import com.habiham.mobile.ui.workouts.WorkoutsViewModel

class MainViewModelFactory(
    private val session: StoredSession,
    private val workoutsRepository: WorkoutsRepository,
    private val bikeRepository: BikeRepository,
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        return when {
            modelClass.isAssignableFrom(WorkoutsViewModel::class.java) ->
                WorkoutsViewModel(session, workoutsRepository) as T
            modelClass.isAssignableFrom(ActiveWorkoutViewModel::class.java) ->
                ActiveWorkoutViewModel(session, workoutsRepository) as T
            modelClass.isAssignableFrom(BikeViewModel::class.java) ->
                BikeViewModel(session, bikeRepository) as T
            else -> throw IllegalArgumentException("Unknown ViewModel: ${modelClass.name}")
        }
    }
}
