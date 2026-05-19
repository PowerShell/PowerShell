/**
 * PromoHunt — script.js
 * ─────────────────────────────────────────────────────────
 * Arquitectura modular pronta para integração com APIs reais.
 *
 * Módulos:
 *   DATA       — mock / futura camada de API
 *   STATE      — estado centralizado da aplicação
 *   RENDER     — funções de renderização de UI
 *   SEARCH     — busca, filtros e ordenação
 *   EVENTS     — listeners e interações
 *   INIT       — bootstrap
 */

'use strict';

/* ══════════════════════════════════════════════════════════
   DATA — Mock products + API layer stub
   ══════════════════════════════════════════════════════════ */
const MOCK_PRODUCTS = [
  /* ── Eletrônicos ─────────────────────────────── */
  {
    id: 1, name: 'Samsung Galaxy S24 FE 128GB 5G Preto',
    category: 'celulares', marketplace: 'mercadolivre',
    image: 'https://images.tcdn.com.br/img/img_prod/1275614/samsung_galaxy_s24_fe_128gb_6_7_5g_preto_1_ed5d1e8e77f62a76ddb73523ea1b9e25.png',
    priceOld: 3499.90, priceCurrent: 2199.90, pixPrice: 2089.90,
    discount: 37, installments: '12x R$ 183,32',
    rating: 4.8, reviews: 2341,
    badges: ['hot', 'frete'],
    url: 'https://www.mercadolivre.com.br/celulares',
    addedAt: new Date('2024-12-01')
  },
  {
    id: 2, name: 'Notebook Lenovo IdeaPad 3i Intel Core i5 16GB RAM SSD 512GB',
    category: 'informatica', marketplace: 'amazon',
    image: 'https://m.media-amazon.com/images/I/61GiblVz5xL._AC_SL1500_.jpg',
    priceOld: 3799.00, priceCurrent: 2649.00, pixPrice: 2516.55,
    discount: 30, installments: '12x R$ 220,75',
    rating: 4.6, reviews: 891,
    badges: ['frete'],
    url: 'https://www.amazon.com.br/notebooks',
    addedAt: new Date('2024-12-02')
  },
  {
    id: 3, name: 'Smart TV LG 55" OLED 4K 120Hz WebOS ThinQ AI',
    category: 'eletronicos', marketplace: 'magalu',
    image: 'https://a-static.mlcdn.com.br/1500x1500/smart-tv-lg-oled-55-polegadas-4k-uhd-wi-fi-bluetooth/magazineluiza/232124200/ab27c8d3474c09c2bcce2fc45949c7c1.jpg',
    priceOld: 6499.00, priceCurrent: 3799.00, pixPrice: 3609.05,
    discount: 42, installments: '10x R$ 379,90',
    rating: 4.9, reviews: 4120,
    badges: ['hot', 'frete', 'new'],
    url: 'https://www.magazineluiza.com.br/tv-video',
    addedAt: new Date('2024-12-03')
  },
  {
    id: 4, name: 'Apple AirPods Pro 2ª Geração com USB-C',
    category: 'eletronicos', marketplace: 'amazon',
    image: 'https://m.media-amazon.com/images/I/61SUj2aKoEL._AC_SL1500_.jpg',
    priceOld: 1899.00, priceCurrent: 1399.00, pixPrice: 1329.05,
    discount: 26, installments: '12x R$ 116,58',
    rating: 4.8, reviews: 7823,
    badges: ['frete'],
    url: 'https://www.amazon.com.br/fones',
    addedAt: new Date('2024-12-04')
  },
  {
    id: 5, name: 'Xiaomi Redmi Note 13 Pro 256GB 5G Camera 200MP',
    category: 'celulares', marketplace: 'shopee',
    image: 'https://cf.shopee.com.br/file/sg-11134201-22110-r9yw8y9r9piv13_tn',
    priceOld: 1999.90, priceCurrent: 1199.90, pixPrice: 1139.90,
    discount: 40, installments: '12x R$ 99,99',
    rating: 4.7, reviews: 5612,
    badges: ['hot'],
    url: 'https://shopee.com.br/celulares',
    addedAt: new Date('2024-12-05')
  },
  {
    id: 6, name: 'Processador AMD Ryzen 5 7600X Box AM5 6 Núcleos',
    category: 'informatica', marketplace: 'mercadolivre',
    image: 'https://http2.mlstatic.com/D_NQ_NP_726421-MLA72081396283_102023-O.webp',
    priceOld: 1349.00, priceCurrent: 899.00, pixPrice: 854.05,
    discount: 33, installments: '6x R$ 149,83',
    rating: 4.9, reviews: 1230,
    badges: ['hot'],
    url: 'https://www.mercadolivre.com.br/informatica',
    addedAt: new Date('2024-12-06')
  },
  /* ── Games ──────────────────────────────────── */
  {
    id: 7, name: 'PlayStation 5 Slim + DualSense Extra + Headset Pulse 3D',
    category: 'games', marketplace: 'magalu',
    image: 'https://a-static.mlcdn.com.br/1500x1500/console-playstation-5-slim-sony-1tb/magazineluiza/239490200/4d7d0d3a1454a6c0f0d7e7e4f0e7e4f0.jpg',
    priceOld: 4699.00, priceCurrent: 3699.00, pixPrice: 3514.05,
    discount: 21, installments: '12x R$ 308,25',
    rating: 4.9, reviews: 9870,
    badges: ['hot', 'frete'],
    url: 'https://www.magazineluiza.com.br/games',
    addedAt: new Date('2024-12-07')
  },
  {
    id: 8, name: 'Controle Xbox Series X/S Carbon Black Sem Fio',
    category: 'games', marketplace: 'amazon',
    image: 'https://m.media-amazon.com/images/I/61mEwnI9TlL._AC_SL1500_.jpg',
    priceOld: 549.00, priceCurrent: 329.00, pixPrice: 312.55,
    discount: 40, installments: '4x R$ 82,25',
    rating: 4.7, reviews: 3421,
    badges: ['frete'],
    url: 'https://www.amazon.com.br/games',
    addedAt: new Date('2024-12-08')
  },
  {
    id: 9, name: 'God of War Ragnarök PS5 — Edição de Lançamento',
    category: 'games', marketplace: 'shopee',
    image: 'https://cf.shopee.com.br/file/br-11134207-7qukx-lhsq8yzthz8j59',
    priceOld: 349.90, priceCurrent: 179.90, pixPrice: 170.90,
    discount: 49, installments: '3x R$ 59,97',
    rating: 4.8, reviews: 6712,
    badges: ['hot'],
    url: 'https://shopee.com.br/games',
    addedAt: new Date('2024-12-09')
  },
  /* ── Moda ───────────────────────────────────── */
  {
    id: 10, name: 'Tênis Nike Air Max 270 Masculino Preto/Branco',
    category: 'moda', marketplace: 'mercadolivre',
    image: 'https://http2.mlstatic.com/D_NQ_NP_694537-MLA48005040441_102021-O.webp',
    priceOld: 699.90, priceCurrent: 369.90, pixPrice: 351.40,
    discount: 47, installments: '6x R$ 61,65',
    rating: 4.6, reviews: 3201,
    badges: ['hot'],
    url: 'https://www.mercadolivre.com.br/moda',
    addedAt: new Date('2024-12-10')
  },
  {
    id: 11, name: 'Jaqueta Adidas Originals SST Track Jacket Feminina',
    category: 'moda', marketplace: 'shopee',
    image: 'https://cf.shopee.com.br/file/sg-11134201-22110-jaqueta-adidas',
    priceOld: 299.90, priceCurrent: 149.90, pixPrice: 142.40,
    discount: 50, installments: '3x R$ 49,97',
    rating: 4.5, reviews: 1820,
    badges: ['hot', 'frete'],
    url: 'https://shopee.com.br/moda',
    addedAt: new Date('2024-12-11')
  },
  {
    id: 12, name: 'Relógio Smartwatch Garmin Vivoactive 5 GPS Monitor Cardíaco',
    category: 'esportes', marketplace: 'amazon',
    image: 'https://m.media-amazon.com/images/I/71YHFjkJMlL._AC_SL1500_.jpg',
    priceOld: 1799.00, priceCurrent: 1299.00, pixPrice: 1234.05,
    discount: 28, installments: '10x R$ 129,90',
    rating: 4.7, reviews: 980,
    badges: ['new', 'frete'],
    url: 'https://www.amazon.com.br/esportes',
    addedAt: new Date('2024-12-12')
  },
  /* ── Casa ───────────────────────────────────── */
  {
    id: 13, name: 'Aspirador de Pó Robô Roborock S7 MaxV Ultra Lavador Auto-Esvaziamento',
    category: 'casa', marketplace: 'magalu',
    image: 'https://a-static.mlcdn.com.br/1500x1500/roborock-s7-maxv-ultra/magazineluiza/235770800/abc.jpg',
    priceOld: 5299.00, priceCurrent: 3499.00, pixPrice: 3324.05,
    discount: 34, installments: '12x R$ 291,58',
    rating: 4.8, reviews: 2341,
    badges: ['hot', 'frete'],
    url: 'https://www.magazineluiza.com.br/casa',
    addedAt: new Date('2024-12-13')
  },
  {
    id: 14, name: 'Panela Elétrica de Pressão Multilaser 5L Digital Inox',
    category: 'casa', marketplace: 'shopee',
    image: 'https://cf.shopee.com.br/file/panela-multilaser',
    priceOld: 399.90, priceCurrent: 199.90, pixPrice: 189.90,
    discount: 50, installments: '3x R$ 66,63',
    rating: 4.4, reviews: 4102,
    badges: ['hot'],
    url: 'https://shopee.com.br/casa',
    addedAt: new Date('2024-12-14')
  },
  {
    id: 15, name: 'Cafeteira Expresso DeLonghi Dedica Style EC685 15 Bar Inox',
    category: 'casa', marketplace: 'amazon',
    image: 'https://m.media-amazon.com/images/I/61XH-+M3JwL._AC_SL1500_.jpg',
    priceOld: 1299.00, priceCurrent: 799.00, pixPrice: 759.05,
    discount: 38, installments: '8x R$ 99,87',
    rating: 4.7, reviews: 2198,
    badges: ['frete', 'new'],
    url: 'https://www.amazon.com.br/casa',
    addedAt: new Date('2024-12-15')
  },
  /* ── Beleza ─────────────────────────────────── */
  {
    id: 16, name: 'Perfume Chanel Nº5 Eau de Parfum Feminino 100ml',
    category: 'beleza', marketplace: 'mercadolivre',
    image: 'https://http2.mlstatic.com/D_NQ_NP_perfume-chanel.webp',
    priceOld: 1099.00, priceCurrent: 749.00, pixPrice: 711.55,
    discount: 32, installments: '6x R$ 124,83',
    rating: 4.9, reviews: 5412,
    badges: ['frete'],
    url: 'https://www.mercadolivre.com.br/beleza',
    addedAt: new Date('2024-12-16')
  },
  {
    id: 17, name: 'Secador de Cabelo Taiff Secador Profissional 2400W Ion',
    category: 'beleza', marketplace: 'shopee',
    image: 'https://cf.shopee.com.br/file/secador-taiff',
    priceOld: 349.90, priceCurrent: 189.90, pixPrice: 180.40,
    discount: 46, installments: '3x R$ 63,30',
    rating: 4.5, reviews: 7821,
    badges: ['hot'],
    url: 'https://shopee.com.br/beleza',
    addedAt: new Date('2024-12-17')
  },
  /* ── Informática ─────────────────────────────  */
  {
    id: 18, name: 'Monitor Gamer LG UltraGear 27" QHD 165Hz IPS 1ms HDR10',
    category: 'informatica', marketplace: 'magalu',
    image: 'https://a-static.mlcdn.com.br/1500x1500/monitor-gamer-lg-ultragear-27/magazineluiza/monitor-lg.jpg',
    priceOld: 2799.00, priceCurrent: 1799.00, pixPrice: 1709.05,
    discount: 36, installments: '10x R$ 179,90',
    rating: 4.8, reviews: 3201,
    badges: ['hot', 'frete'],
    url: 'https://www.magazineluiza.com.br/informatica',
    addedAt: new Date('2024-12-18')
  },
  {
    id: 19, name: 'Teclado Mecânico Redragon Kumara RGB Switch Blue Gamer',
    category: 'informatica', marketplace: 'shopee',
    image: 'https://cf.shopee.com.br/file/teclado-redragon',
    priceOld: 349.90, priceCurrent: 189.90, pixPrice: 180.40,
    discount: 46, installments: '3x R$ 63,30',
    rating: 4.6, reviews: 12043,
    badges: ['hot'],
    url: 'https://shopee.com.br/informatica',
    addedAt: new Date('2024-12-19')
  },
  {
    id: 20, name: 'SSD Samsung 990 PRO NVMe M.2 2TB PCIe 4.0 7450MB/s',
    category: 'informatica', marketplace: 'amazon',
    image: 'https://m.media-amazon.com/images/I/61sXK1lcl+L._AC_SL1500_.jpg',
    priceOld: 999.00, priceCurrent: 599.00, pixPrice: 569.05,
    discount: 40, installments: '6x R$ 99,83',
    rating: 4.9, reviews: 4120,
    badges: ['hot', 'frete'],
    url: 'https://www.amazon.com.br/informatica',
    addedAt: new Date('2024-12-20')
  },
  /* ── Esportes ───────────────────────────────── */
  {
    id: 21, name: 'Bicicleta Elétrica Caloi E-Vibe City Tour 7V Aro 700',
    category: 'esportes', marketplace: 'mercadolivre',
    image: 'https://http2.mlstatic.com/D_NQ_NP_bicicleta-caloi.webp',
    priceOld: 7999.00, priceCurrent: 5499.00, pixPrice: 5224.05,
    discount: 31, installments: '12x R$ 458,25',
    rating: 4.7, reviews: 420,
    badges: ['new', 'frete'],
    url: 'https://www.mercadolivre.com.br/esportes',
    addedAt: new Date('2024-12-21')
  },
  {
    id: 22, name: 'Esteira Eletromagnética Kikos 540E 110V Dobrável',
    category: 'esportes', marketplace: 'magalu',
    image: 'https://a-static.mlcdn.com.br/1500x1500/esteira-kikos/magazineluiza/esteira.jpg',
    priceOld: 1999.00, priceCurrent: 1199.00, pixPrice: 1139.05,
    discount: 40, installments: '12x R$ 99,92',
    rating: 4.4, reviews: 1870,
    badges: ['hot'],
    url: 'https://www.magazineluiza.com.br/esportes',
    addedAt: new Date('2024-12-22')
  },
  /* ── Mais celulares ──────────────────────────  */
  {
    id: 23, name: 'iPhone 15 Pro 256GB Titânio Natural — 1 ano garantia Apple',
    category: 'celulares', marketplace: 'amazon',
    image: 'https://m.media-amazon.com/images/I/61dX4sZ5qGL._AC_SL1500_.jpg',
    priceOld: 9499.00, priceCurrent: 7499.00, pixPrice: 7124.05,
    discount: 21, installments: '12x R$ 624,92',
    rating: 4.9, reviews: 18203,
    badges: ['frete', 'new'],
    url: 'https://www.amazon.com.br/celulares',
    addedAt: new Date('2024-12-23')
  },
  {
    id: 24, name: 'Motorola Edge 50 Pro 512GB 5G Câmera Pantone 50MP OIS',
    category: 'celulares', marketplace: 'shopee',
    image: 'https://cf.shopee.com.br/file/motorola-edge-50',
    priceOld: 3499.00, priceCurrent: 1999.00, pixPrice: 1899.05,
    discount: 43, installments: '12x R$ 166,58',
    rating: 4.7, reviews: 2310,
    badges: ['hot', 'frete'],
    url: 'https://shopee.com.br/celulares',
    addedAt: new Date('2024-12-24')
  },
];

/* Marketplace metadata */
const MARKETPLACES = {
  shopee:       { label: 'Shopee',        color: '#ee4d2d', logo: 'https://upload.wikimedia.org/wikipedia/commons/thumb/f/fe/Shopee.svg/512px-Shopee.svg.png' },
  mercadolivre: { label: 'Mercado Livre', color: '#ffe600', logo: 'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f5/Mercado_Livre_-_2016.svg/320px-Mercado_Livre_-_2016.svg.png' },
  amazon:       { label: 'Amazon',        color: '#ff9900', logo: 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a9/Amazon_logo.svg/320px-Amazon_logo.svg.png' },
  magalu:       { label: 'Magalu',        color: '#0086ff', logo: 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a1/Magazine_Luiza_logo_%282023%29.svg/320px-Magazine_Luiza_logo_%282023%29.svg.png' },
};

/* Autocomplete suggestions */
const SUGGESTIONS = [
  { text: 'Samsung Galaxy S24', icon: '📱' },
  { text: 'Notebook Lenovo', icon: '💻' },
  { text: 'Smart TV LG 55"', icon: '📺' },
  { text: 'AirPods Pro', icon: '🎧' },
  { text: 'PlayStation 5', icon: '🎮' },
  { text: 'Nike Air Max', icon: '👟' },
  { text: 'iPhone 15 Pro', icon: '📱' },
  { text: 'Monitor Gamer', icon: '🖥️' },
  { text: 'SSD NVMe', icon: '💾' },
  { text: 'Cafeteira Expresso', icon: '☕' },
  { text: 'Aspirador Robô', icon: '🤖' },
  { text: 'Perfume Chanel', icon: '🌹' },
];

/* ── Future API integration stub ─────────────────────────── */
const API = {
  /**
   * Fetch products from a real API endpoint.
   * Replace the mock implementation below with actual fetch() calls
   * when the backend / scraping layer is ready.
   *
   * @param {string} query
   * @param {Object} filters
   * @returns {Promise<Array>}
   */
  async search(query, filters = {}) {
    // Simulates network latency
    await new Promise(r => setTimeout(r, 650));
    return MOCK_PRODUCTS;
  }
};

/* ══════════════════════════════════════════════════════════
   STATE
   ══════════════════════════════════════════════════════════ */
const state = {
  allProducts: [],
  filtered:    [],
  displayed:   [],
  query:       '',
  category:    'all',
  marketplace: 'all',
  sort:        'relevance',
  discountMin: 0,
  priceMin:    null,
  priceMax:    null,
  page:        1,
  pageSize:    12,
  loading:     false,
  favorites:   new Set(JSON.parse(localStorage.getItem('ph_favorites') || '[]')),
  isListView:  false,
};

/* ══════════════════════════════════════════════════════════
   RENDER
   ══════════════════════════════════════════════════════════ */

/** Build star string from numeric rating */
function renderStars(rating) {
  const full  = Math.floor(rating);
  const half  = rating % 1 >= 0.5 ? 1 : 0;
  const empty = 5 - full - half;
  return '★'.repeat(full) + (half ? '½' : '') + '☆'.repeat(empty);
}

/** Format number as BRL currency */
function formatBRL(value) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

/** Render badge HTML */
function renderBadges(product) {
  const map = {
    hot:    ['🔥 Em alta',    'hot'],
    new:    ['✨ Novo',       'new'],
    frete:  ['🚚 Frete grátis', 'frete'],
  };
  const b = product.badges || [];
  const discountBadge = `<span class="badge badge--discount">-${product.discount}%</span>`;
  const rest = b.map(k => map[k] ? `<span class="badge badge--${map[k][1]}">${map[k][0]}</span>` : '').join('');
  return discountBadge + rest;
}

/** Render a single product card */
function renderProductCard(product) {
  const isFav = state.favorites.has(product.id);
  const mkt   = MARKETPLACES[product.marketplace] || {};
  const staggerDelay = Math.random() * 0.15;

  return `
    <article class="product-card" role="listitem"
      data-id="${product.id}"
      style="animation-delay:${staggerDelay.toFixed(2)}s"
      onclick="handleCardClick(event, ${product.id})"
    >
      <div class="product-card__image-wrap">
        <div class="badges">${renderBadges(product)}</div>

        <button
          class="btn-fav ${isFav ? 'active' : ''}"
          aria-label="${isFav ? 'Remover dos favoritos' : 'Adicionar aos favoritos'}"
          onclick="toggleFavorite(event, ${product.id})"
        >${isFav ? '❤️' : '🤍'}</button>

        <img
          class="product-card__img"
          src="${product.image}"
          alt="${product.name}"
          loading="lazy"
          onerror="this.src='data:image/svg+xml,<svg xmlns=\\'http://www.w3.org/2000/svg\\' width=\\'200\\' height=\\'200\\'><rect fill=\\'%231a1a2e\\' width=\\'200\\' height=\\'200\\'/>  <text x=\\'50%\\' y=\\'50%\\' dominant-baseline=\\'middle\\' text-anchor=\\'middle\\' fill=\\'%235a5a7a\\' font-size=\\'14\\'>Sem imagem</text></svg>'"
        />
      </div>

      <div class="product-card__body">
        <span class="product-store store--${product.marketplace}">${mkt.label || product.marketplace}</span>

        <h3 class="product-name" title="${product.name}">${product.name}</h3>

        <div class="product-rating">
          <span class="stars">${renderStars(product.rating)}</span>
          <span>${product.rating.toFixed(1)} (${product.reviews.toLocaleString('pt-BR')})</span>
        </div>

        <div class="product-prices">
          <span class="price-old">De ${formatBRL(product.priceOld)}</span>
          <div class="price-row">
            <span class="price-current">${formatBRL(product.priceCurrent)}</span>
            ${product.pixPrice ? `<span class="price-pix">PIX ${formatBRL(product.pixPrice)}</span>` : ''}
          </div>
          ${product.installments ? `<span class="product-installments">${product.installments}</span>` : ''}
        </div>

        <button class="btn-offer" onclick="openOffer(event, ${product.id})">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/></svg>
          Ver oferta
        </button>
      </div>
    </article>
  `;
}

/** Render N skeleton cards */
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

/** Render product grid with current page slice */
function renderProducts(append = false) {
  const grid  = document.getElementById('productsGrid');
  const empty = document.getElementById('emptyState');
  const count = document.getElementById('resultsCount');

  if (!append) grid.innerHTML = '';

  if (state.filtered.length === 0) {
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
  count.innerHTML = `<strong>${total}</strong> oferta${total !== 1 ? 's' : ''} encontrada${total !== 1 ? 's' : ''}`;

  // Toggle load more button
  const hasMore = state.displayed.length < state.filtered.length;
  document.getElementById('loadMoreWrapper').style.display = hasMore ? 'block' : 'none';
}

/** Render favorites panel */
function renderFavorites() {
  const body  = document.getElementById('favPanelBody');
  const count = document.getElementById('navFavCount');

  count.textContent = state.favorites.size;

  if (state.favorites.size === 0) {
    body.innerHTML = `<p class="fav-empty">Nenhum favorito ainda.<br>Clique no 🤍 dos cards para salvar.</p>`;
    return;
  }

  const favItems = MOCK_PRODUCTS.filter(p => state.favorites.has(p.id));
  body.innerHTML = favItems.map(p => `
    <div class="fav-card" onclick="openOffer(null, ${p.id})">
      <img class="fav-card__img" src="${p.image}" alt="${p.name}" loading="lazy"/>
      <div class="fav-card__info">
        <span class="fav-card__name">${p.name}</span>
        <span class="fav-card__price">${formatBRL(p.priceCurrent)}</span>
      </div>
      <button class="fav-card__remove" onclick="toggleFavorite(event, ${p.id})" aria-label="Remover">✕</button>
    </div>
  `).join('');
}

/* ══════════════════════════════════════════════════════════
   SEARCH & FILTERS
   ══════════════════════════════════════════════════════════ */

/** Apply all active filters to state.allProducts → state.filtered */
function applyFilters() {
  let result = [...state.allProducts];

  // Text search
  if (state.query.trim()) {
    const q = state.query.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
    result = result.filter(p => {
      const text = (p.name + ' ' + p.category + ' ' + p.marketplace)
        .toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
      return text.includes(q);
    });
  }

  // Category
  if (state.category !== 'all') {
    result = result.filter(p => p.category === state.category);
  }

  // Marketplace
  if (state.marketplace !== 'all') {
    result = result.filter(p => p.marketplace === state.marketplace);
  }

  // Minimum discount
  if (state.discountMin > 0) {
    result = result.filter(p => p.discount >= state.discountMin);
  }

  // Price range
  if (state.priceMin !== null) {
    result = result.filter(p => p.priceCurrent >= state.priceMin);
  }
  if (state.priceMax !== null) {
    result = result.filter(p => p.priceCurrent <= state.priceMax);
  }

  // Sort
  switch (state.sort) {
    case 'price_asc':  result.sort((a, b) => a.priceCurrent - b.priceCurrent); break;
    case 'price_desc': result.sort((a, b) => b.priceCurrent - a.priceCurrent); break;
    case 'discount':   result.sort((a, b) => b.discount - a.discount); break;
    case 'newest':     result.sort((a, b) => b.addedAt - a.addedAt); break;
    default: break; // relevance — keep original order
  }

  state.filtered = result;
}

/** Run a full search cycle: show skeleton → fetch → filter → render */
async function runSearch() {
  if (state.loading) return;
  state.loading = true;
  state.page = 1;

  const grid = document.getElementById('productsGrid');
  grid.innerHTML = renderSkeletons(12);
  document.getElementById('emptyState').style.display = 'none';
  document.getElementById('loadMoreWrapper').style.display = 'none';

  try {
    state.allProducts = await API.search(state.query);
    applyFilters();
    renderProducts();
  } finally {
    state.loading = false;
  }
}

/** Load next page */
function loadNextPage() {
  if (state.loading) return;
  if (state.displayed.length >= state.filtered.length) return;
  state.page++;
  renderProducts(true);
}

/* ══════════════════════════════════════════════════════════
   PRODUCT ACTIONS
   ══════════════════════════════════════════════════════════ */

/** Open product URL in new tab */
function openOffer(event, productId) {
  if (event) event.stopPropagation();
  const product = MOCK_PRODUCTS.find(p => p.id === productId);
  if (!product) return;
  window.open(product.url, '_blank', 'noopener,noreferrer');
}

/** Card click — open offer (but allow fav button to intercept) */
function handleCardClick(event, productId) {
  if (event.target.closest('.btn-fav')) return;
  openOffer(null, productId);
}

/** Toggle favorite and persist to localStorage */
function toggleFavorite(event, productId) {
  if (event) event.stopPropagation();

  const wasAdded = state.favorites.has(productId);
  wasAdded ? state.favorites.delete(productId) : state.favorites.add(productId);

  localStorage.setItem('ph_favorites', JSON.stringify([...state.favorites]));

  // Update button in grid
  const card = document.querySelector(`.product-card[data-id="${productId}"]`);
  if (card) {
    const btn = card.querySelector('.btn-fav');
    btn.classList.toggle('active', !wasAdded);
    btn.textContent = wasAdded ? '🤍' : '❤️';
    btn.setAttribute('aria-label', wasAdded ? 'Adicionar aos favoritos' : 'Remover dos favoritos');
  }

  renderFavorites();
  showToast(wasAdded ? '💔 Removido dos favoritos' : '❤️ Adicionado aos favoritos!');
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
      <span class="autocomplete-item-icon">${s.icon}</span>
      ${s.text}
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

function navigateAutocomplete(direction) {
  const items = document.querySelectorAll('.autocomplete-item');
  if (!items.length) return;
  items[acIndex]?.classList.remove('selected');
  acIndex = (acIndex + direction + items.length) % items.length;
  items[acIndex].classList.add('selected');
  document.getElementById('searchInput').value = items[acIndex].textContent.trim();
}

/* ══════════════════════════════════════════════════════════
   TOAST
   ══════════════════════════════════════════════════════════ */
let toastTimer = null;
function showToast(message, duration = 2400) {
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

  document.getElementById('searchInput').value  = '';
  document.getElementById('sortSelect').value   = 'relevance';
  document.getElementById('discountFilter').value = '0';
  document.getElementById('priceMin').value     = '';
  document.getElementById('priceMax').value     = '';

  document.querySelectorAll('.cat-pill').forEach(el => el.classList.toggle('active', el.dataset.category === 'all'));
  document.querySelectorAll('.mkt-filter').forEach(el => el.classList.toggle('active', el.dataset.marketplace === 'all'));

  document.getElementById('searchClear').classList.remove('visible');
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
  const searchBtn   = document.getElementById('searchBtn');
  const acList      = document.getElementById('autocompleteList');

  // Search input
  searchInput.addEventListener('input', e => {
    const val = e.target.value;
    searchClear.classList.toggle('visible', val.length > 0);
    showAutocomplete(val);
  });

  searchInput.addEventListener('keydown', e => {
    if (e.key === 'Enter')      { submitSearch(); }
    if (e.key === 'ArrowDown')  { e.preventDefault(); navigateAutocomplete(1); }
    if (e.key === 'ArrowUp')    { e.preventDefault(); navigateAutocomplete(-1); }
    if (e.key === 'Escape')     { acList.classList.remove('open'); }
  });

  searchBtn.addEventListener('click', submitSearch);

  searchClear.addEventListener('click', () => {
    searchInput.value = '';
    searchClear.classList.remove('visible');
    acList.classList.remove('open');
    state.query = '';
    runSearch();
  });

  // Close autocomplete on outside click
  document.addEventListener('click', e => {
    if (!document.getElementById('searchWrapper').contains(e.target)) {
      acList.classList.remove('open');
    }
  });

  // Sort
  document.getElementById('sortSelect').addEventListener('change', e => {
    state.sort = e.target.value;
    applyFilters();
    renderProducts();
  });

  // Discount filter
  document.getElementById('discountFilter').addEventListener('change', e => {
    state.discountMin = Number(e.target.value);
    applyFilters();
    renderProducts();
  });

  // Price range
  let priceTimer;
  function onPriceChange() {
    clearTimeout(priceTimer);
    priceTimer = setTimeout(() => {
      const min = document.getElementById('priceMin').value;
      const max = document.getElementById('priceMax').value;
      state.priceMin = min ? Number(min) : null;
      state.priceMax = max ? Number(max) : null;
      applyFilters();
      renderProducts();
    }, 500);
  }
  document.getElementById('priceMin').addEventListener('input', onPriceChange);
  document.getElementById('priceMax').addEventListener('input', onPriceChange);

  // Category pills
  document.getElementById('categoriesBar').addEventListener('click', e => {
    const pill = e.target.closest('.cat-pill');
    if (!pill) return;
    document.querySelectorAll('.cat-pill').forEach(p => p.classList.remove('active'));
    pill.classList.add('active');
    state.category = pill.dataset.category;
    applyFilters();
    renderProducts();
  });

  // Marketplace filters
  document.getElementById('marketplaceFilters').addEventListener('click', e => {
    const btn = e.target.closest('.mkt-filter');
    if (!btn) return;
    document.querySelectorAll('.mkt-filter').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    state.marketplace = btn.dataset.marketplace;
    applyFilters();
    renderProducts();
  });

  // Reset filters
  document.getElementById('resetFilters').addEventListener('click', resetAll);

  // Load more button (fallback)
  document.getElementById('loadMoreBtn').addEventListener('click', loadNextPage);

  // Infinite scroll via IntersectionObserver
  const sentinel = document.getElementById('scrollSentinel');
  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver(entries => {
      if (entries[0].isIntersecting && !state.loading) {
        loadNextPage();
      }
    }, { rootMargin: '200px' });
    observer.observe(sentinel);
  } else {
    document.getElementById('loadMoreWrapper').style.display = 'block';
  }

  // View toggle
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

  // Theme toggle
  document.getElementById('themeToggle').addEventListener('click', () => {
    document.body.classList.toggle('light-theme');
    const isLight = document.body.classList.contains('light-theme');
    localStorage.setItem('ph_theme', isLight ? 'light' : 'dark');
    showToast(isLight ? '☀️ Tema claro ativado' : '🌙 Tema escuro ativado');
  });

  // Keyboard shortcut: / focuses search
  document.addEventListener('keydown', e => {
    if (e.key === '/' && document.activeElement !== searchInput) {
      e.preventDefault();
      searchInput.focus();
    }
  });
}

/* ══════════════════════════════════════════════════════════
   INIT
   ══════════════════════════════════════════════════════════ */
function init() {
  // Restore saved theme
  if (localStorage.getItem('ph_theme') === 'light') {
    document.body.classList.add('light-theme');
  }

  // Restore favorites count badge
  document.getElementById('navFavCount').textContent = state.favorites.size;

  wireEvents();
  runSearch();
}

document.addEventListener('DOMContentLoaded', init);
