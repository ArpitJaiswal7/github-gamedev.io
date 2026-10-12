window.name = 'portfolio_main';
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
    htmlUrl: 'labs/meowsweeper.html?v=00f06be',
    webglUrl: 'labs/unity-meowsweeper/index.html?v=00f06be',
    hasWebgl: true,
    unitySpecs: 'Unity 6000.3.14f1 (C# / WebGL 2.0 WASM) • Connected to Live Level Server',
    desc: 'MeowSweeper Core & Offline Leaderboard (Headless Unity WebGL WASM wired to Live CDN)'
  },
  'calm-arrow': {
    icon: '🏹',
    title: 'Calm Arrow',
    scene: 'ShapeEscapeScene.unity',
    htmlUrl: 'labs/calm-arrow.html',
    webglUrl: 'labs/unity-calm-arrow/index.html',
    hasWebgl: true,
    unitySpecs: 'Unity 6000.3.14f1 (C# / WebGL 2.0 WASM) • Outside-In Peeling & Obstacle Rebound',
    desc: 'Guaranteed-solvable outside-in peeling, 36x36 shape mask, tactile 3-lives obstacle bonk, and 100 shape levels'
  },
  'calm-jigsaw': {
    icon: '🧩',
    title: 'Calm Jigsaw',
    scene: 'CalmJigsaw_9Signature.unity',
    htmlUrl: 'labs/calm-jigsaw.html',
    webglUrl: 'labs/unity-calm-jigsaw/index.html',
    hasWebgl: true,
    unitySpecs: 'Unity 6000.3.14f1 (C# / WebGL 2.0 WASM) • Procedural Jigsaw & Powerups',
    desc: 'Procedural N×N Bezier Jigsaw + Full Viewport Board & Right Piece Drawer (Unity WebGL WASM)'
  },
  'jigsaw-solitaire': {
    icon: '🃏',
    title: 'Calm Jigsaw Solitaire',
    scene: 'JigsawSolitaireMain.unity',
    htmlUrl: 'labs/jigsaw-solitaire.html',
    webglUrl: 'labs/unity-jigsaw-solitaire/index.html',
    hasWebgl: true,
    unitySpecs: 'Unity 6000.3.14f1 (C# / WebGL 2.0 WASM) • Rectangular Card Seam Merging & Backend Bridge',
    desc: 'Rectangular Card Grid, 0px Seam Merging, Displacement Swapping, Offline Leaderboard & Backend Server'
  },
  'offline-leaderboard': {
    icon: '🏆',
    title: 'Offline Leaderboard',
    scene: 'OfflineLeaderboard_LiveOps.unity',
    htmlUrl: 'labs/offline-leaderboard.html',
    webglUrl: 'labs/offline-leaderboard.html?mode=webgl',
    hasWebgl: false,
    isSoloProduct: true,
    unitySpecs: 'Unity 2022.3 LTS (Sheet 06 Bot Convergence & Anti-Rollback Engine)',
    desc: 'Zero-Server Bot Pacing, League Overtakes & Time-Travel Clamping Simulator'
  },
  'native-image-bridge': {
    icon: '🖼️',
    title: 'Native Image Bridge',
    scene: 'NativeImagePipeline_Sim.unity',
    htmlUrl: 'labs/native-image-bridge.html',
    webglUrl: 'labs/native-image-bridge.html?mode=webgl',
    hasWebgl: false,
    isSoloProduct: true,
    unitySpecs: 'Unity 2022.3 LTS (3-Tier RAM LRU + Native Disk Cache + OTA Queue)',
    desc: '3-Tier Memory LRU + Native Disk I/O + 100+ Gallery Viewport Priority Queue'
  },
  'asset-organizer': {
    icon: '🗂️',
    title: 'Asset Organizer Tool',
    scene: 'AssetOrganizer_EditorWindow.unity',
    htmlUrl: 'labs/asset-organizer.html',
    webglUrl: 'labs/asset-organizer.html?mode=webgl',
    hasWebgl: false,
    isSoloProduct: true,
    unitySpecs: 'Unity EditorWindow API (com.arrowstrike.asset-organizer • AssetDatabase)',
    desc: 'Unity Editor Project Window Badges, C/R Hover Tags & Multi-Asset Explorer Reveal'
  },
  'argos-localization': {
    icon: '🌐',
    title: 'Argos Localization',
    scene: 'ArgosDemo.unity',
    htmlUrl: 'labs/argos-localization.html',
    webglUrl: 'labs/argos-localization.html?mode=webgl',
    hasWebgl: true,
    unitySpecs: 'Unity 6000.3.14f1 (ArgosDemo.unity • Unity.Localization + Offline Python IPC)',
    desc: 'ArgosDemo.unity 6-Locale Switcher, Hierarchy TMP Harvester & Offline Python Bridge'
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

  // Listen for postMessage from embedded lab pages (e.g., dismissing the WebGL fallback screen)
  window.addEventListener('message', (e) => {
    if (!e.data || typeof e.data !== 'object') return;
    if (e.data.type === 'WEBGL_FALLBACK_SWITCH_HTML') {
      activeLabRuntime = 'html';
      const webglPanel = document.getElementById('lab-webgl-panel');
      if (webglPanel) {
        webglPanel.classList.add('hidden');
        webglPanel.classList.remove('flex');
      }
      updateLabUIState();
    } else if (e.data.type === 'WEBGL_LOAD_ERROR') {
      showLabWebglFailurePanel(activeLabDemo, 'load_failed');
    }
  });

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
      const currentItem = UNITY_LAB_MANIFEST[activeLabDemo];
      // Non-interactable for solo products without standalone WebGL builds
      if (currentItem && !currentItem.hasWebgl) return;
      switchLabDemo(activeLabDemo, 'webgl');
    });
  }
  if (btnFooterRuntime) {
    btnFooterRuntime.addEventListener('click', () => {
      const currentItem = UNITY_LAB_MANIFEST[activeLabDemo];
      if (activeLabRuntime === 'html' && currentItem && !currentItem.hasWebgl) return;
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
      bootWebglInIframe(activeLabDemo, true);
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
  const requestedRuntime = params.get('runtime') === 'webgl' ? 'webgl' : 'html';
  if (requestedLab && UNITY_LAB_MANIFEST[requestedLab]) {
    switchLabDemo(requestedLab, requestedRuntime);
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

  // Update 7 Demo Tab Pill Styles
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
  const btnFooterRuntime = document.getElementById('lab-footer-runtime-switch');

  if (dot) {
    dot.className = item.hasWebgl ? 'w-2 h-2 rounded-full bg-emerald-400' : 'w-2 h-2 rounded-full bg-rose-500';
    dot.title = item.hasWebgl ? 'Unity WebGL Runtime Mode Available' : 'WebGL Build Not Available for this Solo Product';
  }

  if (btnHtml && btnWebgl) {
    if (!item.hasWebgl) {
      // Make WebGL button non-interactable for solo products (Offline Leaderboard, Native Image Bridge, Asset Organizer)
      btnWebgl.disabled = true;
      btnWebgl.setAttribute('aria-disabled', 'true');
      btnWebgl.title = 'WebGL Build Not Available for this Solo Product';

      if (activeLabRuntime === 'html') {
        btnHtml.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-sky-500 text-slate-950 shadow';
        btnWebgl.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all flex items-center gap-1.5 text-slate-600 opacity-45 cursor-not-allowed pointer-events-none select-none';
      } else {
        // Selected by default from previous tab while switching to this solo product
        btnHtml.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 text-sky-300 hover:text-white bg-sky-500/15 border border-sky-500/30';
        btnWebgl.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all flex items-center gap-1.5 bg-rose-500/20 text-rose-300 border border-rose-500/40 opacity-85 cursor-not-allowed pointer-events-none select-none';
      }
    } else {
      btnWebgl.disabled = false;
      btnWebgl.removeAttribute('aria-disabled');
      btnWebgl.title = 'Switch to Compiled Unity WebGL Runtime';

      if (activeLabRuntime === 'html') {
        btnHtml.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-sky-500 text-slate-950 shadow';
        btnWebgl.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 text-slate-400 hover:text-white';
      } else {
        btnHtml.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 text-slate-400 hover:text-white';
        btnWebgl.className = 'px-2.5 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-purple-500 text-white shadow';
      }
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
    const modeLabel = activeLabRuntime === 'webgl'
      ? (item.hasWebgl ? 'Unity WebGL' : 'WebGL N/A • Solo Product')
      : 'HTML Sim';
    activeBadge.textContent = `Active Scene: ${item.scene} (${modeLabel})`;
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

  if (btnFooterRuntime && footerRuntimeText) {
    if (!item.hasWebgl && activeLabRuntime === 'html') {
      btnFooterRuntime.disabled = true;
      btnFooterRuntime.className = 'px-2.5 py-0.5 rounded-md bg-slate-800/40 text-slate-500 border border-slate-700/40 text-xs font-sans font-semibold flex items-center gap-1.5 opacity-60 cursor-not-allowed pointer-events-none select-none';
      footerRuntimeText.textContent = '🔒 WebGL Build N/A (Solo Product)';
    } else if (activeLabRuntime === 'webgl') {
      btnFooterRuntime.disabled = false;
      btnFooterRuntime.className = 'px-2.5 py-0.5 rounded-md bg-sky-500/15 hover:bg-sky-500/25 text-sky-300 border border-sky-500/30 text-xs font-sans font-semibold cursor-pointer transition flex items-center gap-1.5';
      footerRuntimeText.textContent = '⚡ Back to Instant HTML Sim (0.2s) →';
    } else {
      btnFooterRuntime.disabled = false;
      btnFooterRuntime.className = 'px-2.5 py-0.5 rounded-md bg-purple-500/15 hover:bg-purple-500/25 text-purple-300 border border-purple-500/30 text-xs font-sans font-semibold cursor-pointer transition flex items-center gap-1.5';
      footerRuntimeText.textContent = 'Switch to Unity WebGL Build →';
    }
  }
}

function showLabWebglFailurePanel(demoKey, reason = 'solo_product') {
  const item = UNITY_LAB_MANIFEST[demoKey] || UNITY_LAB_MANIFEST[activeLabDemo];
  if (!item) return;

  const webglPanel = document.getElementById('lab-webgl-panel');
  const cardIcon = document.getElementById('webgl-card-icon');
  const cardBadge = document.getElementById('webgl-card-badge');
  const cardTitle = document.getElementById('webgl-card-title');
  const cardDesc = document.getElementById('webgl-card-desc');
  const specTag = document.getElementById('webgl-spec-tag');
  const spec1 = document.getElementById('webgl-spec-line1');
  const btnTryBoot = document.getElementById('webgl-btn-try-boot');

  if (cardIcon) cardIcon.textContent = '⚠️';
  if (specTag) specTag.textContent = item.title;
  if (spec1) {
    spec1.innerHTML = reason === 'load_failed'
      ? '&bull; Status: <span class="text-rose-400">Failed to Load WebGL Mechanics Binary (HTTP 404 / WASM Mount Error)</span>'
      : '&bull; Status: <span class="text-rose-400">WebGL Mechanics Build Not Available for This Solo Product</span>';
  }
  if (cardBadge) {
    cardBadge.textContent = reason === 'load_failed'
      ? 'ERR_WEBGL_WASM_LOAD_FAILED • DIAGNOSTIC FALLBACK'
      : 'WEBGL BUILD UNAVAILABLE • SOLO PRODUCT';
  }
  if (cardTitle) {
    cardTitle.textContent = 'Failed to Load Mechanics — Not Available for This Solo Product';
  }
  if (cardDesc) {
    cardDesc.innerHTML = reason === 'load_failed'
      ? `The compiled Unity WebGL build for <strong class="text-white">${item.title}</strong> failed to load or is currently unavailable. You can switch to the <strong class="text-sky-300">⚡ Instant Lab Sim</strong> to inspect and test the live interactive architecture immediately.`
      : `<strong class="text-white">${item.title}</strong> is a standalone architecture subsystem / solo product (${item.unitySpecs}) rather than a standalone compiled WebGL game binary. Its full interactive simulation runs in the <strong class="text-sky-300">⚡ Instant Lab Sim</strong> tab.`;
  }
  if (btnTryBoot) {
    btnTryBoot.classList.toggle('hidden', !item.hasWebgl);
  }

  if (webglPanel) {
    webglPanel.classList.remove('hidden');
    webglPanel.classList.add('flex');
  }
}

function switchLabDemo(demoKey, runtimeMode = 'html', forceReload = false) {
  const item = UNITY_LAB_MANIFEST[demoKey];
  if (!item) return;

  const prevDemo = activeLabDemo;
  const prevRuntime = activeLabRuntime;
  const isSame = (prevDemo === demoKey && prevRuntime === runtimeMode && !forceReload);

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

  // If switching from WebGL fallback overlay back to HTML Sim on the same solo product page, dismiss overlay directly
  if (prevDemo === demoKey && prevRuntime === 'webgl' && runtimeMode === 'html' && !item.hasWebgl && !forceReload) {
    if (webglPanel) {
      webglPanel.classList.add('hidden');
      webglPanel.classList.remove('flex');
    }
    try {
      if (frame.contentWindow) {
        frame.contentWindow.postMessage({ type: 'DISMISS_WEBGL_FALLBACK' }, '*');
        frame.contentWindow.focus();
        return;
      }
    } catch (_) {}
  }

  if (runtimeMode === 'webgl') {
    // If this demo has a WebGL URL (either real WebGL build or solo product ?mode=webgl fallback page inside iframe)
    if (item.webglUrl) {
      bootWebglInIframe(demoKey, forceReload);
      return;
    }

    showLabWebglFailurePanel(demoKey, 'solo_product');
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
    try {
      if (frame.contentWindow) frame.contentWindow.focus();
    } catch (_) {}
  };

  frame.onload = hideLoader;
  // Fallback timeout in case onload fires early on cached local files
  labLoaderTimer = setTimeout(hideLoader, 320);

  // Swap the single iframe src (100% unloads previous DOM, timers & canvases)
  frame.src = forceReload
    ? `${item.htmlUrl}?t=${Date.now()}`
    : item.htmlUrl;
}

async function bootWebglInIframe(demoKey, forceReload = false) {
  const item = UNITY_LAB_MANIFEST[demoKey];
  if (!item || !item.webglUrl) {
    showLabWebglFailurePanel(demoKey, 'solo_product');
    return;
  }

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
    if (loaderIcon) loaderIcon.textContent = item.hasWebgl ? '🎮' : '⚠️';
    if (loaderTitle) {
      loaderTitle.innerHTML = item.hasWebgl
        ? `createUnityInstance("<span class="text-purple-400">${item.webglUrl}</span>")`
        : `SceneManager.LoadSceneAsync("<span class="text-rose-400">${item.scene}</span>")`;
    }
    if (loaderSub) {
      loaderSub.textContent = item.hasWebgl
        ? 'Booting Unity WebGL 2.0 WASM Binary • Single-Iframe Auto GC'
        : 'Checking WebGL build target availability for solo product...';
    }
    loader.classList.remove('hidden');
  }

  const sep = item.webglUrl.includes('?') ? '&' : '?';
  const targetUrl = forceReload ? `${item.webglUrl}${sep}t=${Date.now()}` : item.webglUrl;

  // Pre-flight check when running over HTTP/HTTPS so broken/missing WebGL builds never render a 404 page
  if (item.hasWebgl && window.location.protocol.startsWith('http')) {
    try {
      const checkUrl = item.webglUrl.split('?')[0];
      const res = await fetch(checkUrl, { method: 'HEAD' });
      if (!res.ok) {
        if (loader) loader.classList.add('hidden');
        showLabWebglFailurePanel(demoKey, 'load_failed');
        return;
      }
    } catch (_) {
      if (loader) loader.classList.add('hidden');
      showLabWebglFailurePanel(demoKey, 'load_failed');
      return;
    }
  }

  if (frame) {
    const hideWebglLoader = () => {
      if (loader) loader.classList.add('hidden');
      try {
        if (frame.contentWindow) frame.contentWindow.focus();
      } catch (_) {}
    };
    frame.onload = hideWebglLoader;
    setTimeout(hideWebglLoader, 450);
    frame.src = targetUrl;
  }
}

// Global helper so Shipped Games cards & Blueprints can jump directly to any demo in Unity Lab
window.openLabDemo = function(demoKey, runtimeMode = 'html') {
  switchLabDemo(demoKey, runtimeMode);
};
window.showLabWebglFailurePanel = showLabWebglFailurePanel;

// --------------------------------------------------------
// 4. Blueprint Split-View Terminal (Zone 3)
// --------------------------------------------------------
const blueprintsData = [
  {
    id: 'sheet-10',
    sheetNum: 'Sheet 10 — Live Mermaid',
    title: 'MeowSweeper & Level Generator',
    domain: 'Core Gameplay Logic',
    engine: 'Unity 6000.3.14f1 (WebGL WASM)',
    labDemo: 'meowsweeper',
    mermaidFlowchart: `flowchart TB
  subgraph Inputs["INPUT & LEVEL GENERATOR"]
    direction LR
    Input["MeowGameUI Input<br/>(Tap X / Swipe / DblTap Cat)"]
    Sync["LevelSyncService<br/>(65 CDN Levels + JSON)"]
    Gen["GenerateChainedLevel(N)<br/>(N-Queens + Region Growth)"]
  end

  subgraph Core["CORE MODULE — MEOWSWEEPER ENGINE"]
    Mgr["MeowGameManager.cs<br/>(SetupBoard & boardState[N,N])"]
  end

  subgraph Runtime["RUNTIME & INTERNAL SUBSYSTEMS"]
    direction LR
    Powers["Power-Up Cascade<br/>(SmartHint / Reveal / Mouse)"]
    Lives["5-Lives & Win Check<br/>(remainingHearts & Flash)"]
    LB["OfflineLeaderboardService<br/>(29 Bots & Season Rank)"]
  end

  Input -->|"cell action"| Mgr
  Sync -->|"MeowLevelData"| Mgr
  Gen -->|"regionsFlat / solutionFlat"| Mgr

  Mgr <-->|"3-step hint / reveal"| Powers
  Mgr -->|"validate Cat"| Lives
  Mgr -.->|"SubmitRun()"| LB

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class Mgr coreNode
  class Input,Sync,Gen,Powers,Lives adjNode
  class LB intNode`
  },
  {
    id: 'sheet-02',
    sheetNum: 'Sheet 02 — Live Mermaid',
    title: 'Native Image Pipeline',
    domain: 'Platform Services',
    engine: 'Unity (Android + iOS)',
    labDemo: 'native-image-bridge',
    mermaidFlowchart: `flowchart TB
  subgraph Callers["GAMEPLAY & UI CALLERS"]
    direction LR
    LevelThumb["Level Select Grid<br/>(Thumbnail Batch)"]
    JigsawTex["Jigsaw TextureLoader<br/>(HD Board Artwork)"]
    AvatarUI["Leaderboard UI<br/>(Avatar Sprites)"]
  end

  subgraph Core["CORE MODULE — NATIVE IMAGE BRIDGE"]
    Facade["NativeImagePipeline.cs<br/>(Shared Async API + Deduplicator)"]
  end

  subgraph Backends["PLATFORM BRIDGES & CACHE"]
    direction LR
    Android["Android JNI Bridge<br/>(Glide + OkHttp3)"]
    iOS["iOS ObjC Bridge<br/>(NSURLSession + vImage)"]
    Cache["L1 RAM LRU Pool<br/>& L2 SHA-256 Disk Cache"]
  end

  LevelThumb -->|"LoadImageAsync"| Facade
  JigsawTex -->|"LoadImageAsync"| Facade
  AvatarUI -->|"LoadImageAsync"| Facade

  Facade -->|"AndroidJNI"| Android
  Facade -->|"DllImport"| iOS
  Facade -.->|"LRU / SHA-256"| Cache

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class Facade coreNode
  class LevelThumb,JigsawTex,AvatarUI,Android,iOS adjNode
  class Cache intNode`
  },
  {
    id: 'sheet-09',
    sheetNum: 'Sheet 09 — Live Mermaid',
    title: 'Jigsaw Board Renderer',
    domain: 'Core Gameplay Logic',
    engine: 'Unity',
    labDemo: 'calm-jigsaw',
    mermaidFlowchart: `flowchart TB
  subgraph Inputs["PUZZLE CONFIG & ARTWORK"]
    direction LR
    GridCfg["Grid Preset<br/>(3x3 up to 10x10)"]
    SourceArt["HD Source Texture2D<br/>(Single Shared Atlas)"]
    TouchDrag["Multi-Touch Drag<br/>(Piece Pointer Input)"]
  end

  subgraph Core["CORE MODULE — JIGSAW BOARD RENDERER"]
    Generator["ProceduralBoardGenerator.cs<br/>(Edge Signature Matrix + UV Mapper)"]
  end

  subgraph Runtime["SHADER & SNAP SUBSYSTEMS"]
    direction LR
    Shader["PieceMaskShader<br/>(9-Slice Bevel + Alpha)"]
    SnapGroup["Union-Find Cluster<br/>(Magnetic Neighbor Snap)"]
    UVRect["Oversized UV Quad (+28%)<br/>& Dynamic Z-Order Stack"]
  end

  GridCfg -->|"N x N size"| Generator
  SourceArt -->|"shared atlas"| Generator
  TouchDrag -->|"transform"| Generator

  Generator -->|"UV + mask ID"| Shader
  Generator <-->|"snap check"| SnapGroup
  Generator -.->|"tab bleed margin"| UVRect

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class Generator coreNode
  class GridCfg,SourceArt,TouchDrag,Shader,SnapGroup adjNode
  class UVRect intNode`
  },
  {
    id: 'sheet-06',
    sheetNum: 'Sheet 06 — Live Mermaid',
    title: 'Offline Leaderboard',
    domain: 'LiveOps / Retention',
    engine: 'Unity',
    labDemo: 'offline-leaderboard',
    mermaidFlowchart: `flowchart TB
  subgraph Events["SCORE & CLOCK INPUTS"]
    direction LR
    GameWin["Level Complete<br/>(SubmitRun Score / Ms)"]
    BootClock["System Realtime Clock<br/>(NowSec Elapsed Delta)"]
    ModeCfg["Season Mode Config<br/>(Best Time vs League Pts)"]
  end

  subgraph Core["CORE MODULE — OFFLINE LEADERBOARD"]
    SimEngine["OfflineLeaderboardService.cs<br/>(SyncAndCalculate & ModeSeasonState)"]
  end

  subgraph Runtime["STANDINGS & ANTI-ROLLBACK"]
    direction LR
    LeagueUI["Windowed Standings UI<br/>(Top 3 + Neighborhood)"]
    RewardBox["ClaimSeasonReward<br/>(PlayerPrefs Coin Grant)"]
    AntiCheat["29 Seeded BotState Sim<br/>& Anti-Rollback Clamp"]
  end

  GameWin -->|"rawScore"| SimEngine
  BootClock -->|"dtSec"| SimEngine
  ModeCfg -->|"modeId"| SimEngine

  SimEngine -->|"rank delta"| LeagueUI
  SimEngine -->|"tier chest"| RewardBox
  SimEngine -.->|"lastSeenSec clamp"| AntiCheat

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class SimEngine coreNode
  class GameWin,BootClock,ModeCfg,LeagueUI,RewardBox adjNode
  class AntiCheat intNode`
  },
  {
    id: 'sheet-08',
    sheetNum: 'Sheet 08 — Live Mermaid',
    title: 'Level Sync (OTA Content)',
    domain: 'Platform Services',
    engine: 'Unity',
    labDemo: 'native-image-bridge',
    mermaidFlowchart: `flowchart TB
  subgraph Triggers["BOOT & CDN ENDPOINTS"]
    direction LR
    AppBoot["Game Cold Boot<br/>(Zero-Block Start)"]
    RemoteCDN["GitHub Pages CDN<br/>(manifest.json + levels)"]
    Fallback["Offline Fallback<br/>(65 Linked-Chain Levels)"]
  end

  subgraph Core["CORE MODULE — OTA LEVEL SYNCHRONIZER"]
    SyncMgr["LevelSyncService.cs<br/>(FetchManifest & DownloadLevelCoroutine)"]
  end

  subgraph Runtime["PARSING & REGISTRY"]
    direction LR
    ActiveDB["AvailableLevelIds<br/>& LevelsBySize Registry"]
    JsonParser["MeowLevelData.FromJson<br/>(Parse2DArray Flat)"]
    HashDiff["Timeout Guard (5s)<br/>& Chained Level Fallback"]
  end

  AppBoot -->|"init coroutine"| SyncMgr
  RemoteCDN -->|"UnityWebRequest"| SyncMgr
  Fallback -->|"seed catalog"| SyncMgr

  SyncMgr -->|"index levels"| ActiveDB
  SyncMgr -->|"deserialize"| JsonParser
  SyncMgr -.->|"on offline"| HashDiff

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class SyncMgr coreNode
  class AppBoot,RemoteCDN,Fallback,ActiveDB,JsonParser adjNode
  class HashDiff intNode`
  },
  {
    id: 'sheet-04',
    sheetNum: 'Sheet 04 — Live Mermaid',
    title: 'IAP System Gateway',
    domain: 'Monetization',
    engine: 'Unity (Android + iOS)',
    labDemo: null,
    mermaidFlowchart: `flowchart TB
  subgraph StoreUI["STOREFRONT & CATALOG"]
    direction LR
    ShopBtn["Store Purchase UI<br/>(Purchase SKU)"]
    Catalog["Unified SKU Catalog<br/>(IAP + Soft Coins)"]
    Restore["Restore Purchases<br/>(Sync Entitlements)"]
  end

  subgraph Core["CORE MODULE — IAP SYSTEM GATEWAY"]
    Gateway["IAPStoreGateway.cs<br/>(Dual-Currency Router & Guard)"]
  end

  subgraph Providers["BILLING & CRASH RECOVERY"]
    direction LR
    StoreSDK["Google Play Billing<br/>& Apple StoreKit 2"]
    Inventory["Player Entitlement DB<br/>& Coin Wallet Ledger"]
    GrantFirst["Grant-Then-Confirm Log<br/>& Receipt ID Dedup"]
  end

  ShopBtn -->|"productId"| Gateway
  Catalog -->|"SKU config"| Gateway
  Restore -->|"re-verify"| Gateway

  Gateway <-->|"store tx"| StoreSDK
  Gateway -->|"1. grant save"| Inventory
  Gateway -.->|"2. confirm"| GrantFirst

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class Gateway coreNode
  class ShopBtn,Catalog,Restore,StoreSDK,Inventory adjNode
  class GrantFirst intNode`
  },
  {
    id: 'sheet-13',
    sheetNum: 'Sheet 13 — Live Mermaid',
    title: 'Argos Localization IPC',
    domain: 'Editor Tooling / IPC',
    engine: 'Unity + Python (offline)',
    labDemo: 'argos-localization',
    mermaidFlowchart: `flowchart TB
  subgraph EditorUI["UNITY EDITOR LOCALIZATION"]
    direction LR
    StringTable["Localization String Table<br/>(CSV / ScriptableObject)"]
    LangSelect["Target Locale Batch<br/>(EN to ES/FR/DE/JA/HI)"]
    TokenMask["Token Freezer (__TK0__)<br/>(Protects {0} & Tags)"]
  end

  subgraph Core["CORE MODULE — ARGOS IPC BRIDGE"]
    IPCController["ArgosIPCProcessRunner.cs<br/>(Persistent Subprocess + JSON Pipe)"]
  end

  subgraph PythonWorker["OFFLINE PYTHON WORKER"]
    direction LR
    WorkerLoop["argos_worker.py<br/>(UTF-8 stdin / stdout)"]
    CTranslate["Argos + CTranslate2<br/>(Offline Neural Engine)"]
    Watchdog["Warm Model Cache<br/>& Pipe Watchdog"]
  end

  StringTable -->|"source rows"| IPCController
  LangSelect -->|"locale codes"| IPCController
  TokenMask -->|"masked text"| IPCController

  IPCController <-->|"JSONL stdio"| WorkerLoop
  WorkerLoop <-->|"batch inference"| CTranslate
  IPCController -.->|"keep-alive"| Watchdog

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class IPCController coreNode
  class StringTable,LangSelect,TokenMask,WorkerLoop,CTranslate adjNode
  class Watchdog intNode`
  },
  {
    id: 'sheet-11',
    sheetNum: 'Sheet 11 — Live Mermaid',
    title: 'Asset Organizer Tool',
    domain: 'Unity Editor DX',
    engine: 'Unity Editor',
    labDemo: 'asset-organizer',
    mermaidFlowchart: `flowchart TB
  subgraph EditorHooks["UNITY EDITOR HOOKS"]
    direction LR
    ProjectGUI["projectWindowItemOnGUI<br/>(Row Rect & GUID)"]
    Postproc["AssetPostprocessor<br/>(Import & Move Watcher)"]
    Shortcut["Quick-Pin Dock<br/>& Filter Toolbar"]
  end

  subgraph Core["CORE MODULE — ASSET ORGANIZER"]
    Organizer["AssetOrganizerController.cs<br/>(GUID Registry + Rule Matcher)"]
  end

  subgraph Subsystems["EDITOR UX & METADATA"]
    direction LR
    BadgeDrawer["Custom Category Badges<br/>& Color Row Stripes"]
    RuleScanner["One-Click Folder Audit<br/>& Auto-Mover"]
    GuidMap["O(1) GUID Hash Cache<br/>(Zero Runtime Overhead)"]
  end

  ProjectGUI -->|"visible GUID"| Organizer
  Postproc -->|"asset delta"| Organizer
  Shortcut -->|"pin query"| Organizer

  Organizer -->|"draw overlay"| BadgeDrawer
  Organizer -->|"route folders"| RuleScanner
  Organizer -.->|"GUID lookup"| GuidMap

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class Organizer coreNode
  class ProjectGUI,Postproc,Shortcut,BadgeDrawer,RuleScanner adjNode
  class GuidMap intNode`
  },
  {
    id: 'sheet-12',
    sheetNum: 'Sheet 12 — Live Mermaid',
    title: 'Flame Flicker Effect',
    domain: 'Tech Art & Shaders',
    engine: 'Unity',
    labDemo: 'jigsaw-solitaire',
    mermaidFlowchart: `flowchart TB
  subgraph SceneInputs["UI CANVAS & PRESETS"]
    direction LR
    CanvasImg["UnityEngine.UI.Image<br/>(Target UI Graphic)"]
    PresetCfg["Flicker Profile Preset<br/>(Hearth / Torch / Candle)"]
    LightSync["2D Ambient Glow<br/>(Warmth Synchronizer)"]
  end

  subgraph Core["CORE MODULE — FLAME FLICKER DRIVER"]
    Driver["FlameFlickerDriver.cs<br/>+ ProceduralFlame.shader"]
  end

  subgraph ShaderPass["GPU UNIFORMS & FRAGMENT MATH"]
    direction LR
    MatBlock["MaterialPropertyBlock<br/>(_Phase, _Wind, _Intensity)"]
    FBM["2-Octave Simplex FBM<br/>(UV Domain Warp)"]
    SDF["Teardrop SDF Mask<br/>(Zero Particle GC)"]
  end

  CanvasImg -->|"graphic ref"| Driver
  PresetCfg -->|"turbulence"| Driver
  LightSync <-->|"luminance"| Driver

  Driver -->|"per-frame"| MatBlock
  Driver -->|"noise UV"| FBM
  Driver -.->|"fragment math"| SDF

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class Driver coreNode
  class CanvasImg,PresetCfg,LightSync,MatBlock,FBM adjNode
  class SDF intNode`
  }
];

let activeBlueprintIdx = 0;

function initBlueprintTerminal() {
  const navContainer = document.getElementById('blueprint-nav-list');
  const stageMermaid = document.getElementById('bp-stage-mermaid');
  const stageTitle = document.getElementById('bp-stage-title');
  const zoomBtn = document.getElementById('btn-zoom-blueprint');

  if (!navContainer) return;

  if (window.mermaid) {
    window.mermaid.initialize({
      startOnLoad: false,
      theme: 'dark',
      securityLevel: 'loose',
      fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
      flowchart: {
        htmlLabels: true,
        curve: 'basis',
        nodeSpacing: 26,
        rankSpacing: 34,
        padding: 16,
        subGraphTitleMargin: { top: 8, bottom: 12 },
        useMaxWidth: false
      },
      themeVariables: {
        darkMode: true,
        background: '#0c101a',
        primaryColor: '#0f1b2d',
        primaryTextColor: '#e2e8f0',
        primaryBorderColor: '#38bdf8',
        lineColor: '#64748b',
        secondaryColor: '#1e293b',
        tertiaryColor: '#0d121c',
        clusterBkg: '#0a0f18',
        clusterBorder: '#1e293b'
      }
    });
  }

  async function renderBlueprintStage(bp) {
    if (!bp) return;

    if (stageTitle) {
      stageTitle.textContent = bp.title;
    }

    const definition = bp.mermaidFlowchart;

    if (stageMermaid && window.mermaid) {
      try {
        const renderId = `mermaid-bp-${bp.id}-${Date.now()}`;
        const { svg } = await window.mermaid.render(renderId, definition);
        stageMermaid.innerHTML = svg;
        const svgEl = stageMermaid.querySelector('svg');
        if (svgEl) {
          try {
            const bbox = svgEl.getBBox();
            if (bbox && bbox.width > 0 && bbox.height > 0) {
              const pad = 6;
              svgEl.setAttribute('viewBox', `${bbox.x - pad} ${bbox.y - pad} ${bbox.width + pad * 2} ${bbox.height + pad * 2}`);
            }
          } catch (_) {}
          svgEl.removeAttribute('style');
          svgEl.setAttribute('width', '100%');
          svgEl.setAttribute('height', '100%');
          svgEl.setAttribute('preserveAspectRatio', 'xMidYMid meet');
          svgEl.style.width = '100%';
          svgEl.style.height = '100%';
          svgEl.style.maxHeight = '485px';
          svgEl.style.maxWidth = '100%';
        }
      } catch (err) {
        console.error('Mermaid render error:', err);
        stageMermaid.innerHTML = `<pre class="text-xs text-rose-400 font-mono p-4">${definition}</pre>`;
      }
    }
  }

  blueprintsData.forEach((bp, idx) => {
    const item = document.createElement('button');
    item.className = `w-full text-left px-2.5 py-2 rounded-lg border transition-all flex items-center justify-between gap-1.5 cursor-pointer ${idx === 0 ? 'bg-[#1a2333] border-sky-500/50 text-white' : 'bg-[#0f141f] border-slate-800 text-slate-400 hover:text-slate-200 hover:bg-[#161c28]'}`;
    item.setAttribute('data-index', idx);

    item.innerHTML = `
      <div class="min-w-0">
        <div class="font-bold text-[10.5px] leading-tight truncate">
          ${bp.title}
        </div>
        <div class="text-[9px] text-slate-500 font-mono truncate">${bp.domain}</div>
      </div>
      <span class="text-[9px] px-1 py-0.5 rounded bg-slate-800 text-sky-400 font-mono shrink-0">${bp.sheetNum.split(' ')[1]}</span>
    `;

    item.addEventListener('click', () => {
      activeBlueprintIdx = idx;
      document.querySelectorAll('#blueprint-nav-list button').forEach(b => {
        b.className = 'w-full text-left px-2.5 py-2 rounded-lg border transition-all flex items-center justify-between gap-1.5 cursor-pointer bg-[#0f141f] border-slate-800 text-slate-400 hover:text-slate-200 hover:bg-[#161c28]';
      });
      item.className = 'w-full text-left px-2.5 py-2 rounded-lg border transition-all flex items-center justify-between gap-1.5 cursor-pointer bg-[#1a2333] border-sky-500/50 text-white';

      renderBlueprintStage(bp);
    });

    navContainer.appendChild(item);
  });

  // Render initial blueprint (Sheet 10 -> MeowSweeper & Level Generator)
  renderBlueprintStage(blueprintsData[0]);

  if (zoomBtn) {
    zoomBtn.addEventListener('click', () => {
      const modal = document.getElementById('lightbox-modal');
      const modalImg = document.getElementById('lightbox-img');
      const modalMermaid = document.getElementById('lightbox-mermaid');
      const modalTitle = document.getElementById('lightbox-title');
      const currentBp = blueprintsData[activeBlueprintIdx];

      if (!modal || !currentBp) return;

      if (modalTitle) {
        modalTitle.textContent = `${currentBp.title} — Architecture Flowchart`;
      }

      if (modalMermaid && stageMermaid) {
        if (modalImg) modalImg.classList.add('hidden');
        modalMermaid.classList.remove('hidden');
        modalMermaid.innerHTML = stageMermaid.innerHTML;
        const svgEl = modalMermaid.querySelector('svg');
        if (svgEl) {
          svgEl.style.maxHeight = '82vh';
          svgEl.style.width = '100%';
          svgEl.style.height = '82vh';
        }
      }

      modal.classList.remove('hidden');
      modal.classList.add('flex');
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
