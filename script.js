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
    title: 'Meowdoku Engine & Solver (MeowSweeper)',
    domain: 'Core Gameplay Logic',
    engine: 'Unity 6000.3 + Live CDN',
    desc: 'Multi-touch gesture router with input locks during animations, 5-tier deterministic hint cascade, and cross-level powerup wallet.',
    labDemo: 'meowsweeper',
    mermaidFlowchart: `flowchart TB
  subgraph Inputs["INPUT & CONTENT LAYER"]
    direction LR
    Tap["Single Tap (Mark X)"]
    DblTap["Double Tap (Place Cat)"]
    Drag["Drag Gesture (Paint X)"]
    CDN["Live Level Server (lvl_1..65.json)"]
  end

  subgraph Core["CORE MODULE — MEOWDOKU ENGINE"]
    Controller["PuzzleController (Input Lock + State Machine)"]
  end

  subgraph Adjacent["ADJACENT RUNTIME SYSTEMS"]
    direction LR
    Solver["Deterministic Solver (Validates Moves & Hints)"]
    Wallet["Powerup Wallet (Hint / Cat Reveal / Coins)"]
    Score["Score & Save Progress (Offline Leaderboard Sync)"]
    Penalty["Heart Penalty & Lock (3-Heart Strike Rule)"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    Cascade["5-Tier Hint Cascade (Single-Empty to Elimination)"]
    Undo["Correct-Only Undo Stack"]
    Hash["Save-vs-Level Hash Validation"]
  end

  Tap -->|"mark cell"| Controller
  DblTap -->|"place cat"| Controller
  Drag -->|"batch paint"| Controller
  CDN -->|"manifest + grid config"| Controller

  Controller <-->|"verify solution"| Solver
  Controller <-->|"spend / grant"| Wallet
  Controller -->|"valid placement"| Score
  Controller -->|"invalid placement"| Penalty

  Controller -.-> Cascade
  Controller -.-> Undo
  Controller -.-> Hash

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Controller coreNode
  class Tap,DblTap,Drag,CDN,Solver,Wallet,Score,Penalty adjNode
  class Cascade,Undo,Hash intNode`,
    mermaidEr: `erDiagram
  LEVEL_MANIFEST ||--o{ LEVEL_DATA : "indexes 65 levels"
  LEVEL_DATA ||--|| BOARD_STATE : "initializes"
  BOARD_STATE ||--|{ CELL_NODE : "contains N x N"
  BOARD_STATE ||--o{ HINT_CASCADE_STEP : "evaluates"
  PLAYER_PROFILE ||--|| POWERUP_WALLET : "owns"
  PLAYER_PROFILE ||--o{ BOARD_STATE : "saves progress"
  BOARD_STATE ||--o| LEADERBOARD_ENTRY : "submits on win"

  LEVEL_MANIFEST {
    int version
    int totalLevels
    string_array levelIds
  }
  LEVEL_DATA {
    int id
    int gridSize
    int_matrix regions
    int_matrix solutionCats
    int maxHearts
  }
  BOARD_STATE {
    int levelId
    string levelHash
    int remainingHearts
    int elapsedMs
    boolean isInputLocked
  }
  CELL_NODE {
    int row
    int col
    int regionId
    string state "EMPTY_CROSS_CAT"
    boolean isLocked
  }
  HINT_CASCADE_STEP {
    int techniqueTier "1_to_5"
    string ruleName
    int_array targetCells
  }
  POWERUP_WALLET {
    int hintCount
    int catRevealCount
    int coinsBalance
  }
  LEADERBOARD_ENTRY {
    string presetId "meowsweeper"
    int bestTimeMs
    int starsEarned
  }`
  },
  {
    id: 'sheet-02',
    sheetNum: 'Sheet 02 — Live Mermaid',
    title: 'Native Image Pipeline',
    domain: 'Platform Services',
    engine: 'Unity (Android + iOS)',
    desc: 'Fetches, resizes, and caches remote images across Android, iOS, and Unity Editor through one shared C# API surface.',
    labDemo: 'native-image-bridge',
    mermaidFlowchart: `flowchart TB
  subgraph Callers["GAMEPLAY & UI CALLERS"]
    direction LR
    LevelThumb["Level Select Grid"]
    JigsawTex["Jigsaw Board TextureLoader"]
    AvatarUI["Leaderboard Avatar UI"]
  end

  subgraph Core["CORE MODULE — NATIVE IMAGE BRIDGE"]
    Facade["NativeImagePipeline (Shared C# Async API + Request Deduplicator)"]
  end

  subgraph Bridges["PLATFORM JNI / OBJC BRIDGES"]
    direction LR
    Android["Android Bridge (Glide + OkHttp3 Pool)"]
    iOS["iOS Bridge (NSURLSession + vImage)"]
    Editor["UnityWebRequest Fallback (Editor / WebGL)"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    L1Cache["L1 Memory LRU Cache (Texture2D Pool)"]
    L2Disk["L2 Disk SHA-256 Cache (Application.temporaryCachePath)"]
    Downsample["InSampleSize / Hardware Downsampler"]
  end

  LevelThumb -->|"LoadImageAsync(url, size)"| Facade
  JigsawTex -->|"LoadImageAsync(url, size)"| Facade
  AvatarUI -->|"LoadImageAsync(url, size)"| Facade

  Facade -->|"AndroidJNI"| Android
  Facade -->|"DllImport(__Internal)"| iOS
  Facade -->|"Editor / Fallback"| Editor

  Facade -.-> L1Cache
  Facade -.-> L2Disk
  Facade -.-> Downsample

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Facade coreNode
  class LevelThumb,JigsawTex,AvatarUI,Android,iOS,Editor adjNode
  class L1Cache,L2Disk,Downsample intNode`,
    mermaidEr: `erDiagram
  IMAGE_REQUEST ||--o| INFLIGHT_DEDUP_GROUP : "coalesces callbacks"
  IMAGE_REQUEST ||--o| MEMORY_LRU_ENTRY : "checks L1 first"
  IMAGE_REQUEST ||--o| DISK_CACHE_RECORD : "checks L2 second"
  DISK_CACHE_RECORD ||--|| NATIVE_DECODE_JOB : "decodes off-thread"

  IMAGE_REQUEST {
    string urlHashSha256
    string sourceUrl
    int targetWidth
    int targetHeight
    boolean generateMipmaps
  }
  MEMORY_LRU_ENTRY {
    string urlHashSha256
    int textureInstanceId
    int byteFootprint
    long lastAccessTick
  }
  DISK_CACHE_RECORD {
    string urlHashSha256
    string filePath
    long fileSizeBytes
    string etagHeader
  }
  NATIVE_DECODE_JOB {
    string platformBackend "GLIDE_NSURL_UWR"
    int sampleFactor
    int decodeDurationMs
  }`
  },
  {
    id: 'sheet-09',
    sheetNum: 'Sheet 09 — Live Mermaid',
    title: 'Jigsaw Board Renderer',
    domain: 'Core Gameplay Logic',
    engine: 'Unity',
    desc: 'Dynamic procedural N×N board generator using 9 tab/blank edge signatures with custom shader UV quad projections.',
    labDemo: 'calm-jigsaw',
    mermaidFlowchart: `flowchart TB
  subgraph Inputs["PUZZLE CONFIG & TEXTURE INPUT"]
    direction LR
    GridCfg["Grid Preset (3x3 up to 10x10)"]
    SourceArt["HD Artwork Texture2D"]
    TouchDrag["Pointer / Multi-Touch Drag"]
  end

  subgraph Core["CORE MODULE — JIGSAW BOARD RENDERER"]
    Generator["ProceduralBoardGenerator (Edge Signature Matrix + UV Mapper)"]
  end

  subgraph Runtime["ADJACENT RUNTIME SYSTEMS"]
    direction LR
    Shader["PieceMaskShader (9-Slice Bevel + Alpha Mask)"]
    SnapGroup["Union-Find Cluster Merger (Magnetic Neighbor Snap)"]
    Tray["Scrollable Piece Tray & Board Save State"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    EdgeRng["Complementary Edge Solver (Flat=0 / Tab=+1 / Blank=-1)"]
    UVRect["Oversized UV Quad Offset (+28% Tab Bleed Margin)"]
    ZSort["Dynamic SortingGroup Z-Order Stack"]
  end

  GridCfg -->|"N x N dimensions"| Generator
  SourceArt -->|"shared material atlas"| Generator
  TouchDrag -->|"piece transform"| Generator

  Generator -->|"per-piece UV + mask IDs"| Shader
  Generator <-->|"neighbor tolerance check"| SnapGroup
  Generator -->|"serialize placed state"| Tray

  Generator -.-> EdgeRng
  Generator -.-> UVRect
  Generator -.-> ZSort

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Generator coreNode
  class GridCfg,SourceArt,TouchDrag,Shader,SnapGroup,Tray adjNode
  class EdgeRng,UVRect,ZSort intNode`,
    mermaidEr: `erDiagram
  JIGSAW_BOARD ||--|{ JIGSAW_PIECE : "generates N x N"
  JIGSAW_PIECE }|--|| UNION_CLUSTER : "merges into"
  JIGSAW_PIECE ||--|| EDGE_SIGNATURE : "has 4 edges"
  JIGSAW_PIECE ||--|| SHADER_UV_BLOCK : "projects"

  JIGSAW_BOARD {
    string puzzleId
    int rows
    int cols
    float snapRadiusPx
    int lockedCount
  }
  JIGSAW_PIECE {
    int pieceIndex
    int gridRow
    int gridCol
    float worldX
    float worldY
    boolean isPlacedOnBoard
  }
  EDGE_SIGNATURE {
    int topEdge "0_FLAT_1_TAB_MINUS1_BLANK"
    int rightEdge
    int bottomEdge
    int leftEdge
  }
  UNION_CLUSTER {
    int clusterRootId
    int_array memberPieceIds
    boolean isLockedToGrid
  }
  SHADER_UV_BLOCK {
    float4 uvScaleOffset
    int maskAtlasIndex
  }`
  },
  {
    id: 'sheet-06',
    sheetNum: 'Sheet 06 — Live Mermaid',
    title: 'Offline Leaderboard',
    domain: 'LiveOps / Retention',
    engine: 'Unity',
    desc: 'Zero-server-cost bot simulation engine generating dynamic seasonal leagues, rank advancement, and anti-rollback clamping.',
    labDemo: 'offline-leaderboard',
    mermaidFlowchart: `flowchart TB
  subgraph Events["GAMEPLAY SCORE & CLOCK INPUTS"]
    direction LR
    GameWin["Level Complete (+Score / +Trophies)"]
    BootClock["App Launch / System UTC Clock"]
    GamePreset["Game Profile (MeowSweeper / Calm Jigsaw / Solitaire)"]
  end

  subgraph Core["CORE MODULE — OFFLINE LEADERBOARD ENGINE"]
    SimEngine["LeaderboardSimulator (Deterministic Bot Curve + League State)"]
  end

  subgraph Adjacent["ADJACENT RUNTIME SYSTEMS"]
    direction LR
    LeagueUI["Animated Rank-Up & Division Table UI"]
    RewardBox["Season End Promotion & Chest Grant"]
    SaveStore["Local Encrypted JSON Persistence"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    AntiCheat["Monotonic High-Water Timestamp Clamp (Anti-Rollback)"]
    BotArchetype["Seeded Bot Archetypes (Sprinter / Grinder / Casual)"]
    Rubberband["Dynamic Player-Pacing Rubberband Curve"]
  end

  GameWin -->|"AddPlayerScore(delta)"| SimEngine
  BootClock -->|"SimulateElapsedHours(dt)"| SimEngine
  GamePreset -->|"load division config"| SimEngine

  SimEngine -->|"sorted standings + delta"| LeagueUI
  SimEngine -->|"top-3 promotion"| RewardBox
  SimEngine <-->|"read / write snapshot"| SaveStore

  SimEngine -.-> AntiCheat
  SimEngine -.-> BotArchetype
  SimEngine -.-> Rubberband

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class SimEngine coreNode
  class GameWin,BootClock,GamePreset,LeagueUI,RewardBox,SaveStore adjNode
  class AntiCheat,BotArchetype,Rubberband intNode`,
    mermaidEr: `erDiagram
  SEASON_LEAGUE ||--|{ BOT_COMPETITOR : "simulates 29 bots"
  SEASON_LEAGUE ||--|| PLAYER_STANDING : "tracks user"
  SEASON_LEAGUE ||--|| CLOCK_GUARD : "enforces anti-rollback"
  SEASON_LEAGUE ||--o| REWARD_TIER : "awards on expiry"

  SEASON_LEAGUE {
    string leagueId
    string division "BRONZE_SILVER_GOLD_DIAMOND"
    long seasonStartUtc
    long seasonEndUtc
    int promotionCutoffRank
  }
  PLAYER_STANDING {
    string displayName
    int currentScore
    int previousRank
    int currentRank
  }
  BOT_COMPETITOR {
    int botSeedId
    string handle
    string archetype "SPRINTER_STEADY_CASUAL"
    float skillMultiplier
    int simulatedScore
  }
  CLOCK_GUARD {
    long lastVerifiedUtcMs
    long monotonicUptimeDelta
    boolean rollbackDetected
  }
  REWARD_TIER {
    int minRank
    int maxRank
    int coinsReward
  }`
  },
  {
    id: 'sheet-08',
    sheetNum: 'Sheet 08 — Live Mermaid',
    title: 'Level Sync (OTA Content)',
    domain: 'Platform Services',
    engine: 'Unity',
    desc: 'Background manifest-diffing synchronizer that never blocks game boot sequence, applying content cleanly on subsequent launch.',
    labDemo: 'native-image-bridge',
    mermaidFlowchart: `flowchart TB
  subgraph Triggers["APP LIFECYCLE & CDN ENDPOINTS"]
    direction LR
    AppBoot["Game Boot (Zero-Block Instant Start)"]
    RemoteCDN["Remote CDN (manifest_v2.json + level packs)"]
  end

  subgraph Core["CORE MODULE — OTA LEVEL SYNCHRONIZER"]
    SyncMgr["LevelSyncManager (Non-Blocking Background Diff & Stage)"]
  end

  subgraph Adjacent["ADJACENT RUNTIME SYSTEMS"]
    direction LR
    ActiveDB["Active Runtime Level Registry (Read-Only During Session)"]
    StagingDir["Persistent Staging Directory (.tmp -> Atomic Swap)"]
    Streaming["StreamingAssets Fallback Bundle"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    HashDiff["SHA-256 / Version Manifest Differ"]
    AtomicCommit["Next-Boot Atomic Directory Promotion"]
    RetryQueue["Exponential Backoff & Partial Resume"]
  end

  AppBoot -->|"1. Mount immediately"| ActiveDB
  AppBoot -->|"2. Spawn low-priority task"| SyncMgr
  RemoteCDN -->|"3. Fetch remote manifest"| SyncMgr

  SyncMgr -->|"download changed JSONs"| StagingDir
  StagingDir -->|"promote on next cold boot"| ActiveDB
  Streaming -->|"seed initial install"| ActiveDB

  SyncMgr -.-> HashDiff
  SyncMgr -.-> AtomicCommit
  SyncMgr -.-> RetryQueue

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class SyncMgr coreNode
  class AppBoot,RemoteCDN,ActiveDB,StagingDir,Streaming adjNode
  class HashDiff,AtomicCommit,RetryQueue intNode`,
    mermaidEr: `erDiagram
  REMOTE_MANIFEST ||--|{ LEVEL_ASSET_ENTRY : "declares"
  LOCAL_MANIFEST ||--|{ LEVEL_ASSET_ENTRY : "tracks installed"
  SYNC_DIFF_PLAN ||--o{ STAGED_FILE : "downloads to .tmp"
  STAGED_FILE ||--|| ACTIVE_LEVEL_CACHE : "commits on next boot"

  REMOTE_MANIFEST {
    int manifestVersion
    string cdnBaseUrl
    long publishedUtc
  }
  LEVEL_ASSET_ENTRY {
    string levelKey
    int contentRevision
    string sha256Checksum
    int byteSize
  }
  SYNC_DIFF_PLAN {
    string_array addedKeys
    string_array modifiedKeys
    int totalDownloadBytes
  }
  STAGED_FILE {
    string tempFilePath
    boolean checksumVerified
    boolean readyForSwap
  }
  ACTIVE_LEVEL_CACHE {
    int activeManifestVersion
    string mountedDirectory
  }`
  },
  {
    id: 'sheet-04',
    sheetNum: 'Sheet 04 — Live Mermaid',
    title: 'IAP System Gateway',
    domain: 'Monetization',
    engine: 'Unity (Android + iOS)',
    desc: 'Dual-currency store gateway (real money vs soft currency) with strict grant-then-confirm verification.',
    labDemo: null,
    mermaidFlowchart: `flowchart TB
  subgraph StoreUI["STOREFRONT & CATALOG CALLERS"]
    direction LR
    ShopBtn["Store UI Purchase Button"]
    Catalog["Unified Product Catalog (IAP + Coin Packs)"]
    RestoreBtn["Restore Purchases Flow"]
  end

  subgraph Core["CORE MODULE — IAP SYSTEM GATEWAY"]
    Gateway["IAPStoreGateway (Dual-Currency Router & Transaction Guard)"]
  end

  subgraph Providers["BILLING & ECONOMY BACKENDS"]
    direction LR
    PlatformStore["Google Play Billing / Apple StoreKit 2"]
    SoftWallet["Local Coin / Gem Economy Ledger"]
    Inventory["Player Entitlement & Powerup Inventory"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    GrantFirst["Grant-Then-Confirm Crash Recovery Log"]
    ReceiptVal["Local Signature & Receipt Sanitizer"]
    Idempotency["Processed Transaction ID Dedup Set"]
  end

  ShopBtn -->|"Purchase(productId)"| Gateway
  Catalog -->|"SKU metadata"| Gateway
  RestoreBtn -->|"SyncEntitlements()"| Gateway

  Gateway <-->|"Real-Money SKU"| PlatformStore
  Gateway <-->|"Soft-Currency SKU"| SoftWallet
  Gateway -->|"1. Grant reward + save"| Inventory
  Gateway -->|"2. ConfirmPendingPurchase"| PlatformStore

  Gateway -.-> GrantFirst
  Gateway -.-> ReceiptVal
  Gateway -.-> Idempotency

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Gateway coreNode
  class ShopBtn,Catalog,RestoreBtn,PlatformStore,SoftWallet,Inventory adjNode
  class GrantFirst,ReceiptVal,Idempotency intNode`,
    mermaidEr: `erDiagram
  STORE_CATALOG ||--|{ PRODUCT_SKU : "defines"
  PRODUCT_SKU ||--o{ PURCHASE_TRANSACTION : "initiates"
  PURCHASE_TRANSACTION ||--|| ENTITLEMENT_GRANT : "unlocks"
  PURCHASE_TRANSACTION ||--|| RECEIPT_LEDGER : "records idempotently"

  STORE_CATALOG {
    string catalogVersion
    int skuCount
  }
  PRODUCT_SKU {
    string productId
    string storeType "CONSUMABLE_NONCONSUMABLE"
    string currencyType "REAL_MONEY_OR_COINS"
    float priceAmount
  }
  PURCHASE_TRANSACTION {
    string transactionId
    string productId
    string status "PENDING_GRANTED_CONFIRMED"
    long timestampUtc
  }
  ENTITLEMENT_GRANT {
    string grantId
    string rewardType "NO_ADS_COINS_HINTS"
    int quantity
    boolean persistedToDisk
  }
  RECEIPT_LEDGER {
    string transactionId
    string platformSignature
    boolean acknowledgedWithStore
  }`
  },
  {
    id: 'sheet-13',
    sheetNum: 'Sheet 13 — Live Mermaid',
    title: 'Argos Localization IPC',
    domain: 'Editor Tooling / IPC',
    engine: 'Unity + Python (offline)',
    desc: 'Cross-process communication tool linking Unity C# to an offline Python translation subprocess over stdin/stdout JSON codecs.',
    labDemo: 'argos-localization',
    mermaidFlowchart: `flowchart TB
  subgraph EditorUI["UNITY EDITOR LOCALIZATION WINDOW"]
    direction LR
    StringTable["Localization String Table (CSV / ScriptableObject)"]
    LangSelect["Target Locale Batch Selector (EN to ES/FR/DE/JA/HI)"]
  end

  subgraph Core["CORE MODULE — ARGOS IPC BRIDGE (C# TO PYTHON)"]
    IPCController["ArgosIPCProcessRunner (Persistent Subprocess + JSON Pipe)"]
  end

  subgraph PythonWorker["OFFLINE PYTHON SUBPROCESS"]
    direction LR
    WorkerLoop["argos_worker.py (UTF-8 stdin/stdout Loop)"]
    CTranslate["Argos Translate + CTranslate2 Neural Engine"]
    Glossary["Protected Token & Format-Tag Freezer"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    TokenMask["Placeholder Masker ({0}, RichText Tags, %d to __TK0__)"]
    WarmModel["Single Cold-Boot Model Load (Zero Per-Row Spawn)"]
    TimeoutGuard["Async Cancellation & Pipe Deadlock Watchdog"]
  end

  StringTable -->|"source keys + strings"| IPCController
  LangSelect -->|"target lang codes"| IPCController

  IPCController -->|"JSON Line Request (stdin)"| WorkerLoop
  WorkerLoop <-->|"batch inference"| CTranslate
  WorkerLoop <-->|"freeze/restore tags"| Glossary
  WorkerLoop -->|"JSON Line Response (stdout)"| IPCController
  IPCController -->|"write translated table"| StringTable

  IPCController -.-> TokenMask
  IPCController -.-> WarmModel
  IPCController -.-> TimeoutGuard

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class IPCController coreNode
  class StringTable,LangSelect,WorkerLoop,CTranslate,Glossary adjNode
  class TokenMask,WarmModel,TimeoutGuard intNode`,
    mermaidEr: `erDiagram
  LOCALIZATION_TABLE ||--|{ STRING_ROW : "contains"
  STRING_ROW ||--o{ IPC_BATCH_REQUEST : "batches into"
  IPC_BATCH_REQUEST ||--|| TOKEN_MASK_MAP : "protects tags"
  IPC_BATCH_REQUEST ||--|| IPC_BATCH_RESPONSE : "returns over stdout"

  LOCALIZATION_TABLE {
    string tableAssetGuid
    string sourceLocale "en"
    string_array targetLocales
  }
  STRING_ROW {
    string entryKey
    string sourceText
    string_map localizedValues
  }
  IPC_BATCH_REQUEST {
    string requestId
    string fromLang
    string toLang
    string_array maskedPayloads
  }
  TOKEN_MASK_MAP {
    string placeholderToken "__TK_0__"
    string originalMarkup "{0}_or_color_tag"
  }
  IPC_BATCH_RESPONSE {
    string requestId
    string status "OK_OR_ERR"
    string_array translatedPayloads
    int inferenceMs
  }`
  },
  {
    id: 'sheet-11',
    sheetNum: 'Sheet 11 — Live Mermaid',
    title: 'Asset Organizer Tool',
    domain: 'Unity Editor DX',
    engine: 'Unity Editor',
    desc: 'Developer productivity tool adding project window asset pinning, custom category badges, and asset movement watchers.',
    labDemo: 'asset-organizer',
    mermaidFlowchart: `flowchart TB
  subgraph EditorHooks["UNITY EDITOR CALLBACK HOOKS"]
    direction LR
    ProjectGUI["EditorApplication.projectWindowItemOnGUI"]
    Postproc["AssetPostprocessor (OnPostprocessAllAssets)"]
    Shortcut["Quick-Pin Dock & Filter Toolbar"]
  end

  subgraph Core["CORE MODULE — ASSET ORGANIZER ENGINE"]
    Organizer["AssetOrganizerController (GUID Registry + Rule Matcher)"]
  end

  subgraph Adjacent["ADJACENT EDITOR SUBSYSTEMS"]
    direction LR
    BadgeDrawer["Custom Row Badge & Color Stripe Renderer"]
    RuleScanner["One-Click Folder Audit & Auto-Mover"]
    MetaStore["EditorPrefs / ScriptableObject Metadata DB"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    GuidMap["O(1) GUID-to-Category Lookup Cache (Path-Move Resilient)"]
    ZeroRuntime["Editor-Only Assembly Definition (Zero Build Size Overhead)"]
    BatchMove["AssetDatabase.StartAssetEditing Batch Transaction"]
  end

  ProjectGUI -->|"visible row GUID"| Organizer
  Postproc -->|"moved/imported paths"| Organizer
  Shortcut -->|"pin / filter query"| Organizer

  Organizer -->|"draw tag + icon"| BadgeDrawer
  Organizer -->|"fix misplaced assets"| RuleScanner
  Organizer <-->|"persist GUID tags"| MetaStore

  Organizer -.-> GuidMap
  Organizer -.-> ZeroRuntime
  Organizer -.-> BatchMove

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Organizer coreNode
  class ProjectGUI,Postproc,Shortcut,BadgeDrawer,RuleScanner,MetaStore adjNode
  class GuidMap,ZeroRuntime,BatchMove intNode`,
    mermaidEr: `erDiagram
  ORGANIZER_DATABASE ||--|{ ASSET_GUID_RECORD : "indexes by GUID"
  ORGANIZER_DATABASE ||--|{ CATEGORY_TAG : "defines"
  ORGANIZER_DATABASE ||--|{ FOLDER_ROUTING_RULE : "enforces"
  ASSET_GUID_RECORD }|--|| CATEGORY_TAG : "assigned to"

  ORGANIZER_DATABASE {
    string projectVersion
    int pinnedCount
    int trackedGuidCount
  }
  ASSET_GUID_RECORD {
    string assetGuid "32_char_hex_immutable"
    string cachedAssetPath
    boolean isPinnedFavorite
    long lastModifiedUtc
  }
  CATEGORY_TAG {
    string tagId
    string label "CORE_UI_SHADER_AUDIO"
    string hexColor "#38bdf8"
  }
  FOLDER_ROUTING_RULE {
    string fileExtensionPattern "*.shader_*.prefab"
    string targetFolderPath "Assets/Shaders"
    boolean autoMoveOnImport
  }`
  },
  {
    id: 'sheet-12',
    sheetNum: 'Sheet 12 — Live Mermaid',
    title: 'Flame Flicker Effect',
    domain: 'Tech Art & Shaders',
    engine: 'Unity',
    desc: 'Lightweight 2-file procedural noise shader and C# driver creating dynamic ambient fire visuals on UI canvases.',
    labDemo: 'jigsaw-solitaire',
    mermaidFlowchart: `flowchart TB
  subgraph SceneInputs["UI CANVAS & AMBIENT PRESETS"]
    direction LR
    CanvasImg["UnityEngine.UI.Image / SpriteRenderer"]
    PresetCfg["Flicker Preset (Hearth / Torch / Candle / Campfire)"]
  end

  subgraph Core["CORE MODULE — FLAME FLICKER DRIVER & SHADER"]
    Driver["FlameFlickerDriver.cs + ProceduralFlame.shader"]
  end

  subgraph Adjacent["ADJACENT VISUAL SUBSYSTEMS"]
    direction LR
    MatInstance["Per-Widget MaterialPropertyBlock / Dynamic Material"]
    LightSync["2D Ambient Glow & Vignette Warmth Pulser"]
    SaveBattery["60fps / 30fps Low-Power Thermal Throttler"]
  end

  subgraph Internal["INTERNAL ENGINE SUBSYSTEMS (BELOW CUT-LINE)"]
    direction LR
    FBM["2-Octave Simplex / Value Noise UV Warp"]
    TeardropSDF["Signed Distance Teardrop Core & Outer Halo"]
    ZeroTex["Zero Particle-Spawn GC (100% Fragment Math)"]
  end

  CanvasImg -->|"target graphic"| Driver
  PresetCfg -->|"turbulence + palette"| Driver

  Driver -->|"set _Phase, _Wind, _Intensity"| MatInstance
  Driver -->|"synchronized luminance"| LightSync
  Driver <-->|"frame budget scale"| SaveBattery

  Driver -.-> FBM
  Driver -.-> TeardropSDF
  Driver -.-> ZeroTex

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Driver coreNode
  class CanvasImg,PresetCfg,MatInstance,LightSync,SaveBattery adjNode
  class FBM,TeardropSDF,ZeroTex intNode`,
    mermaidEr: `erDiagram
  FLAME_PRESET ||--|{ SHADER_UNIFORM_FRAME : "drives per frame"
  FLAME_PRESET ||--|| COLOR_RAMP_PALETTE : "blends"
  SHADER_UNIFORM_FRAME ||--|| FRAGMENT_SDF_PASS : "evaluates on GPU"

  FLAME_PRESET {
    string presetId "HEARTH_TORCH_CANDLE"
    float noiseScrollSpeed
    float turbulenceAmplitude
    float windSwayAngle
  }
  COLOR_RAMP_PALETTE {
    string innerCoreHex "#fff7ed"
    string midFlameHex "#f59e0b"
    string outerEmberHex "#dc2626"
  }
  SHADER_UNIFORM_FRAME {
    float timePhase
    float2 uvDistortionOffset
    float luminancePulse
  }
  FRAGMENT_SDF_PASS {
    int noiseOctaves "2"
    float teardropSoftEdge
    int particleAllocations "0_ZERO_GC"
  }`
  }
];

let activeBlueprintIdx = 0;
let activeBlueprintDiagramMode = 'flowchart'; // 'flowchart' | 'er' | 'code'
let lastMermaidGraphicMode = 'flowchart'; // remembers 'flowchart' or 'er' when viewing 'code'

function buildMermaidLiveUrl(definition) {
  try {
    const state = {
      code: definition,
      mermaid: JSON.stringify({ theme: 'dark' }),
      autoSync: true,
      updateDiagram: true
    };
    const json = JSON.stringify(state);
    const base64 = window.btoa(unescape(encodeURIComponent(json)));
    return `https://mermaid.live/edit#base64:${base64}`;
  } catch (_) {
    return 'https://mermaid.live/';
  }
}

function initBlueprintTerminal() {
  const navContainer = document.getElementById('blueprint-nav-list');
  const canvasViewport = document.getElementById('bp-canvas-viewport');
  const stageMermaid = document.getElementById('bp-stage-mermaid');
  const stageCode = document.getElementById('bp-stage-code');
  const stageCodePre = document.getElementById('bp-stage-code-pre');
  const codeHeaderLabel = document.getElementById('bp-code-header-label');
  const copyCodeBtn = document.getElementById('btn-copy-mermaid-code');
  const panzoomToolbar = document.getElementById('bp-panzoom-toolbar');
  const btnZoomIn = document.getElementById('bp-zoom-in');
  const btnZoomOut = document.getElementById('bp-zoom-out');
  const btnZoomReset = document.getElementById('bp-zoom-reset');
  const btnOpenMermaidLive = document.getElementById('btn-open-mermaid-live');
  const stageTitle = document.getElementById('bp-stage-title');
  const stageDomain = document.getElementById('bp-stage-domain');
  const stageDesc = document.getElementById('bp-stage-desc');
  const footerBadge = document.getElementById('bp-stage-footer-badge');
  const btnModeFlowchart = document.getElementById('bp-mode-flowchart');
  const btnModeEr = document.getElementById('bp-mode-er');
  const btnModeCode = document.getElementById('bp-mode-code');
  const zoomBtn = document.getElementById('btn-zoom-blueprint');
  const launchLabBtn = document.getElementById('btn-launch-blueprint-lab');

  if (!navContainer) return;

  // Pan & Zoom state (like mermaid.live)
  let zoomScale = 1;
  let panX = 0;
  let panY = 0;
  let isDragging = false;
  let dragStartX = 0;
  let dragStartY = 0;

  function applyPanZoomTransform() {
    if (stageMermaid) {
      stageMermaid.style.transform = `translate(${panX}px, ${panY}px) scale(${zoomScale})`;
    }
    if (btnZoomReset) {
      btnZoomReset.textContent = `${Math.round(zoomScale * 100)}%`;
    }
  }

  function resetPanZoom() {
    zoomScale = 1;
    panX = 0;
    panY = 0;
    applyPanZoomTransform();
  }

  if (btnZoomIn) {
    btnZoomIn.addEventListener('click', (e) => {
      e.stopPropagation();
      zoomScale = Math.min(2.6, +(zoomScale + 0.2).toFixed(2));
      applyPanZoomTransform();
    });
  }

  if (btnZoomOut) {
    btnZoomOut.addEventListener('click', (e) => {
      e.stopPropagation();
      zoomScale = Math.max(0.5, +(zoomScale - 0.2).toFixed(2));
      applyPanZoomTransform();
    });
  }

  if (btnZoomReset) {
    btnZoomReset.addEventListener('click', (e) => {
      e.stopPropagation();
      resetPanZoom();
    });
  }

  if (canvasViewport) {
    canvasViewport.addEventListener('wheel', (e) => {
      if (activeBlueprintDiagramMode === 'code') return;
      e.preventDefault();
      const delta = e.deltaY < 0 ? 0.12 : -0.12;
      zoomScale = Math.min(2.6, Math.max(0.5, +(zoomScale + delta).toFixed(2)));
      applyPanZoomTransform();
    }, { passive: false });

    canvasViewport.addEventListener('mousedown', (e) => {
      if (activeBlueprintDiagramMode === 'code') return;
      if (e.target.closest('#bp-panzoom-toolbar')) return;
      isDragging = true;
      dragStartX = e.clientX - panX;
      dragStartY = e.clientY - panY;
    });

    window.addEventListener('mousemove', (e) => {
      if (!isDragging) return;
      panX = e.clientX - dragStartX;
      panY = e.clientY - dragStartY;
      applyPanZoomTransform();
    });

    window.addEventListener('mouseup', () => {
      isDragging = false;
    });
  }

  if (window.mermaid) {
    window.mermaid.initialize({
      startOnLoad: false,
      theme: 'dark',
      securityLevel: 'loose',
      fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
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

  function updateDiagramModeButtons() {
    const inactiveClass = 'px-2.5 py-1 rounded-md font-semibold transition-all cursor-pointer flex items-center gap-1.5 text-slate-400 hover:text-white';
    if (btnModeFlowchart) {
      btnModeFlowchart.className = activeBlueprintDiagramMode === 'flowchart'
        ? 'px-2.5 py-1 rounded-md font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-sky-500 text-slate-950 shadow'
        : inactiveClass;
    }
    if (btnModeEr) {
      btnModeEr.className = activeBlueprintDiagramMode === 'er'
        ? 'px-2.5 py-1 rounded-md font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-amber-500 text-slate-950 shadow'
        : inactiveClass;
    }
    if (btnModeCode) {
      btnModeCode.className = activeBlueprintDiagramMode === 'code'
        ? 'px-2.5 py-1 rounded-md font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-emerald-500 text-slate-950 shadow'
        : inactiveClass;
    }
  }

  function getActiveDefinition(bp) {
    const mode = activeBlueprintDiagramMode === 'code' ? lastMermaidGraphicMode : activeBlueprintDiagramMode;
    return mode === 'er' && bp.mermaidEr ? bp.mermaidEr : bp.mermaidFlowchart;
  }

  async function renderBlueprintStage(bp) {
    if (!bp) return;

    stageTitle.textContent = bp.title;
    stageDomain.textContent = `INDEPENDENT SYSTEM — ${bp.domain.toUpperCase()}`;
    stageDesc.textContent = bp.desc;
    syncLaunchLabButton(bp);
    updateDiagramModeButtons();
    resetPanZoom();

    const definition = getActiveDefinition(bp);

    if (btnOpenMermaidLive) {
      btnOpenMermaidLive.href = buildMermaidLiveUrl(definition);
    }

    if (activeBlueprintDiagramMode === 'code') {
      if (stageMermaid) stageMermaid.classList.add('hidden');
      if (panzoomToolbar) panzoomToolbar.classList.add('hidden');
      if (stageCode) stageCode.classList.remove('hidden');
      if (stageCodePre) stageCodePre.textContent = definition;
      if (codeHeaderLabel) {
        codeHeaderLabel.textContent = `Mermaid.js Source (${lastMermaidGraphicMode === 'er' ? 'erDiagram' : 'flowchart TB'}) — ${bp.title}`;
      }
      if (footerBadge) {
        footerBadge.textContent = 'Mermaid.js v10 DSL Source • Click "mermaid.live ↗" to edit interactively';
        footerBadge.className = 'text-emerald-400';
      }
      return;
    }

    if (stageCode) stageCode.classList.add('hidden');
    if (panzoomToolbar) panzoomToolbar.classList.remove('hidden');
    if (stageMermaid) stageMermaid.classList.remove('hidden');

    if (footerBadge) {
      footerBadge.textContent = activeBlueprintDiagramMode === 'flowchart'
        ? 'Live Mermaid.js DOM SVG • Architecture Flowchart (Drag to Pan / Scroll to Zoom)'
        : 'Live Mermaid.js DOM SVG • Entity-Relationship (E-R) Data Schema (Drag to Pan / Scroll to Zoom)';
      footerBadge.className = 'text-sky-400';
    }

    if (stageMermaid && window.mermaid) {
      try {
        const renderId = `mermaid-bp-${bp.id}-${activeBlueprintDiagramMode}-${Date.now()}`;
        const { svg } = await window.mermaid.render(renderId, definition);
        stageMermaid.innerHTML = svg;
        const svgEl = stageMermaid.querySelector('svg');
        if (svgEl) {
          svgEl.style.maxWidth = '100%';
          svgEl.style.maxHeight = '430px';
          svgEl.style.height = 'auto';
        }
      } catch (err) {
        console.error('Mermaid render error:', err);
        stageMermaid.innerHTML = `<pre class="text-xs text-rose-400 font-mono p-4">${definition}</pre>`;
      }
    }
  }

  if (copyCodeBtn) {
    copyCodeBtn.addEventListener('click', () => {
      const bp = blueprintsData[activeBlueprintIdx];
      if (!bp) return;
      const def = getActiveDefinition(bp);
      navigator.clipboard.writeText(def).then(() => {
        copyCodeBtn.textContent = '✅ Copied to Clipboard!';
        setTimeout(() => {
          copyCodeBtn.textContent = '📋 Copy Mermaid Code';
        }, 1600);
      });
    });
  }

  if (btnModeFlowchart) {
    btnModeFlowchart.addEventListener('click', () => {
      activeBlueprintDiagramMode = 'flowchart';
      lastMermaidGraphicMode = 'flowchart';
      renderBlueprintStage(blueprintsData[activeBlueprintIdx]);
    });
  }

  if (btnModeEr) {
    btnModeEr.addEventListener('click', () => {
      activeBlueprintDiagramMode = 'er';
      lastMermaidGraphicMode = 'er';
      renderBlueprintStage(blueprintsData[activeBlueprintIdx]);
    });
  }

  if (btnModeCode) {
    btnModeCode.addEventListener('click', () => {
      activeBlueprintDiagramMode = 'code';
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
          <span class="px-1.5 py-0.2 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 text-[9px] font-mono">MERMAID</span>
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

  // Render initial blueprint (Sheet 10 -> MeowSweeper Mermaid Flowchart & E-R Schema)
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
        modalTitle.textContent = `${currentBp.title} — ${lastMermaidGraphicMode === 'er' ? 'E-R Data Schema' : 'Architecture Flowchart'}`;
      }

      if (modalMermaid && stageMermaid) {
        if (modalImg) modalImg.classList.add('hidden');
        modalMermaid.classList.remove('hidden');
        modalMermaid.innerHTML = stageMermaid.innerHTML;
        const svgEl = modalMermaid.querySelector('svg');
        if (svgEl) {
          svgEl.style.maxHeight = '80vh';
          svgEl.style.width = '100%';
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
