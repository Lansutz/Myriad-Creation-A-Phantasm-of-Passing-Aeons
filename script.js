const canvas = document.querySelector('#cosmos');
const context = canvas.getContext('2d');
const density = document.querySelector('#density');
const counter = document.querySelector('#world-count');
const awaken = document.querySelector('#begin');

let particles = [];
let pointer = { x: 0, y: 0 };
let witnessed = 0;
let active = false;

function createParticle() {
  return {
    x: Math.random() * innerWidth,
    y: Math.random() * innerHeight,
    size: Math.random() * 1.8 + .25,
    vx: (Math.random() - .5) * .22,
    vy: (Math.random() - .5) * .22,
    hue: Math.random() > .72 ? 292 : 210,
  };
}

function reset() {
  const scale = window.devicePixelRatio || 1;
  canvas.width = innerWidth * scale;
  canvas.height = innerHeight * scale;
  canvas.style.width = `${innerWidth}px`;
  canvas.style.height = `${innerHeight}px`;
  context.setTransform(scale, 0, 0, scale, 0, 0);
  particles = Array.from({ length: Number(density.value) }, createParticle);
}

function frame() {
  context.clearRect(0, 0, innerWidth, innerHeight);
  particles.forEach((particle) => {
    const dx = pointer.x - particle.x;
    const dy = pointer.y - particle.y;
    const distance = Math.hypot(dx, dy);
    if (distance < 180 && active) {
      particle.vx -= dx / (distance || 1) * .002;
      particle.vy -= dy / (distance || 1) * .002;
    }
    particle.x = (particle.x + particle.vx + innerWidth) % innerWidth;
    particle.y = (particle.y + particle.vy + innerHeight) % innerHeight;
    particle.vx *= .995;
    particle.vy *= .995;
    context.beginPath();
    context.fillStyle = `hsla(${particle.hue}, 90%, 84%, ${.36 + particle.size / 4})`;
    context.arc(particle.x, particle.y, particle.size, 0, Math.PI * 2);
    context.fill();
  });
  requestAnimationFrame(frame);
}

addEventListener('pointermove', ({ clientX, clientY }) => { pointer = { x: clientX, y: clientY }; });
addEventListener('resize', reset);
density.addEventListener('input', reset);
awaken.addEventListener('click', () => {
  active = !active;
  witnessed += 1;
  counter.textContent = witnessed;
  awaken.textContent = active ? 'Let the worlds drift' : 'Awaken the constellations';
  particles.forEach((particle) => {
    particle.vx += (Math.random() - .5) * 2.5;
    particle.vy += (Math.random() - .5) * 2.5;
  });
});

reset();
frame();
