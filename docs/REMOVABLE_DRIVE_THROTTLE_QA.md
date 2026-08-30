# Removable-Drive Throttle QA Checklist

## Overview

High parallelism on UHS-I / UHS-II SD cards or USB 2.0 card readers causes controller thrashing, head contention, and buffer stalls. Quick Media Ingest enforces single-threaded file copies and caps preview decode workers to 2 on removable storage media (`RemovableDriveIo.cs`).

## Manual QA Checklist

1. [ ] **SD Card Detection:** Insert SD card into reader. Verify `RemovableDriveIo.IsOnRemovableDrive(path)` evaluates to `true`.
2. [ ] **Preview Worker Cap:** Verify thumbnail background rendering uses at most 2 worker threads on the removable path.
3. [ ] **Copy Serialization:** Verify multi-file import copies execute sequentially without multi-thread seek contention.
4. [ ] **Automated Timing Harness:** Run `RemovableDriveThrottleHarness.RunBenchmarkAsync()` to ensure sequential I/O matches or outperforms uncontrolled parallel bursts on flash controllers.
