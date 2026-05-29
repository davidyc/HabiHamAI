package com.habiham.tracking.domain

import com.habiham.tracking.data.model.HabitOverviewDto
import com.habiham.tracking.data.model.TodoItemDto
import com.habiham.tracking.data.model.UserCategoryDto

const val CATEGORY_ALL_KEY = "__all__"
const val CATEGORY_NONE_KEY = "__none__"

data class CategoryGroup<T>(
    val tabKey: String,
    val categoryId: String?,
    val categoryName: String?,
    val tabLabel: String?,
    val showHeader: Boolean,
    val showCategoryColumn: Boolean,
    val doneCount: Int,
    val totalCount: Int,
    val items: List<T>,
)

data class CategoryFilterOption(
    val value: String,
    val label: String,
)

private fun categorySortOrder(categories: List<UserCategoryDto>, categoryId: String?): Int {
    if (categoryId == null) return 9999
    return categories.find { it.id == categoryId }?.sortOrder ?: 9999
}

fun buildHabitCategoryGroups(
    habits: List<HabitOverviewDto>,
    categories: List<UserCategoryDto>,
): List<CategoryGroup<HabitOverviewDto>> {
    val grouped = habits.groupBy { h ->
        h.categoryId?.takeIf { it.isNotBlank() } ?: CATEGORY_NONE_KEY
    }
    return grouped.map { (key, items) ->
        val categoryId = items.firstOrNull()?.categoryId
        val categoryName = items.firstOrNull()?.categoryName
        val tabKey = if (key == CATEGORY_NONE_KEY) CATEGORY_NONE_KEY else key
        CategoryGroup(
            tabKey = tabKey,
            categoryId = categoryId,
            categoryName = categoryName,
            tabLabel = if (categoryId != null && !categoryName.isNullOrBlank()) categoryName else null,
            showHeader = categoryId != null && !categoryName.isNullOrBlank(),
            showCategoryColumn = false,
            doneCount = items.count { it.isDoneToday },
            totalCount = items.size,
            items = items,
        )
    }.sortedWith { a, b ->
        when {
            a.categoryId == null -> 1
            b.categoryId == null -> -1
            else -> categorySortOrder(categories, a.categoryId) - categorySortOrder(categories, b.categoryId)
        }
    }
}

fun habitsAllCategoryGroup(habits: List<HabitOverviewDto>): CategoryGroup<HabitOverviewDto>? {
    if (habits.isEmpty()) return null
    return CategoryGroup(
        tabKey = CATEGORY_ALL_KEY,
        categoryId = null,
        categoryName = null,
        tabLabel = "Все",
        showHeader = false,
        showCategoryColumn = true,
        doneCount = habits.count { it.isDoneToday },
        totalCount = habits.size,
        items = habits,
    )
}

fun habitCategoryFilterOptions(
    allGroup: CategoryGroup<HabitOverviewDto>?,
    groups: List<CategoryGroup<HabitOverviewDto>>,
): List<CategoryFilterOption> {
    val options = mutableListOf<CategoryFilterOption>()
    allGroup?.let {
        options.add(
            CategoryFilterOption(
                value = CATEGORY_ALL_KEY,
                label = "Все (${it.doneCount} / ${it.totalCount})",
            ),
        )
    }
    groups.forEach { g ->
        options.add(
            CategoryFilterOption(
                value = g.tabKey,
                label = "${g.tabLabel ?: "Без категории"} (${g.doneCount} / ${g.totalCount})",
            ),
        )
    }
    return options
}

fun activeHabitCategoryGroup(
    habits: List<HabitOverviewDto>,
    categories: List<UserCategoryDto>,
    categoryTabKey: String,
): CategoryGroup<HabitOverviewDto>? {
    if (habits.isEmpty()) return null
    val allGroup = habitsAllCategoryGroup(habits) ?: return null
    if (categoryTabKey == CATEGORY_ALL_KEY) return allGroup
    val groups = buildHabitCategoryGroups(habits, categories)
    if (groups.isEmpty()) return allGroup
    return groups.find { it.tabKey == categoryTabKey } ?: allGroup
}

fun buildTodoCategoryGroups(
    todos: List<TodoItemDto>,
    categories: List<UserCategoryDto>,
): List<CategoryGroup<TodoItemDto>> {
    val grouped = todos.groupBy { t ->
        t.categoryId?.takeIf { it.isNotBlank() } ?: CATEGORY_NONE_KEY
    }
    return grouped.map { (key, items) ->
        val categoryId = items.firstOrNull()?.categoryId
        val categoryName = items.firstOrNull()?.categoryName
        val tabKey = if (key == CATEGORY_NONE_KEY) CATEGORY_NONE_KEY else key
        CategoryGroup(
            tabKey = tabKey,
            categoryId = categoryId,
            categoryName = categoryName,
            tabLabel = if (categoryId != null && !categoryName.isNullOrBlank()) categoryName else null,
            showHeader = categoryId != null && !categoryName.isNullOrBlank(),
            showCategoryColumn = false,
            doneCount = items.count { it.doneDate != null },
            totalCount = items.size,
            items = items,
        )
    }.sortedWith { a, b ->
        when {
            a.categoryId == null -> 1
            b.categoryId == null -> -1
            else -> categorySortOrder(categories, a.categoryId) - categorySortOrder(categories, b.categoryId)
        }
    }
}

fun todosAllCategoryGroup(todos: List<TodoItemDto>): CategoryGroup<TodoItemDto>? {
    if (todos.isEmpty()) return null
    return CategoryGroup(
        tabKey = CATEGORY_ALL_KEY,
        categoryId = null,
        categoryName = null,
        tabLabel = "Все",
        showHeader = false,
        showCategoryColumn = true,
        doneCount = todos.count { it.doneDate != null },
        totalCount = todos.size,
        items = todos,
    )
}

fun todoCategoryFilterOptions(
    allGroup: CategoryGroup<TodoItemDto>?,
    groups: List<CategoryGroup<TodoItemDto>>,
): List<CategoryFilterOption> {
    val options = mutableListOf<CategoryFilterOption>()
    allGroup?.let {
        options.add(
            CategoryFilterOption(
                value = CATEGORY_ALL_KEY,
                label = "Все (${it.doneCount} / ${it.totalCount})",
            ),
        )
    }
    groups.forEach { g ->
        options.add(
            CategoryFilterOption(
                value = g.tabKey,
                label = "${g.tabLabel ?: "Без категории"} (${g.doneCount} / ${g.totalCount})",
            ),
        )
    }
    return options
}

fun activeTodoCategoryGroup(
    todos: List<TodoItemDto>,
    categories: List<UserCategoryDto>,
    categoryTabKey: String,
): CategoryGroup<TodoItemDto>? {
    if (todos.isEmpty()) return null
    val allGroup = todosAllCategoryGroup(todos) ?: return null
    if (categoryTabKey == CATEGORY_ALL_KEY) return allGroup
    val groups = buildTodoCategoryGroups(todos, categories)
    if (groups.isEmpty()) return allGroup
    return groups.find { it.tabKey == categoryTabKey } ?: allGroup
}
