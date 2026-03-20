// BeautyCons Preview — All effects driven by JS
// Glow, shimmer, luster, levitation, 3D rotation

(function () {
  'use strict';

  // ── GLOW ──
  // Create glow elements based on data attributes
  document.querySelectorAll('.icon[data-glow]').forEach(function (icon) {
    var type = icon.dataset.glow;
    var c1 = icon.dataset.c1 || '#ff44aa';
    var c2 = icon.dataset.c2 || '#aa22ff';
    var isCircle = icon.classList.contains('circle');
    var el = document.createElement('div');
    el.className = 'glow-el';

    var radius = isCircle ? '50%' : '8px';
    var blur = type === 'bloom' ? 7 : type === 'sharp' ? 2 : 4;
    var bg;

    switch (type) {
      case 'neon':
        bg = 'radial-gradient(' + (isCircle ? 'circle' : 'ellipse') + ', ' + c1 + ' 0%, ' + c2 + ' 50%, transparent 72%)';
        break;
      case 'bloom':
        bg = 'radial-gradient(' + (isCircle ? 'circle' : 'ellipse') + ', rgba(255,255,255,0.3) 0%, ' + c1 + ' 28%, ' + c2 + ' 52%, transparent 78%)';
        break;
      case 'halo':
        bg = 'radial-gradient(' + (isCircle ? 'circle' : 'ellipse') + ', transparent 32%, ' + c1 + ' 48%, ' + c2 + ' 62%, transparent 78%)';
        break;
      case 'sharp':
        bg = 'radial-gradient(' + (isCircle ? 'circle' : 'ellipse') + ', ' + c1 + ' 0%, ' + c2 + ' 32%, transparent 48%)';
        break;
      default:
        bg = 'radial-gradient(ellipse, ' + c1 + ' 0%, ' + c2 + ' 50%, transparent 72%)';
    }

    el.style.cssText = 'background:' + bg + ';filter:blur(' + blur + 'px);opacity:0.8;border-radius:' + radius;
    icon.appendChild(el);
  });

  // ── SHIMMER BAR ──
  // Create shimmer elements for cards with data-fx containing "shimmer"
  document.querySelectorAll('.card[data-fx]').forEach(function (card) {
    var fx = card.dataset.fx || '';
    if (fx.indexOf('shimmer') === -1) return;

    var icon = card.querySelector('.icon');
    if (!icon) return;

    var shine = card.dataset.shine || 'white';
    var barColor;
    switch (shine) {
      case 'gold': barColor = 'rgba(255,210,80,0.45)'; break;
      case 'platinum': barColor = 'rgba(200,220,255,0.45)'; break;
      case 'crimson': barColor = 'rgba(255,100,60,0.45)'; break;
      default: barColor = 'rgba(255,255,255,0.4)';
    }

    var container = document.createElement('div');
    container.className = 'shimmer-el';

    var bar = document.createElement('div');
    bar.className = 'bar';
    bar.style.background = 'linear-gradient(90deg, transparent, rgba(255,255,255,0.1), ' + barColor + ', rgba(255,255,255,0.1), transparent)';
    bar._phase = Math.random() * Math.PI * 2; // random start

    container.appendChild(bar);
    icon.appendChild(container);
  });

  // ── LUSTER ──
  document.querySelectorAll('.card[data-fx]').forEach(function (card) {
    var fx = card.dataset.fx || '';
    if (fx.indexOf('luster') === -1) return;

    var icon = card.querySelector('.icon');
    if (!icon) return;

    var shine = card.dataset.shine || 'white';
    var el = document.createElement('div');
    el.className = 'luster-el';

    var grad;
    switch (shine) {
      case 'gold':
        grad = 'linear-gradient(110deg, rgba(255,210,80,0.5) 0%, transparent 50%, rgba(60,40,5,0.3) 100%)';
        break;
      case 'platinum':
        grad = 'linear-gradient(110deg, rgba(200,220,255,0.5) 0%, transparent 50%, rgba(20,30,50,0.3) 100%)';
        break;
      case 'crimson':
        grad = 'linear-gradient(110deg, rgba(255,100,60,0.5) 0%, transparent 50%, rgba(50,5,5,0.3) 100%)';
        break;
      default:
        grad = 'linear-gradient(110deg, rgba(255,255,255,0.45) 0%, transparent 50%, rgba(0,0,0,0.25) 100%)';
    }

    el.style.background = grad;
    icon.appendChild(el);
  });

  // ── ANIMATION LOOP ──
  var shimmerBars = document.querySelectorAll('.shimmer-el .bar');
  var lusterEls = document.querySelectorAll('.luster-el');
  var cards = document.querySelectorAll('.card[data-fx]');

  function tick(time) {
    var t = time * 0.001;

    // Shimmer bars sweep
    shimmerBars.forEach(function (bar) {
      var phase = bar._phase || 0;
      var cycle = ((t * 0.8 + phase) % 4); // 4 second cycle
      var pos;
      if (cycle < 1.5) {
        pos = (cycle / 1.5) * 100 - 10; // sweep 0-100%
      } else {
        pos = -30; // hidden during pause
      }
      bar.style.left = pos + '%';
      bar.style.opacity = (pos >= -10 && pos <= 90) ? 1 : 0;
    });

    // Luster direction flip
    lusterEls.forEach(function (el) {
      var dir = Math.sin(t * 1.2);
      el.style.transform = 'scaleX(' + (dir > 0 ? 1 : -1) + ')';
      el.style.opacity = 0.6 + Math.abs(dir) * 0.3;
    });

    // Card transforms: levitation + 3D rotation
    cards.forEach(function (card) {
      var fx = card.dataset.fx || '';
      var hasLev = fx.indexOf('levitate') !== -1;
      var hasRot = fx.indexOf('rotate3d') !== -1;
      if (!hasLev && !hasRot) return;

      var translateY = hasLev ? Math.sin(t * 1.5) * 4 : 0;
      var rotateY = hasRot ? Math.sin(t * 0.8) * 8 : 0;

      var transform = '';
      if (hasRot) {
        transform += 'perspective(500px) rotateY(' + rotateY + 'deg) ';
      }
      if (hasLev) {
        transform += 'translateY(' + translateY + 'px)';
      }

      card.style.transform = transform;
    });

    requestAnimationFrame(tick);
  }

  requestAnimationFrame(tick);
})();
