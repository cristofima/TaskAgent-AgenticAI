<#
.SYNOPSIS
    Analyzes Git commits and suggests semantic version for release notes generation.

.DESCRIPTION
    This script analyzes Git commits since the last tag (or all commits if no tags exist),
    classifies them by type (feat, fix, refactor, etc.), and suggests a semantic version (X.Y.Z).
    The output is formatted for use with the GitHub Copilot prompt file for release notes.

.PARAMETER FromTag
    Start analyzing from this tag. If not specified, uses the latest tag or all commits if no tags exist.

.PARAMETER ToRef
    Analyze commits up to this reference (default: HEAD).

.PARAMETER CurrentVersion
    Current version (e.g., "1.2.3"). If not specified, will be detected from latest tag or default to "0.0.0".

.EXAMPLE
    .\Analyze-Commits.ps1
    Analyzes commits and displays formatted output for Copilot

.EXAMPLE
    .\Analyze-Commits.ps1 -CurrentVersion "1.0.0"
    Analyzes commits with explicit current version

.EXAMPLE
    .\Analyze-Commits.ps1 -FromTag "v1.0.0" -ToRef "HEAD"
    Analyzes commits between specific tag and HEAD
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$FromTag,

    [Parameter()]
    [string]$ToRef = "HEAD",

    [Parameter()]
    [string]$CurrentVersion
)

# Script configuration
$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptPath "config.json"

#region Helper Functions

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Get-Configuration {
    if (-not (Test-Path $configPath)) {
        Write-ColorOutput "⚠️  Configuration file not found. Using default settings." "Yellow"
        
        return @{
            SemanticVersion = @{
                DefaultVersion = "1.0.0"
                CommitTypes = @{
                    Major = @("breaking", "BREAKING CHANGE")
                    Minor = @("feat", "feature")
                    Patch = @("fix", "refactor", "perf", "security", "docs")
                }
            }
        }
    }
    
    return Get-Content $configPath -Raw | ConvertFrom-Json
}

function Get-LatestTag {
    $tags = git tag -l --sort=-v:refname
    if ($tags) {
        return $tags[0]
    }
    return $null
}

function Get-CurrentVersionFromTag {
    param([string]$Tag)
    
    if ([string]::IsNullOrEmpty($Tag)) {
        return "0.0.0"
    }
    
    # Remove 'v' prefix if exists
    $version = $Tag -replace '^v', ''
    
    # Validate semantic version format
    if ($version -match '^\d+\.\d+\.\d+') {
        return $version
    }
    
    return "0.0.0"
}

function Get-GitCommits {
    param(
        [string]$From,
        [string]$To
    )
    
    $range = if ([string]::IsNullOrEmpty($From)) {
        $To
    } else {
        "$From..$To"
    }
    
    Write-ColorOutput "📋 Analyzing commits in range: $range" "Cyan"
    
    # Get commit hashes first
    $hashes = git log $range --pretty=format:"%H"
    
    if (-not $hashes) {
        Write-ColorOutput "⚠️  No commits found in range: $range" "Yellow"
        return @()
    }
    
    $commits = @()
    
    # Process each commit individually
    foreach ($hash in $hashes) {
        if ([string]::IsNullOrWhiteSpace($hash)) {
            continue
        }
        
        # Get commit details
        $subject = git log -1 $hash --pretty=format:"%s"
        $body = git log -1 $hash --pretty=format:"%b"
        $author = git log -1 $hash --pretty=format:"%an"
        $email = git log -1 $hash --pretty=format:"%ae"
        $date = git log -1 $hash --pretty=format:"%ad" --date=short
        
        # Validate required fields
        if ([string]::IsNullOrWhiteSpace($subject) -or [string]::IsNullOrWhiteSpace($author)) {
            Write-ColorOutput "  ⚠️  Skipping commit $hash - missing subject or author" "Yellow"
            continue
        }
        
        $commit = @{
            Hash = $hash
            Subject = $subject
            Body = if ($body) { $body.Trim() } else { "" }
            Author = $author
            Email = if ($email) { $email } else { "" }
            Date = if ($date) { $date } else { "" }
            Type = "other"
            Scope = ""
            Breaking = $false
        }
        
        # Parse conventional commit format: type(scope): message
        if ($subject -match '^(\w+)(\(([^)]+)\))?:\s*(.+)$') {
            $commit.Type = $Matches[1].ToLower()
            $commit.Scope = if ($Matches[3]) { $Matches[3] } else { "" }
            $commit.Message = $Matches[4].Trim()
        } else {
            $commit.Message = $subject
        }
        
        # Check for breaking changes
        $bodyText = if ($commit.Body) { $commit.Body } else { "" }
        if ($bodyText -match 'BREAKING CHANGE' -or $subject -match '!:') {
            $commit.Breaking = $true
        }
        
        $commits += $commit
    }
    
    return $commits
}

function Get-SuggestedVersion {
    param(
        [array]$Commits,
        [string]$BaseVersion,
        [object]$Config
    )
    
    $version = [version]$BaseVersion
    $major = $version.Major
    $minor = $version.Minor
    $patch = $version.Build
    
    $hasBreaking = $false
    $hasFeature = $false
    $hasPatch = $false
    
    foreach ($commit in $Commits) {
        # Check for breaking changes
        if ($commit.Breaking) {
            $hasBreaking = $true
            continue
        }
        
        # Check commit type
        $type = $commit.Type
        
        if ($Config.SemanticVersion.CommitTypes.Major -contains $type) {
            $hasBreaking = $true
        }
        elseif ($Config.SemanticVersion.CommitTypes.Minor -contains $type) {
            $hasFeature = $true
        }
        elseif ($Config.SemanticVersion.CommitTypes.Patch -contains $type) {
            $hasPatch = $true
        }
    }
    
    # Determine version bump
    if ($hasBreaking) {
        $major++
        $minor = 0
        $patch = 0
        $bumpType = "MAJOR"
    }
    elseif ($hasFeature) {
        $minor++
        $patch = 0
        $bumpType = "MINOR"
    }
    elseif ($hasPatch) {
        $patch++
        $bumpType = "PATCH"
    }
    else {
        $patch++
        $bumpType = "PATCH"
    }
    
    return @{
        Version = "$major.$minor.$patch"
        BumpType = $bumpType
        HasBreaking = $hasBreaking
        HasFeature = $hasFeature
        HasPatch = $hasPatch
    }
}

function Get-CommitStatistics {
    param([array]$Commits)
    
    $stats = @{
        Total = $commits.Count
        ByType = @{}
        Breaking = 0
        Authors = @{}
    }
    
    foreach ($commit in $Commits) {
        # Count by type
        if (-not $stats.ByType.ContainsKey($commit.Type)) {
            $stats.ByType[$commit.Type] = 0
        }
        $stats.ByType[$commit.Type]++
        
        # Count breaking changes
        if ($commit.Breaking) {
            $stats.Breaking++
        }
        
        # Count by author
        if (-not $stats.Authors.ContainsKey($commit.Author)) {
            $stats.Authors[$commit.Author] = 0
        }
        $stats.Authors[$commit.Author]++
    }
    
    return $stats
}

function Format-CommitsForCopilot {
    param(
        [array]$Commits,
        [object]$VersionInfo,
        [object]$Stats,
        [string]$CurrentVersion
    )
    
    # Group commits by type
    $groupedCommits = @{
        feat = @()
        fix = @()
        refactor = @()
        docs = @()
        perf = @()
        test = @()
        chore = @()
        style = @()
        ci = @()
        build = @()
        breaking = @()
        other = @()
    }
    
    foreach ($commit in $Commits) {
        $commitInfo = @{
            Hash = $commit.Hash.Substring(0, 7)
            Message = $commit.Message
            Body = $commit.Body
            Author = $commit.Author
            Date = $commit.Date
        }
        
        if ($commit.Breaking) {
            $groupedCommits.breaking += $commitInfo
        }
        
        $type = $commit.Type
        if ($groupedCommits.ContainsKey($type)) {
            $groupedCommits[$type] += $commitInfo
        } else {
            $groupedCommits.other += $commitInfo
        }
    }
    
    # Build formatted output
    $output = @"
==========================================================
RELEASE NOTES INPUT DATA
==========================================================

COPY THE SECTION BELOW AND USE IT WITH THE COPILOT PROMPT:
/generate-release-notes

----------------------------------------------------------
Current Version: $CurrentVersion
Suggested Version: $($VersionInfo.Version)
Version Bump: $($VersionInfo.BumpType)
Release Date: $(Get-Date -Format "MMMM dd, yyyy")
Total Commits: $($Stats.Total)
Contributors: $($Stats.Authors.Count)

"@

    # Add commit breakdown
    $output += "`nCommits by Type:`n"
    foreach ($type in $Stats.ByType.GetEnumerator() | Sort-Object Value -Descending) {
        $output += "  - $($type.Key): $($type.Value)`n"
    }
    
    # Add breaking changes section if any
    if ($groupedCommits.breaking.Count -gt 0) {
        $output += "`n⚠️  BREAKING CHANGES ($($groupedCommits.breaking.Count)):`n"
        foreach ($commit in $groupedCommits.breaking) {
            $output += "  - [$($commit.Hash)] $($commit.Message) by $($commit.Author)`n"
            if ($commit.Body) {
                # Format body with proper indentation, preserving line breaks
                $bodyLines = $commit.Body -split "`n"
                foreach ($line in $bodyLines) {
                    $trimmedLine = $line.Trim()
                    if ($trimmedLine) {
                        $output += "    $trimmedLine`n"
                    }
                }
            }
        }
    }
    
    # Add features
    if ($groupedCommits.feat.Count -gt 0) {
        $output += "`n✨ New Features ($($groupedCommits.feat.Count)):`n"
        foreach ($commit in $groupedCommits.feat) {
            $output += "  - [$($commit.Hash)] $($commit.Message)`n"
            if ($commit.Body) {
                # Format body with proper indentation, preserving line breaks
                $bodyLines = $commit.Body -split "`n"
                foreach ($line in $bodyLines) {
                    $trimmedLine = $line.Trim()
                    if ($trimmedLine) {
                        $output += "    $trimmedLine`n"
                    }
                }
            }
        }
    }
    
    # Add bug fixes
    if ($groupedCommits.fix.Count -gt 0) {
        $output += "`n🐛 Bug Fixes ($($groupedCommits.fix.Count)):`n"
        foreach ($commit in $groupedCommits.fix) {
            $output += "  - [$($commit.Hash)] $($commit.Message)`n"
            if ($commit.Body) {
                # Format body with proper indentation, preserving line breaks
                $bodyLines = $commit.Body -split "`n"
                foreach ($line in $bodyLines) {
                    $trimmedLine = $line.Trim()
                    if ($trimmedLine) {
                        $output += "    $trimmedLine`n"
                    }
                }
            }
        }
    }
    
    # Add improvements
    if ($groupedCommits.refactor.Count -gt 0 -or $groupedCommits.perf.Count -gt 0) {
        $improvements = $groupedCommits.refactor + $groupedCommits.perf
        $output += "`n🔧 Improvements & Performance ($($improvements.Count)):`n"
        foreach ($commit in $improvements) {
            $output += "  - [$($commit.Hash)] $($commit.Message)`n"
            if ($commit.Body) {
                # Format body with proper indentation, preserving line breaks
                $bodyLines = $commit.Body -split "`n"
                foreach ($line in $bodyLines) {
                    $trimmedLine = $line.Trim()
                    if ($trimmedLine) {
                        $output += "    $trimmedLine`n"
                    }
                }
            }
        }
    }
    
    # Add documentation
    if ($groupedCommits.docs.Count -gt 0) {
        $output += "`n📚 Documentation ($($groupedCommits.docs.Count)):`n"
        foreach ($commit in $groupedCommits.docs) {
            $output += "  - [$($commit.Hash)] $($commit.Message)`n"
            if ($commit.Body) {
                # Format body with proper indentation, preserving line breaks
                $bodyLines = $commit.Body -split "`n"
                foreach ($line in $bodyLines) {
                    $trimmedLine = $line.Trim()
                    if ($trimmedLine) {
                        $output += "    $trimmedLine`n"
                    }
                }
            }
        }
    }
    
    # Add other changes
    $otherTypes = @('chore', 'style', 'ci', 'build', 'test', 'other')
    $otherCommits = @()
    foreach ($type in $otherTypes) {
        $otherCommits += $groupedCommits[$type]
    }
    
    if ($otherCommits.Count -gt 0) {
        $output += "`n🔨 Other Changes ($($otherCommits.Count)):`n"
        foreach ($commit in $otherCommits) {
            $output += "  - [$($commit.Hash)] $($commit.Message)`n"
            if ($commit.Body) {
                # Format body with proper indentation, preserving line breaks
                $bodyLines = $commit.Body -split "`n"
                foreach ($line in $bodyLines) {
                    $trimmedLine = $line.Trim()
                    if ($trimmedLine) {
                        $output += "    $trimmedLine`n"
                    }
                }
            }
        }
    }
    
    # Add contributors
    $contributors = $Commits | Select-Object -ExpandProperty Author -Unique | Sort-Object
    $output += "`n👥 Contributors ($($contributors.Count)):`n"
    foreach ($contributor in $contributors) {
        $output += "  - $contributor`n"
    }
    
    $output += @"

----------------------------------------------------------
NEXT STEPS:
1. Copy the data above
2. Open GitHub Copilot Chat (Ctrl+Shift+I)
3. Type: /generate-release-notes
4. Paste this data when prompted
5. Review and save as RELEASE_NOTES_v$($VersionInfo.Version).md
==========================================================

"@
    
    return $output
}

#endregion

#region Main Script

Write-ColorOutput "`n🚀 Release Notes Commit Analyzer" "Cyan"
Write-ColorOutput "=================================`n" "Cyan"

# Load configuration
$config = Get-Configuration

# Get current version
$isFirstRelease = $false
if ([string]::IsNullOrEmpty($CurrentVersion)) {
    if ([string]::IsNullOrEmpty($FromTag)) {
        $latestTag = Get-LatestTag
        if ($latestTag) {
            $CurrentVersion = Get-CurrentVersionFromTag $latestTag
            $FromTag = $latestTag
            Write-ColorOutput "📌 Detected latest tag: $latestTag (version: $CurrentVersion)" "Green"
        } else {
            $CurrentVersion = $config.SemanticVersion.DefaultVersion
            $isFirstRelease = $true
            Write-ColorOutput "📌 No tags found. This will be the first release (version: $CurrentVersion)" "Yellow"
        }
    } else {
        $CurrentVersion = Get-CurrentVersionFromTag $FromTag
    }
} else {
    Write-ColorOutput "📌 Using provided version: $CurrentVersion" "Green"
}

# Get commits
$commits = Get-GitCommits -From $FromTag -To $ToRef

if ($commits.Count -eq 0) {
    Write-ColorOutput "❌ No commits found. Cannot generate release notes." "Red"
    exit 1
}

Write-ColorOutput "✅ Found $($commits.Count) commits" "Green"

# Calculate statistics
$stats = Get-CommitStatistics -Commits $commits

# Suggest new version
# For first release, use the default version directly without incrementing
if ($isFirstRelease) {
    $versionInfo = @{
        Version = $CurrentVersion
        BumpType = "FIRST"
        HasBreaking = $false
        HasFeature = $false
        HasPatch = $false
    }
} else {
    $versionInfo = Get-SuggestedVersion -Commits $commits -BaseVersion $CurrentVersion -Config $config
}

Write-ColorOutput "`n📊 Quick Summary:" "Cyan"
Write-ColorOutput "  Total commits: $($stats.Total)" "White"
foreach ($type in $stats.ByType.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 5) {
    Write-ColorOutput "  - $($type.Key): $($type.Value)" "Gray"
}
if ($stats.Breaking -gt 0) {
    Write-ColorOutput "  ⚠️  Breaking changes: $($stats.Breaking)" "Yellow"
}

Write-ColorOutput "`n📦 Version Suggestion:" "Cyan"
if ($isFirstRelease) {
    Write-ColorOutput "  Suggested: $($versionInfo.Version) (FIRST release)" "Green"
} else {
    Write-ColorOutput "  Current: $CurrentVersion" "White"
    Write-ColorOutput "  Suggested: $($versionInfo.Version) ($($versionInfo.BumpType) bump)" "Green"
}

# Format output for Copilot
$formattedOutput = Format-CommitsForCopilot -Commits $commits -VersionInfo $versionInfo -Stats $stats -CurrentVersion $CurrentVersion

# Display formatted output
Write-Host "`n"
Write-Host $formattedOutput

# Copy to clipboard if possible
try {
    # Extract just the data section for clipboard
    $clipboardData = $formattedOutput -replace '(?s)^.*?COPY THE SECTION BELOW.*?----------------------------------------------------------\r?\n', '' -replace '(?s)----------------------------------------------------------.*$', ''
    Set-Clipboard -Value $clipboardData.Trim()
    Write-ColorOutput "✅ Commit data copied to clipboard!" "Green"
    Write-ColorOutput "   Paste it in Copilot Chat after typing: /generate-release-notes`n" "Gray"
} catch {
    Write-ColorOutput "⚠️  Could not copy to clipboard automatically" "Yellow"
    Write-ColorOutput "   Copy the data manually from above`n" "Gray"
}

#endregion
