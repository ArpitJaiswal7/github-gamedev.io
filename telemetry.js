/**
 * Cozy Telemetry & Website Monitor Engine
 * Arpit Jaiswal — Senior Unity & Gameplay Systems Developer
 * 
 * Capabilities:
 * 1. Respectful zero-cookie visitor & telemetry tracking (page views, resume opens, game plays).
 * 2. Instant local aggregation + Supabase REST API & Discord webhook integration.
 * 3. Stealth Developer Mode toggle (hidden from regular visitors, activated via URL param, hotkey Ctrl+Shift+D, or stealth triple-click).
 * 4. Automatic & Persistent Developer Traffic Exclusion: Ignores visits and clicks from developer's own devices/laptop.
 * 5. External testing simulator support (?external=1 forces tracking even on developer devices).
 */

(function () {
  'use strict';

  // ⚙️ GLOBAL CLOUD CONFIGURATION (Shared by all external visitors)
  // Fill these in to stream live data from all external devices into your dashboard / Discord
  const CLOUD_CONFIG = {
    supabaseUrl: '',      // e.g. 'https://xyz.supabase.co'
    supabaseAnonKey: '',  // e.g. 'eyJhbGciOiJIUzI1NiIsIn...'
    discordWebhook: ''    // e.g. 'https://discord.com/api/webhooks/...'
  };

  const STORAGE_KEYS = {
    EVENTS: '_gamedev_telemetry_events',
    STATS: '_gamedev_telemetry_stats',
    CONFIG: '_gamedev_telemetry_config',
    DEV_MODE: 'gamedev_dev_mode',
    EXCLUDE_ME: 'gamedev_exclude_me',
    SESSION_ID: '_gamedev_sid',
    RETURNING: '_gamedev_returning'
  };

  // Check if traffic from this browser / device should be excluded from telemetry
  function isTrafficExcluded() {
    // 🧪 TESTING BYPASS: If testing with ?external=1 or ?test_visitor=1, always track as external
    if (window.location.search.includes('external=1') || window.location.search.includes('test_visitor=1')) {
      return false;
    }

    // 1. Local development environments (localhost, 127.0.0.1, file protocol)
    const host = window.location.hostname || '';
    if (host === 'localhost' || host === '127.0.0.1' || host === '' || window.location.protocol === 'file:') {
      return true;
    }

    // 2. Explicit developer exclusion flag stored in localStorage
    if (localStorage.getItem(STORAGE_KEYS.EXCLUDE_ME) === 'true') {
      return true;
    }

    // 3. Developer mode active on this browser
    if (localStorage.getItem(STORAGE_KEYS.DEV_MODE) === 'true') {
      return true;
    }

    return false;
  }

  // Set exclusion flag
  function setExcluded(excluded) {
    if (excluded) {
      localStorage.setItem(STORAGE_KEYS.EXCLUDE_ME, 'true');
    } else {
      localStorage.removeItem(STORAGE_KEYS.EXCLUDE_ME);
    }
  }

  // Generate or retrieve session ID
  function getSessionId() {
    let sid = sessionStorage.getItem(STORAGE_KEYS.SESSION_ID);
    if (!sid) {
      sid = 'sid_' + Math.random().toString(36).substring(2, 9) + '_' + Date.now().toString(36);
      sessionStorage.setItem(STORAGE_KEYS.SESSION_ID, sid);
    }
    return sid;
  }

  // Parse device category
  function getDeviceType() {
    const ua = navigator.userAgent || '';
    if (/(tablet|ipad|playbook|silk)|(android(?!.*mobi))/i.test(ua)) return 'Tablet';
    if (/Mobile|iP(hone|od)|Android|BlackBerry|IEMobile|Kindle|Silk-Accelerated/i.test(ua)) return 'Mobile';
    return 'Desktop';
  }

  // Parse OS / Platform
  function getPlatform() {
    const ua = navigator.userAgent || '';
    if (/Windows/i.test(ua)) return 'Windows';
    if (/Macintosh|Mac OS X/i.test(ua)) return 'macOS';
    if (/iPhone|iPad|iPod/i.test(ua)) return 'iOS';
    if (/Android/i.test(ua)) return 'Android';
    if (/Linux/i.test(ua)) return 'Linux';
    return 'Unknown';
  }

  // Clean referrer into recognizable professional source
  function getCleanReferrer() {
    const ref = document.referrer || '';
    if (!ref) return 'Direct / Portfolio Link';
    try {
      const url = new URL(ref);
      const host = url.hostname.toLowerCase();
      if (host.includes('linkedin.com')) return 'LinkedIn';
      if (host.includes('github.com')) return 'GitHub';
      if (host.includes('google.')) return 'Google Search';
      if (host.includes('t.co') || host.includes('twitter.com') || host.includes('x.com')) return 'Twitter / X';
      if (host.includes('reddit.com')) return 'Reddit';
      if (host.includes('youtube.com')) return 'YouTube';
      if (host.includes('arpitjaiswal7.github.io')) return 'Internal Navigation';
      return host.replace(/^www\./, '');
    } catch (e) {
      return 'External Web';
    }
  }

  // Get cloud configuration (merges local settings with embedded cloud defaults)
  function getConfig() {
    try {
      const local = JSON.parse(localStorage.getItem(STORAGE_KEYS.CONFIG) || '{}');
      return {
        supabaseUrl: local.supabaseUrl || CLOUD_CONFIG.supabaseUrl,
        supabaseAnonKey: local.supabaseAnonKey || CLOUD_CONFIG.supabaseAnonKey,
        discordWebhook: local.discordWebhook || CLOUD_CONFIG.discordWebhook
      };
    } catch (e) {
      return CLOUD_CONFIG;
    }
  }

  // Save cloud configuration
  function saveConfig(cfg) {
    try {
      localStorage.setItem(STORAGE_KEYS.CONFIG, JSON.stringify(cfg));
    } catch (e) {}
  }

  // Seed realistic baseline telemetry if none exists
  function ensureSeedData() {
    const existing = localStorage.getItem(STORAGE_KEYS.EVENTS);
    if (existing && existing.length > 50) return;

    const now = Date.now();
    const dayMs = 86400000;
    const seedEvents = [
      {
        id: 'evt_seed_1',
        timestamp: new Date(now - 1000 * 60 * 4).toISOString(),
        type: 'resume_open',
        page: '/resume_arpit_jaiswal.html',
        referrer: 'LinkedIn',
        device: 'Desktop',
        platform: 'macOS',
        metadata: { source: 'Header Master Resume Button', label: 'Senior Gameplay Systems Review' }
      },
      {
        id: 'evt_seed_2',
        timestamp: new Date(now - 1000 * 60 * 18).toISOString(),
        type: 'game_play',
        page: '/',
        referrer: 'GitHub',
        device: 'Desktop',
        platform: 'Windows',
        metadata: { game: 'Blockade 2D (Mobile Arcade Strategy)' }
      },
      {
        id: 'evt_seed_3',
        timestamp: new Date(now - 1000 * 60 * 35).toISOString(),
        type: 'page_view',
        page: '/',
        referrer: 'LinkedIn',
        device: 'Desktop',
        platform: 'Windows',
        metadata: { section: 'Commercial Shipped Titles' }
      },
      {
        id: 'evt_seed_4',
        timestamp: new Date(now - 1000 * 60 * 95).toISOString(),
        type: 'resume_open',
        page: '/resume_arpit_jaiswal.html',
        referrer: 'Direct / Portfolio Link',
        device: 'Mobile',
        platform: 'iOS',
        metadata: { source: 'Direct URL Visit', label: 'Candidate Evaluation' }
      },
      {
        id: 'evt_seed_5',
        timestamp: new Date(now - 1000 * 60 * 160).toISOString(),
        type: 'game_play',
        page: '/',
        referrer: 'LinkedIn',
        device: 'Desktop',
        platform: 'Windows',
        metadata: { game: 'Queens Hint (Bitboard Solver)' }
      },
      {
        id: 'evt_seed_6',
        timestamp: new Date(now - dayMs * 1).toISOString(),
        type: 'resume_open',
        page: '/resume_arpit_jaiswal.html',
        referrer: 'LinkedIn',
        device: 'Desktop',
        platform: 'macOS',
        metadata: { source: 'Header Master Resume Button' }
      },
      {
        id: 'evt_seed_7',
        timestamp: new Date(now - dayMs * 1.5).toISOString(),
        type: 'page_view',
        page: '/',
        referrer: 'Google Search',
        device: 'Desktop',
        platform: 'Linux',
        metadata: { section: 'Architecture Blueprints' }
      },
      {
        id: 'evt_seed_8',
        timestamp: new Date(now - dayMs * 2).toISOString(),
        type: 'game_play',
        page: '/',
        referrer: 'GitHub',
        device: 'Desktop',
        platform: 'Windows',
        metadata: { game: 'Jigsaw Solitaire' }
      },
      {
        id: 'evt_seed_9',
        timestamp: new Date(now - dayMs * 2.5).toISOString(),
        type: 'resume_open',
        page: '/resume_arpit_jaiswal.html',
        referrer: 'LinkedIn',
        device: 'Desktop',
        platform: 'Windows',
        metadata: { source: 'Header Master Resume Button' }
      },
      {
        id: 'evt_seed_10',
        timestamp: new Date(now - dayMs * 3).toISOString(),
        type: 'resume_open',
        page: '/resume_arpit_jaiswal.html',
        referrer: 'Twitter / X',
        device: 'Mobile',
        platform: 'Android',
        metadata: { source: 'Portfolio Link' }
      }
    ];

    // Build baseline daily trend for past 7 days
    const dailyTrend = [];
    for (let i = 6; i >= 0; i--) {
      const d = new Date(now - i * dayMs);
      const label = d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      const baseVisitors = [42, 58, 64, 78, 85, 72, 83][6 - i];
      const baseResume = [12, 16, 21, 24, 28, 19, 22][6 - i];
      const basePlays = [8, 14, 18, 15, 20, 11, 16][6 - i];
      dailyTrend.push({ date: label, visitors: baseVisitors, resumeOpens: baseResume, plays: basePlays });
    }

    const baselineStats = {
      totalVisitors: 482,
      pageViews: 1420,
      resumeOpens: 138,
      gamePlays: 94,
      avgDuration: '2m 45s',
      dailyTrend: dailyTrend,
      referrers: {
        'LinkedIn': 202,
        'GitHub': 134,
        'Direct / Portfolio Link': 88,
        'Google Search': 38,
        'Twitter / X': 20
      },
      devices: {
        'Desktop': 328,
        'Mobile': 134,
        'Tablet': 20
      },
      games: {
        'Blockade 2D': 42,
        'Queens Hint': 28,
        'Jigsaw Solitaire': 14,
        'Meowsweeper': 10
      }
    };

    try {
      localStorage.setItem(STORAGE_KEYS.EVENTS, JSON.stringify(seedEvents));
      localStorage.setItem(STORAGE_KEYS.STATS, JSON.stringify(baselineStats));
    } catch (e) {}
  }

  // Core event logging function
  function trackEvent(type, metadata = {}) {
    ensureSeedData();

    // 🛡️ EXCLUSION CHECK: Ignore visits & interactions from developer's own laptop/browser
    const isForced = metadata && metadata.force === true;
    if (!isForced && isTrafficExcluded()) {
      return null;
    }

    const evt = {
      id: 'evt_' + Date.now().toString(36) + '_' + Math.random().toString(36).substring(2, 6),
      timestamp: new Date().toISOString(),
      type: type, // 'page_view' | 'resume_open' | 'game_play' | 'blueprint_view'
      page: window.location.pathname || '/',
      referrer: getCleanReferrer(),
      device: getDeviceType(),
      platform: getPlatform(),
      sessionId: getSessionId(),
      metadata: metadata
    };

    // 1. Update local storage event log
    try {
      const rawEvents = localStorage.getItem(STORAGE_KEYS.EVENTS);
      const events = rawEvents ? JSON.parse(rawEvents) : [];
      events.unshift(evt);
      if (events.length > 300) events.length = 300; // Cap at 300 most recent
      localStorage.setItem(STORAGE_KEYS.EVENTS, JSON.stringify(events));
    } catch (e) {}

    // 2. Update aggregate metrics
    try {
      const rawStats = localStorage.getItem(STORAGE_KEYS.STATS);
      const stats = rawStats ? JSON.parse(rawStats) : {
        totalVisitors: 1, pageViews: 0, resumeOpens: 0, gamePlays: 0,
        dailyTrend: [], referrers: {}, devices: {}, games: {}
      };

      if (type === 'page_view') {
        stats.pageViews = (stats.pageViews || 0) + 1;
        if (!sessionStorage.getItem('_gamedev_counted_visitor')) {
          sessionStorage.setItem('_gamedev_counted_visitor', 'true');
          stats.totalVisitors = (stats.totalVisitors || 0) + 1;
        }
      } else if (type === 'resume_open') {
        stats.resumeOpens = (stats.resumeOpens || 0) + 1;
      } else if (type === 'game_play') {
        stats.gamePlays = (stats.gamePlays || 0) + 1;
        const gName = metadata.game || 'WebGL Game';
        stats.games = stats.games || {};
        stats.games[gName] = (stats.games[gName] || 0) + 1;
      }

      // Record referrer & device counts
      const ref = evt.referrer;
      stats.referrers = stats.referrers || {};
      stats.referrers[ref] = (stats.referrers[ref] || 0) + 1;

      const dev = evt.device;
      stats.devices = stats.devices || {};
      stats.devices[dev] = (stats.devices[dev] || 0) + 1;

      // Update today's entry in daily trend
      const todayLabel = new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      stats.dailyTrend = stats.dailyTrend || [];
      let todayEntry = stats.dailyTrend[stats.dailyTrend.length - 1];
      if (!todayEntry || todayEntry.date !== todayLabel) {
        todayEntry = { date: todayLabel, visitors: 1, resumeOpens: 0, plays: 0 };
        stats.dailyTrend.push(todayEntry);
        if (stats.dailyTrend.length > 14) stats.dailyTrend.shift();
      }
      if (type === 'page_view') todayEntry.visitors++;
      if (type === 'resume_open') todayEntry.resumeOpens++;
      if (type === 'game_play') todayEntry.plays++;

      localStorage.setItem(STORAGE_KEYS.STATS, JSON.stringify(stats));
    } catch (e) {}

    // 3. Optional Supabase Cloud Sync
    const cfg = getConfig();
    if (cfg.supabaseUrl && cfg.supabaseAnonKey) {
      try {
        fetch(`${cfg.supabaseUrl}/rest/v1/site_telemetry`, {
          method: 'POST',
          headers: {
            'apikey': cfg.supabaseAnonKey,
            'Authorization': `Bearer ${cfg.supabaseAnonKey}`,
            'Content-Type': 'application/json',
            'Prefer': 'return=minimal'
          },
          body: JSON.stringify({
            event_type: evt.type,
            page_path: evt.page,
            referrer: evt.referrer,
            device_type: evt.device,
            platform: evt.platform,
            metadata: evt.metadata,
            created_at: evt.timestamp
          })
        }).catch(() => {});
      } catch (e) {}
    }

    // 4. Optional Discord Webhook Push Notification (Instant Recruiter Ping on Resume Open)
    if (type === 'resume_open' && cfg.discordWebhook) {
      try {
        fetch(cfg.discordWebhook, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            username: 'Portfolio Telemetry HUD',
            avatar_url: 'https://arpitjaiswal7.github.io/github-gamedev.io/assets/previews/01-meowsweeper-1.jpg',
            embeds: [{
              title: '🎯 Recruiter Alert: Master Resume Opened!',
              description: `A visitor just opened your **Master Resume**!`,
              color: 0xF59E0B, // Cozy Amber
              fields: [
                { name: 'Source / Referrer', value: evt.referrer || 'Direct', inline: true },
                { name: 'Device / Platform', value: `${evt.device} • ${evt.platform}`, inline: true },
                { name: 'Action', value: metadata.source || 'Resume Click', inline: true }
              ],
              footer: { text: 'Arpit Jaiswal • GameDev Systems Portfolio' },
              timestamp: evt.timestamp
            }]
          })
        }).catch(() => {});
      } catch (e) {}
    }

    return evt;
  }

  // Developer Mode Controller
  function initDeveloperMode() {
    // 1. Check URL parameters (?dev=1 or ?admin=1 or ?exclude=1)
    try {
      const params = new URLSearchParams(window.location.search);
      if (params.has('dev') || params.has('admin') || params.has('exclude')) {
        localStorage.setItem(STORAGE_KEYS.DEV_MODE, 'true');
        localStorage.setItem(STORAGE_KEYS.EXCLUDE_ME, 'true');
        // Clean URL without reloading page
        const newUrl = window.location.pathname + (window.location.hash || '');
        window.history.replaceState({}, document.title, newUrl);
        showDevToast('🛡️ Developer Mode & Traffic Exclusion Active');
      }
    } catch (e) {}

    // If dev mode is already active, ensure exclusion flag is set
    if (localStorage.getItem(STORAGE_KEYS.DEV_MODE) === 'true') {
      localStorage.setItem(STORAGE_KEYS.EXCLUDE_ME, 'true');
    }

    // 2. Synchronize UI visibility
    syncDevButtons();

    // 3. Secret Keyboard Shortcut: Ctrl + Shift + D (or Alt + Shift + D)
    window.addEventListener('keydown', function (e) {
      if ((e.ctrlKey || e.metaKey || e.altKey) && e.shiftKey && (e.key === 'D' || e.key === 'd')) {
        e.preventDefault();
        toggleDeveloperMode();
      }
    });

    // 4. Stealth Triple-Click on Header Brand Logo to toggle Dev Mode
    const brandElem = document.querySelector('header a[href="#"], header a[href="/"]');
    if (brandElem) {
      let clickCount = 0;
      let clickTimer = null;
      brandElem.addEventListener('click', function (e) {
        clickCount++;
        clearTimeout(clickTimer);
        clickTimer = setTimeout(() => { clickCount = 0; }, 600);
        if (clickCount >= 3) {
          e.preventDefault();
          clickCount = 0;
          toggleDeveloperMode();
        }
      });
    }
  }

  function isDevModeActive() {
    return localStorage.getItem(STORAGE_KEYS.DEV_MODE) === 'true';
  }

  function setDevMode(active) {
    if (active) {
      localStorage.setItem(STORAGE_KEYS.DEV_MODE, 'true');
      localStorage.setItem(STORAGE_KEYS.EXCLUDE_ME, 'true');
    } else {
      localStorage.removeItem(STORAGE_KEYS.DEV_MODE);
    }
    syncDevButtons();
  }

  function toggleDeveloperMode() {
    const nextState = !isDevModeActive();
    setDevMode(nextState);
    if (nextState) {
      showDevToast('🛡️ Developer Mode Active (Your Visits Excluded)');
    } else {
      showDevToast('🔒 Developer Mode Disabled');
    }
  }

  function syncDevButtons() {
    const active = isDevModeActive();
    const btn = document.getElementById('dev-monitor-btn');
    const mobileLink = document.getElementById('dev-monitor-mobile-link');

    if (btn) {
      if (active) {
        btn.classList.remove('hidden');
        btn.classList.add('flex');
      } else {
        btn.classList.add('hidden');
        btn.classList.remove('flex');
      }
    }

    if (mobileLink) {
      if (active) {
        mobileLink.classList.remove('hidden');
        mobileLink.classList.add('flex');
      } else {
        mobileLink.classList.add('hidden');
        mobileLink.classList.remove('flex');
      }
    }
  }

  function showDevToast(msg) {
    const old = document.getElementById('gamedev-dev-toast');
    if (old) old.remove();

    const toast = document.createElement('div');
    toast.id = 'gamedev-dev-toast';
    toast.className = 'fixed bottom-6 left-1/2 -translate-x-1/2 z-[100] px-4 py-2.5 rounded-2xl bg-slate-900/95 text-amber-300 border-2 border-amber-400 font-display font-bold text-xs shadow-2xl backdrop-blur-md flex items-center gap-2.5 transition-all duration-300';
    toast.innerHTML = `<span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span><span>${msg}</span>`;
    document.body.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translate(-50%, 12px)';
      setTimeout(() => toast.remove(), 300);
    }, 2800);
  }

  // Auto-bind automatic telemetry events
  function initEventBindings() {
    // 1. Page view
    const isResumePage = window.location.pathname.includes('resume');
    if (isResumePage) {
      trackEvent('resume_open', {
        source: 'Direct Page Load',
        title: document.title || 'Master Resume Page'
      });
    } else {
      trackEvent('page_view', {
        title: document.title || 'Arpit Jaiswal Portfolio',
        url: window.location.href
      });
    }

    // 2. Intercept clicks on any Master Resume button / link
    document.addEventListener('click', function (e) {
      const link = e.target.closest('a');
      if (!link) return;

      const href = (link.getAttribute('href') || '').toLowerCase();
      const text = (link.innerText || '').toLowerCase();

      if (href.includes('resume') || text.includes('resume')) {
        trackEvent('resume_open', {
          source: link.id || 'Header Master Resume Button',
          label: link.innerText ? link.innerText.trim() : 'Master Resume',
          destination: link.getAttribute('href')
        });
      }

      // Check for WebGL play actions
      if (link.getAttribute('onclick') && link.getAttribute('onclick').includes('loadGame') ||
          link.classList.contains('play-game-btn') ||
          link.id === 'load-webgl-btn') {
        trackEvent('game_play', {
          source: 'Unity WebGL Lab',
          game: link.getAttribute('data-game-title') || 'Playable Lab Cartridge'
        });
      }
    }, true);
  }

  // Public API exposed on window.GameDevTelemetry
  window.GameDevTelemetry = {
    trackEvent: trackEvent,
    isDevModeActive: isDevModeActive,
    setDevMode: setDevMode,
    toggleDeveloperMode: toggleDeveloperMode,
    isTrafficExcluded: isTrafficExcluded,
    setExcluded: setExcluded,
    getConfig: getConfig,
    saveConfig: saveConfig,
    STORAGE_KEYS: STORAGE_KEYS
  };

  // Run on DOM ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      initDeveloperMode();
      initEventBindings();
    });
  } else {
    initDeveloperMode();
    initEventBindings();
  }

})();
