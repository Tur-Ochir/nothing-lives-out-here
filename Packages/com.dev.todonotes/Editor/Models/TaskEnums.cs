using System;
using UnityEngine;

namespace Dev.TodoNotes.Editor
{
    /// <summary>
    /// Workflow status for a task item.
    /// </summary>
    public enum TaskStatus
    {
        ToDo = 0,
        InProgress = 1,
        Done = 2,
        Blocked = 3
    }

    /// <summary>
    /// Visual color accent tags for notes and cards.
    /// </summary>
    public enum NoteColorTag
    {
        Default = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Orange = 4,
        Red = 5,
        Purple = 6,
        Teal = 7
    }

    /// <summary>
    /// Sorting modes for task lists.
    /// </summary>
    public enum TaskSortMode
    {
        CustomOrder = 0,
        PriorityDesc = 1,
        PriorityAsc = 2,
        DateCreatedDesc = 3,
        DateCreatedAsc = 4,
        Category = 5,
        Alphabetical = 6
    }

    /// <summary>
    /// Status filter options.
    /// </summary>
    public enum TaskFilterStatus
    {
        All = 0,
        ActiveOnly = 1,
        CompletedOnly = 2,
        BlockedOnly = 3
    }

    /// <summary>
    /// View mode for tasks (List view vs Kanban board).
    /// </summary>
    public enum TaskViewMode
    {
        List = 0,
        Kanban = 1
    }
}
