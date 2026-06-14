#!/usr/bin/env bash
# WPF-adapted file line limits with grandfather list for Sprint 1 remediation
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
XAML_LIMIT=800
VIEWMODEL_LIMIT=400
CORE_LIMIT=200
ERRORS=0

# WPF-adapted file line limits with grandfather list for Sprint 1 remediation
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
XAML_LIMIT=800
VIEWMODEL_LIMIT=400
CORE_LIMIT=200
ERRORS=0

# Grandfathered until Core/ViewModel splits land for Milestone 9 FTP pipeline
GRANDFATHER=(
  "QuickMediaIngest/Core/ThumbnailService.cs"
  "QuickMediaIngest/Core/Ftp/FtpThumbnailPipeline.cs"
  "QuickMediaIngest/Core/Ftp/FtpTieredPreviewLoader.cs"
  "QuickMediaIngest/Core/Ftp/FtpFileDownloader.cs"
  "QuickMediaIngest/ViewModels/MainViewModel.Config.partial.cs"
  "QuickMediaIngest/ViewModels/MainViewModel.Ftp.partial.cs"
  "QuickMediaIngest/ViewModels/MainViewModel.Thumbnails.partial.cs"
)

is_grandfathered() {
  local rel="$1"
  for g in "${GRANDFATHER[@]}"; do
    if [ "$rel" = "$g" ]; then
      return 0
    fi
  done
  return 1
}

check_file() {
  local file="$1"
  local limit="$2"
  local label="$3"
  local rel="${file#$ROOT/}"
  rel="${rel//\\//}"

  if is_grandfathered "$rel"; then
    echo "SKIP [grandfather] $rel"
    return
  fi

  local lines
  lines=$(wc -l < "$file" | tr -d ' ')
  if [ "$lines" -gt "$limit" ]; then
    echo "FAIL [$label] $rel: $lines lines (max $limit)"
    ERRORS=$((ERRORS + 1))
  fi
}

echo "Checking .xaml file limits (max $XAML_LIMIT lines)..."
while IFS= read -r -d '' file; do
  check_file "$file" "$XAML_LIMIT" "xaml"
done < <(find "$ROOT/QuickMediaIngest" -type f -name "*.xaml" ! -path "*/bin/*" ! -path "*/obj/*" -print0 2>/dev/null)

echo "Checking ViewModels/*.cs limits (max $VIEWMODEL_LIMIT lines)..."
while IFS= read -r -d '' file; do
  check_file "$file" "$VIEWMODEL_LIMIT" "viewmodel"
done < <(find "$ROOT/QuickMediaIngest/ViewModels" -type f -name "*.cs" ! -path "*/bin/*" ! -path "*/obj/*" -print0 2>/dev/null)

echo "Checking *.xaml.cs limits (max $VIEWMODEL_LIMIT lines)..."
while IFS= read -r -d '' file; do
  check_file "$file" "$VIEWMODEL_LIMIT" "codebehind"
done < <(find "$ROOT/QuickMediaIngest" -type f -name "*.xaml.cs" ! -path "*/bin/*" ! -path "*/obj/*" -print0 2>/dev/null)

echo "Checking Core/**/*.cs limits (max $CORE_LIMIT lines)..."
while IFS= read -r -d '' file; do
  check_file "$file" "$CORE_LIMIT" "core"
done < <(find "$ROOT/QuickMediaIngest/Core" -type f -name "*.cs" ! -path "*/bin/*" ! -path "*/obj/*" -print0 2>/dev/null)

if [ "$ERRORS" -gt 0 ]; then
  echo "$ERRORS file(s) exceed line limits"
  exit 1
fi

echo "All file line limits OK"
