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

  // ── LUSTER (canvas-based, reads icon pixels) ──
  document.querySelectorAll('.card[data-fx]').forEach(function (card) {
    var fx = card.dataset.fx || '';
    if (fx.indexOf('luster') === -1) return;

    var iconEl = card.querySelector('.icon');
    var img = card.querySelector('.icon img');
    if (!iconEl || !img) return;

    var shine = card.dataset.shine || 'white';
    var canvas = document.createElement('canvas');
    canvas.width = 56;
    canvas.height = 56;
    canvas.className = 'luster-el';
    canvas._shine = shine;
    canvas._img = img;
    canvas._ready = false;

    // Once the image loads, cache its pixel data
    function cachePixels() {
      var ctx = canvas.getContext('2d');
      ctx.drawImage(img, 0, 0, 56, 56);
      try {
        canvas._pixels = ctx.getImageData(0, 0, 56, 56);
        canvas._ready = true;
      } catch (e) {
        // Cross-origin: fall back to gradient overlay
        canvas._ready = false;
      }
    }

    if (img.complete) {
      cachePixels();
    } else {
      img.addEventListener('load', cachePixels);
    }

    iconEl.appendChild(canvas);
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

      // Luster — canvas-based per-pixel lighting synced with shimmer
      if (c.luster && c.luster._ready && c.luster._pixels) {
        var canvas = c.luster;
        var ctx = canvas.getContext('2d');
        var src = canvas._pixels;
        var out = ctx.createImageData(56, 56);
        var dir = wave; // -1 to 1, synced with shimmer
        var strength = Math.abs(dir) * 0.8;
        var shine = canvas._shine || 'white';

        // Tint colors per shine style
        var tR = 1, tG = 1, tB = 1;
        var sR = 0, sG = 0, sB = 0;
        switch (shine) {
          case 'gold': tR = 1; tG = 0.85; tB = 0.4; sR = 0.3; sG = 0.2; sB = 0.05; break;
          case 'platinum': tR = 0.85; tG = 0.9; tB = 1; sR = 0.1; sG = 0.12; sB = 0.2; break;
          case 'crimson': tR = 1; tG = 0.5; tB = 0.35; sR = 0.2; sG = 0.03; sB = 0.03; break;
        }

        // Light direction angle shifts with the wave (diagonal sweep)
        var lightAngle = 0.35 + dir * 0.8; // radians, sweeps ~-0.45 to 1.15
        var cosA = Math.cos(lightAngle);
        var sinA = Math.sin(lightAngle);

        for (var i = 0; i < src.data.length; i += 4) {
          var idx = i / 4;
          var px = idx % 56;
          var py = (idx / 56) | 0;
          var a = src.data[i + 3];
          if (a === 0) { out.data[i + 3] = 0; continue; }

          var nx = px / 56;
          var ny = py / 56;

          // Project pixel position onto light direction vector
          // This creates a diagonal sweep, not just left-right
          var proj = (nx - 0.5) * cosA + (ny - 0.5) * sinA;
          var lightFactor = proj * 2.5 * dir;
          lightFactor = Math.max(-1, Math.min(1, lightFactor)) * strength;

          var r = src.data[i] / 255;
          var g = src.data[i + 1] / 255;
          var b = src.data[i + 2] / 255;

          if (lightFactor > 0) {
            // Saturate
            var lum = 0.299 * r + 0.587 * g + 0.114 * b;
            var sat = 1 + lightFactor * 1.5;
            r = lum + (r - lum) * sat;
            g = lum + (g - lum) * sat;
            b = lum + (b - lum) * sat;
            r = Math.max(0, r); g = Math.max(0, g); b = Math.max(0, b);

            // Screen brightness (reduced)
            var lift = lightFactor * 0.6;
            r = 1 - (1 - r) * (1 - lift);
            g = 1 - (1 - g) * (1 - lift);
            b = 1 - (1 - b) * (1 - lift);

            // Tint (reduced)
            var ta = lightFactor * 0.2;
            r = r * (1 - ta) + tR * ta;
            g = g * (1 - ta) + tG * ta;
            b = b * (1 - ta) + tB * ta;
          } else {
            var dim = 1 + lightFactor * 0.3;
            r *= dim; g *= dim; b *= dim;
          }

          out.data[i] = Math.min(255, Math.max(0, r * 255));
          out.data[i + 1] = Math.min(255, Math.max(0, g * 255));
          out.data[i + 2] = Math.min(255, Math.max(0, b * 255));
          out.data[i + 3] = a;
        }

        ctx.putImageData(out, 0, 0);
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
