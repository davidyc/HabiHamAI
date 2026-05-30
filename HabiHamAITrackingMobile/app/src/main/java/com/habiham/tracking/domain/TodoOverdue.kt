package com.habiham.tracking.domain

import com.habiham.tracking.data.model.TodoItemDto
import com.habiham.tracking.util.todayIso

fun isTodoOverdue(todo: TodoItemDto): Boolean {
    if (!todo.doneDate.isNullOrBlank()) return false
    val due = todo.dueDate?.take(10)?.takeIf { it.isNotBlank() } ?: return false
    return due < todayIso()
}
