# 📋 Editor Tasks & Notes Manager (`com.dev.todonotes`)

A modern, native Unity Editor utility designed to streamline game development workflows by keeping tasks, design notes, scratchpads, and in-scene sticky note pins directly inside Unity.

---

## 🚀 Features

- **📋 Task & To-Do List Manager**:
  - Track tasks by status: `To Do`, `In Progress`, `Done`, `Blocked`.
  - Color-coded priority badges: `Urgent` (Red), `High` (Orange), `Medium` (Yellow/Blue), `Low` (Grey/Green).
  - Categorization: `Feature`, `Bug`, `Refactor`, `Art`, `Audio`, `UI`, `Optimization`, `General`, plus custom categories.
  - **Direct Unity Object Linking**: Drag and drop any Asset (Prefab, Script, Scene, Material, ScriptableObject) or Scene GameObject into a task. Click the Ping button to highlight it instantly in the Project window or Scene hierarchy!
  - Real-time search and multi-criteria filtering (by status, priority, category).
  - Dynamic completion progress bar.

- **📝 Notes & Documentation Explorer**:
  - Two-pane master-detail view for managing design notes, architectural plans, and bug logs.
  - Pin important notes to the top.
  - Color tags for quick visual categorization.
  - Multi-asset attachment support per note.

- **⚡ Quick Scratchpad**:
  - Instant, distraction-free scratchpad for jotting thoughts or pasting console logs.
  - Automatically persisted across domain reloads.
  - One-click "Convert to Task" and "Convert to Note" buttons.

- **📌 In-Scene Sticky Pins (`SceneTaskMarker`)**:
  - Drop 3D task markers directly into your scene at world coordinates.
  - View, frame, and jump to in-scene pins from the Editor Window.

- **💾 Data Persistence & Export**:
  - Stored cleanly in a project `ScriptableObject` database with full `Undo`/`Redo` support.
  - Export tasks & notes to **Markdown (`.md`)** for team docs or **JSON** for backups.

---

## ⌨️ Accessing the Window

Open the manager via either:
- Menu: **`Tools > Task & Notes Manager`**
- Menu: **`Window > General > Task & Notes Manager`**
- Hotkey: **`Ctrl + Alt + T`** (Windows) / **`Cmd + Alt + T`** (Mac)

---

## 📦 Package Structure

```
Packages/com.dev.todonotes/
├── Runtime/          # Runtime / Scene-level components (SceneTaskMarker)
└── Editor/           # EditorWindow, Models, Inspectors, Exporters, and Styles
```
