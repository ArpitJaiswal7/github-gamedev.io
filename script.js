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
  subgraph Top["INPUT & CDN CONTENT"]
    Gestures["Touch Gesture Router<br/>(Tap X / DblTap Cat / Drag)"]
    CDN["Live Level CDN<br/>(lvl_1..65.json + Manifest)"]
  end

  Controller["PuzzleController<br/>(State Machine & Input Lock)"]

  subgraph Mid["RUNTIME & VALIDATION"]
    Solver["Deterministic Solver<br/>(Move Check & 5-Tier Hints)"]
    Economy["Wallet & 3-Heart Rule<br/>(Coins / Hints / Score Sync)"]
  end

  subgraph Bot["INTERNAL ENGINE (CUT-LINE)"]
    Cascade["5-Tier Hint Cascade<br/>(Single-Empty to Elimination)"]
    UndoHash["Correct-Only Undo Stack<br/>& Save Hash Validator"]
  end

  Gestures -->|"cell action"| Controller
  CDN -->|"grid config"| Controller
  Controller <-->|"validate"| Solver
  Controller <-->|"state / wallet"| Economy
  Solver -.-> Cascade
  Controller -.-> UndoHash

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Controller coreNode
  class Gestures,CDN,Solver,Economy adjNode
  class Cascade,UndoHash intNode`,
    mermaidEr: `erDiagram
  LEVEL_DATA ||--|| BOARD_STATE : "initializes"
  PLAYER_WALLET ||--o{ BOARD_STATE : "funds"
  BOARD_STATE ||--|{ CELL_NODE : "contains N x N"
  BOARD_STATE ||--o{ HINT_CASCADE : "evaluates"

  LEVEL_DATA {
    int id
    int gridSize
    int_matrix regions
    int_matrix solutionCats
  }
  PLAYER_WALLET {
    int hintCount
    int catRevealCount
    int coinsBalance
  }
  BOARD_STATE {
    int levelId
    string levelHash
    int remainingHearts
    int elapsedMs
  }
  CELL_NODE {
    int row_col
    int regionId
    string state
  }
  HINT_CASCADE {
    int tier_1_to_5
    string ruleName
    int_array targetCells
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
    LevelUI["Level Grid & Avatar UI<br/>LoadImageAsync(url, size)"]
    BoardTex["Jigsaw Board Loader<br/>HD Texture Request"]
  end

  Facade["NativeImagePipeline<br/>(Shared C# API & In-Flight Deduplicator)"]

  subgraph Bridges["PLATFORM NATIVE BRIDGES"]
    Mobile["Android Glide + OkHttp3<br/>& iOS NSURLSession + vImage"]
    Fallback["UnityWebRequest Fallback<br/>(Editor / WebGL Target)"]
  end

  subgraph Cache["INTERNAL CACHE & DECODE (CUT-LINE)"]
    L1L2["L1 Memory LRU Pool<br/>& L2 SHA-256 Disk Cache"]
    Downsample["Hardware InSampleSize<br/>Off-Thread Downsampler"]
  end

  LevelUI -->|"async request"| Facade
  BoardTex -->|"async request"| Facade
  Facade -->|"JNI / ObjC"| Mobile
  Facade -->|"Editor"| Fallback
  Facade -.-> L1L2
  Mobile -.-> Downsample

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Facade coreNode
  class LevelUI,BoardTex,Mobile,Fallback adjNode
  class L1L2,Downsample intNode`,
    mermaidEr: `erDiagram
  IMAGE_REQUEST ||--o| MEMORY_LRU_ENTRY : "checks L1"
  IMAGE_REQUEST ||--o| DISK_CACHE_RECORD : "checks L2"
  IMAGE_REQUEST ||--|| NATIVE_DECODE_JOB : "dispatches"
  DISK_CACHE_RECORD ||--|| NATIVE_DECODE_JOB : "streams bytes"

  IMAGE_REQUEST {
    string urlHashSha256
    string sourceUrl
    int targetWidth
    int targetHeight
  }
  MEMORY_LRU_ENTRY {
    string urlHashSha256
    int textureInstanceId
    int byteFootprint
  }
  DISK_CACHE_RECORD {
    string urlHashSha256
    string filePath
    string etagHeader
  }
  NATIVE_DECODE_JOB {
    string platformBackend
    int sampleFactor
    int decodeMs
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
  subgraph Inputs["BOARD CONFIG & ARTWORK"]
    Config["Grid Preset (3x3..10x10)<br/>& Pointer Drag Input"]
    Artwork["HD Source Texture2D<br/>(Single Shared Atlas)"]
  end

  Generator["ProceduralBoardGenerator<br/>(Edge Matrix & UV Projection)"]

  subgraph Runtime["RUNTIME SHADER & CLUSTERS"]
    Shader["PieceMaskShader<br/>(9-Slice Bevel & Alpha Cut)"]
    Cluster["Union-Find Snap Group<br/>& Scrollable Piece Tray"]
  end

  subgraph Internal["INTERNAL GEOMETRY (CUT-LINE)"]
    Edges["Complementary Edge Solver<br/>(Flat=0 / Tab=+1 / Blank=-1)"]
    UVs["Oversized UV Quad (+28%)<br/>& Dynamic Z-Order Stack"]
  end

  Config -->|"dimensions"| Generator
  Artwork -->|"shared atlas"| Generator
  Generator -->|"UV + mask ID"| Shader
  Generator <-->|"magnetic snap"| Cluster
  Generator -.-> Edges
  Shader -.-> UVs

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Generator coreNode
  class Config,Artwork,Shader,Cluster adjNode
  class Edges,UVs intNode`,
    mermaidEr: `erDiagram
  JIGSAW_BOARD ||--|{ JIGSAW_PIECE : "generates N x N"
  UNION_CLUSTER ||--|{ JIGSAW_PIECE : "groups snapped"
  JIGSAW_PIECE ||--|| EDGE_SIGNATURE : "has 4 edges"
  JIGSAW_PIECE ||--|| SHADER_UV_BLOCK : "projects"

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
  subgraph Events["SCORE & CLOCK INPUTS"]
    WinEvent["Level Win (+Score)<br/>& Game Preset Selector"]
    Clock["System UTC Clock<br/>(Elapsed Offline Hours)"]
  end

  SimEngine["LeaderboardSimulator<br/>(Deterministic Bot Curve & League State)"]

  subgraph Outputs["UI & PERSISTENCE"]
    LeagueUI["Animated Standings UI<br/>& Division Promotion Chest"]
    Storage["Encrypted Local JSON<br/>Snapshot Persistence"]
  end

  subgraph Internal["INTERNAL SIMULATION (CUT-LINE)"]
    Bots["Seeded Bot Archetypes<br/>(Sprinter / Steady / Casual)"]
    Clamp["Monotonic Timestamp Clamp<br/>& Player Rubberband Curve"]
  end

  WinEvent -->|"AddScore(delta)"| SimEngine
  Clock -->|"SimulateDelta(dt)"| SimEngine
  SimEngine -->|"rank delta"| LeagueUI
  SimEngine <-->|"save / load"| Storage
  SimEngine -.-> Bots
  SimEngine -.-> Clamp

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class SimEngine coreNode
  class WinEvent,Clock,LeagueUI,Storage adjNode
  class Bots,Clamp intNode`,
    mermaidEr: `erDiagram
  SEASON_LEAGUE ||--|{ BOT_COMPETITOR : "simulates 29 bots"
  SEASON_LEAGUE ||--|| PLAYER_STANDING : "tracks user"
  SEASON_LEAGUE ||--|| CLOCK_GUARD : "clamps time"
  SEASON_LEAGUE ||--o| REWARD_TIER : "grants chest"

  SEASON_LEAGUE {
    string leagueId
    string divisionName
    long seasonEndUtc
  }
  PLAYER_STANDING {
    string displayName
    int currentScore
    int currentRank
  }
  BOT_COMPETITOR {
    int botSeedId
    string archetype
    int simulatedScore
  }
  CLOCK_GUARD {
    long lastVerifiedUtc
    boolean rollbackFlag
  }
  REWARD_TIER {
    int cutoffRank
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
  subgraph Triggers["BOOT & REMOTE CDN"]
    AppBoot["Cold Boot Sequence<br/>(Zero-Block Instant Mount)"]
    CDN["Remote OTA Server<br/>(manifest_v2.json + Packs)"]
  end

  SyncMgr["LevelSyncManager<br/>(Background Manifest Differ & Downloader)"]

  subgraph Storage["RUNTIME & STAGING DIRS"]
    ActiveDB["Active Level Registry<br/>(Read-Only During Session)"]
    Staging["Staging Directory (.tmp)<br/>& StreamingAssets Seed"]
  end

  subgraph Internal["INTERNAL SYNC GUARANTEES (CUT-LINE)"]
    HashDiff["SHA-256 Checksum Differ<br/>& Exponential Retry Queue"]
    AtomicSwap["Next-Boot Atomic Swap<br/>(Prevents Mid-Game Mutation)"]
  end

  AppBoot -->|"1. mount active"| ActiveDB
  AppBoot -->|"2. async check"| SyncMgr
  CDN -->|"remote manifest"| SyncMgr
  SyncMgr -->|"write .tmp"| Staging
  Staging -->|"next boot swap"| ActiveDB
  SyncMgr -.-> HashDiff
  Staging -.-> AtomicSwap

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class SyncMgr coreNode
  class AppBoot,CDN,ActiveDB,Staging adjNode
  class HashDiff,AtomicSwap intNode`,
    mermaidEr: `erDiagram
  REMOTE_MANIFEST ||--|{ LEVEL_ASSET_ENTRY : "declares"
  SYNC_DIFF_PLAN ||--|{ LEVEL_ASSET_ENTRY : "compares"
  SYNC_DIFF_PLAN ||--o{ STAGED_FILE : "downloads .tmp"
  STAGED_FILE ||--|| ACTIVE_LEVEL_CACHE : "swaps on boot"

  REMOTE_MANIFEST {
    int manifestVersion
    string cdnBaseUrl
    long publishedUtc
  }
  LEVEL_ASSET_ENTRY {
    string levelKey
    int contentRevision
    string sha256Checksum
  }
  SYNC_DIFF_PLAN {
    string_array changedKeys
    int downloadBytes
  }
  STAGED_FILE {
    string tempFilePath
    boolean hashVerified
  }
  ACTIVE_LEVEL_CACHE {
    int activeVersion
    string mountedDir
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
  subgraph Storefront["STORE UI & CATALOG"]
    BuyBtn["Store Purchase / Restore UI<br/>Purchase(productId)"]
    Catalog["Unified SKU Catalog<br/>(Real Money + Soft Coins)"]
  end

  Gateway["IAPStoreGateway<br/>(Dual-Currency Router & Crash Guard)"]

  subgraph Backends["BILLING & LEDGER BACKENDS"]
    StoreSDK["Google Play Billing<br/>& Apple StoreKit 2"]
    Inventory["Player Entitlement DB<br/>& Local Coin Wallet"]
  end

  subgraph Internal["INTERNAL SAFETY (CUT-LINE)"]
    GrantFirst["Grant-Then-Confirm Flow<br/>(Zero Lost Purchases)"]
    Dedup["Receipt Signature Check<br/>& Transaction ID Dedup"]
  end

  BuyBtn -->|"initiate SKU"| Gateway
  Catalog -->|"SKU metadata"| Gateway
  Gateway <-->|"platform billing"| StoreSDK
  Gateway -->|"1. grant & save"| Inventory
  Gateway -.-> GrantFirst
  Gateway -.-> Dedup

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Gateway coreNode
  class BuyBtn,Catalog,StoreSDK,Inventory adjNode
  class GrantFirst,Dedup intNode`,
    mermaidEr: `erDiagram
  PRODUCT_SKU ||--o{ PURCHASE_TX : "initiates"
  PURCHASE_TX ||--|| ENTITLEMENT_GRANT : "unlocks"
  PURCHASE_TX ||--|| RECEIPT_LEDGER : "verifies"
  STORE_CATALOG ||--|{ PRODUCT_SKU : "defines"

  STORE_CATALOG {
    string catalogVersion
    int skuCount
  }
  PRODUCT_SKU {
    string productId
    string currencyType
    float priceAmount
  }
  PURCHASE_TX {
    string transactionId
    string status
    long timestampUtc
  }
  ENTITLEMENT_GRANT {
    string rewardType
    int quantity
    boolean savedToDisk
  }
  RECEIPT_LEDGER {
    string transactionId
    boolean storeConfirmed
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
  subgraph EditorUI["UNITY EDITOR LOCALIZATION"]
    Table["Localization String Table<br/>(CSV / ScriptableObject)"]
    Locales["Target Locale Selector<br/>(EN to ES / FR / DE / JA / HI)"]
  end

  IPC["ArgosIPCProcessRunner<br/>(Persistent Subprocess & JSON Pipe)"]

  subgraph Worker["OFFLINE PYTHON WORKER"]
    PyLoop["argos_worker.py<br/>(UTF-8 stdin / stdout Loop)"]
    Neural["Argos + CTranslate2<br/>Offline Neural Engine"]
  end

  subgraph Internal["INTERNAL IPC SAFEGUARDS (CUT-LINE)"]
    Masker["Token Masker (__TK0__)<br/>Protects {0} & RichText Tags"]
    Watchdog["Warm Model Cache<br/>& Pipe Timeout Watchdog"]
  end

  Table -->|"source rows"| IPC
  Locales -->|"target codes"| IPC
  IPC <-->|"JSONL stdio"| PyLoop
  PyLoop <-->|"batch translate"| Neural
  IPC -.-> Masker
  PyLoop -.-> Watchdog

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class IPC coreNode
  class Table,Locales,PyLoop,Neural adjNode
  class Masker,Watchdog intNode`,
    mermaidEr: `erDiagram
  LOCALIZATION_TABLE ||--|{ STRING_ROW : "contains"
  IPC_BATCH_JOB ||--|{ STRING_ROW : "translates"
  IPC_BATCH_JOB ||--|| TOKEN_MASK_MAP : "freezes tags"
  IPC_BATCH_JOB ||--|| WORKER_RESPONSE : "returns JSONL"

  LOCALIZATION_TABLE {
    string tableAssetGuid
    string sourceLocale
    string_array targetLocales
  }
  STRING_ROW {
    string entryKey
    string sourceText
    string_map localizedMap
  }
  IPC_BATCH_JOB {
    string requestId
    string from_to_lang
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
    desc: 'Developer productivity tool adding project window asset pinning, custom category badges, and asset movement watchers.',
    labDemo: 'asset-organizer',
    mermaidFlowchart: `flowchart TB
  subgraph Hooks["UNITY EDITOR HOOKS"]
    GUIHook["projectWindowItemOnGUI<br/>(Row Rect & GUID Callback)"]
    Postproc["AssetPostprocessor<br/>(Import & Move Watcher)"]
  end

  Organizer["AssetOrganizerController<br/>(GUID Registry & Rule Engine)"]

  subgraph EditorUI["EDITOR UX & STORAGE"]
    Badges["Custom Category Badges<br/>& Quick-Pin Favorites Bar"]
    RuleFix["One-Click Folder Audit<br/>& ScriptableObject DB"]
  end

  subgraph Internal["INTERNAL DX OPTIMIZATIONS (CUT-LINE)"]
    GuidCache["O(1) GUID Hash Lookup<br/>(Rename & Move Resilient)"]
    ZeroBuild["Editor-Only Assembly<br/>(Zero Runtime Build Overhead)"]
  end

  GUIHook -->|"visible GUID"| Organizer
  Postproc -->|"asset delta"| Organizer
  Organizer -->|"render overlay"| Badges
  Organizer <-->|"audit & persist"| RuleFix
  Organizer -.-> GuidCache
  Organizer -.-> ZeroBuild

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Organizer coreNode
  class GUIHook,Postproc,Badges,RuleFix adjNode
  class GuidCache,ZeroBuild intNode`,
    mermaidEr: `erDiagram
  ORGANIZER_DB ||--|{ ASSET_GUID_NODE : "indexes"
  ORGANIZER_DB ||--|{ CATEGORY_BADGE : "defines"
  ORGANIZER_DB ||--|{ ROUTING_RULE : "enforces"
  CATEGORY_BADGE ||--o{ ASSET_GUID_NODE : "tags"

  ORGANIZER_DB {
    string projectVersion
    int pinnedCount
  }
  ASSET_GUID_NODE {
    string assetGuidHex
    string cachedPath
    boolean isPinned
  }
  CATEGORY_BADGE {
    string tagId
    string badgeLabel
    string hexColor
  }
  ROUTING_RULE {
    string extPattern
    string targetFolder
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
  subgraph Inputs["UI WIDGET & PRESET"]
    CanvasImg["UnityEngine.UI.Image<br/>or SpriteRenderer Target"]
    Preset["Flicker Profile Preset<br/>(Hearth / Torch / Candle)"]
  end

  Driver["FlameFlickerDriver.cs<br/>+ ProceduralFlame.shader"]

  subgraph Visuals["GPU UNIFORMS & LIGHTING"]
    Uniforms["MaterialPropertyBlock<br/>(_Phase, _Wind, _Intensity)"]
    Glow["Synchronized 2D Glow<br/>& Thermal FPS Throttler"]
  end

  subgraph Internal["FRAGMENT SHADER MATH (CUT-LINE)"]
    FBM["2-Octave Simplex FBM<br/>UV Domain Warping"]
    SDF["Teardrop SDF Mask<br/>(Zero Particle GC Alloc)"]
  end

  CanvasImg -->|"graphic ref"| Driver
  Preset -->|"color + speed"| Driver
  Driver -->|"per-frame uniforms"| Uniforms
  Driver -->|"ambient pulse"| Glow
  Uniforms -.-> FBM
  Uniforms -.-> SDF

  classDef coreNode fill:#261c10,stroke:#f59e0b,stroke-width:2.5px,color:#ffffff
  classDef adjNode fill:#0f1b2d,stroke:#38bdf8,stroke-width:1.5px,color:#e2e8f0
  classDef intNode fill:#0d121c,stroke:#64748b,stroke-width:1px,stroke-dasharray: 5 5,color:#94a3b8

  class Driver coreNode
  class CanvasImg,Preset,Uniforms,Glow adjNode
  class FBM,SDF intNode`,
    mermaidEr: `erDiagram
  FLAME_PRESET ||--|| COLOR_RAMP : "blends"
  FLAME_PRESET ||--|{ SHADER_UNIFORM : "drives"
  SHADER_UNIFORM ||--|| FRAGMENT_SDF_PASS : "evaluates"

  FLAME_PRESET {
    string presetId
    float scrollSpeed
    float turbulence
    float windSway
  }
  COLOR_RAMP {
    string coreHex
    string midFlameHex
    string outerEmberHex
  }
  SHADER_UNIFORM {
    float timePhase
    float2 uvWarpOffset
    float glowPulse
  }
  FRAGMENT_SDF_PASS {
    int noiseOctaves
    float teardropEdge
    int gcAllocations
  }`
  }
];

let activeBlueprintIdx = 0;
let activeBlueprintDiagramMode = 'flowchart'; // 'flowchart' | 'er'

function initBlueprintTerminal() {
  const navContainer = document.getElementById('blueprint-nav-list');
  const stageMermaid = document.getElementById('bp-stage-mermaid');
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
        nodeSpacing: 28,
        rankSpacing: 34,
        padding: 12,
        useMaxWidth: true
      },
      er: {
        layoutDirection: 'TB',
        minEntityWidth: 95,
        minEntityHeight: 60,
        entityPadding: 10,
        useMaxWidth: true
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
    const inactiveClass = 'px-3 py-1.5 rounded-md font-semibold transition-all cursor-pointer flex items-center gap-1.5 text-slate-400 hover:text-white';
    if (btnModeFlowchart) {
      btnModeFlowchart.className = activeBlueprintDiagramMode === 'flowchart'
        ? 'px-3 py-1.5 rounded-md font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-sky-500 text-slate-950 shadow'
        : inactiveClass;
    }
    if (btnModeEr) {
      btnModeEr.className = activeBlueprintDiagramMode === 'er'
        ? 'px-3 py-1.5 rounded-md font-bold transition-all cursor-pointer flex items-center gap-1.5 bg-amber-500 text-slate-950 shadow'
        : inactiveClass;
    }
  }

  async function renderBlueprintStage(bp) {
    if (!bp) return;

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
          svgEl.style.maxHeight = '510px';
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
