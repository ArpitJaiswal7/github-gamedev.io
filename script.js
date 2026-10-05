// ========================================================
// Arpit Jaiswal - Senior Unity & Systems Engineer Portfolio
// Interactive Engine: Unity Workstation + CAD Terminal
// ========================================================

document.addEventListener('DOMContentLoaded', () => {
  initNavigation();
  initResumeDropdown();
  initUnityLab();
  initBlueprintTerminal();
  initLightbox();

  if (window.lucide) {
    lucide.createIcons();
  }
});

// --------------------------------------------------------
// 1. Navigation: Smooth Scrolling & Dynamic Highlighting
// --------------------------------------------------------
let isManualScrolling = false;
let scrollTimeout = null;

function initNavigation() {
  const navLinks = document.querySelectorAll('.nav-link');
  const brandLogo = document.getElementById('brand-logo');
  const mobileToggle = document.getElementById('mobile-menu-toggle');
  const mobileMenu = document.getElementById('mobile-nav-menu');
  const mobileMenuIcon = document.getElementById('mobile-menu-icon');

  // Smooth scroll handler for nav links
  navLinks.forEach(link => {
    link.addEventListener('click', (e) => {
      const targetHash = link.getAttribute('href');
      if (!targetHash || !targetHash.startsWith('#')) return;

      e.preventDefault();
      const targetSection = document.querySelector(targetHash);
      if (!targetSection) return;

      isManualScrolling = true;
      clearTimeout(scrollTimeout);

      // Fixed sticky header height offset (80px + padding)
      const headerOffset = 88;
      const elementPosition = targetSection.getBoundingClientRect().top;
      const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

      window.scrollTo({
        top: offsetPosition,
        behavior: 'smooth'
      });

      // Instantly update active class on clicked link
      setActiveNavLink(targetHash);

      // Update URL hash without causing a page jump
      if (history.pushState) {
        history.pushState(null, '', targetHash);
      }

      // Close mobile menu if open
      if (mobileMenu && !mobileMenu.classList.contains('hidden')) {
        mobileMenu.classList.add('hidden');
        if (mobileMenuIcon) {
          mobileMenuIcon.setAttribute('data-lucide', 'menu');
          if (window.lucide) lucide.createIcons();
        }
      }

      // Reset manual scroll lockout after smooth scroll settles
      scrollTimeout = setTimeout(() => {
        isManualScrolling = false;
      }, 850);
    });
  });

  // Brand logo scroll to top
  if (brandLogo) {
    brandLogo.addEventListener('click', (e) => {
      e.preventDefault();
      window.scrollTo({ top: 0, behavior: 'smooth' });
      removeActiveNavLinks();
      if (history.pushState) {
        history.pushState(null, '', window.location.pathname);
      }
    });
  }

  // Mobile menu toggle
  if (mobileToggle && mobileMenu) {
    mobileToggle.addEventListener('click', (e) => {
      e.stopPropagation();
      const isHidden = mobileMenu.classList.toggle('hidden');
      if (mobileMenuIcon) {
        mobileMenuIcon.setAttribute('data-lucide', isHidden ? 'menu' : 'x');
        if (window.lucide) lucide.createIcons();
      }
    });

    // Close mobile menu if clicked outside
    document.addEventListener('click', (e) => {
      if (!mobileMenu.contains(e.target) && !mobileToggle.contains(e.target)) {
        mobileMenu.classList.add('hidden');
        if (mobileMenuIcon) {
          mobileMenuIcon.setAttribute('data-lucide', 'menu');
          if (window.lucide) lucide.createIcons();
        }
      }
    });
  }

  // Active state synchronization on natural scroll
  const sections = document.querySelectorAll('section[id]');
  window.addEventListener('scroll', () => {
    if (isManualScrolling) return;

    const scrollY = window.pageYOffset;
    const headerOffset = 120;

    sections.forEach(section => {
      const sectionHeight = section.offsetHeight;
      const sectionTop = section.offsetTop - headerOffset;
      const sectionId = '#' + section.getAttribute('id');

      if (scrollY >= sectionTop && scrollY < sectionTop + sectionHeight) {
        setActiveNavLink(sectionId);
      }
    });

    // If at top of page, clear highlights
    if (scrollY < 150) {
      removeActiveNavLinks();
    }
  }, { passive: true });
}

function setActiveNavLink(hash) {
  document.querySelectorAll('.nav-link').forEach(link => {
    if (link.getAttribute('href') === hash) {
      link.classList.add('active');
    } else {
      link.classList.remove('active');
    }
  });
}

function removeActiveNavLinks() {
  document.querySelectorAll('.nav-link').forEach(link => {
    link.classList.remove('active');
  });
}

// --------------------------------------------------------
// 2. Split Download Resume Button & Dropdown
// --------------------------------------------------------
function initResumeDropdown() {
  const container = document.getElementById('resume-dropdown-container');
  const toggleBtn = document.getElementById('resume-dropdown-toggle');
  const menu = document.getElementById('resume-dropdown-menu');
  const caretIcon = document.getElementById('resume-caret-icon');
  const webpageBtn = document.getElementById('btn-webpage-resume');

  if (!toggleBtn || !menu) return;

  function openDropdown() {
    menu.classList.remove('hidden');
    toggleBtn.setAttribute('aria-expanded', 'true');
    if (caretIcon) caretIcon.classList.add('rotate-180');
  }

  function closeDropdown() {
    menu.classList.add('hidden');
    toggleBtn.setAttribute('aria-expanded', 'false');
    if (caretIcon) caretIcon.classList.remove('rotate-180');
  }

  toggleBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    const isClosed = menu.classList.contains('hidden');
    if (isClosed) {
      openDropdown();
    } else {
      closeDropdown();
    }
  });

  // Close dropdown on outside click
  document.addEventListener('click', (e) => {
    if (container && !container.contains(e.target)) {
      closeDropdown();
    }
  });

  // Close on Escape key
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      closeDropdown();
    }
  });

  // Close on selecting any item
  document.querySelectorAll('.resume-menu-item').forEach(item => {
    item.addEventListener('click', () => {
      closeDropdown();
    });
  });

  // Webpage HTML View Feedback Toast
  if (webpageBtn) {
    webpageBtn.addEventListener('click', (e) => {
      e.preventDefault();
      showToast('Interactive Web Resume (.html): Coming soon! Arpit will share the dedicated HTML document shortly. In the meantime, you can download the full Word (.docx) or PDF version above.');
    });
  }
}

// Toast Notification Utility
function showToast(message) {
  let toast = document.getElementById('global-toast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'global-toast';
    toast.className = 'fixed bottom-6 right-6 z-50 max-w-sm p-4 rounded-xl bg-slate-900 border border-sky-500/40 text-slate-100 text-xs shadow-2xl transition-all duration-300 transform translate-y-4 opacity-0 flex items-start gap-3 backdrop-blur-md';
    document.body.appendChild(toast);
  }

  toast.innerHTML = `
    <div class="p-1 rounded-md bg-sky-500/20 text-sky-400 mt-0.5">
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
    </div>
    <div class="flex-1 leading-relaxed">${message}</div>
    <button onclick="this.parentElement.classList.add('opacity-0', 'translate-y-4')" class="text-slate-400 hover:text-white">&times;</button>
  `;

  // Trigger animation
  requestAnimationFrame(() => {
    toast.classList.remove('opacity-0', 'translate-y-4');
    toast.classList.add('opacity-100', 'translate-y-0');
  });

  setTimeout(() => {
    toast.classList.remove('opacity-100', 'translate-y-0');
    toast.classList.add('opacity-0', 'translate-y-4');
  }, 4500);
}

// --------------------------------------------------------
// 3. Unity Lab: Interactive Workstation & Mechanics
// --------------------------------------------------------
let flameAnimationId = null;

function initUnityLab() {
  const mechanicButtons = document.querySelectorAll('.mechanic-selector-btn');
  const meowdokuView = document.getElementById('meowdoku-game-view');
  const jigsawView = document.getElementById('jigsaw-game-view');
  const flameView = document.getElementById('flame-game-view');
  const aspectSelect = document.getElementById('viewport-aspect-select');
  const viewportScreen = document.getElementById('active-viewport-screen');
  const btnMaximize = document.getElementById('btn-maximize-viewport');
  const consoleLogs = document.getElementById('unity-console-logs');
  const btnClearConsole = document.getElementById('btn-clear-console');

  function logConsole(message, type = 'info') {
    if (!consoleLogs) return;
    const time = new Date().toLocaleTimeString('en-US', { hour12: false });
    const logItem = document.createElement('div');
    const color = type === 'warning' ? 'text-amber-400' : (type === 'success' ? 'text-emerald-400' : 'text-slate-300');
    logItem.className = `flex items-center gap-2 ${color}`;
    logItem.innerHTML = `<span class="text-slate-500 font-mono text-[10px]">[${time}]</span> <span>${message}</span>`;
    consoleLogs.appendChild(logItem);
    consoleLogs.scrollTop = consoleLogs.scrollHeight;
  }

  // Initial welcome log
  logConsole('Unity 2026.1 LTS - Ready. Script Assemblies reloaded.', 'success');
  logConsole('Meowdoku solver initialized. Deterministic hint tree ready.');

  // Mechanic switching
  mechanicButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      mechanicButtons.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      const mechanic = btn.getAttribute('data-mechanic');
      if (flameAnimationId) {
        cancelAnimationFrame(flameAnimationId);
        flameAnimationId = null;
      }

      if (mechanic === 'meowdoku') {
        meowdokuView.classList.remove('hidden');
        meowdokuView.classList.add('flex');
        jigsawView.classList.add('hidden');
        flameView.classList.add('hidden');
        logConsole('Switched active scene to: Meowdoku Solver (Discrete Multi-touch)');
      } else if (mechanic === 'jigsaw') {
        meowdokuView.classList.add('hidden');
        jigsawView.classList.remove('hidden');
        jigsawView.classList.add('flex');
        flameView.classList.add('hidden');
        renderJigsawMesh();
        logConsole('Switched active scene to: Jigsaw 9-Signature Procedural Mesh');
      } else if (mechanic === 'flame') {
        meowdokuView.classList.add('hidden');
        jigsawView.classList.add('hidden');
        flameView.classList.remove('hidden');
        flameView.classList.add('flex');
        startFlameSimulation();
        logConsole('Switched active scene to: Flame Noise Procedural Shader');
      }
    });
  });

  // Viewport aspect ratio switching
  if (aspectSelect && viewportScreen) {
    aspectSelect.addEventListener('change', () => {
      const mode = aspectSelect.value;
      viewportScreen.classList.remove('viewport-portrait', 'viewport-landscape', 'viewport-free');
      if (mode === 'portrait') {
        viewportScreen.classList.add('viewport-portrait');
        logConsole('Camera projection updated: 9:16 Portrait (Mobile Native)');
      } else if (mode === 'landscape') {
        viewportScreen.classList.add('viewport-landscape');
        logConsole('Camera projection updated: 16:9 Landscape');
      } else {
        viewportScreen.classList.add('viewport-free');
        logConsole('Camera projection updated: Free Aspect Dynamic Scale');
      }
    });
  }

  // Maximize button toggles free aspect
  if (btnMaximize && aspectSelect && viewportScreen) {
    btnMaximize.addEventListener('click', () => {
      if (viewportScreen.classList.contains('viewport-free')) {
        aspectSelect.value = 'portrait';
        aspectSelect.dispatchEvent(new Event('change'));
      } else {
        aspectSelect.value = 'free';
        aspectSelect.dispatchEvent(new Event('change'));
      }
    });
  }

  // Play / Pause / Reload buttons
  const btnPlay = document.getElementById('unity-btn-play');
  const btnPause = document.getElementById('unity-btn-pause');
  const btnReload = document.getElementById('unity-btn-reload');

  if (btnPlay) {
    btnPlay.addEventListener('click', () => {
      logConsole('PlayMode started: 60.0 FPS, V-Sync enabled, 0 GC allocations.', 'success');
    });
  }
  if (btnPause) {
    btnPause.addEventListener('click', () => {
      logConsole('PlayMode paused (TimeScale: 0.0)', 'warning');
    });
  }
  if (btnReload) {
    btnReload.addEventListener('click', () => {
      logConsole('Compiling scripts & reloading domain assemblies...', 'info');
      setTimeout(() => {
        logConsole('Assembly reload complete: 0 errors, 0 warnings.', 'success');
      }, 400);
    });
  }

  // Clear console
  if (btnClearConsole && consoleLogs) {
    btnClearConsole.addEventListener('click', () => {
      consoleLogs.innerHTML = '';
      logConsole('Console cleared.');
    });
  }

  // Initialize mini Meowdoku board
  initMeowdokuBoard(logConsole);
}

// --------------------------------------------------------
// 4. Meowdoku Mini Demo Logic
// --------------------------------------------------------
function initMeowdokuBoard(logger) {
  const board = document.getElementById('meowdoku-board');
  const btnHint = document.getElementById('btn-meowdoku-hint');
  const btnReset = document.getElementById('btn-meowdoku-reset');
  if (!board) return;

  const gridSize = 5;
  // 5x5 board state: 0 = empty, 1 = queen 🐱, 2 = cross ✕
  let boardState = [
    [0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0],
    [0, 0, 0, 0, 0]
  ];

  function renderGrid() {
    board.innerHTML = '';
    for (let r = 0; r < gridSize; r++) {
      for (let c = 0; c < gridSize; c++) {
        const cell = document.createElement('div');
        cell.className = 'meow-cell';
        cell.dataset.r = r;
        cell.dataset.c = c;

        const val = boardState[r][c];
        if (val === 1) {
          cell.textContent = '🐱';
        } else if (val === 2) {
          cell.textContent = '✕';
          cell.classList.add('marked-cross');
        } else {
          cell.textContent = '';
        }

        // Left-click cycles: Empty -> 🐱 -> ✕ -> Empty
        cell.addEventListener('click', () => {
          boardState[r][c] = (boardState[r][c] + 1) % 3;
          renderGrid();
          if (logger) {
            const sym = boardState[r][c] === 1 ? '🐱 Queen' : (boardState[r][c] === 2 ? '✕ Cross' : 'Empty');
            logger(`User touch event: Cell (${r}, ${c}) set to ${sym}`);
          }
        });

        // Right click toggles cross
        cell.addEventListener('contextmenu', (e) => {
          e.preventDefault();
          boardState[r][c] = boardState[r][c] === 2 ? 0 : 2;
          renderGrid();
        });

        board.appendChild(cell);
      }
    }
  }

  // Hint cascade demo
  if (btnHint) {
    btnHint.addEventListener('click', () => {
      btnHint.disabled = true;
      btnHint.classList.add('opacity-50');
      if (logger) logger('Executing deterministic hint technique cascade...');

      setTimeout(() => {
        // Find first empty cell
        let targetR = -1, targetC = -1;
        for (let r = 0; r < gridSize; r++) {
          for (let c = 0; c < gridSize; c++) {
            if (boardState[r][c] === 0) {
              targetR = r;
              targetC = c;
              break;
            }
          }
          if (targetR !== -1) break;
        }

        if (targetR !== -1) {
          const cellEl = board.querySelector(`[data-r="${targetR}"][data-c="${targetC}"]`);
          if (cellEl) {
            cellEl.classList.add('highlight-hint');
            if (logger) logger(`[Technique 1: Row Scan] Discovered forced candidate at (${targetR}, ${targetC}).`, 'success');
          }

          setTimeout(() => {
            boardState[targetR][targetC] = 1;
            renderGrid();
            btnHint.disabled = false;
            btnHint.classList.remove('opacity-50');
            if (logger) logger(`Placed guaranteed Queen at (${targetR}, ${targetC}). Locks released.`, 'success');
          }, 600);
        } else {
          btnHint.disabled = false;
          btnHint.classList.remove('opacity-50');
          if (logger) logger('All grid cells occupied. Reset to replay.', 'warning');
        }
      }, 300);
    });
  }

  if (btnReset) {
    btnReset.addEventListener('click', () => {
      boardState = [
        [0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0],
        [0, 0, 0, 0, 0]
      ];
      renderGrid();
      if (logger) logger('Meowdoku board state cleared.');
    });
  }

  renderGrid();
}

// --------------------------------------------------------
// 5. Jigsaw Procedural Mesh Canvas Preview
// --------------------------------------------------------
function renderJigsawMesh() {
  const canvas = document.getElementById('jigsaw-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const w = canvas.width;
  const h = canvas.height;

  ctx.fillStyle = '#0f172a';
  ctx.fillRect(0, 0, w, h);

  const cols = 3;
  const rows = 3;
  const cellW = w / cols;
  const cellH = h / rows;

  ctx.strokeStyle = '#38bdf8';
  ctx.lineWidth = 2.5;

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const x = c * cellW;
      const y = r * cellH;

      ctx.save();
      ctx.translate(x, y);

      // Draw jigsaw piece boundary with simulated tab/blank Bezier bumps
      ctx.beginPath();
      ctx.rect(4, 4, cellW - 8, cellH - 8);
      ctx.fillStyle = (r + c) % 2 === 0 ? 'rgba(56, 189, 248, 0.12)' : 'rgba(245, 158, 11, 0.12)';
      ctx.fill();
      ctx.stroke();

      // Draw center signature ID
      ctx.fillStyle = '#94a3b8';
      ctx.font = '10px monospace';
      ctx.textAlign = 'center';
      ctx.fillText(`SIG-${r * 3 + c + 1}`, cellW / 2, cellH / 2 + 4);

      ctx.restore();
    }
  }

  // Draw procedural connector tabs
  ctx.fillStyle = '#38bdf8';
  ctx.beginPath();
  ctx.arc(cellW, cellH / 2, 7, 0, Math.PI * 2);
  ctx.arc(cellW * 2, cellH * 1.5, 7, 0, Math.PI * 2);
  ctx.fill();
}

// --------------------------------------------------------
// 6. Procedural Flame Noise Simulation
// --------------------------------------------------------
function startFlameSimulation() {
  const canvas = document.getElementById('flame-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const w = canvas.width;
  const h = canvas.height;

  const particles = [];
  for (let i = 0; i < 40; i++) {
    particles.push({
      x: w / 2 + (Math.random() - 0.5) * 60,
      y: h - 20 - Math.random() * 80,
      vx: (Math.random() - 0.5) * 1.5,
      vy: -1.5 - Math.random() * 2.5,
      radius: 12 + Math.random() * 16,
      life: Math.random() * 0.8 + 0.2
    });
  }

  function loop() {
    ctx.fillStyle = '#080c14';
    ctx.fillRect(0, 0, w, h);

    particles.forEach(p => {
      p.x += p.vx;
      p.y += p.vy;
      p.life -= 0.02;
      p.radius *= 0.98;

      if (p.life <= 0 || p.y < 20) {
        p.x = w / 2 + (Math.random() - 0.5) * 60;
        p.y = h - 25;
        p.vx = (Math.random() - 0.5) * 1.5;
        p.vy = -1.5 - Math.random() * 2.5;
        p.radius = 12 + Math.random() * 16;
        p.life = 1.0;
      }

      const grad = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.radius);
      grad.addColorStop(0, 'rgba(255, 200, 50, 0.8)');
      grad.addColorStop(0.5, 'rgba(255, 80, 0, 0.5)');
      grad.addColorStop(1, 'rgba(100, 0, 0, 0)');

      ctx.fillStyle = grad;
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
      ctx.fill();
    });

    flameAnimationId = requestAnimationFrame(loop);
  }

  loop();
}

// --------------------------------------------------------
// 7. Blueprint Split-View Terminal (Zone 3)
// --------------------------------------------------------
const blueprintsData = [
  {
    id: 'sheet-02',
    sheetNum: 'Sheet 02 — Surface Cut',
    title: 'Native Image Pipeline',
    domain: 'Platform Services',
    engine: 'Unity (Android + iOS)',
    desc: 'Fetches, resizes, and caches remote images across Android, iOS, and Unity Editor through one shared C# API surface.',
    img: 'assets/blueprints/sheet-02-native-image-pipeline.png'
  },
  {
    id: 'sheet-09',
    sheetNum: 'Sheet 09 — Surface Cut',
    title: 'Jigsaw Board Renderer',
    domain: 'Core Gameplay Logic',
    engine: 'Unity',
    desc: 'Dynamic procedural N×N board generator using 9 tab/blank edge signatures with custom shader UV quad projections.',
    img: 'assets/blueprints/sheet-09-jigsaw-board-renderer.png'
  },
  {
    id: 'sheet-10',
    sheetNum: 'Sheet 10 — Surface Cut',
    title: 'Meowdoku Engine & Solver',
    domain: 'Core Gameplay Logic',
    engine: 'Unity',
    desc: 'Multi-touch gesture router with input locks during animations and an ordered hint-technique cascade.',
    img: 'assets/blueprints/sheet-10-meowdoku.png'
  },
  {
    id: 'sheet-06',
    sheetNum: 'Sheet 06 — Surface Cut',
    title: 'Offline Leaderboard',
    domain: 'LiveOps / Retention',
    engine: 'Unity',
    desc: 'Zero-server-cost bot simulation engine generating dynamic seasonal leagues, rank advancement, and anti-rollback clamping.',
    img: 'assets/blueprints/sheet-06-offline-leaderboard.png'
  },
  {
    id: 'sheet-08',
    sheetNum: 'Sheet 08 — Surface Cut',
    title: 'Level Sync (OTA Content)',
    domain: 'Platform Services',
    engine: 'Unity',
    desc: 'Background manifest-diffing synchronizer that never blocks game boot sequence, applying content cleanly on subsequent launch.',
    img: 'assets/blueprints/sheet-08-level-sync.png'
  },
  {
    id: 'sheet-04',
    sheetNum: 'Sheet 04 — Surface Cut',
    title: 'IAP System Gateway',
    domain: 'Monetization',
    engine: 'Unity (Android + iOS)',
    desc: 'Dual-currency store gateway (real money vs soft currency) with strict grant-then-confirm verification.',
    img: 'assets/blueprints/sheet-04-iap-system.png'
  },
  {
    id: 'sheet-13',
    sheetNum: 'Sheet 13 — Surface Cut',
    title: 'Argos Localization IPC',
    domain: 'Editor Tooling / IPC',
    engine: 'Unity + Python (offline)',
    desc: 'Cross-process communication tool linking Unity C# to an offline Python translation subprocess over stdin/stdout JSON codecs.',
    img: 'assets/blueprints/sheet-13-argos-localization.png'
  },
  {
    id: 'sheet-11',
    sheetNum: 'Sheet 11 — Surface Cut',
    title: 'Asset Organizer Tool',
    domain: 'Unity Editor DX',
    engine: 'Unity Editor',
    desc: 'Developer productivity tool adding project window asset pinning, custom category badges, and asset movement watchers.',
    img: 'assets/blueprints/sheet-11-asset-organizer-tool.png'
  },
  {
    id: 'sheet-12',
    sheetNum: 'Sheet 12 — Surface Cut',
    title: 'Flame Flicker Effect',
    domain: 'Tech Art & Shaders',
    engine: 'Unity',
    desc: 'Lightweight 2-file procedural noise shader and C# driver creating dynamic ambient fire visuals on UI canvases.',
    img: 'assets/blueprints/sheet-12-flame-flicker-effect.png'
  }
];

function initBlueprintTerminal() {
  const navContainer = document.getElementById('blueprint-nav-list');
  const stageImg = document.getElementById('bp-stage-img');
  const stageTitle = document.getElementById('bp-stage-title');
  const stageDomain = document.getElementById('bp-stage-domain');
  const stageDesc = document.getElementById('bp-stage-desc');
  const zoomBtn = document.getElementById('btn-zoom-blueprint');

  if (!navContainer) return;

  blueprintsData.forEach((bp, idx) => {
    const item = document.createElement('button');
    item.className = `w-full text-left p-2.5 rounded-lg border transition-all flex items-center justify-between ${idx === 0 ? 'bg-[#1a2333] border-sky-500/50 text-white' : 'bg-[#0f141f] border-slate-800 text-slate-400 hover:text-slate-200 hover:bg-[#161c28]'}`;
    item.setAttribute('data-index', idx);

    item.innerHTML = `
      <div>
        <div class="font-bold text-xs">${bp.title}</div>
        <div class="text-[10px] text-slate-500 font-mono">${bp.domain}</div>
      </div>
      <span class="text-[10px] px-1.5 py-0.5 rounded bg-slate-800 text-sky-400 font-mono">${bp.sheetNum.split(' ')[1]}</span>
    `;

    item.addEventListener('click', () => {
      document.querySelectorAll('#blueprint-nav-list button').forEach(b => {
        b.className = 'w-full text-left p-2.5 rounded-lg border transition-all flex items-center justify-between bg-[#0f141f] border-slate-800 text-slate-400 hover:text-slate-200 hover:bg-[#161c28]';
      });
      item.className = 'w-full text-left p-2.5 rounded-lg border transition-all flex items-center justify-between bg-[#1a2333] border-sky-500/50 text-white';

      stageTitle.textContent = bp.title;
      stageDomain.textContent = `INDEPENDENT SYSTEM — ${bp.domain.toUpperCase()}`;
      stageDesc.textContent = bp.desc;
      stageImg.src = bp.img;
    });

    navContainer.appendChild(item);
  });

  if (zoomBtn && stageImg) {
    zoomBtn.addEventListener('click', () => {
      const modal = document.getElementById('lightbox-modal');
      const modalImg = document.getElementById('lightbox-img');
      const modalTitle = document.getElementById('lightbox-title');
      if (modal && modalImg) {
        modalTitle.textContent = stageTitle.textContent;
        modalImg.src = stageImg.src;
        modal.classList.remove('hidden');
        modal.classList.add('flex');
      }
    });
  }
}

// --------------------------------------------------------
// 8. Lightbox Inspection Modal
// --------------------------------------------------------
function initLightbox() {
  const modal = document.getElementById('lightbox-modal');
  const closeBtn = document.getElementById('lightbox-close');

  if (modal && closeBtn) {
    closeBtn.addEventListener('click', () => {
      modal.classList.add('hidden');
      modal.classList.remove('flex');
    });

    modal.addEventListener('click', (e) => {
      if (e.target === modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
      }
    });

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
      }
    });
  }
}
