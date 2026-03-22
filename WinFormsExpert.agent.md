# WinFormsExpert.agent.md

# WinForms Development Guidelines

## 🎯 Critical Generic WinForms Issue: Dealing with Two Code Contexts

| Designer Code | .designer.cs, inside InitializeComponent | Serialization-centric (assume C# 2.0 language features) | Simple, predictable, parsable |
| Regular Code | .cs files, event handlers, business logic | Modern C# 11-14 | Use ALL modern features aggressively |

Decision: In .designer.cs or `InitializeComponent` → Designer rules. Otherwise → Modern C# rules.

## 🚨 Designer File Rules (TOP PRIORITY)

### ❌ Prohibited in InitializeComponent
| Control Flow | if, for, foreach, while, goto, switch, try/catch, lock, await, VB: On Error/Resume | Designer cannot parse |
| Operators | ? : (ternary), ??/?./?[] (null coalescing/conditional), nameof() | Not in serialization format |
| Functions | Lambdas, local functions, collection expressions (...=[] or ...=[1,2,3]) | Breaks Designer parser |
| Backing fields | Only add variables with class field scope to ControlCollections, never local variables! | Designer cannot parse |

Allowed method calls: Designer-supporting interface methods like `SuspendLayout`, `ResumeLayout`, `BeginInit`, `EndInit`

### ❌ Prohibited in .designer.cs File
❌ Method definitions (except `InitializeComponent`, `Dispose`, preserve existing additional constructors)
❌ Properties
❌ Lambda expressions, DO ALSO NOT bind events in `InitializeComponent` to Lambdas!
❌ Complex logic
❌ `??`/`?.`/`?[]` (null coalescing/conditional), `nameof()`
❌ Collection Expressions

### ✅ Correct Pattern
✅ File-scope namespace definitions (preferred)

### 📋 Required Structure of InitializeComponent Method
1. Instantiate controls
2. Create components container
3. Suspend layout for container(s)
4. Configure controls
5. Configure Form/UserControl LAST
6. Resume layout(s)
7. Backing fields at EOF

(Try meaningful naming of controls, derive style from existing codebase, if possible.)

Remember: Complex UI configuration logic goes in main .cs file, NOT .designer.cs.

## Modern C# Features (Regular Code Only)

### Style Guidelines
- Using directives: Assume global
- Primitives: int, string, not Int32, String
- Instantiation: Target-typed
- prefer types over var
- Event handlers: Nullable sender
- Events: Nullable
- Trivia: Empty lines before return/code blocks
- this qualifier: Avoid
- Argument validation: Always; throw helpers for .NET 8+
- Using statements: Modern syntax

### Property Patterns (CRITICAL - Common Bug Source!)
- => new Type(): Creates NEW instance EVERY access (likely memory leak)
- { get; } = new(): Creates ONCE at construction (cached/constant)
- => _field ?? Default: Computed/dynamic value

### Prefer Switch Expressions over If-Else Chains
### Prefer Pattern Matching in Event Handlers

## When designing Form/UserControl from scratch

### File Structure
- C#: FormName.cs + FormName.Designer.cs | Form or UserControl
- Main file: Logic and event handlers
- Designer file: Infrastructure, constructors, `Dispose`, `InitializeComponent`, control definitions

### C# Conventions
- File-scoped namespaces
- Assume global using directives
- NRTs OK in main Form/UserControl file; forbidden in code-behind `.designer.cs`
- Event handlers: `object? sender`
- Events: nullable (`EventHandler?`)

## Classic Data Binding and MVVM Data Binding (.NET 8+)

### Data Binding Rules
- Object DataSources: `INotifyPropertyChanged`, `BindingList<T>` required, prefer `ObservableObject` from MVVM CommunityToolkit.
- `ObservableCollection<T>`: Requires `BindingList<T>` adapter.
- One-way-to-source: Unsupported in WinForms DataBinding (workaround: additional dedicated VM property with NO-OP property setter).

### Add Object DataSource to Solution, treat ViewModels also as DataSources
- To make types as DataSource accessible for the Designer, create `.datasource` file in `Properties\DataSources\`.
- Use BindingSource components in Forms/UserControls to bind to the DataSource type as "Mediator" instance between View and ViewModel.

### New MVVM Command Binding APIs in .NET 8+
- Control.DataContext: Ambient property for MVVM
- ButtonBase.Command: ICommand binding
- ToolStripItem.Command: ICommand binding
- *.CommandParameter: Auto-passed to command

### MVVM Pattern in WinForms (.NET 8+)
- If asked to create or refactor a WinForms project to MVVM, identify (if already exists) or create a dedicated class library for ViewModels based on the MVVM CommunityToolkit
- Reference MVVM ViewModel class library from the WinForms project
- Import ViewModels via Object DataSources as described above
- Use new `Control.DataContext` for passing ViewModel as data sources down the control hierarchy for nested Form/UserControl scenarios
- Use `Button[Base].Command` or `ToolStripItem.Command` for MVVM command bindings. Use the CommandParameter property for passing parameters.
- Use the `Parse` and `Format` events of `Binding` objects for custom data conversions (`IValueConverter` workaround), if necessary.

## WinForms Async Patterns (.NET 9+)

### Control.InvokeAsync Overload Selection
- Sync action, no return: InvokeAsync(Action)
- Async operation, no return: InvokeAsync(Func<CT, ValueTask>)
- Sync function, returns T: InvokeAsync<T>(Func<T>)
- Async operation, returns T: InvokeAsync<T>(Func<CT, ValueTask<T>>)

### Fire-and-Forget Trap
- Avoid fire-and-forget; always await async operations

### Form Async Methods (.NET 9+)
- ShowAsync(): Completes when form closes
- ShowDialogAsync(): Modal with dedicated message queue

### CRITICAL: Async EventHandler Pattern
- All async event handlers should nest await calls in try/catch

## Exception Handling in WinForms

### Application-Level Exception Handling
- AppDomain.CurrentDomain.UnhandledException: Catches exceptions from any thread in the AppDomain
- Application.ThreadException: Catches exceptions on the UI thread only

### Exception Dispatch in Async/Await Context
- Use ExceptionDispatchInfo.Capture(ex).Throw() to preserve stack traces

## CRITICAL: Manage CodeDOM Serialization
- Use [DefaultValue], [DesignerSerializationVisibility.Hidden], ShouldSerialize*/Reset* for property serialization

## WinForms Design Principles

### Core Rules
- Use adequate margins/padding; prefer TableLayoutPanel (TLP)/FlowLayoutPanel (FLP) over absolute positioning
- Layout cell-sizing: Rows: AutoSize > Percent > Absolute; Columns: AutoSize > Percent > Absolute
- For new Forms/UserControls: Assume 96 DPI/100% for AutoScaleMode and scaling
- Be DarkMode-aware in .NET 9+: Application.IsDarkModeEnabled

### Layout Strategy
- Use multiple or nested TLPs for logical sections
- Main form uses either SplitContainer or an "outer" TLP
- Each UI-section gets its own nested TLP or UserControl
- Individual TLPs should be 2-4 columns max
- Use GroupBoxes with nested TLPs for grouping
- RadioButtons: single-column, auto-size-cells TLP inside AutoGrow/AutoSize GroupBox
- Large content area scrolling: Use nested panel controls with AutoScroll

### Common Layout Patterns
- Single-line TextBox: 2-column TLP (Label, TextBox)
- Multi-line TextBox: 2-column TLP or 1-column TLP with label above

### Container Sizing (CRITICAL - Prevents Clipping)
- GroupBox/Panel inside TLP cells: AutoSize = true, AutoSizeMode = GrowOnly, Dock = Fill

### Modal Dialog Button Placement
- Pattern A: Bottom-right buttons (OK/Cancel) in FlowLayoutPanel, FlowDirection = RightToLeft
- Pattern B: Top-right stacked buttons (wizards/browsers) in FlowLayoutPanel, FlowDirection = TopDown

### Complex Layouts
- For complex layouts, use dedicated UserControls for logical sections

### Modal Dialogs
- Dialog buttons: AcceptButton (OK), CancelButton (Cancel)
- Validation: Perform on Form, not on Field scope
- Use DataContext property (.NET 8+) of Form to pass and return modal data objects

### Layout Recipes
- MainForm: MenuStrip, optional ToolStrip, content area, StatusStrip
- Simple Entry Form: Data entry fields on left, buttons on right
- Tabs: Only for distinct tasks

### Accessibility
- Set AccessibleName and AccessibleDescription on actionable controls
- Maintain logical control tab order via TabIndex
- Verify keyboard-only navigation, mnemonics, and screen reader compatibility

### TreeView and ListView
- TreeView: Must have visible, default-expanded root node
- ListView: Prefer for small lists with fewer columns
- Content setup: Generate in code, NOT in designer code-behind
- ListView columns: Set to -1 (size to longest content) or -2 (size to header name) after populating
- SplitContainer: Use for resizable panes

### DataGridView
- Prefer derived class with double buffering enabled
- Configure colors for DarkMode
- Large data: page/virtualize

### Resources and Localization
- String literal constants for UI display in resource files
- Account for localized string lengths
- Prefer font-based icons (Segoe UI Symbol)

## Critical Reminders
1. InitializeComponent code serves as serialization format - more like XML, not C#
2. Two contexts, two rule sets - designer code-behind vs regular code
3. Validate form/control names before generating code
4. Stick to coding style rules for InitializeComponent
5. Designer files never use NRT annotations
6. Modern C# features for regular code ONLY
7. Data binding: Treat ViewModels as DataSources, remember Command and CommandParameter properties

## Additional Links
- https://github.com/github/awesome-copilot
- https://github.com/github/awesome-copilot/issues
- https://github.com/github/awesome-copilot/pulls
- https://github.com/github/awesome-copilot/discussions
- https://github.com/github/awesome-copilot/actions
- https://github.com/github/awesome-copilot/models
- https://github.com/github/awesome-copilot/tree/main
- https://github.com/github/awesome-copilot/tree/main/agents
