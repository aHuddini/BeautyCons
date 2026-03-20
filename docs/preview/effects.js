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

  // ── LINK SHIMMER + LUSTER PER CARD ──
  // Find pairs so they share the same phase
  var animCards = [];
  document.querySelectorAll('.card[data-fx]').forEach(function (card) {
    var fx = card.dataset.fx || '';
    var bar = card.querySelector('.shimmer-el .bar');
    var luster = card.querySelector('.luster-el');
    var phase = bar ? (bar._phase || 0) : Math.random() * Math.PI * 2;
    animCards.push({
      el: card,
      bar: bar,
      luster: luster,
      phase: phase,
      fx: fx
    });
  });

  function tick(time) {
    var t = time * 0.001;

    animCards.forEach(function (c) {
      // Shared wave — same frequency and phase for shimmer + luster
      var wave = Math.sin(t * 0.7 + c.phase);

      // Shimmer bar sweep
      if (c.bar) {
        var pos = (wave + 1) * 0.5 * 120 - 20;
        var fade = 1 - Math.pow(Math.abs(wave), 4);
        c.bar.style.left = pos + '%';
        c.bar.style.opacity = fade;
      }

      // Luster follows the same wave — angle tracks the bar position
      if (c.luster) {
        var angle = 110 + wave * 60;
        var intensity = 0.75 + wave * 0.25;
        var shine = c.luster._shine || 'white';
        var bright, dark;
        switch (shine) {
          case 'gold': bright = 'rgba(255,210,80,' + (intensity + 0.1) + ')'; dark = 'rgba(60,40,5,0.3)'; break;
          case 'platinum': bright = 'rgba(200,220,255,' + (intensity + 0.1) + ')'; dark = 'rgba(20,30,50,0.3)'; break;
          case 'crimson': bright = 'rgba(255,100,60,' + (intensity + 0.1) + ')'; dark = 'rgba(50,5,5,0.3)'; break;
          default: bright = 'rgba(255,255,255,' + (intensity + 0.1) + ')'; dark = 'rgba(0,0,0,0.25)';
        }
        c.luster.style.background = 'linear-gradient(' + angle + 'deg, ' + bright + ' 0%, transparent 50%, ' + dark + ' 100%)';
      }

      // Transforms
      var hasLev = c.fx.indexOf('levitate') !== -1;
      var hasRot = c.fx.indexOf('rotate3d') !== -1;
      if (hasLev || hasRot) {
        var translateY = hasLev ? Math.sin(t * 1.5) * 7 : 0;
        var rotateY = hasRot ? Math.sin(t * 0.7) * 18 : 0;
        var transform = '';
        if (hasRot) transform += 'perspective(150px) rotateY(' + rotateY + 'deg) ';
        if (hasLev) transform += 'translateY(' + translateY + 'px)';
        c.el.style.transform = transform;
      }
    });

    requestAnimationFrame(tick);
  }

  requestAnimationFrame(tick);
})();
