namespace NotificationService.Api;


public static class DashboardHtml
{
    public static string GetHtml() => """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Notification Operations Console</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght@400;500;600&display=swap" rel="stylesheet">
  <style>
    :root {
      --bg: #090d16;
      --card-bg: rgba(17, 24, 39, 0.75);
      --card-border: rgba(255, 255, 255, 0.08);
      --text-main: #F3F4F6;
      --text-muted: #9CA3AF;
      --discord: #5865F2;
      --discord-hover: #4752C4;
      --accent-cyan: #06B6D4;
      --accent-emerald: #10B981;
      --accent-amber: #F59E0B;
      --accent-rose: #EF4444;
      --glow-blue: rgba(6, 182, 212, 0.25);
    }

    * {
      box-sizing: border-box;
      margin: 0;
      padding: 0;
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
    }

    body {
      background-color: var(--bg);
      background-image: 
        radial-gradient(at 0% 0%, rgba(88, 101, 242, 0.12) 0px, transparent 50%),
        radial-gradient(at 100% 0%, rgba(6, 182, 212, 0.1) 0px, transparent 50%),
        radial-gradient(at 50% 100%, rgba(239, 68, 68, 0.08) 0px, transparent 50%);
      background-attachment: fixed;
      color: var(--text-main);
      min-height: 100vh;
      padding: 24px;
    }

    .container {
      max-width: 1280px;
      margin: 0 auto;
    }

    header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 18px 24px;
      background: var(--card-bg);
      backdrop-filter: blur(16px);
      border: 1px solid var(--card-border);
      border-radius: 16px;
      margin-bottom: 24px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.35);
    }

    .brand {
      display: flex;
      align-items: center;
      gap: 14px;
    }

    .brand-icon {
      width: 44px;
      height: 44px;
      background: linear-gradient(135deg, var(--discord), #8B5CF6);
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 22px;
      box-shadow: 0 4px 16px rgba(88, 101, 242, 0.4);
    }

    .brand-title h1 {
      font-size: 20px;
      font-weight: 700;
      letter-spacing: -0.02em;
      background: linear-gradient(to right, #ffffff, #94A3B8);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }

    .brand-title p {
      font-size: 13px;
      color: var(--text-muted);
      font-weight: 400;
    }

    .status-badge {
      display: flex;
      align-items: center;
      gap: 8px;
      background: rgba(16, 185, 129, 0.12);
      border: 1px solid rgba(16, 185, 129, 0.3);
      padding: 6px 14px;
      border-radius: 9999px;
      font-size: 13px;
      font-weight: 600;
      color: var(--accent-emerald);
    }

    .status-dot {
      width: 8px;
      height: 8px;
      background-color: var(--accent-emerald);
      border-radius: 50%;
      box-shadow: 0 0 10px var(--accent-emerald);
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; transform: scale(1); }
      50% { opacity: 0.4; transform: scale(0.85); }
    }

    /* KPI Metrics Grid */
    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }

    .kpi-card {
      background: var(--card-bg);
      backdrop-filter: blur(12px);
      border: 1px solid var(--card-border);
      border-radius: 14px;
      padding: 18px;
      display: flex;
      flex-direction: column;
      gap: 8px;
      transition: transform 0.2s, border-color 0.2s;
    }

    .kpi-card:hover {
      transform: translateY(-2px);
      border-color: rgba(255, 255, 255, 0.18);
    }

    .kpi-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      color: var(--text-muted);
      font-size: 13px;
      font-weight: 500;
    }

    .kpi-value {
      font-size: 28px;
      font-weight: 700;
      letter-spacing: -0.02em;
    }

    .rate-bar-container {
      width: 100%;
      height: 8px;
      background: rgba(255, 255, 255, 0.08);
      border-radius: 999px;
      overflow: hidden;
      margin-top: 4px;
    }

    .rate-bar-fill {
      height: 100%;
      width: 0%;
      background: linear-gradient(90deg, var(--accent-emerald), var(--accent-amber), var(--accent-rose));
      transition: width 0.4s ease;
      border-radius: 999px;
    }

    /* Main Grid Layout */
    .main-grid {
      display: grid;
      grid-template-columns: 460px 1fr;
      gap: 24px;
    }

    @media (max-width: 1024px) {
      .main-grid { grid-template-columns: 1fr; }
    }

    .card {
      background: var(--card-bg);
      backdrop-filter: blur(16px);
      border: 1px solid var(--card-border);
      border-radius: 16px;
      padding: 24px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.25);
    }

    .card-title {
      font-size: 16px;
      font-weight: 600;
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 20px;
      padding-bottom: 12px;
      border-bottom: 1px solid rgba(255, 255, 255, 0.06);
    }

    /* Form Styles */
    .form-group {
      margin-bottom: 16px;
    }

    .form-label {
      display: block;
      font-size: 13px;
      font-weight: 500;
      color: var(--text-muted);
      margin-bottom: 8px;
    }

    .level-selector {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 8px;
    }

    .level-btn {
      background: rgba(255, 255, 255, 0.04);
      border: 1px solid rgba(255, 255, 255, 0.08);
      color: var(--text-muted);
      padding: 9px 6px;
      border-radius: 10px;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
    }

    .level-btn:hover {
      background: rgba(255, 255, 255, 0.08);
      color: var(--text-main);
    }

    .level-btn.active-info {
      background: rgba(6, 182, 212, 0.15);
      border-color: var(--accent-cyan);
      color: var(--accent-cyan);
      box-shadow: 0 0 14px rgba(6, 182, 212, 0.2);
    }

    .level-btn.active-warning {
      background: rgba(245, 158, 11, 0.15);
      border-color: var(--accent-amber);
      color: var(--accent-amber);
      box-shadow: 0 0 14px rgba(245, 158, 11, 0.2);
    }

    .level-btn.active-error {
      background: rgba(249, 115, 22, 0.15);
      border-color: #f97316;
      color: #f97316;
      box-shadow: 0 0 14px rgba(249, 115, 22, 0.2);
    }

    .level-btn.active-critical {
      background: rgba(239, 68, 68, 0.18);
      border-color: var(--accent-rose);
      color: var(--accent-rose);
      box-shadow: 0 0 16px rgba(239, 68, 68, 0.3);
    }

    .input-field {
      width: 100%;
      background: rgba(0, 0, 0, 0.35);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 10px;
      padding: 10px 14px;
      color: var(--text-main);
      font-size: 14px;
      transition: border-color 0.2s, box-shadow 0.2s;
      outline: none;
    }

    .input-field:focus {
      border-color: var(--discord);
      box-shadow: 0 0 0 3px rgba(88, 101, 242, 0.25);
    }

    textarea.input-field {
      resize: vertical;
      min-height: 80px;
      font-size: 13px;
      line-height: 1.5;
    }

    /* Scenario Pills */
    .scenario-container {
      margin-bottom: 16px;
    }

    .scenario-pills {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      margin-top: 6px;
    }

    .scenario-pill {
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.08);
      color: var(--text-muted);
      font-size: 11px;
      padding: 5px 10px;
      border-radius: 999px;
      cursor: pointer;
      transition: all 0.15s;
    }

    .scenario-pill:hover {
      background: rgba(88, 101, 242, 0.18);
      color: #fff;
      border-color: var(--discord);
    }

    .btn-row {
      display: flex;
      flex-direction: column;
      gap: 10px;
      margin-top: 20px;
    }

    .btn {
      width: 100%;
      padding: 12px 18px;
      border-radius: 10px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      border: none;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      transition: all 0.2s;
    }

    .btn-primary {
      background: linear-gradient(135deg, var(--discord), #4338CA);
      color: #fff;
      box-shadow: 0 4px 14px rgba(88, 101, 242, 0.35);
    }

    .btn-primary:hover {
      background: linear-gradient(135deg, #6875F5, var(--discord));
      transform: translateY(-1px);
      box-shadow: 0 6px 18px rgba(88, 101, 242, 0.45);
    }

    .btn-burst {
      background: rgba(245, 158, 11, 0.12);
      border: 1px solid rgba(245, 158, 11, 0.3);
      color: var(--accent-amber);
    }

    .btn-burst:hover {
      background: rgba(245, 158, 11, 0.22);
      border-color: var(--accent-amber);
      transform: translateY(-1px);
    }

    /* Live Stream / Table */
    .stream-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .stream-meta {
      font-size: 12px;
      color: var(--text-muted);
      font-family: 'JetBrains Mono', monospace;
    }

    .events-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
      max-height: 600px;
      overflow-y: auto;
      padding-right: 4px;
    }

    .events-list::-webkit-scrollbar {
      width: 6px;
    }
    .events-list::-webkit-scrollbar-thumb {
      background: rgba(255, 255, 255, 0.12);
      border-radius: 999px;
    }

    .event-card {
      background: rgba(0, 0, 0, 0.3);
      border: 1px solid rgba(255, 255, 255, 0.06);
      border-radius: 12px;
      padding: 14px 16px;
      display: flex;
      flex-direction: column;
      gap: 8px;
      transition: all 0.2s ease;
      animation: fadeIn 0.3s ease;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(-6px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .event-card:hover {
      border-color: rgba(255, 255, 255, 0.14);
      background: rgba(0, 0, 0, 0.45);
    }

    .event-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .event-source {
      font-weight: 600;
      font-size: 13px;
      display: flex;
      align-items: center;
      gap: 6px;
      color: #E2E8F0;
    }

    .badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 3px 8px;
      border-radius: 6px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }

    .badge-info { background: rgba(6, 182, 212, 0.18); color: var(--accent-cyan); }
    .badge-warning { background: rgba(245, 158, 11, 0.18); color: var(--accent-amber); }
    .badge-error { background: rgba(249, 115, 22, 0.18); color: #f97316; }
    .badge-critical { background: rgba(239, 68, 68, 0.22); color: var(--accent-rose); }

    .event-msg {
      font-size: 13px;
      color: #CBD5E1;
      line-height: 1.4;
    }

    .event-llm {
      background: rgba(88, 101, 242, 0.08);
      border-left: 3px solid var(--discord);
      padding: 8px 12px;
      border-radius: 0 8px 8px 0;
      font-size: 12px;
      color: #E0E7FF;
      font-style: italic;
    }

    .event-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 11px;
      color: var(--text-muted);
      border-top: 1px solid rgba(255, 255, 255, 0.04);
      padding-top: 6px;
      margin-top: 2px;
    }

    .tag-forwarded {
      color: var(--accent-emerald);
      font-weight: 600;
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .tag-filtered {
      color: var(--text-muted);
      font-weight: 500;
    }

    .empty-state {
      text-align: center;
      padding: 48px 24px;
      color: var(--text-muted);
      font-size: 14px;
    }
  </style>
</head>
<body>
  <div class="container">
    <header>
      <div class="brand">
        <div class="brand-icon">🔔</div>
        <div class="brand-title">
          <h1>Notification Forwarding Service</h1>
          <p>ASP.NET Core Minimal API • OpenAI Summarization • Discord Webhook • Rate Limited (10/min)</p>
        </div>
      </div>
      <div class="status-badge">
        <div class="status-dot"></div>
        <span>Service Healthy</span>
      </div>
    </header>

    <!-- KPI Metrics -->
    <div class="metrics-grid">
      <div class="kpi-card">
        <div class="kpi-header">
          <span>Total Ingested</span>
          <span>📥</span>
        </div>
        <div class="kpi-value" id="kpi-ingested">0</div>
        <div style="font-size:11px; color:var(--text-muted);">All incoming HTTP POSTs</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-header">
          <span>Forwarded to Discord</span>
          <span>🚀</span>
        </div>
        <div class="kpi-value" style="color:var(--accent-emerald)" id="kpi-forwarded">0</div>
        <div style="font-size:11px; color:var(--text-muted);">Level &ge; warning processed</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-header">
          <span>Filtered (Not Forwarded)</span>
          <span>🛡️</span>
        </div>
        <div class="kpi-value" style="color:var(--accent-cyan)" id="kpi-filtered">0</div>
        <div style="font-size:11px; color:var(--text-muted);">Info severity ignored safely</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-header">
          <span>Rate Limit (Current Window)</span>
          <span>⏱️</span>
        </div>
        <div class="kpi-value" id="kpi-rate">0 <span style="font-size:14px; color:var(--text-muted); font-weight:400;">/ 10 per min</span></div>
        <div class="rate-bar-container">
          <div class="rate-bar-fill" id="rate-bar" style="width: 0%;"></div>
        </div>
      </div>
    </div>

    <!-- Main Section -->
    <div class="main-grid">
      <!-- Left: Interactive Form -->
      <div class="card">
        <div class="card-title">
          <span>📤</span>
          <span>Send Live Notification</span>
        </div>

        <form id="notify-form" onsubmit="event.preventDefault(); sendNotification();">
          <div class="form-group">
            <label class="form-label">Severity Level</label>
            <div class="level-selector">
              <button type="button" class="level-btn active-warning" data-level="warning" onclick="selectLevel('warning')">
                <span>⚠️</span>
                <span>Warning</span>
              </button>
              <button type="button" class="level-btn" data-level="error" onclick="selectLevel('error')">
                <span>🔥</span>
                <span>Error</span>
              </button>
              <button type="button" class="level-btn" data-level="critical" onclick="selectLevel('critical')">
                <span>🚨</span>
                <span>Critical</span>
              </button>
              <button type="button" class="level-btn" data-level="info" onclick="selectLevel('info')">
                <span>ℹ️</span>
                <span>Info</span>
              </button>
            </div>
          </div>

          <div class="scenario-container">
            <label class="form-label">Scenario Presets</label>
            <div class="scenario-pills">
              <span class="scenario-pill" onclick="applyPreset('warning', 'payment-service', 'Timeout connecting to upstream payment gateway after 3 retries.')">⚠️ Payment Timeout</span>
              <span class="scenario-pill" onclick="applyPreset('error', 'order-processor', 'Deadlock detected on orders table during high throughput batch.')">🔥 DB Deadlock</span>
              <span class="scenario-pill" onclick="applyPreset('critical', 'auth-service', 'TLS certificate expiring in 2 hours across authentication cluster.')">🚨 SSL Expiring</span>
              <span class="scenario-pill" onclick="applyPreset('info', 'user-service', 'User id 489201 successfully updated shipping preferences.')">ℹ️ User Activity (Info)</span>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Originating Source</label>
            <input type="text" id="input-source" class="input-field" value="payment-service" placeholder="e.g. payment-service, auth-api" required />
          </div>

          <div class="form-group">
            <label class="form-label">Alert Message</label>
            <textarea id="input-message" class="input-field" rows="3" placeholder="Describe the notification details..." required>Timeout connecting to upstream payment gateway after 3 retries.</textarea>
          </div>

          <div class="btn-row">
            <button type="submit" class="btn btn-primary" id="btn-submit">
              <span>🚀</span>
              <span>Dispatch Notification (POST /notifications)</span>
            </button>
            <button type="button" class="btn btn-burst" onclick="simulateBurst()">
              <span>⚡</span>
              <span>Execute 15-Request Concurrency Test</span>
            </button>
          </div>
        </form>
      </div>

      <!-- Right: Real-time Live Stream -->
      <div class="card">
        <div class="stream-header">
          <div class="card-title" style="margin-bottom:0; border:none; padding:0;">
            <span>📡</span>
            <span>Live Forwarding & Activity Stream</span>
          </div>
          <div class="stream-meta" id="stream-pulse">● POLLING LIVE</div>
        </div>

        <div class="events-list" id="events-container">
          <div class="empty-state">
            <div style="font-size:32px; margin-bottom:10px;">📫</div>
            <p>No notifications recorded in current session.</p>
            <p style="font-size:12px; margin-top:4px;">Dispatched events will appear in this real-time stream.</p>
          </div>
        </div>
      </div>
    </div>
  </div>

  <script>
    let currentLevel = 'warning';

    function selectLevel(level) {
      currentLevel = level;
      document.querySelectorAll('.level-btn').forEach(btn => {
        btn.className = 'level-btn';
        if (btn.getAttribute('data-level') === level) {
          btn.classList.add('active-' + level);
        }
      });
    }

    function applyPreset(level, source, message) {
      selectLevel(level);
      document.getElementById('input-source').value = source;
      document.getElementById('input-message').value = message;
    }

    async function sendNotification() {
      const source = document.getElementById('input-source').value.trim();
      const message = document.getElementById('input-message').value.trim();
      const btn = document.getElementById('btn-submit');

      btn.disabled = true;
      btn.style.opacity = '0.6';

      try {
        const payload = {
          level: currentLevel,
          source: source,
          message: message,
          timestamp: new Date().toISOString()
        };

        const res = await fetch('/notifications', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });

        const data = await res.json();
        await refreshStats();
      } catch (err) {
        console.error('Error sending notification:', err);
      } finally {
        btn.disabled = false;
        btn.style.opacity = '1';
      }
    }

    async function simulateBurst() {
      const btn = event.currentTarget;
      btn.disabled = true;
      btn.innerText = '⚡ Firing 15 requests in burst...';

      const promises = [];
      for (let i = 1; i <= 15; i++) {
        const p = fetch('/notifications', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            level: i % 3 === 0 ? 'critical' : 'warning',
            source: `burst-node-${i}`,
            message: `Burst notification #${i}: rapid threshold stress test event.`,
            timestamp: new Date().toISOString()
          })
        });
        promises.push(p);
      }

      await Promise.all(promises);
      await refreshStats();
      btn.disabled = false;
      btn.innerHTML = '<span>⚡</span><span>Execute 15-Request Concurrency Test</span>';
    }

    async function refreshStats() {
      try {
        const res = await fetch('/api/stats');
        if (!res.ok) return;
        const stats = await res.json();

        document.getElementById('kpi-ingested').innerText = stats.totalIngested;
        document.getElementById('kpi-forwarded').innerText = stats.totalForwarded;
        document.getElementById('kpi-filtered').innerText = stats.totalFiltered;

        const rateUsed = stats.currentPermitsUsed || 0;
        document.getElementById('kpi-rate').innerHTML = `${rateUsed} <span style="font-size:14px; color:var(--text-muted); font-weight:400;">/ 10 per min</span>`;
        document.getElementById('rate-bar').style.width = Math.min((rateUsed / 10) * 100, 100) + '%';

        renderEvents(stats.recentEvents || []);
      } catch (err) {
        console.warn('Polling error:', err);
      }
    }

    function renderEvents(events) {
      const container = document.getElementById('events-container');
      if (events.length === 0) {
        container.innerHTML = `
          <div class="empty-state">
            <div style="font-size:32px; margin-bottom:10px;">📫</div>
            <p>No notifications recorded in current session.</p>
            <p style="font-size:12px; margin-top:4px;">Dispatched events will appear in this real-time stream.</p>
          </div>
        `;
        return;
      }

      container.innerHTML = events.map(ev => {
        const timeStr = new Date(ev.timestamp).toLocaleTimeString();
        const badgeClass = `badge-${ev.level.toLowerCase()}`;
        const isFwd = ev.shouldForward;

        return `
          <div class="event-card">
            <div class="event-top">
              <span class="event-source">
                <span class="badge ${badgeClass}">${ev.level}</span>
                <span>${escapeHtml(ev.source)}</span>
              </span>
              <span style="font-size:11px; color:var(--text-muted); font-family:'JetBrains Mono'">${timeStr}</span>
            </div>

            <div class="event-msg">${escapeHtml(ev.message)}</div>

            ${ev.generatedAlert ? `
              <div class="event-llm">
                <strong>Forwarded Digest:</strong> "${escapeHtml(ev.generatedAlert)}"
              </div>
            ` : ''}

            <div class="event-footer">
              <span style="font-size:11px; color:${isFwd ? '#10B981' : '#9CA3AF'}">
                ${isFwd ? '🚀 Forwarded to Discord (Rate-Limited)' : '🛡️ Filtered (Level &lt; Warning)'}
              </span>
              <span style="font-size:11px; color:#A5B4FC; font-weight:500;">
                Status: ${escapeHtml(ev.status)}
              </span>
            </div>
          </div>
        `;
      }).join('');
    }

    function escapeHtml(text) {
      if (!text) return '';
      return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // Auto-poll stats every 2 seconds
    setInterval(refreshStats, 2000);
    refreshStats();
  </script>
</body>
</html>
""";
}
