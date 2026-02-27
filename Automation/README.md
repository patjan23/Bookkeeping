# Automation — WPF-MCP + UIAutomation Smoke Flow

## Overview

Two automation approaches are provided:

| Approach | File | When to use |
|---|---|---|
| Native WPF UIAutomation (no deps) | `smoke-flow.ps1` | Local Windows machine, no server needed |
| WPF-MCP server via Claude Code | `mcp-config.json` | AI-driven UI testing via MCP protocol |

---

## A) Native UIAutomation smoke flow

### Prerequisites
- Windows 10/11
- .NET 9 SDK installed
- App built: `dotnet build ..\Bookkeeping.sln -c Debug`

### Run
```powershell
# With build step
.\smoke-flow.ps1 -BuildFirst

# App already running
.\smoke-flow.ps1
```

### What it does
1. Launches `Bookkeeping.App.exe` (or attaches)
2. Clicks **Transactions** in the sidebar
3. Clicks **+ Add Transaction**
4. Fills: Amount=250, Description="Smoke test grocery run", picks first Category + Account
5. Clicks **Save**
6. Searches the DataGrid for the new description
7. Reports PASS or FAIL

All element IDs are set via `AutomationProperties.AutomationId` in XAML:

| AutomationId | Control |
|---|---|
| `Nav_Transactions` | Sidebar Transactions button |
| `Btn_AddTransaction` | Add Transaction button |
| `Field_Amount` | Amount TextBox in dialog |
| `Field_Description` | Description TextBox |
| `Field_Category` | Category ComboBox |
| `Field_Account` | Account ComboBox |
| `Btn_Save` | Save button in dialog |
| `TransactionsGrid` | DataGrid on Transactions page |

---

## B) WPF-MCP server (AI-driven)

### Setup

1. Install Node.js (LTS)
2. Copy `mcp-config.json` to `%APPDATA%\Claude\claude_desktop_config.json`
   (or add the `wpf-mcp` entry to an existing config)
3. Start the Bookkeeping app:
   ```powershell
   dotnet run --project ..\Bookkeeping.App
   ```
4. Open Claude Desktop (or Claude Code CLI)

### Example Claude Code prompt

Once Claude Code has the `wpf-mcp` MCP server connected, paste:

```
Using wpf-mcp:
1. Find the window named "Bookkeeping"
2. Click element with AutomationId "Nav_Transactions"
3. Click "Btn_AddTransaction"
4. In the dialog, set "Field_Amount" to "99.99"
5. Set "Field_Description" to "MCP smoke test"
6. Expand "Field_Category" and select the first item
7. Expand "Field_Account" and select the first item
8. Click "Btn_Save"
9. Verify the DataGrid "TransactionsGrid" contains text "MCP smoke test"
10. Report PASS or FAIL
```

### MCP server info

The `@lobehub/wpf-mcp-server` package exposes tools for:
- `wpf_find_window` — attach to a WPF window by title/process
- `wpf_find_element` — find element by AutomationId, Name, or ControlType
- `wpf_click` — invoke click on an element
- `wpf_set_value` — set a text value
- `wpf_get_text` — read element text
- `wpf_screenshot` — capture current state

See the package README for full tool reference.
