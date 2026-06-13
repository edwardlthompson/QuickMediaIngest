"""Extract MainWindow overlay XAML sections into UserControls."""
import os
import re

root = os.path.join(os.path.dirname(__file__), "..", "QuickMediaIngest")
controls = os.path.join(root, "Controls")
os.makedirs(controls, exist_ok=True)

xaml_path = os.path.join(root, "MainWindow.xaml")
xaml_lines = open(xaml_path, encoding="utf-8").read().splitlines(keepends=True)

VISUAL_BRUSH_FIX = (
    'Visual="{Binding ElementName=MainChromeRoot, '
    'RelativeSource={RelativeSource AncestorType=Window}}"'
)

HANDLERS = {
    "DialogOverlaysView": [
        "AddFtpOverlay_Loaded",
        "AddFtpOverlay_Unloaded",
        "OpenLogs_Click",
        "ReportBug_Click",
        "PasswordBox_PasswordChanged",
    ],
    "ImportHistoryOverlayView": [
        "CloseImportHistory_Click",
        "ImportHistory_Clear_Click",
    ],
    "PreferencesOverlayView": [
        "CloseSettings_Click",
        "Browse_Click",
    ],
    "ScanExclusionsOverlayView": [
        "CloseScanExclusions_Click",
    ],
}


def fix_inner(lines):
    out = []
    for line in lines:
        if line.startswith("    "):
            line = line[4:]
        if 'ElementName=MainChromeRoot}"' in line:
            line = line.replace(
                'Visual="{Binding ElementName=MainChromeRoot}"',
                VISUAL_BRUSH_FIX,
            )
        out.append(line)
    return out


def write_usercontrol(name, inner_lines, grid_attrs=""):
    inner = fix_inner(inner_lines)
    uc_header = f'''<UserControl x:Class="QuickMediaIngest.Controls.{name}"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="clr-namespace:QuickMediaIngest"
    xmlns:models="clr-namespace:QuickMediaIngest.Core.Models"
    xmlns:loc="clr-namespace:QuickMediaIngest.Localization"
    xmlns:vm="clr-namespace:QuickMediaIngest.ViewModels"
    xmlns:converters="clr-namespace:QuickMediaIngest.Converters"
    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Grid>
'''
    uc_footer = "    </Grid>\n</UserControl>\n"
    folder = os.path.join(controls, name.replace("View", ""))
    os.makedirs(folder, exist_ok=True)
    open(os.path.join(folder, f"{name}.xaml"), "w", encoding="utf-8").write(
        uc_header + "".join(inner) + uc_footer
    )

    forwards = HANDLERS.get(name, [])
    methods = []
    for h in forwards:
        methods.append(
            f"        private void {h}(object sender, RoutedEventArgs e) =>\n"
            f"            (Window.GetWindow(this) as MainWindow)?.{h}(sender, e);\n"
        )
    cs = (
        "using System.Windows;\n"
        "using System.Windows.Controls;\n\n"
        "namespace QuickMediaIngest.Controls\n"
        "{\n"
        f"    public partial class {name} : UserControl\n"
        "    {\n"
        f"        public {name}()\n"
        "        {\n"
        "            InitializeComponent();\n"
        "        }\n\n"
        + "".join(methods)
        + "    }\n"
        "}\n"
    )
    open(os.path.join(folder, f"{name}.xaml.cs"), "w", encoding="utf-8").write(cs)

    host = f'        <controls:{name} x:Name="{name}Control"{grid_attrs} />\n'
    return host


# 1-based inclusive line ranges
extractions = [
    ("DialogOverlaysView", 61, 416, ' Grid.ColumnSpan="2"'),
    ("ImportHistoryOverlayView", 417, 462, ' Grid.ColumnSpan="2"'),
    ("PreferencesOverlayView", 1148, 1347, ""),
    ("ScanExclusionsOverlayView", 1350, 1427, ""),
]

placeholders = []
for name, start, end, attrs in extractions:
    inner = xaml_lines[start - 1 : end]
    ph = write_usercontrol(name, inner, attrs)
    placeholders.append((start, end, ph))
    print(f"Extracted {name}: {start}-{end}")

out = []
idx = 0
line_num = 1
for start, end, ph in sorted(placeholders, key=lambda x: x[0]):
    while line_num < start:
        out.append(xaml_lines[idx])
        idx += 1
        line_num += 1
    out.append(ph)
    idx += end - start + 1
    line_num = end + 1

while idx < len(xaml_lines):
    out.append(xaml_lines[idx])
    idx += 1

xaml_text = "".join(out)
if "xmlns:controls=" not in xaml_text:
    xaml_text = xaml_text.replace(
        'xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"',
        'xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"\n    xmlns:controls="clr-namespace:QuickMediaIngest.Controls"',
        1,
    )

open(xaml_path, "w", encoding="utf-8").write(xaml_text)
print(f"MainWindow.xaml now {len(xaml_text.splitlines())} lines")
