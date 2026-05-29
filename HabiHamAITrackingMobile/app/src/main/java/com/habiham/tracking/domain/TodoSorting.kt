package com.habiham.tracking.domain

import com.habiham.tracking.data.model.TodoItemDto
import java.text.Collator
import java.util.Locale

private val ruCollator: Collator = Collator.getInstance(Locale.forLanguageTag("ru"))

enum class TodoSortKey(val label: String) {
    Title("Задача"),
    Category("Категория"),
    DueDate("Дедлайн"),
    Status("Статус"),
}

enum class TodoSortDir { Asc, Desc }

data class TodoTableSort(
    val key: TodoSortKey? = null,
    val dir: TodoSortDir = TodoSortDir.Asc,
)

fun cycleTodoTableSort(current: TodoTableSort, key: TodoSortKey): TodoTableSort {
    if (current.key == key) {
        val nextDir = if (current.dir == TodoSortDir.Asc) TodoSortDir.Desc else TodoSortDir.Asc
        return TodoTableSort(key = key, dir = nextDir)
    }
    return TodoTableSort(key = key, dir = TodoSortDir.Asc)
}

fun sortTodoItems(items: List<TodoItemDto>, sort: TodoTableSort): List<TodoItemDto> {
    val key = sort.key ?: return items
    val mult = if (sort.dir == TodoSortDir.Asc) 1 else -1
    return items.sortedWith { a, b ->
        mult * compareTodoItems(a, b, key)
    }
}

private fun compareTodoItems(a: TodoItemDto, b: TodoItemDto, key: TodoSortKey): Int = when (key) {
    TodoSortKey.Title ->
        ruCollator.compare(a.title, b.title)
    TodoSortKey.DueDate -> {
        val aDate = a.dueDate?.takeIf { it.isNotBlank() }
        val bDate = b.dueDate?.takeIf { it.isNotBlank() }
        when {
            aDate == null && bDate == null -> 0
            aDate == null -> 1
            bDate == null -> -1
            else -> aDate.compareTo(bDate)
        }
    }
    TodoSortKey.Status -> {
        val aDone = if (a.doneDate != null) 1 else 0
        val bDone = if (b.doneDate != null) 1 else 0
        when {
            aDone != bDone -> aDone - bDone
            aDone == 1 -> (a.doneDate ?: "").compareTo(b.doneDate ?: "")
            else -> ruCollator.compare(a.title, b.title)
        }
    }
    TodoSortKey.Category ->
        ruCollator.compare(a.categoryName ?: "", b.categoryName ?: "")
}
