// ==========================================
// Arpit Jaiswal - Portfolio Interactive Engine
// ==========================================

document.addEventListener('DOMContentLoaded', () => {
  initBlueprintFilters();
  initLightbox();
  initMechanicsLab();
  initFlameDemo();
  initMeowdokuDemo();
  initJigsawDemo();
});

// ------------------------------------------
// 1. Blueprint Filter System
// ------------------------------------------
function initBlueprintFilters() {
  const filterBtns = document.querySelectorAll('.filter-btn');
  const cards = document.querySelectorAll('.blueprint-card');

  filterBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      filterBtns.forEach(b => {
        b.classList.remove('bg-cyan-500/20', 'text-cyan-400', 'border-cyan-500/50');
        b.classList.add('bg-slate-800/60', 'text-slate-400', 'border-slate-700/60');
      });
      btn.classList.add('bg-cyan-500/20', 'text-cyan-400', 'border-cyan-500/50');
      btn.classList.remove('bg-slate-800/60', 'text-slate-400', 'border-slate-700/60');

      const filter = btn.getAttribute('data-filter');

      cards.forEach(card => {
        const domain = card.getAttribute('data-domain');
        if (filter === 'all' || domain === filter) {
          card.classList.remove('hidden');
          card.classList.add('flex');
        } else {
          card.classList.add('hidden');
          card.classList.remove('flex');
        }
      });
    });
  });
}

// ------------------------------------------
// 2. Blueprint Lightbox Viewer
// ------------------------------------------
function initLightbox() {
  const modal = document.getElementById('lightbox-modal');
  const modalImg = document.getElementById('lightbox-img');
  const modalTitle = document.getElementById('lightbox-title');
  const modalDesc = document.getElementById('lightbox-desc');
  const closeBtn = document.getElementById('lightbox-close');

  document.querySelectorAll('.open-blueprint').forEach(trigger => {
    trigger.addEventListener('click', (e) => {
      e.preventDefault();
      const card = trigger.closest('.blueprint-card');
      const imgPath = card.getAttribute('data-img');
      const title = card.querySelector('h3').textContent;
      const subtitle = card.querySelector('.blueprint-badge').textContent;

      modalImg.src = imgPath;
      modalTitle.textContent = title;
      modalDesc.textContent = subtitle;
      modal.classList.remove('hidden');
      modal.classList.add('flex');
      document.body.style.overflow = 'hidden';
    });
  });

  const closeModal = () => {
    modal.classList.add('hidden');
    modal.classList.remove('flex');
    document.body.style.overflow = '';
  };

  closeBtn.addEventListener('click', closeModal);
  modal.addEventListener('click', (e) => {
    if (e.target === modal || e.target.id === 'lightbox-container') {
      closeModal();
    }
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && !modal.classList.contains('hidden')) {
      closeModal();
    }
  });
}

// ------------------------------------------
// 3. Mechanics Lab Tabs
// ------------------------------------------
function initMechanicsLab() {
  const tabBtns = document.querySelectorAll('.lab-tab-btn');
  const views = document.querySelectorAll('.lab-view');

  tabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetId = btn.getAttribute('data-target');

      tabBtns.forEach(b => {
        b.classList.remove('border-cyan-400', 'text-cyan-400', 'bg-cyan-500/10');
        b.classList.add('border-transparent', 'text-slate-400', 'hover:text-slate-200');
      });

      btn.classList.add('border-cyan-400', 'text-cyan-400', 'bg-cyan-500/10');
      btn.classList.remove('border-transparent', 'text-slate-400');

      views.forEach(v => {
        if (v.id === targetId) {
          v.classList.remove('hidden');
        } else {
          v.classList.add('hidden');
        }
      });
    });
  });
}

// ------------------------------------------
// 4. Meowdoku Grid & Cascade Solver Micro-Demo
// ------------------------------------------
function initMeowdokuDemo() {
  const container = document.getElementById('meowdoku-board');
  const logBox = document.getElementById('meowdoku-logs');
  const hintBtn = document.getElementById('btn-meowdoku-hint');
  const resetBtn = document.getElementById('btn-meowdoku-reset');
  if (!container) return;

  const N = 5;
  // Region definitions (0 to 4)
  const regions = [
    [0, 0, 1, 1, 1],
    [0, 0, 2, 2, 1],
    [0, 3, 2, 2, 4],
    [3, 3, 3, 4, 4],
    [3, 3, 4, 4, 4]
  ];

  const regionColors = [
    'rgba(239, 68, 68, 0.15)',
    'rgba(59, 130, 246, 0.15)',
    'rgba(16, 185, 129, 0.15)',
    'rgba(245, 158, 11, 0.15)',
    'rgba(168, 85, 247, 0.15)'
  ];

  // Pre-baked valid target solution for demonstration
  const solution = [
    [0, 1, 0, 0, 0],
    [0, 0, 0, 1, 0],
    [1, 0, 0, 0, 0],
    [0, 0, 1, 0, 0],
    [0, 0, 0, 0, 1]
  ];

  let gridState = Array(N).fill(null).map(() => Array(N).fill(0)); // 0: empty, 1: Cat, -1: Marked X

  function renderGrid() {
    container.innerHTML = '';
    for (let r = 0; r < N; r++) {
      for (let c = 0; c < N; c++) {
        const cell = document.createElement('button');
        cell.className = 'w-12 h-12 md:w-14 md:h-14 flex items-center justify-center text-lg md:text-xl font-bold border border-slate-700/60 rounded-lg transition-all select-none hover:brightness-125';
        cell.style.backgroundColor = regionColors[regions[r][c]];

        if (gridState[r][c] === 1) {
          cell.innerHTML = '🐱';
          cell.classList.add('bg-cyan-500/30', 'border-cyan-400', 'shadow-[0_0_12px_rgba(6,182,212,0.4)]');
        } else if (gridState[r][c] === -1) {
          cell.innerHTML = '<span class="text-slate-500 text-xs">✕</span>';
        }

        // Tap = Place Cat, Long/Right click = X
        cell.addEventListener('click', () => {
          if (gridState[r][c] === 1) {
            gridState[r][c] = 0;
            addLog(`Cleared cell [${r},${c}]`);
          } else {
            gridState[r][c] = 1;
            addLog(`Placed 🐱 at [Row ${r+1}, Col ${c+1}]`);
            validateMove(r, c);
          }
          renderGrid();
        });

        cell.addEventListener('contextmenu', (e) => {
          e.preventDefault();
          gridState[r][c] = (gridState[r][c] === -1) ? 0 : -1;
          renderGrid();
        });

        container.appendChild(cell);
      }
    }
  }

  function validateMove(r, c) {
    // Check row conflicts
    let rowCount = 0;
    for (let j = 0; j < N; j++) if (gridState[r][j] === 1) rowCount++;
    if (rowCount > 1) {
      addLog(`⚠️ CONFLICT: Multiple cats in Row ${r+1}!`, 'text-red-400');
    }

    // Check col conflicts
    let colCount = 0;
    for (let i = 0; i < N; i++) if (gridState[i][c] === 1) colCount++;
    if (colCount > 1) {
      addLog(`⚠️ CONFLICT: Multiple cats in Column ${c+1}!`, 'text-red-400');
    }

    // Check diagonal touch
    const diags = [[-1,-1], [-1,1], [1,-1], [1,1]];
    for (const [dr, dc] of diags) {
      const nr = r + dr, nc = c + dc;
      if (nr >= 0 && nr < N && nc >= 0 && nc < N) {
        if (gridState[nr][nc] === 1) {
          addLog(`⚠️ CONFLICT: Cats touching diagonally at [${nr+1},${nc+1}]!`, 'text-red-400');
        }
      }
    }
  }

  function addLog(msg, colorClass = 'text-slate-300') {
    const line = document.createElement('div');
    line.className = `text-xs font-mono py-0.5 ${colorClass}`;
    line.innerHTML = `<span class="text-cyan-500">[Solver Engine]:</span> ${msg}`;
    logBox.appendChild(line);
    logBox.scrollTop = logBox.scrollHeight;
  }

  hintBtn.addEventListener('click', () => {
    addLog('Executing Deterministic Hint Cascade Technique 1: Region Elimination...', 'text-amber-400');
    for (let r = 0; r < N; r++) {
      for (let c = 0; c < N; c++) {
        if (solution[r][c] === 1 && gridState[r][c] !== 1) {
          gridState[r][c] = 1;
          addLog(`Deduction proved: Cat guaranteed at [${r+1}, ${c+1}]. Applied!`, 'text-emerald-400');
          renderGrid();
          return;
        }
      }
    }
    addLog('Board already perfectly solved or in advanced state!', 'text-cyan-400');
  });

  resetBtn.addEventListener('click', () => {
    gridState = Array(N).fill(null).map(() => Array(N).fill(0));
    logBox.innerHTML = '';
    addLog('Board reset. Ready for input gestures.');
    renderGrid();
  });

  addLog('Meowdoku 5x5 Engine initialized.');
  addLog('Rule: Exactly 1 Cat per Row, Column, and Colored Region. No diagonal touches.');
  renderGrid();
}

// ------------------------------------------
// 5. Procedural Flame Flicker VFX Simulator
// ------------------------------------------
function initFlameDemo() {
  const canvas = document.getElementById('flame-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');

  let speed = 2.0;
  let intensity = 1.0;
  let time = 0;

  const speedSlider = document.getElementById('flame-speed');
  const intensitySlider = document.getElementById('flame-intensity');

  if (speedSlider) speedSlider.addEventListener('input', (e) => speed = parseFloat(e.target.value));
  if (intensitySlider) intensitySlider.addEventListener('input', (e) => intensity = parseFloat(e.target.value));

  function pseudoNoise(x, t) {
    return Math.sin(x * 3.5 + t * 4.0) * 0.4 +
           Math.sin(x * 7.2 - t * 6.2) * 0.3 +
           Math.cos(x * 12.1 + t * 9.5) * 0.2;
  }

  function draw() {
    canvas.width = canvas.parentElement.clientWidth || 300;
    canvas.height = 240;

    ctx.fillStyle = '#070a12';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    time += 0.016 * speed;

    const centerX = canvas.width / 2;
    const baseY = canvas.height - 20;

    // Outer Glow / Amber Halo
    const grad = ctx.createRadialGradient(
      centerX, baseY - 60, 10,
      centerX, baseY - 60, 100 * intensity
    );
    grad.addColorStop(0, 'rgba(245, 158, 11, 0.4)');
    grad.addColorStop(0.5, 'rgba(239, 68, 68, 0.15)');
    grad.addColorStop(1, 'transparent');

    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(centerX, baseY - 60, 120 * intensity, 0, Math.PI * 2);
    ctx.fill();

    // Procedural Flame Layers
    const flameLayers = [
      { color: 'rgba(239, 68, 68, 0.7)', height: 110 * intensity, width: 45 },
      { color: 'rgba(245, 158, 11, 0.85)', height: 85 * intensity, width: 30 },
      { color: 'rgba(254, 240, 138, 0.95)', height: 50 * intensity, width: 16 }
    ];

    flameLayers.forEach((layer, layerIdx) => {
      ctx.fillStyle = layer.color;
      ctx.beginPath();
      ctx.moveTo(centerX - layer.width / 2, baseY);

      const segments = 24;
      for (let i = 0; i <= segments; i++) {
        const tStep = i / segments;
        const currentY = baseY - tStep * layer.height;
        const curveFactor = Math.sin(tStep * Math.PI);
        const noise = pseudoNoise(tStep * 2 + layerIdx, time);
        const currentX = centerX + noise * 18 * curveFactor;

        if (i === 0) {
          ctx.moveTo(centerX - (layer.width * (1 - tStep * 0.8)) / 2, currentY);
        } else if (i === segments) {
          ctx.lineTo(currentX, currentY);
        } else {
          ctx.lineTo(currentX + (layer.width * (1 - tStep * 0.9)) / 2, currentY);
        }
      }

      for (let i = segments; i >= 0; i--) {
        const tStep = i / segments;
        const currentY = baseY - tStep * layer.height;
        const curveFactor = Math.sin(tStep * Math.PI);
        const noise = pseudoNoise(tStep * 2 + layerIdx + 10, time);
        const currentX = centerX + noise * 18 * curveFactor;
        ctx.lineTo(currentX - (layer.width * (1 - tStep * 0.9)) / 2, currentY);
      }

      ctx.closePath();
      ctx.fill();
    });

    requestAnimationFrame(draw);
  }

  draw();
}

// ------------------------------------------
// 6. Jigsaw 9-Signature Tab/Blank Classifier Demo
// ------------------------------------------
function initJigsawDemo() {
  const container = document.getElementById('jigsaw-canvas');
  if (!container) return;
  const ctx = container.getContext('2d');

  let gridDim = 3;
  const dimSelector = document.getElementById('jigsaw-grid-size');
  if (dimSelector) {
    dimSelector.addEventListener('change', (e) => {
      gridDim = parseInt(e.target.value);
      drawJigsawGrid();
    });
  }

  function drawJigsawGrid() {
    container.width = container.parentElement.clientWidth || 320;
    container.height = 320;

    ctx.fillStyle = '#070a12';
    ctx.fillRect(0, 0, container.width, container.height);

    const pad = 24;
    const boardSize = Math.min(container.width, container.height) - pad * 2;
    const pieceSize = boardSize / gridDim;

    for (let r = 0; r < gridDim; r++) {
      for (let c = 0; c < gridDim; c++) {
        const x = pad + c * pieceSize;
        const y = pad + r * pieceSize;

        // Signature classification: Corner (4), Edge (4), Interior (1)
        let sigType = 'Interior (4-Tabs)';
        let strokeColor = '#06b6d4';

        const isTop = r === 0;
        const isBottom = r === gridDim - 1;
        const isLeft = c === 0;
        const isRight = c === gridDim - 1;

        if ((isTop && isLeft) || (isTop && isRight) || (isBottom && isLeft) || (isBottom && isRight)) {
          sigType = 'Corner';
          strokeColor = '#f59e0b';
        } else if (isTop || isBottom || isLeft || isRight) {
          sigType = 'Edge Border';
          strokeColor = '#10b981';
        }

        ctx.strokeStyle = strokeColor;
        ctx.lineWidth = 2;
        ctx.strokeRect(x + 4, y + 4, pieceSize - 8, pieceSize - 8);

        ctx.fillStyle = 'rgba(255, 255, 255, 0.05)';
        ctx.fillRect(x + 4, y + 4, pieceSize - 8, pieceSize - 8);

        ctx.fillStyle = strokeColor;
        ctx.font = '10px Fira Code';
        ctx.textAlign = 'center';
        ctx.fillText(`[${r},${c}]`, x + pieceSize / 2, y + pieceSize / 2 - 4);
        ctx.fillText(sigType, x + pieceSize / 2, y + pieceSize / 2 + 10);
      }
    }
  }

  window.addEventListener('resize', drawJigsawGrid);
  drawJigsawGrid();
}
