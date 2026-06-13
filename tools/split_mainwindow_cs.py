"""Split MainWindow.xaml.cs into partial files under 400 lines."""
import os
import re

MAX_LINES = 400
TARGET = 300

root = os.path.join(os.path.dirname(__file__), "..", "QuickMediaIngest")
mw_path = os.path.join(root, "MainWindow.xaml.cs")
content = open(mw_path, encoding="utf-8").read()
lines = content.splitlines(keepends=True)

# Strip trailing converter namespace block if present
conv_idx = content.find("\n// Converter to invert boolean")
if conv_idx >= 0:
    content = content[:conv_idx]
    lines = content.splitlines(keepends=True)

ns_line = next(i for i, l in enumerate(lines) if l.startswith("namespace QuickMediaIngest"))
class_line = next(i for i, l in enumerate(lines) if "public partial class MainWindow" in l)

depth = 0
class_close = None
for i in range(class_line, len(lines)):
    depth += lines[i].count("{") - lines[i].count("}")
    if i > class_line and depth == 0:
        class_close = i
        break

usings = "".join(lines[: ns_line])
body = lines[class_line + 2 : class_close]


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


def refine(chunks):
    refined = []
    for chunk in chunks:
        total = 35 + len(chunk) + 2
        if total <= MAX_LINES:
            refined.append(chunk)
            continue
        sub_target = max(180, len(chunk) // ((total // MAX_LINES) + 1))
        sub = []
        current = []
        depth = 1
        for line in chunk:
            current.append(line)
            depth += line.count("{") - line.count("}")
            if depth == 1 and len(current) >= sub_target:
                sub.append(current)
                current = []
        if current:
            sub.append(current)
        if len(sub) <= 1:
            raise SystemExit(f"Cannot split chunk of {len(chunk)} lines")
        refined.extend(sub)
    return refined


chunks = refine(split_body(body, TARGET))
suffixes = ["Chrome", "Ribbon", "Settings", "Input", "Sidebar", "Part6", "Part7", "Part8"]

header = (
    usings
    + "namespace QuickMediaIngest\n{\n"
    + "    public partial class MainWindow : Window\n"
    + "    {\n"
)
footer = "    }\n}\n"

for f in os.listdir(root):
    if f.startswith("MainWindow.") and f.endswith(".partial.cs"):
        os.remove(os.path.join(root, f))

for idx, chunk in enumerate(chunks):
    fname = "MainWindow.xaml.cs" if idx == 0 else f"MainWindow.{suffixes[idx - 1] if idx - 1 < len(suffixes) else f'Part{idx}'}.partial.cs"
    path = os.path.join(root, fname)
    text = header + "".join(chunk) + footer
    total = len(text.splitlines())
    if total > MAX_LINES:
        raise SystemExit(f"{fname} is {total} lines")
    open(path, "w", encoding="utf-8").write(text)
    print(f"{fname}: body={len(chunk)} total={total}")

print(f"Chunks: {len(chunks)}")
