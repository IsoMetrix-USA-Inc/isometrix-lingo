# IsometrixLingo v5.0.0 Release Notes

**Release Date:** July 1, 2026

## 🎉 Major Feature: Git Change Tracking

Version 5.0.0 introduces **Git Change Tracking** (LP-13426), a developer-focused workflow that detects exactly which translation keys were **added** or **modified** between two branches, lets you review and approve them, and carries that information through export, re-import, and deployment.

---

## ✨ New Features

### Git-Based Change Detection
- **Branch Comparison**: Compares a *deployed* branch against a *release* branch per repository to find changed translation files.
- **Multi-Repository Support**: Detect changes across several repositories in a single workflow.
- **JSON & RESX Diffing**: Parses git diffs for both `.json` and `.resx` translation files into added/modified change types.
- **Value-Aware Comparison**: Reordered keys with unchanged values are no longer falsely flagged as modified.
- **Automatic Fetch**: Fetches the latest remote state before comparing.

### Branch Comparison Dialog
- **Unified Single/Multi-Repo Workflow**: One consistent dialog for one or many repositories.
- **Debounced Per-Field Validation**: Branch names validated as you type, with inline red errors and green valid indicators.
- **Gated Confirmation**: Confirm is enabled only when all repositories have valid branches.
- **Smart Defaults**: Auto-detects `main`/`master` and supports local branches.

### Review & Approve Workflow
- **Approve Action**: Mark modified/added keys as reviewed with a dedicated action icon.
- **Visual Indicators**: Theme-aware row highlighting for modified/added/approved keys in both light and dark modes.
- **Include-Reviewed Sub-Filter**: Indented filter to show or hide already-reviewed keys.
- **Persistent Approval State**: Approvals are retained across sessions and survive export/re-import.

### Portable Change Metadata
- **Export Metadata**: Change information (added/modified/approved keys) is written into the export with repository/folder-prefixed paths so it stays portable.
- **Round-Trip Re-Import**: Re-importing restores change tracking and approval state, matching keys regardless of location.
- **Per-File-Pair Badges**: Change counts shown as badges on each file pair.
- **Deployment-Safe**: `metadata.json` is excluded from deployment so it is never copied to the target location.

### Detection UX
- **Spinner Progress Dialog**: A loading spinner with a status message is shown during change detection.
- **Faster Detection**: Removed artificial delays and moved git operations off the UI thread for a responsive, quicker experience.

---

## 🐛 Fixes & Improvements

- **Windows Title Bar**: Replaced the duplicated/overlapping native app-name with a custom title bar and caption buttons on Windows (`WindowDecorations.BorderOnly`); macOS keeps its native traffic lights.
- **Path Matching**: Robust relative-to-absolute path resolution and repo-prefix handling so changed files are reliably matched to imported keys.

---

## 🔧 Technical Implementation

### New Services
- **GitDiffService** (LibGit2Sharp): `FetchRepositoryAsync`, `GetCommitHash`, `GetChangedFiles`, `GetFileDiff`, `ParseJsonDiff`, `ParseResxDiff`.
- **MetadataExportService** / **MetadataImportService**: Write, read, and sync portable change metadata (`ModifiedKeys` / `AddedKeys` / `ApprovedKeys`).

### New UI Components
- **BranchComparisonDialog** (+ ViewModel): Branch configuration and validation.
- **ProgressDialog** (+ ViewModel): Spinner-based progress indicator.

### Models
- **ChangeType** enum and extended **TranslationKey** model for change state.
- **ChangeMetadata** / **RepositoryChangeInfo** / **FileChangeInfo** for portable tracking.

---

## ✅ Testing

- Unit and integration tests covering JSON/RESX diff parsing (reorder, modification, addition), path matching, and the complete filter workflow.
- Manual end-to-end validation: import → detect → review/approve → export → re-import → deploy.

---

## 📦 Downloads

- **macOS (Apple Silicon)**: `isometrix-lingo-v5.0.0-macos-arm64.tar.gz`
- **Windows (x64)**: `isometrix-lingo-v5.0.0-windows-x64.zip`
