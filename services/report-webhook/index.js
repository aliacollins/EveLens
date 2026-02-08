const express = require('express');

const app = express();
const PORT = process.env.PORT || 3000;
const GITHUB_TOKEN = process.env.GITHUB_TOKEN;
const GITHUB_OWNER = process.env.GITHUB_OWNER || 'aliacollins';
const GITHUB_REPO = process.env.GITHUB_REPO || 'evemon';

app.use(express.json({ limit: '100kb' }));

// Rate limiting: 3 requests per IP per 5-minute window
const rateLimitMap = new Map();
const RATE_LIMIT_MAX = 3;
const RATE_LIMIT_WINDOW_MS = 5 * 60 * 1000;

function getRateLimitKey(req) {
  return req.headers['x-forwarded-for']?.split(',')[0]?.trim() || req.ip;
}

function checkRateLimit(ip) {
  const now = Date.now();
  let entry = rateLimitMap.get(ip);

  if (!entry || now - entry.windowStart > RATE_LIMIT_WINDOW_MS) {
    entry = { windowStart: now, count: 0 };
    rateLimitMap.set(ip, entry);
  }

  entry.count++;
  return entry.count <= RATE_LIMIT_MAX;
}

// Periodically clean up stale rate limit entries
setInterval(() => {
  const now = Date.now();
  for (const [ip, entry] of rateLimitMap) {
    if (now - entry.windowStart > RATE_LIMIT_WINDOW_MS) {
      rateLimitMap.delete(ip);
    }
  }
}, RATE_LIMIT_WINDOW_MS);

// Health check
app.get('/health', (_req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// Report submission endpoint
app.post('/api/report', async (req, res) => {
  try {
    const ip = getRateLimitKey(req);
    if (!checkRateLimit(ip)) {
      return res.status(429).json({
        success: false,
        error: 'Rate limit exceeded. Please try again in a few minutes.'
      });
    }

    const { title, reportType, version, reportBody, crashSummary } = req.body;

    // Validate required fields
    if (!title || !reportType || !version || !reportBody) {
      return res.status(400).json({
        success: false,
        error: 'Missing required fields: title, reportType, version, reportBody'
      });
    }

    if (reportType !== 'crash' && reportType !== 'diagnostic') {
      return res.status(400).json({
        success: false,
        error: 'reportType must be "crash" or "diagnostic"'
      });
    }

    if (!GITHUB_TOKEN) {
      return res.status(500).json({
        success: false,
        error: 'Server configuration error: GITHUB_TOKEN not set'
      });
    }

    // Truncate report body to 60K chars
    const truncatedReport = reportBody.length > 60000
      ? reportBody.substring(0, 60000) + '\n\n[Report truncated at 60,000 characters]'
      : reportBody;

    const os = req.body.os || 'Unknown';

    // Build the issue body
    let issueBody = `## Environment\n`;
    issueBody += `- **EVEMon Version:** ${version}\n`;
    issueBody += `- **OS:** ${os}\n`;
    issueBody += `- **Report Type:** ${reportType}\n`;

    if (crashSummary) {
      issueBody += `\n## Crash Summary\n`;
      issueBody += `\`${crashSummary}\`\n`;
    }

    issueBody += `\n<details>\n<summary>Full Report</summary>\n\n`;
    issueBody += `\`\`\`\n${truncatedReport}\n\`\`\`\n`;
    issueBody += `</details>\n`;

    // Determine labels
    const labels = ['bug', 'auto-reported', reportType];

    // Create GitHub issue
    const response = await fetch(
      `https://api.github.com/repos/${GITHUB_OWNER}/${GITHUB_REPO}/issues`,
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${GITHUB_TOKEN}`,
          'Accept': 'application/vnd.github+json',
          'Content-Type': 'application/json',
          'X-GitHub-Api-Version': '2022-11-28',
          'User-Agent': 'evemon-report-webhook'
        },
        body: JSON.stringify({
          title,
          body: issueBody,
          labels
        })
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      console.error(`GitHub API error: ${response.status} ${errorText}`);
      return res.status(502).json({
        success: false,
        error: 'Failed to create GitHub issue'
      });
    }

    const issue = await response.json();
    return res.status(201).json({
      success: true,
      issueUrl: issue.html_url,
      issueNumber: issue.number
    });
  } catch (err) {
    console.error('Unexpected error:', err);
    return res.status(500).json({
      success: false,
      error: 'Internal server error'
    });
  }
});

app.listen(PORT, () => {
  console.log(`Report webhook listening on port ${PORT}`);
});
