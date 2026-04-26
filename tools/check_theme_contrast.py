#!/usr/bin/env python3
"""
check_theme_contrast.py
Scan XAML theme files for color definitions and report WCAG contrast ratios.
Usage: python tools/check_theme_contrast.py [--dir QuickMediaIngest/Themes]
"""
import re
import os
import argparse
from math import pow

HEX_RE = re.compile(r"#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})")
COLOR_DEF_RE = re.compile(r"<Color[^>]*x:Key=\"([^\"]+)\"[^>]*>\s*(#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8}))\s*</Color>")
BRUSH_RE = re.compile(r"Color=\"(#[0-9A-Fa-f]{6,8})\"")


def hex_to_rgb(hexstr):
    h = hexstr.lstrip('#')
    if len(h) == 8:  # ARGB or RRGGBBAA? assume RRGGBBAA or AARRGGBB uncertain; handle both by taking last 6
        # prefer last 6 characters (RGB)
        h = h[-6:]
    r = int(h[0:2], 16)
    g = int(h[2:4], 16)
    b = int(h[4:6], 16)
    return r, g, b


def srgb_to_linear(c):
    c = c / 255.0
    if c <= 0.03928:
        return c / 12.92
    return pow((c + 0.055) / 1.055, 2.4)


def relative_luminance(rgb):
    r, g, b = rgb
    r_l = srgb_to_linear(r)
    g_l = srgb_to_linear(g)
    b_l = srgb_to_linear(b)
    return 0.2126 * r_l + 0.7152 * g_l + 0.0722 * b_l


def contrast_ratio(color1, color2):
    l1 = relative_luminance(color1)
    l2 = relative_luminance(color2)
    lighter = max(l1, l2)
    darker = min(l1, l2)
    return (lighter + 0.05) / (darker + 0.05)


def scan_xaml_for_colors(path):
    text = open(path, 'r', encoding='utf-8').read()
    colors = {}
    for m in COLOR_DEF_RE.finditer(text):
        key = m.group(1)
        hexc = m.group(2)
        colors[key] = hexc
    # find SolidColorBrush and other Color attributes
    for m in re.finditer(r"x:Key=\"([^\"]+)\"[\s\S]{0,120}?Color=\"(#[0-9A-Fa-f]{6,8})\"", text):
        key = m.group(1)
        colors.setdefault(key, m.group(2))
    # standalone brushes
    for m in re.finditer(r"<SolidColorBrush[^>]*x:Key=\"([^\"]+)\"[^>]*Color=\"(#[0-9A-Fa-f]{6,8})\"", text):
        key = m.group(1)
        colors.setdefault(key, m.group(2))
    return colors


def find_theme_files(base_dir):
    candidates = []
    for root, dirs, files in os.walk(base_dir):
        for f in files:
            if f.lower().endswith('.xaml'):
                candidates.append(os.path.join(root, f))
    return candidates


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--dir', default='QuickMediaIngest', help='Root folder to scan (default: QuickMediaIngest)')
    args = p.parse_args()

    root = args.dir
    if not os.path.isdir(root):
        print('Directory not found:', root)
        return 2

    files = find_theme_files(root)
    theme_colors = {}
    for f in files:
        rel = os.path.relpath(f)
        try:
            cs = scan_xaml_for_colors(f)
            if cs:
                theme_colors[rel] = cs
        except Exception as e:
            print('Error reading', rel, e)

    # Evaluate contrast for common pairs
    failures = []
    print('\nFound theme files with color definitions:')
    for fn, cs in sorted(theme_colors.items()):
        print(' -', fn)
    print('\nChecking typical text/background pairs (TextPrimary vs Background, TextSecondary vs Background)')

    for fn, cs in sorted(theme_colors.items()):
        bg_keys = [k for k in cs.keys() if 'background' in k.lower() or 'Background' in k]
        text_primary_keys = [k for k in cs.keys() if 'textprimary' in k.lower() or 'TextPrimary' in k]
        text_secondary_keys = [k for k in cs.keys() if 'textsecondary' in k.lower() or 'TextSecondary' in k]
        # fallback: Theme.Background and Theme.TextPrimary
        if not bg_keys and ('Theme.Background' in cs):
            bg_keys = ['Theme.Background']
        for bgk in bg_keys:
            try:
                bg = hex_to_rgb(cs[bgk])
            except Exception:
                continue
            for tk in (text_primary_keys or ['Theme.TextPrimary']):
                if tk not in cs:
                    continue
                try:
                    txt = hex_to_rgb(cs[tk])
                except Exception:
                    continue
                ratio = contrast_ratio(txt, bg)
                ok = ratio >= 4.5
                status = 'PASS' if ok else 'FAIL'
                print(f"{fn}: {tk} ({cs[tk]}) vs {bgk} ({cs[bgk]}): {ratio:.2f} -> {status}")
                if not ok:
                    failures.append((fn, tk, bgk, ratio))
            for tk in text_secondary_keys:
                if tk not in cs:
                    continue
                try:
                    txt = hex_to_rgb(cs[tk])
                except Exception:
                    continue
                ratio = contrast_ratio(txt, bg)
                ok = ratio >= 4.5
                status = 'PASS' if ok else 'FAIL'
                print(f"{fn}: {tk} ({cs[tk]}) vs {bgk} ({cs[bgk]}): {ratio:.2f} -> {status}")
                if not ok:
                    failures.append((fn, tk, bgk, ratio))

    if failures:
        print('\nSummary: FAILURES detected for some text/background pairs.')
        return 1
    print('\nSummary: All checked pairs meet WCAG 2.0 AA contrast >= 4.5.')
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
