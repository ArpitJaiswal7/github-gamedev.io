/**
 * Cozy Telemetry & Website Monitor Engine
 * Arpit Jaiswal — Senior Unity & Gameplay Systems Developer
 * 
 * Capabilities:
 * 1. Respectful zero-cookie visitor & telemetry tracking (page views, resume opens, game plays).
 * 2. Automatic & privacy-friendly IP Geolocation resolution (city, region, country, flag emoji with zero user prompts).
 * 3. Instant local aggregation + Supabase REST API & Discord webhook integration.
 * 4. Stealth Developer Mode toggle (hidden from regular visitors, activated via URL param, hotkey Ctrl+Shift+D, or stealth triple-click).
 * 5. Automatic & Persistent Developer Traffic Exclusion: Ignores visits and clicks from developer's own devices/laptop.
 * 6. Hard Clean Slate: Unconditionally purges all legacy / historical / test data.
 */

(function () {
  'use strict';

  // ⚙️ GLOBAL CLOUD CONFIGURATION (Shared by all external visitors)
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
    GEO_CACHE: '_gamedev_geo_cache',
    CLEAN_FLAG: '_gamedev_clean_slate_v3'
  };

  // Cached Geolocation Object
  let cachedGeo = null;

  // Asynchronously resolve visitor city and country via zero-prompt IP lookup
  async function resolveVisitorGeo() {
    if (cachedGeo) return cachedGeo;

    // Check session cache
    try {
      const stored = sessionStorage.getItem(STORAGE_KEYS.GEO_CACHE);
      if (stored) {
        cachedGeo = JSON.parse(stored);
        return cachedGeo;
      }
    } catch (e) {}

    // 1. Primary IP Geo provider: ipwho.is (CORS-friendly, zero API key required)
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 2500);
      const res = await fetch('https://ipwho.is/', { signal: controller.signal, cache: 'force-cache' });
      clearTimeout(timeoutId);
      if (res.ok) {
        const data = await res.json();
        if (data.success !== false) {
          cachedGeo = {
            city: data.city || 'Unknown City',
            region: data.region || '',
            country: data.country || 'Global Visitor',
            countryCode: data.country_code || '',
            flag: data.flag && data.flag.emoji ? data.flag.emoji : '📍'
          };
          try {
            sessionStorage.setItem(STORAGE_KEYS.GEO_CACHE, JSON.stringify(cachedGeo));
          } catch (e) {}
          return cachedGeo;
        }
      }
    } catch (err) {}

    // 2. Secondary fallback: freeipapi.com
    try {
      const controller2 = new AbortController();
      const timeoutId2 = setTimeout(() => controller2.abort(), 2000);
      const res2 = await fetch('https://freeipapi.com/api/json', { signal: controller2.signal });
      clearTimeout(timeoutId2);
      if (res2.ok) {
        const d2 = await res2.json();
        cachedGeo = {
          city: d2.cityName || 'Unknown City',
          region: d2.regionName || '',
          country: d2.countryName || 'Global Visitor',
          countryCode: d2.countryCode || '',
          flag: '📍'
        };
        try {
          sessionStorage.setItem(STORAGE_KEYS.GEO_CACHE, JSON.stringify(cachedGeo));
        } catch (e) {}
        return cachedGeo;
      }
    } catch (e2) {}

    // Default fallback if offline or blocked
    cachedGeo = {
      city: 'Unknown City',
      region: '',
      country: 'Global Visitor',
      countryCode: '',
      flag: '🌐'
    };
    return cachedGeo;
  }

  // Pre-warm geo lookup immediately on script load
  resolveVisitorGeo();

  // Hard Reset: Unconditionally wipes all data and sets pure zero state
  function resetAllData() {
    const todayLabel = new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    const zeroStats = {
      totalVisitors: 0,
      pageViews: 0,
      resumeOpens: 0,
      gamePlays: 0,
      avgDuration: '0s',
      dailyTrend: [{ date: todayLabel, visitors: 0, resumeOpens: 0, plays: 0 }],
      referrers: {},
      devices: {},
      cities: {},
      countries: {},
      games: {}
    };
    try {
      localStorage.setItem(STORAGE_KEYS.STATS, JSON.stringify(zeroStats));
      localStorage.setItem(STORAGE_KEYS.EVENTS, JSON.stringify([]));
      sessionStorage.removeItem(STORAGE_KEYS.SESSION_ID);
      sessionStorage.removeItem('_gamedev_counted_visitor');
    } catch (e) {}
  }

  // Mandatory one-time automatic purge of all legacy / test data across any browser visiting the site
  function enforceCleanSlate() {
    try {
      if (localStorage.getItem(STORAGE_KEYS.CLEAN_FLAG) !== 'purged') {
        resetAllData();
        localStorage.setItem(STORAGE_KEYS.CLEAN_FLAG, 'purged');
      }
    } catch (e) {}
  }

  enforceCleanSlate();

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

  function setExcluded(excluded) {
    if (excluded) {
      localStorage.setItem(STORAGE_KEYS.EXCLUDE_ME, 'true');
    } else {
      localStorage.removeItem(STORAGE_KEYS.EXCLUDE_ME);
    }
  }

  function getSessionId() {
    let sid = sessionStorage.getItem(STORAGE_KEYS.SESSION_ID);
    if (!sid) {
      sid = 'sid_' + Math.random().toString(36).substring(2, 9) + '_' + Date.now().toString(36);
      sessionStorage.setItem(STORAGE_KEYS.SESSION_ID, sid);
    }
    return sid;
  }

  function getDeviceType() {
    const ua = navigator.userAgent || '';
    if (/(tablet|ipad|playbook|silk)|(android(?!.*mobi))/i.test(ua)) return 'Tablet';
    if (/Mobile|iP(hone|od)|Android|BlackBerry|IEMobile|Kindle|Silk-Accelerated/i.test(ua)) return 'Mobile';
    return 'Desktop';
  }

  function getPlatform() {
    const ua = navigator.userAgent || '';
    if (/Windows/i.test(ua)) return 'Windows';
    if (/Macintosh|Mac OS X/i.test(ua)) return 'macOS';
    if (/iPhone|iPad|iPod/i.test(ua)) return 'iOS';
    if (/Android/i.test(ua)) return 'Android';
    if (/Linux/i.test(ua)) return 'Linux';
    return 'Unknown';
  }

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

  function saveConfig(cfg) {
    try {
      localStorage.setItem(STORAGE_KEYS.CONFIG, JSON.stringify(cfg));
    } catch (e) {}
  }

  // Core event logging function
  async function trackEvent(type, metadata = {}) {
    // 🛡️ EXCLUSION CHECK
    const isForced = metadata && metadata.force === true;
    if (!isForced && isTrafficExcluded()) {
      return null;
    }

    // Resolve or retrieve cached geo
    let geo = cachedGeo;
    if (!geo) {
      geo = await resolveVisitorGeo();
    }

    // Allow metadata override for simulations
    if (metadata.city) geo = { ...geo, city: metadata.city, country: metadata.country || geo.country, countryCode: metadata.countryCode || geo.countryCode, flag: metadata.flag || geo.flag };

    const evt = {
      id: 'evt_' + Date.now().toString(36) + '_' + Math.random().toString(36).substring(2, 6),
      timestamp: new Date().toISOString(),
      type: type,
      page: window.location.pathname || '/',
      referrer: getCleanReferrer(),
      device: getDeviceType(),
      platform: getPlatform(),
      sessionId: getSessionId(),
      city: geo.city || 'Unknown City',
      region: geo.region || '',
      country: geo.country || 'Global Visitor',
      countryCode: geo.countryCode || '',
      flag: geo.flag || '📍',
      metadata: metadata
    };

    // 1. Update local storage event log
    try {
      const rawEvents = localStorage.getItem(STORAGE_KEYS.EVENTS);
      const events = rawEvents ? JSON.parse(rawEvents) : [];
      events.unshift(evt);
      if (events.length > 300) events.length = 300;
      localStorage.setItem(STORAGE_KEYS.EVENTS, JSON.stringify(events));
    } catch (e) {}

    // 2. Update aggregate metrics
    try {
      const rawStats = localStorage.getItem(STORAGE_KEYS.STATS);
      const stats = rawStats ? JSON.parse(rawStats) : {
        totalVisitors: 0, pageViews: 0, resumeOpens: 0, gamePlays: 0,
        dailyTrend: [], referrers: {}, devices: {}, cities: {}, countries: {}, games: {}
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

      // Record city & country counts
      if (evt.city && evt.city !== 'Unknown City') {
        const cityKey = `${evt.flag} ${evt.city}, ${evt.countryCode || evt.country}`;
        stats.cities = stats.cities || {};
        stats.cities[cityKey] = (stats.cities[cityKey] || 0) + 1;
      }

      if (evt.country && evt.country !== 'Global Visitor') {
        stats.countries = stats.countries || {};
        stats.countries[evt.country] = (stats.countries[evt.country] || 0) + 1;
      }

      // Update today's entry in daily trend
      const todayLabel = new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      stats.dailyTrend = stats.dailyTrend || [];
      let todayEntry = stats.dailyTrend[stats.dailyTrend.length - 1];
      if (!todayEntry || todayEntry.date !== todayLabel) {
        todayEntry = { date: todayLabel, visitors: 0, resumeOpens: 0, plays: 0 };
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
            city: evt.city,
            country: evt.country,
            country_code: evt.countryCode,
            metadata: evt.metadata,
            created_at: evt.timestamp
          })
        }).catch(() => {});
      } catch (e) {}
    }

    // 4. Optional Discord Webhook Push Notification (Instant Recruiter Ping on Resume Open with City!)
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
                { name: '📍 City / Location', value: `${evt.flag} ${evt.city}, ${evt.country}`, inline: true },
                { name: 'Source / Referrer', value: evt.referrer || 'Direct', inline: true },
                { name: 'Device / Platform', value: `${evt.device} • ${evt.platform}`, inline: true }
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
    try {
      const params = new URLSearchParams(window.location.search);
      if (params.has('dev') || params.has('admin') || params.has('exclude')) {
        localStorage.setItem(STORAGE_KEYS.DEV_MODE, 'true');
        localStorage.setItem(STORAGE_KEYS.EXCLUDE_ME, 'true');
        const newUrl = window.location.pathname + (window.location.hash || '');
        window.history.replaceState({}, document.title, newUrl);
        showDevToast('🛡️ Developer Mode & Traffic Exclusion Active');
      }
    } catch (e) {}

    if (localStorage.getItem(STORAGE_KEYS.DEV_MODE) === 'true') {
      localStorage.setItem(STORAGE_KEYS.EXCLUDE_ME, 'true');
    }

    syncDevButtons();

    window.addEventListener('keydown', function (e) {
      if ((e.ctrlKey || e.metaKey || e.altKey) && e.shiftKey && (e.key === 'D' || e.key === 'd')) {
        e.preventDefault();
        toggleDeveloperMode();
      }
    });

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

  function initEventBindings() {
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

  // Public API
  window.GameDevTelemetry = {
    trackEvent: trackEvent,
    resolveVisitorGeo: resolveVisitorGeo,
    isDevModeActive: isDevModeActive,
    setDevMode: setDevMode,
    toggleDeveloperMode: toggleDeveloperMode,
    isTrafficExcluded: isTrafficExcluded,
    setExcluded: setExcluded,
    resetAllData: resetAllData,
    getConfig: getConfig,
    saveConfig: saveConfig,
    STORAGE_KEYS: STORAGE_KEYS
  };

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
