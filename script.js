// ========================================================
// Arpit Jaiswal - Senior Unity & Systems Engineer Portfolio
// Interactive Engine: Unity Workstation + CAD Terminal
// ========================================================

document.addEventListener('DOMContentLoaded', () => {
  initUnityWorkstation();
  initBlueprintTerminal();
  initMeowdokuGame();
  initFlameShaderDemo();
  initJigsawClassifierDemo();
  initLightbox();
});

// --------------------------------------------------------
// 1. Unity Editor Workstation Interactivity (Zone 2)
// --------------------------------------------------------
function initUnityWorkstation() {
  const aspectSelect = document.getElementById('viewport-aspect-select');
  const viewportScreen = document.getElementById('active-viewport-screen');
  const mechanicBtns = document.querySelectorAll('.mechanic-selector-btn');
  const consoleBox = document.getElementById('unity-console-logs');
  const clearConsoleBtn = document.getElementById('btn-clear-console');

  // Aspect Ratio Switching
  if (aspectSelect && viewportScreen) {
    aspectSelect.addEventListener('change', (e) => {
      const val = e.target.value;
      viewportScreen.classList.remove('viewport-portrait', 'viewport-landscape', 'viewport-free');
      if (val === 'portrait') {
        viewportScreen.classList.add('viewport-portrait');
        addConsoleLog(`[Display] Aspect ratio set to 9:16 Portrait (Mobile) [250x440]`);
      } else if (val === 'landscape') {
        viewportScreen.classList.add('viewport-landscape');
        addConsoleLog(`[Display] Aspect ratio set to 16:9 Landscape`);
      } else {
        viewportScreen.classList.add('viewport-free');
        addConsoleLog(`[Display] Aspect ratio set to Free Aspect`);
      }
    });
  }

  // Hierarchy Tree Mechanic Selection
  const views = {
    meowdoku: document.getElementById('meowdoku-game-view'),
    jigsaw: document.getElementById('jigsaw-game-view'),
    flame: document.getElementById('flame-game-view')
  };

  mechanicBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      mechanicBtns.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      const target = btn.getAttribute('data-mechanic');
      Object.keys(views).forEach(key => {
        if (views[key]) {
          if (key === target) {
            views[key].classList.remove('hidden');
            views[key].classList.add('flex');
          } else {
            views[key].classList.add('hidden');
            views[key].classList.remove('flex');
          }
        }
      });

      addConsoleLog(`[SceneManager] Loaded Scene: Core_${target.toUpperCase()}_Sandbox`);
    });
  });

  // Clear Console
  if (clearConsoleBtn && consoleBox) {
    clearConsoleBtn.addEventListener('click', () => {
      consoleBox.innerHTML = '';
      addConsoleLog(`[Console] Cleared logs. Standing by.`);
    });
  }

  // Maximize Viewport
  const maxBtn = document.getElementById('btn-maximize-viewport');
  if (maxBtn) {
    maxBtn.addEventListener('click', () => {
      const modal = document.getElementById('lightbox-modal');
      const modalImg = document.getElementById('lightbox-img');
      const modalTitle = document.getElementById('lightbox-title');
      if (modal && modalImg) {
        modalTitle.textContent = "Viewport Fullscreen Mode [Press ESC to return]";
        modalImg.src = "assets/games/meowsweeper-banner.jpg";
        modal.classList.remove('hidden');
        modal.classList.add('flex');
      }
    });
  }

  addConsoleLog(`[Engine] Unity 2026.1 LTS Engine initialized.`);
  addConsoleLog(`[Profiler] Initial memory: 18.2 MB | Draw Calls: 14 | V-Sync: 60 FPS.`);
}

function addConsoleLog(msg, type = 'info') {
  const box = document.getElementById('unity-console-logs');
  if (!box) return;
  const line = document.createElement('div');
  line.className = 'py-0.5 flex items-start gap-1.5';
  
  const time = new Date().toTimeString().split(' ')[0];
  line.innerHTML = `<span class="text-slate-500">[${time}]</span> <span class="${type === 'warn' ? 'text-amber-400' : 'text-slate-300'}">${msg}</span>`;
  box.appendChild(line);
  box.scrollTop = box.scrollHeight;
}

// --------------------------------------------------------
// 2. Blueprint Split-View Terminal (Zone 3)
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
      addConsoleLog(`[BlueprintTerminal] Swapped active schematic to: ${bp.title}`);
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
// 3. Meowdoku Solver Logic (Playable)
// --------------------------------------------------------
function initMeowdokuGame() {
  const container = document.getElementById('meowdoku-board');
  const hintBtn = document.getElementById('btn-meowdoku-hint');
  const resetBtn = document.getElementById('btn-meowdoku-reset');
  if (!container) return;

  const N = 5;
  const regions = [
    [0, 0, 1, 1, 1],
    [0, 0, 2, 2, 1],
    [0, 3, 2, 2, 4],
    [3, 3, 3, 4, 4],
    [3, 3, 4, 4, 4]
  ];

  const regionColors = [
    'rgba(239, 68, 68, 0.2)',
    'rgba(59, 130, 246, 0.2)',
    'rgba(16, 185, 129, 0.2)',
    'rgba(245, 158, 11, 0.2)',
    'rgba(168, 85, 247, 0.2)'
  ];

  const solution = [
    [0, 1, 0, 0, 0],
    [0, 0, 0, 1, 0],
    [1, 0, 0, 0, 0],
    [0, 0, 1, 0, 0],
    [0, 0, 0, 0, 1]
  ];

  let grid = Array(N).fill(null).map(() => Array(N).fill(0));

  function render() {
    container.innerHTML = '';
    for (let r = 0; r < N; r++) {
      for (let c = 0; c < N; c++) {
        const cell = document.createElement('button');
        cell.className = 'w-9 h-9 sm:w-11 sm:h-11 flex items-center justify-center font-bold text-base rounded border border-slate-700/60 transition-all select-none hover:brightness-125';
        cell.style.backgroundColor = regionColors[regions[r][c]];

        if (grid[r][c] === 1) {
          cell.innerHTML = '🐱';
          cell.classList.add('bg-sky-500/40', 'border-sky-400');
        } else if (grid[r][c] === -1) {
          cell.innerHTML = '<span class="text-slate-500 text-xs">✕</span>';
        }

        cell.addEventListener('click', () => {
          grid[r][c] = (grid[r][c] === 1) ? 0 : 1;
          addConsoleLog(`[Meowdoku] Placed input at [Row ${r+1}, Col ${c+1}]`);
          render();
        });

        cell.addEventListener('contextmenu', (e) => {
          e.preventDefault();
          grid[r][c] = (grid[r][c] === -1) ? 0 : -1;
          render();
        });

        container.appendChild(cell);
      }
    }
  }

  if (hintBtn) {
    hintBtn.addEventListener('click', () => {
      addConsoleLog(`[Solver] Running Hint Cascade: Proving Region Exclusion...`);
      for (let r = 0; r < N; r++) {
        for (let c = 0; c < N; c++) {
          if (solution[r][c] === 1 && grid[r][c] !== 1) {
            grid[r][c] = 1;
            addConsoleLog(`[Solver] Proved Cat location at [Row ${r+1}, Col ${c+1}]. Applied!`);
            render();
            return;
          }
        }
      }
      addConsoleLog(`[Solver] Board already completed!`);
    });
  }

  if (resetBtn) {
    resetBtn.addEventListener('click', () => {
      grid = Array(N).fill(null).map(() => Array(N).fill(0));
      addConsoleLog(`[Meowdoku] Reset puzzle state.`);
      render();
    });
  }

  render();
}

// --------------------------------------------------------
// 4. Flame Shader Demo
// --------------------------------------------------------
function initFlameShaderDemo() {
  const canvas = document.getElementById('flame-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  let t = 0;

  function render() {
    canvas.width = 280;
    canvas.height = 260;
    ctx.fillStyle = '#151515';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    t += 0.05;
    const cx = canvas.width / 2;
    const cy = canvas.height - 30;

    // Gradient halo
    const rad = ctx.createRadialGradient(cx, cy - 60, 5, cx, cy - 60, 90);
    rad.addColorStop(0, 'rgba(245, 158, 11, 0.5)');
    rad.addColorStop(0.6, 'rgba(239, 68, 68, 0.15)');
    rad.addColorStop(1, 'transparent');
    ctx.fillStyle = rad;
    ctx.beginPath();
    ctx.arc(cx, cy - 60, 100, 0, Math.PI * 2);
    ctx.fill();

    // Procedural flame body
    ctx.fillStyle = '#f59e0b';
    ctx.beginPath();
    ctx.moveTo(cx - 20, cy);
    for (let i = 0; i <= 20; i++) {
      const step = i / 20;
      const curY = cy - step * 100;
      const noise = Math.sin(step * 4 + t) * 12;
      ctx.lineTo(cx + (20 * (1 - step)) + noise, curY);
    }
    for (let i = 20; i >= 0; i--) {
      const step = i / 20;
      const curY = cy - step * 100;
      const noise = Math.sin(step * 4 + t + 2) * 12;
      ctx.lineTo(cx - (20 * (1 - step)) + noise, curY);
    }
    ctx.closePath();
    ctx.fill();

    requestAnimationFrame(render);
  }
  render();
}

// --------------------------------------------------------
// 5. Jigsaw Classifier Demo
// --------------------------------------------------------
function initJigsawClassifierDemo() {
  const canvas = document.getElementById('jigsaw-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const dim = 3;

  function render() {
    canvas.width = 280;
    canvas.height = 280;
    ctx.fillStyle = '#151515';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    const pad = 20;
    const size = (canvas.width - pad * 2) / dim;

    for (let r = 0; r < dim; r++) {
      for (let c = 0; c < dim; c++) {
        const x = pad + c * size;
        const y = pad + r * size;
        ctx.strokeStyle = '#38bdf8';
        ctx.lineWidth = 1.5;
        ctx.strokeRect(x + 2, y + 2, size - 4, size - 4);

        ctx.fillStyle = 'rgba(56, 189, 248, 0.08)';
        ctx.fillRect(x + 2, y + 2, size - 4, size - 4);

        ctx.fillStyle = '#94a3b8';
        ctx.font = '10px monospace';
        ctx.textAlign = 'center';
        ctx.fillText(`[${r},${c}]`, x + size / 2, y + size / 2);
      }
    }
  }
  render();
}

// --------------------------------------------------------
// 6. Lightbox Inspection Modal
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
