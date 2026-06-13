"""Split MainViewModel.cs into partial files under 400 lines."""
import os
import re

MAX_LINES = 400
TARGET = 320

root = os.path.join(os.path.dirname(__file__), "..", "QuickMediaIngest")
vm_dir = os.path.join(root, "ViewModels")
src = os.path.join(vm_dir, "MainViewModel.cs")
KEEP = {"MainViewModel.Config.partial.cs", "MainViewModel.Tokens.partial.cs"}

content = open(src, encoding="utf-8").read()
lines = content.splitlines(keepends=True)

ns_match = re.search(r"(^namespace QuickMediaIngest\.ViewModels\r?\n\{)", content, re.M)
if not ns_match:
    raise SystemExit("namespace not found")
ns_open_line = content[: ns_match.start()].count("\n")
vm_line = next(i for i, l in enumerate(lines) if "public partial class MainViewModel" in l)

depth = 0
class_close = None
for i in range(vm_line, len(lines)):
    depth += lines[i].count("{") - lines[i].count("}")
    if i > vm_line and depth == 0:
        class_close = i
        break
if class_close is None:
    raise SystemExit("class close not found")

usings = "".join(lines[: ns_open_line])
before_types = "".join(lines[ns_open_line + 2 : vm_line])
after_types = "".join(lines[class_close + 1 : -1])

support = (
    usings
    + "namespace QuickMediaIngest.ViewModels\n{\n"
    + before_types
    + after_types
    + "}\n"
)
open(os.path.join(vm_dir, "MainViewModel.SupportTypes.cs"), "w", encoding="utf-8").write(support)

body = lines[vm_line + 2 : class_close]


def split_body(body_lines, target):
    chunks = []
    current = []
    depth = 1
    for line in body_lines:
        current.append(line)
        depth += line.count("{") - line.count("}")
        if depth == 1 and len(current) >= target:
            chunks.append(current)
            current = []
    if current:
        chunks.append(current)
    return chunks


def refine_chunks(chunks):
    refined = []
    for chunk in chunks:
        header = 35  # usings + namespace + class open
        footer = 2
        total = header + len(chunk) + footer
        if total <= MAX_LINES:
            refined.append(chunk)
            continue
        # Re-split oversized chunk at any depth-1 boundary
        sub = []
        current = []
        depth = 1
        sub_target = max(200, len(chunk) // ((total // MAX_LINES) + 1))
        for line in chunk:
            current.append(line)
            depth += line.count("{") - line.count("}")
            if depth == 1 and len(current) >= sub_target:
                sub.append(current)
                current = []
        if current:
            sub.append(current)
        if len(sub) <= 1:
            raise SystemExit(f"Cannot split chunk of {len(chunk)} lines (method block too large)")
        refined.extend(sub)
    return refined


chunks = refine_chunks(split_body(body, TARGET))

suffixes = [
    "UiState",
    "Ftp",
    "Scan",
    "Import",
    "Filters",
    "Updates",
    "History",
    "Exclusions",
    "Part9",
    "Part10",
    "Part11",
    "Part12",
    "Part13",
    "Part14",
    "Part15",
    "Part16",
    "Part17",
    "Part18",
]

partial_header = (
    usings
    + "namespace QuickMediaIngest.ViewModels\n{\n"
    + "    public partial class MainViewModel : ObservableObject\n"
    + "    {\n"
)
partial_footer = "    }\n}\n"

for f in os.listdir(vm_dir):
    if f.startswith("MainViewModel.") and f.endswith(".partial.cs") and f not in KEEP:
        os.remove(os.path.join(vm_dir, f))

for idx, chunk in enumerate(chunks):
    if idx == 0:
        fname = "MainViewModel.cs"
    else:
        suffix = suffixes[idx - 1] if idx - 1 < len(suffixes) else f"Part{idx}"
        fname = f"MainViewModel.{suffix}.partial.cs"
    path = os.path.join(vm_dir, fname)
    text = partial_header + "".join(chunk) + partial_footer
    total = len(text.splitlines())
    if total > MAX_LINES:
        raise SystemExit(f"{fname} still {total} lines")
    open(path, "w", encoding="utf-8").write(text)
    print(f"{fname}: body={len(chunk)} total={total}")

print(f"Chunks: {len(chunks)}")
