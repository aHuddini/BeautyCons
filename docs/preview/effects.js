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
    el._shine = shine;
    icon.appendChild(el);
  });

  // ── ANIMATION LOOP ──
  var shimmerBars = document.querySelectorAll('.shimmer-el .bar');
  var lusterEls = document.querySelectorAll('.luster-el');
  var cards = document.querySelectorAll('.card[data-fx]');

  function tick(time) {
    var t = time * 0.001;

    // Shimmer bars — smooth sine sweep, no abrupt cuts
    shimmerBars.forEach(function (bar) {
      var phase = bar._phase || 0;
      // Smooth sine: -1 to 1 mapped to -20% to 100% position
      var wave = Math.sin(t * 0.7 + phase);
      var pos = (wave + 1) * 0.5 * 120 - 20; // range: -20 to 100
      // Fade at edges using a smooth envelope
      var fade = 1 - Math.pow(Math.abs(wave), 4); // fades near extremes
      bar.style.left = pos + '%';
      bar.style.opacity = fade * 0.9;
    });

    // Luster — smooth gradient angle shift, no scaleX flip
    lusterEls.forEach(function (el) {
      var angle = 110 + Math.sin(t * 1.0) * 40; // sweeps 70-150deg
      var intensity = 0.6 + Math.sin(t * 1.0) * 0.2;
      var shine = el._shine || 'white';
      var bright, dark;
      switch (shine) {
        case 'gold': bright = 'rgba(255,210,80,' + (intensity + 0.1) + ')'; dark = 'rgba(60,40,5,0.3)'; break;
        case 'platinum': bright = 'rgba(200,220,255,' + (intensity + 0.1) + ')'; dark = 'rgba(20,30,50,0.3)'; break;
        case 'crimson': bright = 'rgba(255,100,60,' + (intensity + 0.1) + ')'; dark = 'rgba(50,5,5,0.3)'; break;
        default: bright = 'rgba(255,255,255,' + (intensity + 0.1) + ')'; dark = 'rgba(0,0,0,0.25)';
      }
      el.style.background = 'linear-gradient(' + angle + 'deg, ' + bright + ' 0%, transparent 50%, ' + dark + ' 100%)';
    });

    // Card transforms: levitation + 3D rotation
    cards.forEach(function (card) {
      var fx = card.dataset.fx || '';
      var hasLev = fx.indexOf('levitate') !== -1;
      var hasRot = fx.indexOf('rotate3d') !== -1;
      if (!hasLev && !hasRot) return;

      var translateY = hasLev ? Math.sin(t * 1.5) * 5 : 0;
      var rotateY = hasRot ? Math.sin(t * 0.7) * 12 : 0;

      var transform = '';
      if (hasRot) {
        transform += 'perspective(200px) rotateY(' + rotateY + 'deg) ';
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
