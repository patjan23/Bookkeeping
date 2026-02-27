#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Smoke-test the Bookkeeping WPF app using WPF UI Automation (UIA).
    Requires: Windows, .NET 9, app already running OR starts it automatically.

.DESCRIPTION
    1. Launches Bookkeeping.App.exe (or attaches if already running)
    2. Navigates to Transactions
    3. Clicks "Add Transaction"
    4. Fills in the form fields
    5. Saves and verifies the new row appears in the DataGrid

    Uses Windows UIAutomation COM APIs directly (no third-party tool needed).
    All AutomationId values match the AutomationProperties.AutomationId set in XAML.
#>

param(
    [string]$AppExe = "$PSScriptRoot\..\Bookkeeping.App\bin\Debug\net9.0-windows\Bookkeeping.App.exe",
    [switch]$BuildFirst
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── 0. Build if requested ───────────────────────────────────────────────────
if ($BuildFirst) {
    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build "$PSScriptRoot\..\Bookkeeping.sln" -c Debug --nologo -q
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

# ── 1. Load UIAutomation assemblies ─────────────────────────────────────────
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$UIA    = [System.Windows.Automation.AutomationElement]
$Cond   = [System.Windows.Automation.Condition]
$PropId = [System.Windows.Automation.AutomationElement]

function Get-Element {
    param($Root, [string]$AutomationId, [int]$TimeoutSec = 10)
    $cond    = New-Object System.Windows.Automation.PropertyCondition(
                   $UIA::AutomationIdProperty, $AutomationId)
    $deadline = [DateTime]::Now.AddSeconds($TimeoutSec)
    while ([DateTime]::Now -lt $deadline) {
        $el = $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 200
    }
    throw "Element '$AutomationId' not found within ${TimeoutSec}s"
}

function Click-Element {
    param($Element)
    $inv = $Element.GetCurrentPattern(
               [System.Windows.Automation.InvokePattern]::Pattern)
    $inv.Invoke()
    Start-Sleep -Milliseconds 300
}

function Set-TextValue {
    param($Element, [string]$Value)
    $vp = $Element.GetCurrentPattern(
              [System.Windows.Automation.ValuePattern]::Pattern)
    $vp.SetValue($Value)
}

function Select-ComboValue {
    param($Element, [string]$ItemName)
    $ep = $Element.GetCurrentPattern(
              [System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $ep.Expand()
    Start-Sleep -Milliseconds 200

    $cond = New-Object System.Windows.Automation.PropertyCondition(
                $UIA::NameProperty, $ItemName)
    $item = $Element.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $item) { throw "ComboBox item '$ItemName' not found" }
    $sel = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $sel.Select()
    Start-Sleep -Milliseconds 200
}

# ── 2. Launch or attach ──────────────────────────────────────────────────────
$proc = Get-Process -Name "Bookkeeping.App" -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "Launching app: $AppExe" -ForegroundColor Cyan
    if (-not (Test-Path $AppExe)) {
        throw "App not found at $AppExe — run with -BuildFirst or build manually first."
    }
    $proc = Start-Process -FilePath $AppExe -PassThru
    Start-Sleep -Seconds 3
}
Write-Host "Attached to process $($proc.Id)" -ForegroundColor Green

# ── 3. Find main window ──────────────────────────────────────────────────────
$desktop = $UIA::RootElement
$winCond = New-Object System.Windows.Automation.PropertyCondition(
               $UIA::NameProperty, "Bookkeeping")
$window  = $null
$deadline = [DateTime]::Now.AddSeconds(15)
while ([DateTime]::Now -lt $deadline) {
    $window = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Children, $winCond)
    if ($window) { break }
    Start-Sleep -Milliseconds 300
}
if (-not $window) { throw "Main window not found" }
Write-Host "Found main window" -ForegroundColor Green

# ── 4. Navigate to Transactions ──────────────────────────────────────────────
Write-Host "Navigating to Transactions..." -ForegroundColor Cyan
$navBtn = Get-Element $window "Nav_Transactions"
Click-Element $navBtn
Start-Sleep -Milliseconds 500

# ── 5. Click Add Transaction ─────────────────────────────────────────────────
Write-Host "Opening Add Transaction dialog..." -ForegroundColor Cyan
$addBtn = Get-Element $window "Btn_AddTransaction"
Click-Element $addBtn
Start-Sleep -Milliseconds 800

# ── 6. Find dialog ────────────────────────────────────────────────────────────
$dialogCond = New-Object System.Windows.Automation.PropertyCondition(
                  $UIA::NameProperty, "AddEditTransactionDialog")
$dialog = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Children, $dialogCond)
if (-not $dialog) {
    # Also search descendants of desktop
    $dialog = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Subtree, $dialogCond)
}
if (-not $dialog) { throw "Add/Edit Transaction dialog did not appear" }
Write-Host "Dialog found" -ForegroundColor Green

# ── 7. Fill in form ──────────────────────────────────────────────────────────
Write-Host "Filling form..." -ForegroundColor Cyan

$amountField = Get-Element $dialog "Field_Amount"
Set-TextValue $amountField "250"

$descField = Get-Element $dialog "Field_Description"
Set-TextValue $descField "Smoke test grocery run"

# Type: Expense (default — skip if already set)
# Category and Account: pick first available item (just expand & pick)
$catCombo = Get-Element $dialog "Field_Category"
$ep = $catCombo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
$ep.Expand()
Start-Sleep -Milliseconds 300
$firstCat = $catCombo.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    New-Object System.Windows.Automation.PropertyCondition(
        $UIA::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem))
if ($firstCat) {
    $sel = $firstCat.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $sel.Select()
}

Start-Sleep -Milliseconds 200
$accCombo = Get-Element $dialog "Field_Account"
$ep2 = $accCombo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
$ep2.Expand()
Start-Sleep -Milliseconds 300
$firstAcc = $accCombo.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    New-Object System.Windows.Automation.PropertyCondition(
        $UIA::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem))
if ($firstAcc) {
    $sel2 = $firstAcc.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $sel2.Select()
}

# ── 8. Save ───────────────────────────────────────────────────────────────────
Write-Host "Saving transaction..." -ForegroundColor Cyan
$saveBtn = Get-Element $dialog "Btn_Save"
Click-Element $saveBtn
Start-Sleep -Milliseconds 1000

# ── 9. Verify row appears ─────────────────────────────────────────────────────
Write-Host "Verifying transaction in grid..." -ForegroundColor Cyan
$grid = Get-Element $window "TransactionsGrid"
$descCond = New-Object System.Windows.Automation.PropertyCondition(
                $UIA::NameProperty, "Smoke test grocery run")
$newRow = $grid.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $descCond)

if ($newRow) {
    Write-Host "PASS: Transaction 'Smoke test grocery run' found in grid." -ForegroundColor Green
} else {
    Write-Host "FAIL: Transaction not found in grid." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Smoke flow completed successfully." -ForegroundColor Green
