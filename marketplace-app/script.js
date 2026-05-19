/**
 * PromoHunt — script.js
 * ─────────────────────────────────────────────────────────
 * Módulos:
 *   DATA        — constantes de UI (marketplaces, sugestões)
 *   guessCategory — inferência de categoria local (espelho do backend)
 *   API         — camada de dados: ML direto + backend proxy
 *   STATE       — estado centralizado
 *   RENDER      — funções de renderização
 *   SEARCH      — filtragem e ordenação local
 *   ACTIONS     — abrir ofertas, favoritos
 *   FAV PANEL   — painel lateral de favoritos
 *   AUTOCOMPLETE
 *   TOAST
 *   EVENTS      — todos os listeners
 *   INIT        — bootstrap
 */

'use strict';

/* ══════════════════════════════════════════════════════════
   DATA — constantes de UI
   ══════════════════════════════════════════════════════════ */
const MARKETPLACES = {
  shopee       : { label: 'Shopee',        color: '#ee4d2d', logo: 'https://upload.wikimedia.org/wikipedia/commons/thumb/f/fe/Shopee.svg/512px-Shopee.svg.png' },
  mercadolivre : { label: 'Mercado Livre', color: '#ffe600', logo: 'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f5/Mercado_Livre_-_2016.svg/320px-Mercado_Livre_-_2016.svg.png' },
  amazon       : { label: 'Amazon',        color: '#ff9900', logo: 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a9/Amazon_logo.svg/320px-Amazon_logo.svg.png' },
  magalu       : { label: 'Magalu',        color: '#0086ff', logo: 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a1/Magazine_Luiza_logo_%282023%29.svg/320px-Magazine_Luiza_logo_%282023%29.svg.png' },
};

const SUGGESTIONS = [
  { text: 'Samsung Galaxy S24',  icon: '📱' },
  { text: 'iPhone 15 Pro',       icon: '📱' },
  { text: 'Notebook Lenovo',     icon: '💻' },
  { text: 'Smart TV 55 polegadas', icon: '📺' },
  { text: 'AirPods Pro',         icon: '🎧' },
  { text: 'PlayStation 5',       icon: '🎮' },
  { text: 'Xbox Series X',       icon: '🎮' },
  { text: 'Nike Air Max',        icon: '👟' },
  { text: 'Monitor Gamer',       icon: '🖥️' },
  { text: 'SSD NVMe 1TB',        icon: '💾' },
  { text: 'Cafeteira Expresso',  icon: '☕' },
  { text: 'Aspirador Robô',      icon: '🤖' },
  { text: 'Xiaomi Redmi Note',   icon: '📱' },
  { text: 'RTX 4060',            icon: '🖥️' },
  { text: 'Headset Gamer',       icon: '🎧' },
];

/** Infere categoria — espelho do guessCategory do backend */
function guessCategory(name = '') {
  const n = name.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
  if (/celular|iphone|galaxy|smartphone|redmi|motorola|poco|zenfone/.test(n))            return 'celulares';
  if (/notebook|laptop|macbook|ultrabook|teclado|mouse|ssd|placa|processador|memoria/.test(n)) return 'informatica';
  if (/\btv\b|televisao|smart tv|monitor|projetor|som |caixa de som|headset|fone|soundbar/.test(n)) return 'eletronicos';
  if (/game|playstation|xbox|nintendo|joystick|controle|videogame|ps5|ps4/.test(n))      return 'games';
  if (/tenis |camisa|calca|vestido|sapato|sandalia|jaqueta|nike|adidas|puma|roupas/.test(n)) return 'moda';
  if (/fogao|geladeira|aspirador|cafeteira|panela|liquidificador|micro-ondas|ventilador|ar.condicionado/.test(n)) return 'casa';
  if (/perfume|maquiagem|shampoo|creme|hidratante|protetor|batom|secador/.test(n))       return 'beleza';
  if (/bicicleta|musculacao|academia|futebol|corrida|natacao|fitness|esteira/.test(n))   return 'esportes';
  return 'eletronicos';
}

/* ══════════════════════════════════════════════════════════
   API — camada de dados
   ══════════════════════════════════════════════════════════ */
const API = {
  BACKEND     : 'http://localhost:3001',
  backendOnline: false,

  /** Verifica se o backend local está rodando */
  async checkBackend() {
    try {
      const ctrl = new AbortController();
      const timer = setTimeout(() => ctrl.abort(), 1800);
      const res = await fetch(`${this.BACKEND}/health`, { signal: ctrl.signal });
      clearTimeout(timer);
      this.backendOnline = res.ok;
    } catch {
      this.backendOnline = false;
    }
    this._updateStatusBar();
    return this.backendOnline;
  },

  /** Busca produtos. Se backend online → usa rota combinada.
   *  Se offline → usa apenas Mercado Livre direto do browser. */
  async search(query) {
    const q = (query || '').trim() || 'smartphone';

    if (this.backendOnline) {
      try {
        const res  = await fetch(
          `${this.BACKEND}/api/search?q=${encodeURIComponent(q)}&limit=24`,
          { signal: AbortSignal.timeout(15000) }
        );
        const data = await res.json();
        this._reportErrors(data.errors);
        return data.products || [];
      } catch (err) {
        console.warn('[API] Backend falhou, usando fallback ML direto:', err.message);
        this.backendOnline = false;
        this._updateStatusBar();
      }
    }

    /* Fallback: Mercado Livre diretamente (CORS habilitado na API deles) */
    showToast('ℹ️ Backend offline — exibindo apenas Mercado Livre');
    return this._fetchML(q);
  },

  /** Chama a API pública do Mercado Livre diretamente do browser */
  async _fetchML(query) {
    const url = `https://api.mercadolibre.com/sites/MLB/search?q=${encodeURIComponent(query)}&limit=24&sort=relevance`;
    const res  = await fetch(url, { signal: AbortSignal.timeout(10000) });
    if (!res.ok) throw new Error(`ML API: ${res.status}`);
    const data = await res.json();
    return (data.results || []).map(item => this._normML(item));
  },

  /** Normaliza item da API do ML para o schema interno */
  _normML(item) {
    const priceOld = item.original_price || null;
    const discount = priceOld
      ? Math.max(0, Math.round((1 - item.price / priceOld) * 100))
      : 0;
    return {
      id           : `ml_${item.id}`,
      name         : item.title,
      category     : guessCategory(item.title),
      marketplace  : 'mercadolivre',
      image        : (item.thumbnail || '').replace('-I.jpg', '-O.jpg').replace('-I.webp', '-O.webp'),
      priceOld,
      priceCurrent : item.price,
      pixPrice     : null,
      discount,
      installments : item.installments
        ? `${item.installments.quantity}x R$ ${Number(item.installments.amount).toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`
        : null,
      rating   : 4.5,
      reviews  : item.sold_quantity || 0,
      badges   : [
        ...(item.shipping?.free_shipping ? ['frete'] : []),
        ...(discount >= 30 ? ['hot'] : []),
      ],
      url      : item.permalink,
      addedAt  : new Date().toISOString(),
    };
  },

  _reportErrors(errors = {}) {
    const failed = Object.entries(errors).filter(([, v]) => v);
    if (failed.length) {
      console.warn('[API] Erros por marketplace:', Object.fromEntries(failed));
    }
  },

  _updateStatusBar() {
    const bar = document.getElementById('backendStatusBar');
    if (!bar) return;
    if (this.backendOnline) {
      bar.className = 'backend-status backend-status--online';
      bar.innerHTML = '🟢 Backend online — buscando em todos os marketplaces';
    } else {
      bar.className = 'backend-status backend-status--offline';
      bar.innerHTML = `🟡 Backend offline — exibindo apenas Mercado Livre &nbsp;·&nbsp; <a href="#" onclick="API.retryBackend()">Tentar novamente</a>`;
    }
    bar.style.display = 'flex';
  },

  async retryBackend() {
    showToast('🔄 Verificando backend…');
    await this.checkBackend();
    if (this.backendOnline) runSearch();
  },
};

/* ══════════════════════════════════════════════════════════
   STATE
   ══════════════════════════════════════════════════════════ */

/** Carrega favoritos salvos. Schema v2: array de objetos de produto. */
function loadFavorites() {
  try {
    const saved = JSON.parse(localStorage.getItem('ph_favorites_v2') || '[]');
    return new Map(saved.map(p => [p.id, p]));
  } catch { return new Map(); }
}

function saveFavorites() {
  localStorage.setItem('ph_favorites_v2', JSON.stringify([...state.favorites.values()]));
}

const state = {
  allProducts : [],
  filtered    : [],
  displayed   : [],
  query       : '',
  category    : 'all',
  marketplace : 'all',
  sort        : 'relevance',
  discountMin : 0,
  priceMin    : null,
  priceMax    : null,
  page        : 1,
  pageSize    : 12,
  loading     : false,
  favorites   : loadFavorites(),   /* Map<id, product> */
  isListView  : false,
};

/* ══════════════════════════════════════════════════════════
   RENDER
   ══════════════════════════════════════════════════════════ */
function renderStars(rating) {
  const r   = Math.min(5, Math.max(0, Number(rating) || 4.5));
  const full = Math.floor(r);
  const half = (r % 1) >= 0.5 ? 1 : 0;
  return '★'.repeat(full) + (half ? '½' : '') + '☆'.repeat(5 - full - half);
}

function formatBRL(value) {
  return Number(value).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function renderBadges(product) {
  const map = {
    hot   : ['🔥 Em alta',     'hot'],
    new   : ['✨ Novo',        'new'],
    frete : ['🚚 Frete grátis','frete'],
  };
  const discountBadge = product.discount > 0
    ? `<span class="badge badge--discount">-${product.discount}%</span>`
    : '';
  const rest = (product.badges || [])
    .filter(k => map[k])
    .map(k => `<span class="badge badge--${map[k][1]}">${map[k][0]}</span>`)
    .join('');
  return discountBadge + rest;
}

function renderProductCard(product) {
  const isFav = state.favorites.has(product.id);
  const mkt   = MARKETPLACES[product.marketplace] || { label: product.marketplace };

  /* IDs são strings (ex: "ml_MLB123") — precisam de aspas no onclick */
  const safeId = JSON.stringify(product.id);

  return `
    <article class="product-card" role="listitem"
      data-id="${product.id}"
      onclick="handleCardClick(event, ${safeId})"
    >
      <div class="product-card__image-wrap">
        <div class="badges">${renderBadges(product)}</div>

        <button
          class="btn-fav ${isFav ? 'active' : ''}"
          aria-label="${isFav ? 'Remover dos favoritos' : 'Adicionar aos favoritos'}"
          onclick="toggleFavorite(event, ${safeId})"
        >${isFav ? '❤️' : '🤍'}</button>

        <img
          class="product-card__img"
          src="${product.image}"
          alt="${product.name}"
          loading="lazy"
          onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22200%22 height=%22200%22%3E%3Crect fill=%22%231a1a2e%22 width=%22200%22 height=%22200%22/%3E%3Ctext x=%2250%25%22 y=%2250%25%22 dominant-baseline=%22middle%22 text-anchor=%22middle%22 fill=%22%235a5a7a%22 font-size=%2214%22%3ESem imagem%3C/text%3E%3C/svg%3E'"
        />
      </div>

      <div class="product-card__body">
        <span class="product-store store--${product.marketplace}">${mkt.label}</span>

        <h3 class="product-name" title="${product.name}">${product.name}</h3>

        <div class="product-rating">
          <span class="stars">${renderStars(product.rating)}</span>
          <span>${Number(product.rating).toFixed(1)}
            ${product.reviews > 0 ? `(${product.reviews.toLocaleString('pt-BR')})` : ''}
          </span>
        </div>

        <div class="product-prices">
          ${product.priceOld
            ? `<span class="price-old">De ${formatBRL(product.priceOld)}</span>`
            : ''}
          <div class="price-row">
            <span class="price-current">${formatBRL(product.priceCurrent)}</span>
            ${product.pixPrice
              ? `<span class="price-pix">PIX ${formatBRL(product.pixPrice)}</span>`
              : ''}
          </div>
          ${product.installments
            ? `<span class="product-installments">${product.installments}</span>`
            : ''}
        </div>

        <button class="btn-offer" onclick="openOffer(event, ${safeId})">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/>
            <polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/>
          </svg>
          Ver oferta
        </button>
      </div>
    </article>
  `;
}

function renderSkeletons(n = 12) {
  return Array.from({ length: n }, () => `
    <div class="skeleton-card">
      <div class="skeleton-img"></div>
      <div class="skeleton-body">
        <div class="skeleton-line" style="width:40%;height:10px"></div>
        <div class="skeleton-line" style="width:90%"></div>
        <div class="skeleton-line" style="width:75%"></div>
        <div class="skeleton-line" style="width:55%;height:20px;margin-top:8px"></div>
        <div class="skeleton-line" style="width:100%;height:38px;border-radius:8px;margin-top:6px"></div>
      </div>
    </div>
  `).join('');
}

function renderProducts(append = false) {
  const grid  = document.getElementById('productsGrid');
  const empty = document.getElementById('emptyState');
  const count = document.getElementById('resultsCount');

  if (!append) grid.innerHTML = '';

  if (!state.filtered.length) {
    empty.style.display = 'block';
    count.innerHTML = 'Nenhum resultado';
    return;
  }

  empty.style.display = 'none';

  const start = (state.page - 1) * state.pageSize;
  const slice = state.filtered.slice(start, start + state.pageSize);
  state.displayed = [...(append ? state.displayed : []), ...slice];

  grid.insertAdjacentHTML('beforeend', slice.map(renderProductCard).join(''));

  const total = state.filtered.length;
  count.innerHTML = `<strong>${total.toLocaleString('pt-BR')}</strong> oferta${total !== 1 ? 's' : ''} encontrada${total !== 1 ? 's' : ''}`;

  document.getElementById('loadMoreWrapper').style.display =
    state.displayed.length < state.filtered.length ? 'block' : 'none';
}

function renderFavorites() {
  const body  = document.getElementById('favPanelBody');
  const badge = document.getElementById('navFavCount');

  badge.textContent = state.favorites.size;

  if (!state.favorites.size) {
    body.innerHTML = `<p class="fav-empty">Nenhum favorito ainda.<br>Clique no 🤍 dos cards para salvar.</p>`;
    return;
  }

  body.innerHTML = [...state.favorites.values()].map(p => `
    <div class="fav-card" onclick="openOffer(null, ${JSON.stringify(p.id)})">
      <img class="fav-card__img" src="${p.image}" alt="${p.name}" loading="lazy"
        onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%2272%22 height=%2272%22%3E%3Crect fill=%22%231a1a2e%22 width=%2272%22 height=%2272%22/%3E%3C/svg%3E'"/>
      <div class="fav-card__info">
        <span class="fav-card__name">${p.name}</span>
        <span class="fav-card__price">${formatBRL(p.priceCurrent)}</span>
      </div>
      <button class="fav-card__remove" onclick="toggleFavorite(event, ${JSON.stringify(p.id)})" aria-label="Remover">✕</button>
    </div>
  `).join('');
}

/* ══════════════════════════════════════════════════════════
   SEARCH & FILTERS — filtragem local sobre os dados retornados
   ══════════════════════════════════════════════════════════ */
function applyFilters() {
  let result = [...state.allProducts];

  /* Filtro textual local (complementa a busca da API) */
  if (state.query.trim()) {
    const q = state.query.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
    result = result.filter(p => {
      const text = `${p.name} ${p.category} ${p.marketplace}`
        .toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
      return text.includes(q);
    });
  }

  if (state.category    !== 'all') result = result.filter(p => p.category    === state.category);
  if (state.marketplace !== 'all') result = result.filter(p => p.marketplace === state.marketplace);
  if (state.discountMin  >   0)    result = result.filter(p => p.discount    >= state.discountMin);
  if (state.priceMin    !== null)  result = result.filter(p => p.priceCurrent >= state.priceMin);
  if (state.priceMax    !== null)  result = result.filter(p => p.priceCurrent <= state.priceMax);

  switch (state.sort) {
    case 'price_asc' : result.sort((a, b) => a.priceCurrent - b.priceCurrent); break;
    case 'price_desc': result.sort((a, b) => b.priceCurrent - a.priceCurrent); break;
    case 'discount'  : result.sort((a, b) => b.discount     - a.discount);     break;
    case 'newest'    : result.sort((a, b) => new Date(b.addedAt) - new Date(a.addedAt)); break;
    default: break; /* relevance — mantém ordem da API */
  }

  state.filtered = result;
}

async function runSearch() {
  if (state.loading) return;
  state.loading = true;
  state.page    = 1;

  const grid = document.getElementById('productsGrid');
  grid.innerHTML = renderSkeletons(12);
  document.getElementById('emptyState').style.display       = 'none';
  document.getElementById('loadMoreWrapper').style.display  = 'none';

  try {
    state.allProducts = await API.search(state.query);
    applyFilters();
    renderProducts();
  } catch (err) {
    console.error('[runSearch]', err);
    showToast('❌ Erro ao buscar produtos. Verifique sua conexão.');
    grid.innerHTML = '';
    document.getElementById('emptyState').style.display = 'block';
  } finally {
    state.loading = false;
  }
}

function loadNextPage() {
  if (state.loading) return;
  if (state.displayed.length >= state.filtered.length) return;
  state.page++;
  renderProducts(true);
}

/* ══════════════════════════════════════════════════════════
   ACTIONS
   ══════════════════════════════════════════════════════════ */
function openOffer(event, productId) {
  if (event) event.stopPropagation();
  const product = state.allProducts.find(p => p.id === productId)
               || [...state.favorites.values()].find(p => p.id === productId);
  if (!product?.url) return;
  window.open(product.url, '_blank', 'noopener,noreferrer');
}

function handleCardClick(event, productId) {
  if (event.target.closest('.btn-fav')) return;
  openOffer(null, productId);
}

function toggleFavorite(event, productId) {
  if (event) event.stopPropagation();
  const wasFav = state.favorites.has(productId);

  if (wasFav) {
    state.favorites.delete(productId);
  } else {
    const product = state.allProducts.find(p => p.id === productId);
    if (product) state.favorites.set(productId, product);
  }

  saveFavorites();

  /* Atualiza botão no card */
  const card = document.querySelector(`.product-card[data-id="${CSS.escape(productId)}"]`);
  if (card) {
    const btn = card.querySelector('.btn-fav');
    const now = !wasFav;
    btn.classList.toggle('active', now);
    btn.textContent = now ? '❤️' : '🤍';
    btn.setAttribute('aria-label', now ? 'Remover dos favoritos' : 'Adicionar aos favoritos');
  }

  renderFavorites();
  showToast(wasFav ? '💔 Removido dos favoritos' : '❤️ Adicionado aos favoritos!');
}

/* ══════════════════════════════════════════════════════════
   FAVORITES PANEL
   ══════════════════════════════════════════════════════════ */
function openFavoritesPanel() {
  renderFavorites();
  document.getElementById('favPanel').classList.add('open');
  document.getElementById('overlay').classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeFavoritesPanel() {
  document.getElementById('favPanel').classList.remove('open');
  document.getElementById('overlay').classList.remove('open');
  document.body.style.overflow = '';
}

/* ══════════════════════════════════════════════════════════
   AUTOCOMPLETE
   ══════════════════════════════════════════════════════════ */
let acIndex = -1;

function showAutocomplete(query) {
  const list = document.getElementById('autocompleteList');
  if (!query.trim()) { list.classList.remove('open'); return; }

  const q = query.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
  const matches = SUGGESTIONS.filter(s =>
    s.text.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').includes(q)
  ).slice(0, 6);

  if (!matches.length) { list.classList.remove('open'); return; }

  list.innerHTML = matches.map((s, i) => `
    <li class="autocomplete-item" role="option" data-index="${i}" tabindex="-1">
      <span class="autocomplete-item-icon">${s.icon}</span>${s.text}
    </li>
  `).join('');
  list.classList.add('open');
  acIndex = -1;

  list.querySelectorAll('.autocomplete-item').forEach(item => {
    item.addEventListener('click', () => {
      document.getElementById('searchInput').value = item.textContent.trim();
      list.classList.remove('open');
      submitSearch();
    });
  });
}

function navigateAutocomplete(dir) {
  const items = document.querySelectorAll('.autocomplete-item');
  if (!items.length) return;
  items[acIndex]?.classList.remove('selected');
  acIndex = (acIndex + dir + items.length) % items.length;
  items[acIndex].classList.add('selected');
  document.getElementById('searchInput').value = items[acIndex].textContent.trim();
}

/* ══════════════════════════════════════════════════════════
   TOAST
   ══════════════════════════════════════════════════════════ */
let toastTimer = null;
function showToast(message, duration = 2600) {
  const toast = document.getElementById('toast');
  toast.textContent = message;
  toast.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toast.classList.remove('show'), duration);
}

/* ══════════════════════════════════════════════════════════
   RESET
   ══════════════════════════════════════════════════════════ */
function resetAll() {
  state.query       = '';
  state.category    = 'all';
  state.marketplace = 'all';
  state.sort        = 'relevance';
  state.discountMin = 0;
  state.priceMin    = null;
  state.priceMax    = null;

  document.getElementById('searchInput').value    = '';
  document.getElementById('sortSelect').value     = 'relevance';
  document.getElementById('discountFilter').value = '0';
  document.getElementById('priceMin').value       = '';
  document.getElementById('priceMax').value       = '';
  document.getElementById('searchClear').classList.remove('visible');

  document.querySelectorAll('.cat-pill').forEach(el =>
    el.classList.toggle('active', el.dataset.category === 'all'));
  document.querySelectorAll('.mkt-filter').forEach(el =>
    el.classList.toggle('active', el.dataset.marketplace === 'all'));

  runSearch();
}

/* ══════════════════════════════════════════════════════════
   EVENTS
   ══════════════════════════════════════════════════════════ */
function submitSearch() {
  state.query = document.getElementById('searchInput').value;
  document.getElementById('autocompleteList').classList.remove('open');
  runSearch();
}

function wireEvents() {
  const searchInput = document.getElementById('searchInput');
  const searchClear = document.getElementById('searchClear');
  const acList      = document.getElementById('autocompleteList');

  searchInput.addEventListener('input', e => {
    searchClear.classList.toggle('visible', e.target.value.length > 0);
    showAutocomplete(e.target.value);
  });

  searchInput.addEventListener('keydown', e => {
    if (e.key === 'Enter')     { submitSearch(); }
    if (e.key === 'ArrowDown') { e.preventDefault(); navigateAutocomplete(1); }
    if (e.key === 'ArrowUp')   { e.preventDefault(); navigateAutocomplete(-1); }
    if (e.key === 'Escape')    { acList.classList.remove('open'); }
  });

  document.getElementById('searchBtn').addEventListener('click', submitSearch);

  searchClear.addEventListener('click', () => {
    searchInput.value = '';
    searchClear.classList.remove('visible');
    acList.classList.remove('open');
    state.query = '';
    runSearch();
  });

  document.addEventListener('click', e => {
    if (!document.getElementById('searchWrapper').contains(e.target)) {
      acList.classList.remove('open');
    }
  });

  document.getElementById('sortSelect').addEventListener('change', e => {
    state.sort = e.target.value;
    applyFilters(); renderProducts();
  });

  document.getElementById('discountFilter').addEventListener('change', e => {
    state.discountMin = Number(e.target.value);
    applyFilters(); renderProducts();
  });

  let priceTimer;
  const onPrice = () => {
    clearTimeout(priceTimer);
    priceTimer = setTimeout(() => {
      const min = document.getElementById('priceMin').value;
      const max = document.getElementById('priceMax').value;
      state.priceMin = min ? Number(min) : null;
      state.priceMax = max ? Number(max) : null;
      applyFilters(); renderProducts();
    }, 500);
  };
  document.getElementById('priceMin').addEventListener('input', onPrice);
  document.getElementById('priceMax').addEventListener('input', onPrice);

  document.getElementById('categoriesBar').addEventListener('click', e => {
    const pill = e.target.closest('.cat-pill');
    if (!pill) return;
    document.querySelectorAll('.cat-pill').forEach(p => p.classList.remove('active'));
    pill.classList.add('active');
    state.category = pill.dataset.category;
    applyFilters(); renderProducts();
  });

  document.getElementById('marketplaceFilters').addEventListener('click', e => {
    const btn = e.target.closest('.mkt-filter');
    if (!btn) return;
    document.querySelectorAll('.mkt-filter').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    state.marketplace = btn.dataset.marketplace;
    applyFilters(); renderProducts();
  });

  document.getElementById('resetFilters').addEventListener('click', resetAll);

  document.getElementById('loadMoreBtn').addEventListener('click', loadNextPage);

  /* Infinite scroll */
  if ('IntersectionObserver' in window) {
    new IntersectionObserver(entries => {
      if (entries[0].isIntersecting && !state.loading) loadNextPage();
    }, { rootMargin: '300px' }).observe(document.getElementById('scrollSentinel'));
  } else {
    document.getElementById('loadMoreWrapper').style.display = 'block';
  }

  /* View toggle */
  document.getElementById('viewGrid').addEventListener('click', () => {
    state.isListView = false;
    document.getElementById('productsGrid').classList.remove('list-view');
    document.getElementById('viewGrid').classList.add('active');
    document.getElementById('viewList').classList.remove('active');
  });
  document.getElementById('viewList').addEventListener('click', () => {
    state.isListView = true;
    document.getElementById('productsGrid').classList.add('list-view');
    document.getElementById('viewList').classList.add('active');
    document.getElementById('viewGrid').classList.remove('active');
  });

  /* Tema */
  document.getElementById('themeToggle').addEventListener('click', () => {
    document.body.classList.toggle('light-theme');
    const light = document.body.classList.contains('light-theme');
    localStorage.setItem('ph_theme', light ? 'light' : 'dark');
    showToast(light ? '☀️ Tema claro ativado' : '🌙 Tema escuro ativado');
  });

  /* Atalho / → foca busca */
  document.addEventListener('keydown', e => {
    if (e.key === '/' && document.activeElement !== searchInput) {
      e.preventDefault(); searchInput.focus();
    }
  });
}

/* ══════════════════════════════════════════════════════════
   INIT
   ══════════════════════════════════════════════════════════ */

/** Injeta a barra de status do backend no DOM */
function injectStatusBar() {
  const bar = document.createElement('div');
  bar.id = 'backendStatusBar';
  bar.className = 'backend-status';
  bar.style.display = 'none';
  /* Insere logo abaixo do hero */
  const hero = document.querySelector('.hero');
  hero?.insertAdjacentElement('afterend', bar);

  /* Adiciona estilo inline para não precisar alterar o CSS */
  const style = document.createElement('style');
  style.textContent = `
    .backend-status {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 8px 20px;
      font-size: .8rem;
      border-bottom: 1px solid var(--border);
      background: var(--bg-filter);
    }
    .backend-status a { color: var(--accent-1); cursor: pointer; }
    .backend-status--online  { color: #4ade80; }
    .backend-status--offline { color: #f59e0b; }
  `;
  document.head.appendChild(style);
}

async function init() {
  if (localStorage.getItem('ph_theme') === 'light') {
    document.body.classList.add('light-theme');
  }

  document.getElementById('navFavCount').textContent = state.favorites.size;

  injectStatusBar();
  wireEvents();

  /* Verifica backend em paralelo com a primeira busca */
  API.checkBackend().then(() => {
    /* Se backend mudou de estado durante a busca inicial, recarrega */
    if (API.backendOnline && state.allProducts.every(p => p.marketplace === 'mercadolivre')) {
      runSearch();
    }
  });

  /* Busca inicial com "smartphone" para mostrar produtos imediatamente */
  await runSearch();
}

document.addEventListener('DOMContentLoaded', init);
