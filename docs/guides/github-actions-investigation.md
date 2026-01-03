# GitHub Actions Workflow Investigation Guide

This guide documents the tested process for investigating GitHub Actions workflow failures using the available MCP tools.

## Overview

When investigating CI/workflow failures, you have access to GitHub MCP server tools that allow you to:
- List workflow runs with filters
- Get workflow run details
- List jobs for specific runs
- Download and analyze job logs

## Important: Response Size Management

**CRITICAL:** Always use narrow filters when calling `list_workflow_runs` to prevent large responses from consuming context window.

### Recommended Filter Pattern

```json
{
  "method": "list_workflow_runs",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "resource_id": "ci.yml",
  "per_page": 1,
  "workflow_runs_filter": {
    "branch": "your-branch-name",
    "status": "completed"
  }
}
```

## Step-by-Step Investigation Process

### Step 1: List Recent Workflow Runs

**Tool:** `github-mcp-server-actions_list`

**Parameters:**
- `method`: `"list_workflow_runs"`
- `owner`: Repository owner (e.g., `"mikejhill"`)
- `repo`: Repository name (e.g., `"mic-passthrough"`)
- `resource_id`: Workflow file name (e.g., `"ci.yml"` or `"release.yml"`)
- `per_page`: **MUST be 1-3** to avoid large responses
- `page`: 1 (for most recent results)
- `workflow_runs_filter`: Object with:
  - `branch`: Specific branch name (highly recommended)
  - `status`: "completed", "in_progress", "queued", etc.

**Example:**
```json
{
  "method": "list_workflow_runs",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "resource_id": "ci.yml",
  "per_page": 1,
  "page": 1,
  "workflow_runs_filter": {
    "branch": "copilot/test-github-actions-process",
    "status": "completed"
  }
}
```

**Output:** Returns workflow run metadata including:
- `id`: Workflow run ID (needed for subsequent steps)
- `status`: "completed", "in_progress", etc.
- `conclusion`: "success", "failure", "cancelled", etc.
- `html_url`: Link to the run in GitHub UI

### Step 2: Get Workflow Run Details

**Tool:** `github-mcp-server-actions_get`

**Parameters:**
- `method`: `"get_workflow_run"`
- `owner`: Repository owner
- `repo`: Repository name
- `resource_id`: Workflow run ID from Step 1

**Example:**
```json
{
  "method": "get_workflow_run",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "resource_id": "20673434005"
}
```

**Output:** Returns detailed run metadata, similar to Step 1 but with additional context.

### Step 3: List Jobs for the Workflow Run

**Tool:** `github-mcp-server-actions_list`

**Parameters:**
- `method`: `"list_workflow_jobs"`
- `owner`: Repository owner
- `repo`: Repository name
- `resource_id`: Workflow run ID

**Example:**
```json
{
  "method": "list_workflow_jobs",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "resource_id": "20673434005"
}
```

**Output:** Returns all jobs in the workflow run with:
- `id`: Job ID (needed for getting logs)
- `name`: Job name (e.g., "Build and Test", "license-compliance / ORT License Compliance")
- `status`: Job status
- `conclusion`: Job conclusion ("success", "failure", etc.)
- `steps`: Array of steps with their status

### Step 4: Get Workflow Run Logs (Download All)

**Tool:** `github-mcp-server-actions_get`

**Parameters:**
- `method`: `"get_workflow_run_logs_url"`
- `owner`: Repository owner
- `repo`: Repository name
- `resource_id`: Workflow run ID

**Example:**
```json
{
  "method": "get_workflow_run_logs_url",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "resource_id": "20673434005"
}
```

**Output:** Returns a download URL for the complete workflow logs as a ZIP archive.

**Important:** This response includes an optimization tip suggesting to use `get_job_logs` with `failed_only=true`, but **that tool does not exist** in the current MCP server implementation.

### Step 5: Download and Extract Logs

Since the job logs tool doesn't exist, you need to download the ZIP file and extract logs manually:

```bash
# Download logs
curl -s "<logs_url>" --output /tmp/workflow_logs.zip

# List contents
unzip -l /tmp/workflow_logs.zip

# Extract specific job logs
unzip -o /tmp/workflow_logs.zip "job-name/step-name.txt"

# View logs
tail -100 "/tmp/job-name/step-name.txt"
```

**Example:**
```bash
# Download
curl -s "https://results-receiver.actions.githubusercontent.com/rest/runs/..." --output /tmp/workflow_logs.zip

# List
unzip -l /tmp/workflow_logs.zip

# Extract failed step
unzip -o /tmp/workflow_logs.zip "license-compliance _ ORT License Compliance/3_Run ORT (analyze, evaluate, report).txt"

# View last 100 lines
tail -100 "/tmp/license-compliance _ ORT License Compliance/3_Run ORT (analyze, evaluate, report).txt"
```

## Common Investigation Scenarios

### Scenario 1: User Reports "CI is Failing"

1. List recent workflow runs for the specific branch and workflow file
   - Use narrow filters: `per_page: 1`, specific branch
2. Get workflow run details to see overall status
3. List jobs to identify which job(s) failed
4. Download logs and extract the failed job/step logs
5. Analyze logs for error messages
6. Propose fix based on error

### Scenario 2: Check Current Build Status

1. List workflow runs with `status: "in_progress"` or `"completed"`
2. Review most recent run's conclusion
3. If failed, proceed with investigation steps above

### Scenario 3: Investigate Test Failures

1. List workflow runs for `ci.yml`
2. Find runs with `conclusion: "failure"`
3. List jobs and find "Build and Test" or similar job
4. Download logs and extract test step logs
5. Parse test failure output
6. Identify which tests failed and why

### Scenario 4: Debug Release Workflow Issues

1. List workflow runs for `release.yml`
2. Filter by tag-triggered events if possible
3. Verify build, test, and release creation steps
4. Check artifact upload logs if needed

## Available Filtering Options

When listing workflow runs, you can filter by:

- **status**: `"queued"`, `"in_progress"`, `"completed"`, `"requested"`, `"waiting"`
- **branch**: Filter to specific branch (e.g., `"main"`, `"develop"`)
- **event**: Filter by trigger event (`"push"`, `"pull_request"`, `"tag"`, etc.)
- **actor**: Filter to runs triggered by specific user

**Always use as many filters as possible** to minimize response size.

## Tool Limitations

### Missing Tool: `github-mcp-server-get_job_logs`

The documentation references a tool called `github-mcp-server-get_job_logs` with parameters:
- `job_id` or `run_id`
- `failed_only` (for getting all failed job logs)
- `return_content`
- `tail_lines`

**This tool does NOT exist** in the current MCP server implementation. Instead, you must:
1. Use `get_workflow_run_logs_url` to get the download URL
2. Download the ZIP file with `curl`
3. Extract and view logs manually with `unzip` and `tail`

## Best Practices

1. **Always start with narrow filters**
   - Use `per_page: 1` for initial investigation
   - Specify branch name when known
   - Filter by status to reduce results

2. **Progressive refinement**
   - Start with minimal results
   - Only request more if needed
   - Use specific job/step names when extracting logs

3. **Efficient log analysis**
   - Use `tail -N` to get last N lines (focus on errors at end)
   - Use `grep` to search for specific error patterns
   - Extract only the specific step logs that failed

4. **Provide specific error messages**
   - Quote exact error messages from logs
   - Include relevant context (file names, line numbers)
   - Reference the specific job and step that failed

## Example Complete Investigation

```bash
# 1. List recent runs (narrow filter)
# → Returns run_id: 20673434005, conclusion: "failure"

# 2. Get run details
# → Confirms failure, provides context

# 3. List jobs
# → Identifies failed job: "license-compliance / ORT License Compliance" (id: 59358549325)
# → Failed step: "Run ORT (analyze, evaluate, report)"

# 4. Get logs URL
# → Returns download URL

# 5. Download and analyze
curl -s "<url>" --output /tmp/logs.zip
unzip -l /tmp/logs.zip
unzip -o /tmp/logs.zip "license-compliance _ ORT License Compliance/3_Run ORT (analyze, evaluate, report).txt"
tail -100 "/tmp/license-compliance _ ORT License Compliance/3_Run ORT (analyze, evaluate, report).txt"

# Analysis reveals:
# Error: invalid value for -r: file ".ort/policy/rules.kts" does not exist.
```

## Triggering and Managing Workflows

### Run a Workflow Manually

**Tool:** `github-mcp-server-actions_run_trigger`

```json
{
  "method": "run_workflow",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "workflow_id": "ci.yml",
  "ref": "main",
  "inputs": {}
}
```

### Rerun a Failed Workflow

```json
{
  "method": "rerun_workflow_run",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "run_id": 20673434005
}
```

### Rerun Only Failed Jobs

```json
{
  "method": "rerun_failed_jobs",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "run_id": 20673434005
}
```

### Cancel a Running Workflow

```json
{
  "method": "cancel_workflow_run",
  "owner": "mikejhill",
  "repo": "mic-passthrough",
  "run_id": 20673434005
}
```

## Summary

The GitHub Actions investigation process works well with the available tools, but requires manual log download and extraction since the `get_job_logs` tool is not implemented. Always use narrow filters to manage response sizes and prevent context overflow.
