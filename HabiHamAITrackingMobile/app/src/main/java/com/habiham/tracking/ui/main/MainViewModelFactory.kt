package com.habiham.tracking.ui.main

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.habiham.tracking.data.prefs.StoredSession
import com.habiham.tracking.data.repository.TrackingRepository
import com.habiham.tracking.ui.habits.HabitsViewModel
import com.habiham.tracking.ui.todos.TodosViewModel

class MainViewModelFactory(
    private val session: StoredSession,
    private val trackingRepository: TrackingRepository,
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        return when {
            modelClass.isAssignableFrom(HabitsViewModel::class.java) ->
                HabitsViewModel(session, trackingRepository) as T
            modelClass.isAssignableFrom(TodosViewModel::class.java) ->
                TodosViewModel(session, trackingRepository) as T
            else -> throw IllegalArgumentException("Unknown ViewModel: ${modelClass.name}")
        }
    }
}
