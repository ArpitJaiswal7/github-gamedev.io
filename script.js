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
// 3. Unity Lab: Single-Iframe 6-Artifact + Dual-Runtime Controller
// --------------------------------------------------------
const UNITY_LAB_MANIFEST = {
  'meowsweeper': {
    icon: '🐱',
    title: 'MeowSweeper',
    scene: 'MeowSweeper_Lab.unity',
    htmlUrl: 'labs/meowsweeper.html',
    webglUrl: 'webgl/meowsweeper/index.html',
    hasWebgl: true,
    unitySpecs: 'Unity 2022.3 LTS (IL2CPP / WebGL 2.0) • Discrete Multi-Touch + Hint Cascade',
    desc: 'Meowdoku Hybrid Solver + Deterministic Hint Cascade'
  },
  'calm-jigsaw': {
    icon: '🧩',
    title: 'Calm Jigsaw',
    scene: 'CalmJigsaw_9Signature.unity',
    htmlUrl: 'labs/calm-jigsaw.html',
    webglUrl: 'webgl/calm-jigsaw/index.html',
    hasWebgl: true,
    unitySpecs: 'Unity 2022.3 LTS (URP / Procedural 9-Signature Bezier Mesh)',
    desc: 'Procedural N×N Bezier Mesh + Full Viewport Board & Right Piece Drawer'
  },
  'jigsaw-solitaire': {
    icon: '🃏',
    title: 'Calm Jigsaw Solitaire',
    scene: 'CalmJigsawSolitaire.unity',
    htmlUrl: 'labs/jigsaw-solitaire.html',
    webglUrl: 'webgl/jigsaw-solitaire/index.html',
    hasWebgl: true,
    unitySpecs: 'Unity 2022.3 LTS (Spatial Group Merging + Ring Expand Shader)',
    desc: 'Solitaire Column Queues + Spatial Cluster Merging & Powerups'
  },
  'offline-leaderboard': {
    icon: '🏆',
    title: 'Offline Leaderboard',
    scene: 'OfflineLeaderboard_LiveOps.unity',
    htmlUrl: 'labs/offline-leaderboard.html',
    webglUrl: 'webgl/offline-leaderboard/index.html',
    hasWebgl: true,
    unitySpecs: 'Unity 2022.3 LTS (Sheet 06 Bot Convergence & Anti-Rollback Engine)',
    desc: 'Zero-Server Bot Pacing, League Overtakes & Time-Travel Clamping Simulator'
  },
  'native-image-bridge': {
    icon: '🖼️',
    title: 'Native Image Bridge',
    scene: 'NativeImagePipeline_Sim.unity',
    htmlUrl: 'labs/native-image-bridge.html',
    webglUrl: 'webgl/native-image-bridge/index.html',
    hasWebgl: true,
    unitySpecs: 'Unity 2022.3 LTS (3-Tier RAM LRU + Native Disk Cache + OTA Queue)',
    desc: '3-Tier Memory LRU + Native Disk I/O + 100+ Gallery Viewport Priority Queue'
  },
  'asset-organizer': {
    icon: '🗂️',
    title: 'Asset Organizer Tool',
    scene: 'AssetOrganizer_EditorWindow.unity',
    htmlUrl: 'labs/asset-organizer.html',
    webglUrl: null,
    hasWebgl: false,
    unitySpecs: 'Unity EditorWindow API (com.arrowstrike.asset-organizer • AssetDatabase)',
    desc: 'Unity Editor Project Window Badges, C/R Hover Tags & Multi-Asset Explorer Reveal'
  }
};

let activeLabDemo = 'meowsweeper';
let activeLabRuntime = 'html'; // 'html' | 'webgl'
let labLoaderTimer = null;

function initUnityLab() {
  const tabButtons = document.querySelectorAll('.lab-tab-btn');
  const btnRuntimeHtml = document.getElementById('lab-runtime-html');
  const btnRuntimeWebgl = document.getElementById('lab-runtime-webgl');
  const btnReload = document.getElementById('lab-btn-reload');
  const btnFullscreen = document.getElementById('lab-btn-fullscreen');
  const btnFooterRuntime = document.getElementById('lab-footer-runtime-switch');
  const btnWebglBackHtml = document.getElementById('webgl-btn-back-html');
  const btnWebglTryBoot = document.getElementById('webgl-btn-try-boot');
  const workstation = document.getElementById('unity-lab-workstation');

  // Tab Click Listeners
  tabButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const demoKey = btn.getAttribute('data-lab-demo');
      if (demoKey && UNITY_LAB_MANIFEST[demoKey]) {
        switchLabDemo(demoKey, activeLabRuntime);
      }
    });
  });

  // Runtime Mode Toggle Listeners
  if (btnRuntimeHtml) {
    btnRuntimeHtml.addEventListener('click', () => {
      switchLabDemo(activeLabDemo, 'html');
    });
  }
  if (btnRuntimeWebgl) {
    btnRuntimeWebgl.addEventListener('click', () => {
      switchLabDemo(activeLabDemo, 'webgl');
    });
  }
  if (btnFooterRuntime) {
    btnFooterRuntime.addEventListener('click', () => {
      const nextRuntime = activeLabRuntime === 'html' ? 'webgl' : 'html';
      switchLabDemo(activeLabDemo, nextRuntime);
    });
  }
  if (btnWebglBackHtml) {
    btnWebglBackHtml.addEventListener('click', () => {
      switchLabDemo(activeLabDemo, 'html');
    });
  }
  if (btnWebglTryBoot) {
    btnWebglTryBoot.addEventListener('click', () => {
      bootWebglInIframe(activeLabDemo);
    });
  }

  // Reload Current Demo
  if (btnReload) {
    btnReload.addEventListener('click', () => {
      switchLabDemo(activeLabDemo, activeLabRuntime, true);
    });
  }

  // Fullscreen Workstation Wrapper Toggle (Keeps Top Switcher Bar Visible in Fullscreen!)
  if (btnFullscreen && workstation) {
    btnFullscreen.addEventListener('click', () => {
      if (!document.fullscreenElement) {
        if (workstation.requestFullscreen) {
          workstation.requestFullscreen().catch(() => {
            workstation.classList.toggle('lab-theater-fullscreen');
            updateFullscreenButtonUI();
          });
        } else {
          workstation.classList.toggle('lab-theater-fullscreen');
          updateFullscreenButtonUI();
        }
      } else {
        if (document.exitFullscreen) {
          document.exitFullscreen();
        }
      }
    });

    document.addEventListener('fullscreenchange', updateFullscreenButtonUI);
  }

  // Check URL query/hash on initial load (e.g., ?lab=calm-jigsaw)
  const params = new URLSearchParams(window.location.search);
  const requestedLab = params.get('lab');
  if (requestedLab && UNITY_LAB_MANIFEST[requestedLab]) {
    switchLabDemo(requestedLab, 'html');
  } else {
    updateLabUIState();
  }
}

function updateFullscreenButtonUI() {
  const label = document.getElementById('lab-fullscreen-label');
  const icon = document.getElementById('lab-fullscreen-icon');
  const workstation = document.getElementById('unity-lab-workstation');
  const isFull = Boolean(document.fullscreenElement) || (workstation && workstation.classList.contains('lab-theater-fullscreen'));

  if (label) {
    label.textContent = isFull ? 'Exit Fullscreen' : 'Fullscreen';
  }
  if (icon) {
    icon.setAttribute('data-lucide', isFull ? 'minimize-2' : 'maximize-2');
    if (window.lucide) lucide.createIcons();
  }
}

function updateLabUIState() {
  const item = UNITY_LAB_MANIFEST[activeLabDemo];
  if (!item) return;

  // Update 6 Demo Tab Pill Styles
  document.querySelectorAll('.lab-tab-btn').forEach(btn => {
    const key = btn.getAttribute('data-lab-demo');
    if (key === activeLabDemo) {
      btn.className = 'lab-tab-btn px-2.5 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5 bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm';
    } else {
      btn.className = 'lab-tab-btn px-2.5 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5 bg-[#21262d] text-slate-300 hover:text-white border border-transparent hover:border-slate-700';
    }
  });

  // Update Dual-Runtime Segmented Buttons
  const btnHtml = document.getElementById('lab-runtime-html');
  const btnWebgl = document.getElementById('lab-runtime-webgl');
  const dot = document.getElementById('lab-webgl-dot');

  if (dot) {
    dot.className = item.hasWebgl ? 'w-2 h-2 rounded-full bg-emerald-400' : 'w-2 h-2 rounded-full bg-amber-400';
  }

  if (btnHtml && btnWebgl) {
    if (activeLabRuntime === 'html') {
      btnHtml.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-sky-500 text-slate-950 shadow';
      btnWebgl.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 text-slate-400 hover:text-white';
    } else {
      btnHtml.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 text-slate-400 hover:text-white';
      btnWebgl.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-purple-500 text-white shadow';
    }
  }

  // Update Header Badge, New Tab Link, and Footer Telemetry
  const activeBadge = document.getElementById('lab-active-badge');
  const newTabLink = document.getElementById('lab-btn-newtab');
  const footerSource = document.getElementById('lab-footer-source');
  const footerDesc = document.getElementById('lab-footer-desc');
  const footerRuntimeText = document.getElementById('lab-footer-runtime-text');

  const activeSourceUrl = (activeLabRuntime === 'webgl' && item.webglUrl) ? item.webglUrl : item.htmlUrl;

  if (activeBadge) {
    activeBadge.textContent = `Active Scene: ${item.scene} (${activeLabRuntime === 'webgl' ? 'Unity WebGL' : 'HTML Sim'})`;
  }
  if (newTabLink) {
    newTabLink.href = activeSourceUrl;
  }
  if (footerSource) {
    footerSource.textContent = activeSourceUrl;
  }
  if (footerDesc) {
    footerDesc.textContent = item.desc;
  }
  if (footerRuntimeText) {
    footerRuntimeText.textContent = activeLabRuntime === 'html'
      ? 'Switch to Unity WebGL Build →'
      : '⚡ Back to Instant HTML Sim (0.2s) →';
  }
}

function switchLabDemo(demoKey, runtimeMode = 'html', forceReload = false) {
  const item = UNITY_LAB_MANIFEST[demoKey];
  if (!item) return;

  const isSame = (activeLabDemo === demoKey && activeLabRuntime === runtimeMode && !forceReload);
  activeLabDemo = demoKey;
  activeLabRuntime = runtimeMode;

  updateLabUIState();
  if (isSame) return;

  const frame = document.getElementById('unity-lab-frame');
  const loader = document.getElementById('lab-scene-loader');
  const loaderIcon = document.getElementById('lab-loader-icon');
  const loaderTitle = document.getElementById('lab-loader-title');
  const loaderSub = document.getElementById('lab-loader-sub');
  const webglPanel = document.getElementById('lab-webgl-panel');

  if (!frame) return;

  clearTimeout(labLoaderTimer);

  if (runtimeMode === 'webgl') {
    // Populate WebGL Runtime Launcher Card
    const cardIcon = document.getElementById('webgl-card-icon');
    const cardBadge = document.getElementById('webgl-card-badge');
    const cardTitle = document.getElementById('webgl-card-title');
    const cardDesc = document.getElementById('webgl-card-desc');
    const spec1 = document.getElementById('webgl-spec-line1');
    const btnTryBoot = document.getElementById('webgl-btn-try-boot');

    if (cardIcon) cardIcon.textContent = item.icon;
    if (spec1) spec1.textContent = `• Target: ${item.unitySpecs}`;

    if (!item.hasWebgl) {
      if (cardBadge) cardBadge.textContent = 'UNITY EDITORWINDOW TOOL';
      if (cardTitle) cardTitle.textContent = `${item.title} — Unity Editor API Simulator`;
      if (cardDesc) {
        cardDesc.innerHTML = `Because <strong class="text-white">${item.title}</strong> is a Unity Editor extension (<code class="text-sky-300 font-mono">UnityEditor.AssetDatabase</code>), its full interactive experience runs in the <strong class="text-sky-300">⚡ Instant Lab Sim</strong> tab (which also includes the downloadable <code class="text-emerald-300 font-mono">.unitypackage</code>!).`;
      }
      if (btnTryBoot) btnTryBoot.classList.add('hidden');
    } else {
      if (cardBadge) cardBadge.textContent = 'UNITY WEBGL 2.0 WASM BUILD';
      if (cardTitle) cardTitle.textContent = `${item.title} — Compiled Unity WebGL Runtime`;
      if (cardDesc) {
        cardDesc.innerHTML = `Ready to mount compiled C# / IL2CPP WebAssembly build from <code class="text-purple-300 font-mono">${item.webglUrl}</code> inside this single isolated viewport.`;
      }
      if (btnTryBoot) btnTryBoot.classList.remove('hidden');
    }

    if (webglPanel) {
      webglPanel.classList.remove('hidden');
      webglPanel.classList.add('flex');
    }
    return;
  }

  // Hide WebGL overlay when in Instant HTML Sim mode
  if (webglPanel) {
    webglPanel.classList.add('hidden');
    webglPanel.classList.remove('flex');
  }

  // Show Authentic Unity SceneManager Transition Curtain
  if (loader) {
    if (loaderIcon) loaderIcon.textContent = item.icon;
    if (loaderTitle) {
      loaderTitle.innerHTML = `SceneManager.LoadSceneAsync("<span class="text-sky-400">${item.scene}</span>")`;
    }
    if (loaderSub) {
      loaderSub.textContent = 'GC.Collect() complete • Single instance active in iframe';
    }
    loader.classList.remove('hidden');
  }

  const hideLoader = () => {
    clearTimeout(labLoaderTimer);
    if (loader) loader.classList.add('hidden');
  };

  frame.onload = hideLoader;
  // Fallback timeout in case onload fires early on cached local files
  labLoaderTimer = setTimeout(hideLoader, 320);

  // Swap the single iframe src (100% unloads previous DOM, timers & canvases)
  frame.src = forceReload
    ? `${item.htmlUrl}?t=${Date.now()}`
    : item.htmlUrl;
}

function bootWebglInIframe(demoKey) {
  const item = UNITY_LAB_MANIFEST[demoKey];
  if (!item || !item.webglUrl) return;

  const frame = document.getElementById('unity-lab-frame');
  const webglPanel = document.getElementById('lab-webgl-panel');
  const loader = document.getElementById('lab-scene-loader');
  const loaderIcon = document.getElementById('lab-loader-icon');
  const loaderTitle = document.getElementById('lab-loader-title');
  const loaderSub = document.getElementById('lab-loader-sub');

  if (webglPanel) {
    webglPanel.classList.add('hidden');
    webglPanel.classList.remove('flex');
  }

  if (loader) {
    if (loaderIcon) loaderIcon.textContent = '🎮';
    if (loaderTitle) {
      loaderTitle.innerHTML = `createUnityInstance("<span class="text-purple-400">${item.webglUrl}</span>")`;
    }
    if (loaderSub) {
      loaderSub.textContent = 'Booting Unity WebGL 2.0 WASM Binary • Single-Iframe Auto GC';
    }
    loader.classList.remove('hidden');
  }

  if (frame) {
    frame.onload = () => {
      if (loader) loader.classList.add('hidden');
    };
    setTimeout(() => {
      if (loader) loader.classList.add('hidden');
    }, 450);
    frame.src = item.webglUrl;
  }
}

// Global helper so Shipped Games cards & Blueprints can jump directly to any demo in Unity Lab
window.openLabDemo = function(demoKey, runtimeMode = 'html') {
  switchLabDemo(demoKey, runtimeMode);
};

// --------------------------------------------------------
// 4. Blueprint Split-View Terminal (Zone 3)
// --------------------------------------------------------
const blueprintsData = [
  {
    id: 'sheet-02',
    sheetNum: 'Sheet 02 — Surface Cut',
    title: 'Native Image Pipeline',
    domain: 'Platform Services',
    engine: 'Unity (Android + iOS)',
    desc: 'Fetches, resizes, and caches remote images across Android, iOS, and Unity Editor through one shared C# API surface.',
    img: 'assets/blueprints/sheet-02-native-image-pipeline.png',
    labDemo: 'native-image-bridge'
  },
  {
    id: 'sheet-09',
    sheetNum: 'Sheet 09 — Surface Cut',
    title: 'Jigsaw Board Renderer',
    domain: 'Core Gameplay Logic',
    engine: 'Unity',
    desc: 'Dynamic procedural N×N board generator using 9 tab/blank edge signatures with custom shader UV quad projections.',
    img: 'assets/blueprints/sheet-09-jigsaw-board-renderer.png',
    labDemo: 'calm-jigsaw'
  },
  {
    id: 'sheet-10',
    sheetNum: 'Sheet 10 — Surface Cut',
    title: 'Meowdoku Engine & Solver',
    domain: 'Core Gameplay Logic',
    engine: 'Unity',
    desc: 'Multi-touch gesture router with input locks during animations and an ordered hint-technique cascade.',
    img: 'assets/blueprints/sheet-10-meowdoku.png',
    labDemo: 'meowsweeper'
  },
  {
    id: 'sheet-06',
    sheetNum: 'Sheet 06 — Surface Cut',
    title: 'Offline Leaderboard',
    domain: 'LiveOps / Retention',
    engine: 'Unity',
    desc: 'Zero-server-cost bot simulation engine generating dynamic seasonal leagues, rank advancement, and anti-rollback clamping.',
    img: 'assets/blueprints/sheet-06-offline-leaderboard.png',
    labDemo: 'offline-leaderboard'
  },
  {
    id: 'sheet-08',
    sheetNum: 'Sheet 08 — Surface Cut',
    title: 'Level Sync (OTA Content)',
    domain: 'Platform Services',
    engine: 'Unity',
    desc: 'Background manifest-diffing synchronizer that never blocks game boot sequence, applying content cleanly on subsequent launch.',
    img: 'assets/blueprints/sheet-08-level-sync.png',
    labDemo: 'native-image-bridge'
  },
  {
    id: 'sheet-04',
    sheetNum: 'Sheet 04 — Surface Cut',
    title: 'IAP System Gateway',
    domain: 'Monetization',
    engine: 'Unity (Android + iOS)',
    desc: 'Dual-currency store gateway (real money vs soft currency) with strict grant-then-confirm verification.',
    img: 'assets/blueprints/sheet-04-iap-system.png',
    labDemo: null
  },
  {
    id: 'sheet-13',
    sheetNum: 'Sheet 13 — Surface Cut',
    title: 'Argos Localization IPC',
    domain: 'Editor Tooling / IPC',
    engine: 'Unity + Python (offline)',
    desc: 'Cross-process communication tool linking Unity C# to an offline Python translation subprocess over stdin/stdout JSON codecs.',
    img: 'assets/blueprints/sheet-13-argos-localization.png',
    labDemo: null
  },
  {
    id: 'sheet-11',
    sheetNum: 'Sheet 11 — Surface Cut',
    title: 'Asset Organizer Tool',
    domain: 'Unity Editor DX',
    engine: 'Unity Editor',
    desc: 'Developer productivity tool adding project window asset pinning, custom category badges, and asset movement watchers.',
    img: 'assets/blueprints/sheet-11-asset-organizer-tool.png',
    labDemo: 'asset-organizer'
  },
  {
    id: 'sheet-12',
    sheetNum: 'Sheet 12 — Surface Cut',
    title: 'Flame Flicker Effect',
    domain: 'Tech Art & Shaders',
    engine: 'Unity',
    desc: 'Lightweight 2-file procedural noise shader and C# driver creating dynamic ambient fire visuals on UI canvases.',
    img: 'assets/blueprints/sheet-12-flame-flicker-effect.png',
    labDemo: 'jigsaw-solitaire'
  }
];

function initBlueprintTerminal() {
  const navContainer = document.getElementById('blueprint-nav-list');
  const stageImg = document.getElementById('bp-stage-img');
  const stageTitle = document.getElementById('bp-stage-title');
  const stageDomain = document.getElementById('bp-stage-domain');
  const stageDesc = document.getElementById('bp-stage-desc');
  const zoomBtn = document.getElementById('btn-zoom-blueprint');
  const launchLabBtn = document.getElementById('btn-launch-blueprint-lab');

  if (!navContainer) return;

  function syncLaunchLabButton(bp) {
    if (!launchLabBtn) return;
    if (bp.labDemo && UNITY_LAB_MANIFEST[bp.labDemo]) {
      launchLabBtn.classList.remove('hidden');
      launchLabBtn.classList.add('flex');
      launchLabBtn.onclick = () => {
        window.openLabDemo(bp.labDemo, 'html');
      };
    } else {
      launchLabBtn.classList.add('hidden');
      launchLabBtn.classList.remove('flex');
    }
  }

  // Sync initial blueprint (Sheet 02 -> Native Image Bridge)
  syncLaunchLabButton(blueprintsData[0]);

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
      syncLaunchLabButton(bp);
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
// 5. Lightbox Inspection Modal
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
