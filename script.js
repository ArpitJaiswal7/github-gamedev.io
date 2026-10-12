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
    mermaidFlowchart: `flowchart TD
  L1["LevelSyncService.cs<br/>FetchManifestCoroutine()<br/>DownloadLevelCoroutine()"]
  R1["MeowGameUI.cs (OnGUI / Update)<br/>CreateCatTexture() & RegionColors[12]<br/>DOUBLE_TAP_WINDOW (0.34s)"]

  L2["GenerateChainedLevel(N)<br/>GenerateValidCatSolution(N)<br/>IsEliminatedByEarlier() + 50% 1-Cell"]
  R2["Pointer & Touch Input Router<br/>SingleTapCross(r,c) & uSwipeTarget<br/>DoubleTapCat(r,c)"]

  L3["MeowLevelData.cs<br/>FromJson() & Parse2DArray()<br/>regionsFlat[N*N] / solutionFlat[N*N]"]
  R3["MeowGameManager.cs<br/>SetupBoard() & defaultCatCoord<br/>boardState[N,N] (0:Empty 1:X 2:Cat)"]

  L4["Power-Ups & 5-Lives Engine<br/>UseSmartHint() / UseCatReveal()<br/>UseMouseHelper() / remainingHearts"]
  R4["OfflineLeaderboardService.cs<br/>CheckWinCondition() -> SubmitRun()<br/>SyncAndCalculate() (29 BotState)"]

  L1 -->|"offline / shuffle"| L2
  L1 -->|"remote JSON"| L3
  R1 -->|"screen to (r,c)"| R2
  L2 -->|"perm[regions]"| L3
  R2 -->|"mutate cell"| R3
  L3 -->|"HasCat / GetRegion"| L4
  R3 -->|"power-up / strike"| L4
  R3 -->|"placedCats == N"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R3 coreNode
  class L1,R1,L3,R2 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  LEVEL_SYNC_SERVICE ||--|{ MEOW_LEVEL_DATA : "GenerateChainedLevel(N)"
  MEOW_GAME_UI ||--|| MEOW_GAME_MANAGER : "SingleTap / DoubleTapCat"
  LEVEL_SYNC_SERVICE ||--|| MEOW_GAME_MANAGER : "DownloadLevelCoroutine"
  MEOW_LEVEL_DATA ||--o{ MODE_SEASON_STATE : "gridSize / tier"
  MEOW_GAME_MANAGER ||--|| OFFLINE_LEADERBOARD_SERVICE : "SubmitRun(elapsedMs)"

  LEVEL_SYNC_SERVICE {
    string LIVE_REPO_BASE_URL
    List_string AvailableLevelIds
    Dict_int_List LevelsBySize
    bool levelToggle50Percent
  }
  MEOW_GAME_UI {
    Rect currentBoardRect
    Texture2D_array regionTextures
    int uSwipeTarget
    float DOUBLE_TAP_WINDOW
  }
  MEOW_LEVEL_DATA {
    string levelId
    int gridSize
    bool hasSingleCellColor
    Vector2Int defaultCatCoord
    int_array regionsFlat
    int_array solutionFlat
  }
  MEOW_GAME_MANAGER {
    int_matrix boardState
    bool_matrix isHighlightedCell
    bool_matrix hasMouseBadge
    int remainingHearts
    int powerCatReveal_Hint_Mouse
    float currentRunTimer
  }
  MODE_SEASON_STATE {
    string seasonId
    string modeId
    bool isCumulative
    int playerScore
    int currentRank
    List_BotState bots_29
  }
  OFFLINE_LEADERBOARD_SERVICE {
    string ActiveMode
    string PREF_WALLET
    string PREF_BEST_MS
    string PREF_LEAGUE_PTS
  }`
  },
  {
    id: 'sheet-02',
    sheetNum: 'Sheet 02 — Live Mermaid',
    title: 'Native Image Pipeline',
    domain: 'Platform Services',
    engine: 'Unity (Android + iOS)',
    labDemo: 'native-image-bridge',
    mermaidFlowchart: `flowchart TD
  L1["Level Select & Avatar UI<br/>LoadImageAsync(url, size)"]
  R1["Jigsaw Board TextureLoader<br/>HD Puzzle Source Request"]

  L2["In-Flight Request Deduplicator<br/>Coalesces Concurrent Callbacks"]
  R2["NativeImagePipeline.cs<br/>Shared Async C# API Surface"]

  L3["Android JNI Bridge<br/>Glide + OkHttp3 Thread Pool"]
  R3["iOS ObjC / Editor Fallback<br/>NSURLSession + UnityWebRequest"]

  L4["L1 RAM LRU Texture2D Pool<br/>& L2 SHA-256 Disk Cache"]
  R4["Hardware InSampleSize<br/>Off-Thread Bitmap Decoder"]

  L1 -->|"request"| L2
  R1 -->|"request"| R2
  L2 -->|"deduped"| L3
  R2 -->|"dispatch"| R3
  L3 -->|"cache write"| L4
  R3 -->|"decode"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R2 coreNode
  class L1,R1,L3,R3 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  IMAGE_REQUEST ||--o| MEMORY_LRU_ENTRY : "checks L1"
  DEDUP_CALLBACK_GROUP ||--|| NATIVE_DECODE_JOB : "dispatches"
  MEMORY_LRU_ENTRY ||--o| DISK_CACHE_RECORD : "falls back L2"
  NATIVE_DECODE_JOB ||--|| TEXTURE2D_HANDLE : "uploads GPU"

  IMAGE_REQUEST {
    string urlHashSha256
    string sourceUrl
    int targetWidth_Height
  }
  DEDUP_CALLBACK_GROUP {
    string urlHashSha256
    int subscriberCount
  }
  MEMORY_LRU_ENTRY {
    int textureInstanceId
    int byteFootprint
  }
  NATIVE_DECODE_JOB {
    string platformBackend
    int sampleFactor
  }
  DISK_CACHE_RECORD {
    string filePath
    string etagHeader
  }
  TEXTURE2D_HANDLE {
    int width_height
    string format_RGBA32
  }`
  },
  {
    id: 'sheet-09',
    sheetNum: 'Sheet 09 — Live Mermaid',
    title: 'Jigsaw Board Renderer',
    domain: 'Core Gameplay Logic',
    engine: 'Unity',
    labDemo: 'calm-jigsaw',
    mermaidFlowchart: `flowchart TD
  L1["Grid Preset (3x3..10x10)<br/>& Pointer Drag Input"]
  R1["HD Source Texture2D<br/>Single Shared Material Atlas"]

  L2["Complementary Edge Solver<br/>(Flat=0 / Tab=+1 / Blank=-1)"]
  R2["ProceduralBoardGenerator.cs<br/>N x N Piece Spawner & UV Mapper"]

  L3["PieceMaskShader.shader<br/>9-Slice Bevel & Alpha Cutout"]
  R3["Union-Find Cluster Merger<br/>Magnetic Neighbor Snap Check"]

  L4["Oversized UV Quad (+28%)<br/>Prevents Tab Bleed Clipping"]
  R4["Dynamic SortingGroup Stack<br/>& Scrollable Piece Tray"]

  L1 -->|"dimensions"| L2
  R1 -->|"shared atlas"| R2
  L2 -->|"edge IDs"| L3
  R2 -->|"piece transform"| R3
  L3 -->|"UV offset"| L4
  R3 -->|"z-order & tray"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R2 coreNode
  class L1,R1,L3,R3 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  JIGSAW_BOARD ||--|{ JIGSAW_PIECE : "generates N x N"
  UNION_CLUSTER ||--|{ EDGE_SIGNATURE : "validates snap"
  JIGSAW_PIECE ||--|| SHADER_UV_BLOCK : "projects"
  EDGE_SIGNATURE ||--|| TRAY_SAVE_STATE : "serializes"

  JIGSAW_BOARD {
    string puzzleId
    int rows_cols
    float snapRadiusPx
  }
  UNION_CLUSTER {
    int clusterRootId
    int_array memberIds
    boolean isLocked
  }
  JIGSAW_PIECE {
    int pieceIndex
    int gridRow_Col
    float worldX_Y
  }
  EDGE_SIGNATURE {
    int top_right
    int bottom_left
  }
  SHADER_UV_BLOCK {
    float4 uvScaleOffset
    int maskAtlasId
  }
  TRAY_SAVE_STATE {
    int lockedCount
    float elapsedSec
  }`
  },
  {
    id: 'sheet-06',
    sheetNum: 'Sheet 06 — Live Mermaid',
    title: 'Offline Leaderboard',
    domain: 'LiveOps / Retention',
    engine: 'Unity',
    labDemo: 'offline-leaderboard',
    mermaidFlowchart: `flowchart TD
  L1["Level Win (+Score / BestMs)<br/>SubmitRun(modeId, rawScore)"]
  R1["System Realtime Clock<br/>NowSec() & AdvanceTimeHours()"]

  L2["ModeSeasonState Manager<br/>GetOrCreateSeason(modeId)"]
  R2["OfflineLeaderboardService.cs<br/>SyncAndCalculate(modeId)"]

  L3["29 Seeded BotState Sim<br/>(elite / rival / steady / casual)"]
  R3["Monotonic Timestamp Clamp<br/>Anti-Rollback Clock Guard"]

  L4["Windowed Standings Builder<br/>Top 3 + Player Neighborhood"]
  R4["ClaimSeasonReward() & Wallet<br/>PlayerPrefs Persistence"]

  L1 -->|"score delta"| L2
  R1 -->|"dtSec"| R2
  L2 -->|"bot curves"| L3
  R2 -->|"lastSeenSec"| R3
  L3 -->|"sorted ranks"| L4
  R3 -->|"reward tier"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R2 coreNode
  class L1,R1,L3,R3 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  MODE_SEASON_STATE ||--|{ BOT_STATE : "simulates 29 bots"
  OFFLINE_LEADERBOARD_SVC ||--|| SYNC_RESULT : "computes"
  BOT_STATE ||--|| ROW_VIEW : "formats row"
  SYNC_RESULT ||--|| PLAYER_PREFS_WALLET : "grants coins"

  MODE_SEASON_STATE {
    string seasonId
    string modeId
    bool isCumulative
    double endTimeSec
  }
  OFFLINE_LEADERBOARD_SVC {
    string ActiveMode
    double simOffsetSec
  }
  BOT_STATE {
    string botId
    string archetype
    int score_targetEnd
  }
  SYNC_RESULT {
    int playerRank
    int rankDelta
    string phaseText
  }
  ROW_VIEW {
    int rank
    string formattedScore
    string rewardLabel
  }
  PLAYER_PREFS_WALLET {
    int MeowWebGL_WalletCoins
    int MeowWebGL_BestTimeMs
  }`
  },
  {
    id: 'sheet-08',
    sheetNum: 'Sheet 08 — Live Mermaid',
    title: 'Level Sync (OTA Content)',
    domain: 'Platform Services',
    engine: 'Unity',
    labDemo: 'native-image-bridge',
    mermaidFlowchart: `flowchart TD
  L1["Cold Boot Sequence<br/>Zero-Block Instant Mount"]
  R1["Remote GitHub Pages CDN<br/>manifest.json + levels/*.json"]

  L2["Active Level Registry<br/>AvailableLevelIds & LevelsBySize"]
  R2["LevelSyncService.cs<br/>FetchManifestCoroutine()"]

  L3["Manifest Parser & Differ<br/>ParseManifestLevels(json)"]
  R3["DownloadLevelCoroutine(id)<br/>UnityWebRequest Async Fetch"]

  L4["Offline 65-Level Catalog<br/>& GenerateValidPuzzle(N)"]
  R4["MeowLevelData.FromJson()<br/>Zero-Allocation Array Parser"]

  L1 -->|"mount"| L2
  R1 -->|"HTTP GET"| R2
  L2 -->|"index"| L3
  R2 -->|"fetch level"| R3
  L3 -->|"fallback"| L4
  R3 -->|"deserialize"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R2 coreNode
  class L1,R1,L3,R3 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  REMOTE_MANIFEST ||--|{ LEVEL_MANIFEST_META : "indexes"
  LEVEL_SYNC_SERVICE ||--|{ DOWNLOAD_COROUTINE : "runs"
  LEVEL_MANIFEST_META ||--|| MEOW_LEVEL_DATA : "maps to"
  DOWNLOAD_COROUTINE ||--|| CHAINED_FALLBACK : "triggers on timeout"

  REMOTE_MANIFEST {
    string LIVE_REPO_BASE_URL
    int totalLevels_65
  }
  LEVEL_SYNC_SERVICE {
    List_string AvailableLevelIds
    Dict_int_List LevelsBySize
  }
  LEVEL_MANIFEST_META {
    string levelId
    int gridSize
    string tier_hash
  }
  DOWNLOAD_COROUTINE {
    string url
    int timeoutSec_5
  }
  MEOW_LEVEL_DATA {
    int_array regionsFlat
    int_array solutionFlat
  }
  CHAINED_FALLBACK {
    int gridSize_N
    bool useSingleCellColor
  }`
  },
  {
    id: 'sheet-04',
    sheetNum: 'Sheet 04 — Live Mermaid',
    title: 'IAP System Gateway',
    domain: 'Monetization',
    engine: 'Unity (Android + iOS)',
    labDemo: null,
    mermaidFlowchart: `flowchart TD
  L1["Store UI Purchase Button<br/>Purchase(productId)"]
  R1["Unified SKU Catalog<br/>Real Money & Soft Coins"]

  L2["Transaction Guard & Router<br/>Validates Active SKU State"]
  R2["IAPStoreGateway.cs<br/>Dual-Currency Store Controller"]

  L3["Google Play Billing<br/>& Apple StoreKit 2 Bridge"]
  R3["Grant-Then-Confirm Flow<br/>Crash-Safe Entitlement Write"]

  L4["Receipt Signature Validator<br/>& Transaction ID Dedup"]
  R4["Player Entitlement DB<br/>& Local Coin Ledger"]

  L1 -->|"initiate"| L2
  R1 -->|"metadata"| R2
  L2 -->|"platform SDK"| L3
  R2 -->|"1. grant"| R3
  L3 -->|"2. verify"| L4
  R3 -->|"persist"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R2 coreNode
  class L1,R1,L3,R3 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  STORE_CATALOG ||--|{ PRODUCT_SKU : "defines"
  IAP_GATEWAY ||--|{ PURCHASE_TX : "processes"
  PRODUCT_SKU ||--|| ENTITLEMENT_GRANT : "unlocks"
  PURCHASE_TX ||--|| RECEIPT_LEDGER : "confirms"

  STORE_CATALOG {
    string catalogVersion
    int skuCount
  }
  IAP_GATEWAY {
    bool isInitialized
    string activeStore
  }
  PRODUCT_SKU {
    string productId
    string currencyType
    float priceAmount
  }
  PURCHASE_TX {
    string transactionId
    string status
  }
  ENTITLEMENT_GRANT {
    string rewardType
    int quantity
  }
  RECEIPT_LEDGER {
    string signatureHash
    boolean storeConfirmed
  }`
  },
  {
    id: 'sheet-13',
    sheetNum: 'Sheet 13 — Live Mermaid',
    title: 'Argos Localization IPC',
    domain: 'Editor Tooling / IPC',
    engine: 'Unity + Python (offline)',
    labDemo: 'argos-localization',
    mermaidFlowchart: `flowchart TD
  L1["Unity Localization Table<br/>CSV & ScriptableObject Rows"]
  R1["Target Locale Batcher<br/>(EN to ES / FR / DE / JA / HI)"]

  L2["Token Masker (__TK0__)<br/>Freezes {0} & RichText Tags"]
  R2["ArgosIPCProcessRunner.cs<br/>Persistent Subprocess & Pipe"]

  L3["argos_worker.py<br/>UTF-8 stdin / stdout JSONL"]
  R3["Argos + CTranslate2<br/>Offline Neural Translation"]

  L4["Token Unmasker & Validator<br/>Restores Protected Markup"]
  R4["Warm Model Memory Cache<br/>& Pipe Timeout Watchdog"]

  L1 -->|"source text"| L2
  R1 -->|"locale codes"| R2
  L2 -->|"masked JSONL"| L3
  R2 -->|"stdin pipe"| R3
  L3 -->|"unmask"| L4
  R3 -->|"stdout JSONL"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R2 coreNode
  class L1,R1,L3,R3 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  LOCALIZATION_TABLE ||--|{ STRING_ROW : "contains"
  ARGOS_IPC_RUNNER ||--|{ IPC_BATCH_JOB : "streams stdio"
  STRING_ROW ||--|| TOKEN_MASK_MAP : "freezes tags"
  IPC_BATCH_JOB ||--|| WORKER_RESPONSE : "returns JSONL"

  LOCALIZATION_TABLE {
    string tableAssetGuid
    string sourceLocale
  }
  ARGOS_IPC_RUNNER {
    int pythonPid
    bool isModelWarm
  }
  STRING_ROW {
    string entryKey
    string sourceText
  }
  IPC_BATCH_JOB {
    string requestId
    string targetLang
  }
  TOKEN_MASK_MAP {
    string tokenKey
    string rawMarkup
  }
  WORKER_RESPONSE {
    string status
    int inferenceMs
  }`
  },
  {
    id: 'sheet-11',
    sheetNum: 'Sheet 11 — Live Mermaid',
    title: 'Asset Organizer Tool',
    domain: 'Unity Editor DX',
    engine: 'Unity Editor',
    labDemo: 'asset-organizer',
    mermaidFlowchart: `flowchart TD
  L1["projectWindowItemOnGUI<br/>Row Rect & GUID Callback"]
  R1["AssetPostprocessor<br/>Import & Move Watcher"]

  L2["O(1) GUID Hash Registry<br/>Rename & Move Resilient"]
  R2["AssetOrganizerController.cs<br/>EditorWindow & Rule Engine"]

  L3["Custom Category Badges<br/>& Quick-Pin Favorites Bar"]
  R3["One-Click Folder Audit<br/>& Auto-Route Mover"]

  L4["ScriptableObject Metadata<br/>& EditorPrefs Persistence"]
  R4["Editor-Only Assembly Def<br/>Zero Runtime Build Overhead"]

  L1 -->|"row GUID"| L2
  R1 -->|"moved paths"| R2
  L2 -->|"draw badge"| L3
  R2 -->|"route rule"| R3
  L3 -->|"save pin"| L4
  R3 -->|"batch edit"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R2 coreNode
  class L1,R1,L3,R3 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  ORGANIZER_DB ||--|{ ASSET_GUID_NODE : "indexes"
  EDITOR_WINDOW_UI ||--|{ CATEGORY_BADGE : "renders"
  ASSET_GUID_NODE ||--|| ROUTING_RULE : "matches"
  CATEGORY_BADGE ||--|| PINNED_FAVORITE : "bookmarks"

  ORGANIZER_DB {
    string projectVersion
    int trackedCount
  }
  EDITOR_WINDOW_UI {
    string searchFilter
    int activeTab
  }
  ASSET_GUID_NODE {
    string assetGuidHex
    string cachedPath
  }
  CATEGORY_BADGE {
    string badgeLabel
    string hexColor
  }
  ROUTING_RULE {
    string extPattern
    string targetFolder
  }
  PINNED_FAVORITE {
    int sortOrder
    long pinnedUtc
  }`
  },
  {
    id: 'sheet-12',
    sheetNum: 'Sheet 12 — Live Mermaid',
    title: 'Flame Flicker Effect',
    domain: 'Tech Art & Shaders',
    engine: 'Unity',
    labDemo: 'jigsaw-solitaire',
    mermaidFlowchart: `flowchart TD
  L1["UnityEngine.UI.Image<br/>or SpriteRenderer Target"]
  R1["Flicker Profile Preset<br/>(Hearth / Torch / Candle)"]

  L2["FlameFlickerDriver.cs<br/>Per-Frame C# Uniform Driver"]
  R2["ProceduralFlame.shader<br/>Custom UI Fragment Shader"]

  L3["MaterialPropertyBlock<br/>(_Phase, _Wind, _Intensity)"]
  R3["2-Octave Simplex FBM<br/>UV Domain Warping"]

  L4["Synchronized 2D Glow<br/>& Thermal FPS Throttler"]
  R4["Teardrop SDF Core Mask<br/>Zero Particle GC Allocations"]

  L1 -->|"graphic ref"| L2
  R1 -->|"palette"| R2
  L2 -->|"set uniforms"| L3
  R2 -->|"sample noise"| R3
  L3 -->|"pulse light"| L4
  R3 -->|"alpha mask"| R4

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1.5px,stroke-dasharray: 5 5,color:#cbd5e1

  class L2,R2 coreNode
  class L1,R1,L3,R3 adjNode
  class L4,R4 intNode`,
    mermaidEr: `erDiagram
  FLAME_PRESET ||--|| COLOR_RAMP : "blends"
  FLAME_DRIVER_CS ||--|{ SHADER_UNIFORM : "updates"
  COLOR_RAMP ||--|| FRAGMENT_SDF_PASS : "tints"
  SHADER_UNIFORM ||--|| GLOW_SYNCHRONIZER : "pulses"

  FLAME_PRESET {
    string presetId
    float scrollSpeed
    float turbulence
  }
  FLAME_DRIVER_CS {
    int targetFpsCap
    bool usePropBlock
  }
  COLOR_RAMP {
    string coreHex
    string midFlameHex
  }
  SHADER_UNIFORM {
    float timePhase
    float2 uvWarpOffset
  }
  FRAGMENT_SDF_PASS {
    int noiseOctaves_2
    int gcAlloc_0
  }
  GLOW_SYNCHRONIZER {
    float baseAlpha
    float pulseRadius
  }`
  }
];

let activeBlueprintIdx = 0;
let activeBlueprintDiagramMode = 'flowchart'; // 'flowchart' | 'er'

function initBlueprintTerminal() {
  const navContainer = document.getElementById('blueprint-nav-list');
  const stageMermaid = document.getElementById('bp-stage-mermaid');
  const stageTitle = document.getElementById('bp-stage-title');
  const btnModeFlowchart = document.getElementById('bp-mode-flowchart');
  const btnModeEr = document.getElementById('bp-mode-er');
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
        nodeSpacing: 22,
        rankSpacing: 28,
        padding: 8,
        useMaxWidth: false
      },
      er: {
        layoutDirection: 'TB',
        minEntityWidth: 90,
        minEntityHeight: 55,
        entityPadding: 8,
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

  function updateDiagramModeButtons() {
    const inactiveClass = 'px-3 py-1 rounded-md font-semibold transition-all cursor-pointer flex items-center gap-1.5 text-slate-400 hover:text-white';
    if (btnModeFlowchart) {
      btnModeFlowchart.className = activeBlueprintDiagramMode === 'flowchart'
        ? 'px-3 py-1 rounded-md font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-sky-500 text-slate-950 shadow'
        : inactiveClass;
    }
    if (btnModeEr) {
      btnModeEr.className = activeBlueprintDiagramMode === 'er'
        ? 'px-3 py-1 rounded-md font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-amber-500 text-slate-950 shadow'
        : inactiveClass;
    }
  }

  async function renderBlueprintStage(bp) {
    if (!bp) return;

    if (stageTitle) {
      stageTitle.textContent = bp.title;
    }
    updateDiagramModeButtons();

    const definition = activeBlueprintDiagramMode === 'er' && bp.mermaidEr
      ? bp.mermaidEr
      : bp.mermaidFlowchart;

    if (stageMermaid && window.mermaid) {
      try {
        const renderId = `mermaid-bp-${bp.id}-${activeBlueprintDiagramMode}-${Date.now()}`;
        const { svg } = await window.mermaid.render(renderId, definition);
        stageMermaid.innerHTML = svg;
        const svgEl = stageMermaid.querySelector('svg');
        if (svgEl) {
          svgEl.removeAttribute('style');
          svgEl.setAttribute('width', '100%');
          svgEl.setAttribute('height', '100%');
          svgEl.setAttribute('preserveAspectRatio', 'xMidYMid meet');
          svgEl.style.width = '100%';
          svgEl.style.height = '100%';
          svgEl.style.maxHeight = '525px';
          svgEl.style.maxWidth = '100%';
        }
      } catch (err) {
        console.error('Mermaid render error:', err);
        stageMermaid.innerHTML = `<pre class="text-xs text-rose-400 font-mono p-4">${definition}</pre>`;
      }
    }
  }

  if (btnModeFlowchart) {
    btnModeFlowchart.addEventListener('click', () => {
      activeBlueprintDiagramMode = 'flowchart';
      renderBlueprintStage(blueprintsData[activeBlueprintIdx]);
    });
  }

  if (btnModeEr) {
    btnModeEr.addEventListener('click', () => {
      activeBlueprintDiagramMode = 'er';
      renderBlueprintStage(blueprintsData[activeBlueprintIdx]);
    });
  }

  blueprintsData.forEach((bp, idx) => {
    const item = document.createElement('button');
    item.className = `w-full text-left p-2.5 rounded-lg border transition-all flex items-center justify-between cursor-pointer ${idx === 0 ? 'bg-[#1a2333] border-sky-500/50 text-white' : 'bg-[#0f141f] border-slate-800 text-slate-400 hover:text-slate-200 hover:bg-[#161c28]'}`;
    item.setAttribute('data-index', idx);

    item.innerHTML = `
      <div>
        <div class="font-bold text-xs flex items-center gap-1.5">
          <span>${bp.title}</span>
        </div>
        <div class="text-[10px] text-slate-500 font-mono">${bp.domain}</div>
      </div>
      <span class="text-[10px] px-1.5 py-0.5 rounded bg-slate-800 text-sky-400 font-mono">${bp.sheetNum.split(' ')[1]}</span>
    `;

    item.addEventListener('click', () => {
      activeBlueprintIdx = idx;
      document.querySelectorAll('#blueprint-nav-list button').forEach(b => {
        b.className = 'w-full text-left p-2.5 rounded-lg border transition-all flex items-center justify-between cursor-pointer bg-[#0f141f] border-slate-800 text-slate-400 hover:text-slate-200 hover:bg-[#161c28]';
      });
      item.className = 'w-full text-left p-2.5 rounded-lg border transition-all flex items-center justify-between cursor-pointer bg-[#1a2333] border-sky-500/50 text-white';

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
        modalTitle.textContent = `${currentBp.title} — ${activeBlueprintDiagramMode === 'er' ? 'E-R Data Schema' : 'Architecture Flowchart'}`;
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
