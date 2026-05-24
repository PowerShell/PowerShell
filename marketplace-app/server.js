/**
 * PromoHunt — server.js
 * Backend proxy para busca em marketplaces brasileiros.
 *
 * Execução:
 *   npm install && npm start
 *   (ou: npm run dev  — reinicia ao salvar)
 *
 * Endpoints:
 *   GET /health
 *   GET /api/search?q=<termo>&limit=<n>          — todos os marketplaces
 *   GET /api/search/mercadolivre?q=<t>&limit=<n>
 *   GET /api/search/shopee?q=<t>&limit=<n>
 *   GET /api/search/magalu?q=<t>&limit=<n>
 *   GET /api/search/amazon?q=<t>&limit=<n>
 */

import express  from 'express';
import cors     from 'cors';
import axios    from 'axios';
import * as cheerio from 'cheerio';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname  = dirname(__filename);

const app  = express();
const PORT = 3001;

app.use(cors({ origin: '*' }));
app.use(express.json());

/* Serve os arquivos do frontend (index.html, style.css, script.js)
   para que http://localhost:3001 abra o app direto. */
app.use(express.static(__dirname));

/* ══════════════════════════════════════════════════════════
   SHARED UTILS
   ══════════════════════════════════════════════════════════ */

/** Headers que imitam um browser real para evitar bloqueios */
const BROWSER_HEADERS = {
  'User-Agent'      : 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36',
  'Accept'          : 'text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8',
  'Accept-Language' : 'pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7',
  'Accept-Encoding' : 'gzip, deflate, br',
  'Cache-Control'   : 'no-cache',
  'DNT'             : '1',
  'Connection'      : 'keep-alive',
  'Sec-Fetch-Dest'  : 'document',
  'Sec-Fetch-Mode'  : 'navigate',
  'Sec-Fetch-Site'  : 'none',
  'Upgrade-Insecure-Requests': '1',
};

/** Formata valor em reais com 2 casas */
function brl(value) {
  return Number(value).toLocaleString('pt-BR', { minimumFractionDigits: 2 });
}

/** Infere categoria a partir do nome do produto */
function guessCategory(name = '') {
  const n = name.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
  if (/celular|iphone|galaxy|smartphone|redmi|motorola|poco|zenfone/.test(n)) return 'celulares';
  if (/notebook|laptop|macbook|ultrabook|chromebook|teclado|mouse|ssd|placa de video|processador|memoria ram|gabinete/.test(n)) return 'informatica';
  if (/\btv\b|televisao|smart tv|monitor|projetor|som |caixa de som|headset|fone de ouvido|soundbar/.test(n)) return 'eletronicos';
  if (/game|playstation|xbox|nintendo|joystick|controle|videogame|ps5|ps4|jogo para/.test(n)) return 'games';
  if (/tenis |camisa|calca|vestido|sapato|sandalia|jaqueta|moda|nike|adidas|puma|roupas/.test(n)) return 'moda';
  if (/fogao|geladeira|aspirador|cafeteira|panela|liquidificador|micro-ondas|ventilador|ar.condicionado|robo|vassoura/.test(n)) return 'casa';
  if (/perfume|maquiagem|shampoo|creme|hidratante|protetor solar|batom|secador|babyliss|ghd/.test(n)) return 'beleza';
  if (/bicicleta|musculacao|academia|futebol|corrida|natacao|fitness|esteira|haltere|kettlebell/.test(n)) return 'esportes';
  return 'eletronicos';
}

/** Calcula desconto percentual */
function calcDiscount(priceOld, priceCurrent) {
  if (!priceOld || priceOld <= priceCurrent) return 0;
  return Math.round((1 - priceCurrent / priceOld) * 100);
}

/** Axios instance com timeout padrão */
const http = axios.create({ timeout: 12000 });

/* ══════════════════════════════════════════════════════════
   NOTA SOBRE MERCADO LIVRE
   O ML é chamado diretamente pelo browser (CORS aberto).
   O backend NÃO chama o ML para evitar bloqueio por IP de servidor.
   ══════════════════════════════════════════════════════════ */

/* ══════════════════════════════════════════════════════════
   SHOPEE — Flash Sale (endpoint público) + busca com cookies
   ══════════════════════════════════════════════════════════ */
const SHOPEE_HEADERS = {
  'User-Agent'         : BROWSER_HEADERS['User-Agent'],
  'Accept'             : 'application/json, text/plain, */*',
  'Accept-Language'    : 'pt-BR,pt;q=0.9',
  'Referer'            : 'https://shopee.com.br/',
  'Origin'             : 'https://shopee.com.br',
  'X-API-SOURCE'       : 'pc',
  'X-Shopee-Language'  : 'pt-BR',
  'sec-ch-ua'          : '"Chromium";v="124","Google Chrome";v="124","Not-A.Brand";v="99"',
  'sec-ch-ua-mobile'   : '?0',
  'sec-ch-ua-platform' : '"Windows"',
  'Sec-Fetch-Dest'     : 'empty',
  'Sec-Fetch-Mode'     : 'cors',
  'Sec-Fetch-Site'     : 'same-origin',
};

/* Passo 1: busca sessões de flash sale ativas */
async function getShopeeFlashSession() {
  const { data } = await http.get(
    'https://shopee.com.br/api/v4/flash_sale/get_all_sessions?device_auth=0&need_main_image=1',
    { headers: { ...SHOPEE_HEADERS, 'Referer': 'https://shopee.com.br/flash_sale' } }
  );
  const sessions = data?.data?.sessions || [];
  /* Prefere sessão em andamento (status=1), senão pega a próxima (status=2) */
  return sessions.find(s => s.status === 1)
      || sessions.find(s => s.status === 2)
      || sessions[0]
      || null;
}

/* Passo 2: busca itens da sessão */
async function getShopeeFlashItems(sessionId, limit = 20) {
  const url = `https://shopee.com.br/api/v4/flash_sale/get_flash_sale_item_list`
    + `?need_main_image=1&sort_soldout=1&limit=${limit}&session_id=${sessionId}&offset=0`;
  const { data } = await http.get(url, {
    headers: { ...SHOPEE_HEADERS, 'Referer': 'https://shopee.com.br/flash_sale' }
  });
  return data?.data?.items || [];
}

/* Busca via API de search v4 (requer cookies — melhor esforço) */
async function searchShopeeKeyword(query, limit = 20) {
  const kw  = encodeURIComponent(query);
  const url = `https://shopee.com.br/api/v4/search/search_items?by=relevancy&keyword=${kw}&limit=${limit}&newest=0&order=desc&page_type=search&scenario=PAGE_GLOBAL_SEARCH&version=2`;
  const { data } = await http.get(url, {
    headers: { ...SHOPEE_HEADERS, 'Referer': `https://shopee.com.br/search?keyword=${kw}` }
  });
  if (!data?.items?.length) throw new Error('sem itens');
  return data.items.map(({ item_basic: it }) => normalizeShopeeItem(it)).filter(Boolean);
}

/* Entry point: tenta keyword search; se falhar, usa flash deals */
async function searchShopee(query, limit = 20) {
  /* Tenta busca por keyword */
  try {
    return await searchShopeeKeyword(query, limit);
  } catch (err) {
    const status = err.response?.status;
    if (status !== 403 && status !== 429 && status !== undefined) throw err;
    console.warn(`[shopee] keyword search bloqueado (${status || err.message}) — usando flash deals`);
  }
  /* Fallback: flash deals do momento */
  return getShopeeFlashDeals(limit);
}

/* Retorna os deals atuais do flash sale */
async function getShopeeFlashDeals(limit = 20) {
  const session = await getShopeeFlashSession();
  if (!session?.promotionid) return [];
  const items = await getShopeeFlashItems(session.promotionid, limit);
  return items.map(item => normalizeShopeeFlashItem(item)).filter(Boolean);
}

function normalizeShopeeItem(it) {
  if (!it?.itemid) return null;
  const price    = (it.price || 0) / 100000;
  const priceOld = it.price_before_discount ? it.price_before_discount / 100000 : null;
  const discount = it.raw_discount || calcDiscount(priceOld, price);
  return {
    id           : `sp_${it.itemid}`,
    name         : it.name || '',
    category     : guessCategory(it.name || ''),
    marketplace  : 'shopee',
    image        : `https://cf.shopee.com.br/file/${it.image}`,
    priceOld,
    priceCurrent : price,
    pixPrice     : null,
    discount,
    installments : null,
    rating       : Number((it.item_rating?.rating_star || 4.5).toFixed(1)),
    reviews      : it.sold || 0,
    badges       : [
      ...(discount >= 30 ? ['hot'] : []),
      ...(it.is_official_shop ? ['new'] : []),
    ],
    url     : `https://shopee.com.br/product/${it.shopid}/${it.itemid}`,
    addedAt : new Date().toISOString(),
  };
}

function normalizeShopeeFlashItem(raw) {
  /* Flash sale items têm estrutura diferente: item_basic está no nível raiz */
  const it = raw?.item_basic || raw;
  if (!it?.itemid) return null;
  const price    = (it.price || 0) / 100000;
  const priceOld = it.price_before_discount ? it.price_before_discount / 100000 : null;
  const discount = it.raw_discount || calcDiscount(priceOld, price);
  return {
    id           : `sp_${it.itemid}`,
    name         : it.name || '',
    category     : guessCategory(it.name || ''),
    marketplace  : 'shopee',
    image        : `https://cf.shopee.com.br/file/${it.image}`,
    priceOld,
    priceCurrent : price,
    pixPrice     : null,
    discount,
    installments : null,
    rating       : Number((it.item_rating?.rating_star || 4.5).toFixed(1)),
    reviews      : it.sold || 0,
    badges       : ['hot', ...(discount >= 20 ? [] : [])],
    url          : `https://shopee.com.br/product/${it.shopid}/${it.itemid}`,
    addedAt      : new Date().toISOString(),
  };
}

/* ══════════════════════════════════════════════════════════
   MAGAZINE LUIZA — scraping com Cheerio
   Tenta o JSON embutido pelo Next.js; fallback para seletores HTML.
   ══════════════════════════════════════════════════════════ */
async function searchMagalu(query, limit = 20) {
  const slug = query.trim().replace(/\s+/g, '%20');
  const url  = `https://www.magazineluiza.com.br/busca/${encodeURIComponent(query)}/`;

  const { data: html } = await http.get(url, {
    headers: {
      ...BROWSER_HEADERS,
      'Referer': 'https://www.magazineluiza.com.br/',
    },
  });

  const $   = cheerio.load(html);
  let items = [];

  /* Tentativa 1: JSON embutido pelo Next.js */
  const nextData = $('script#__NEXT_DATA__').html();
  if (nextData) {
    try {
      const json = JSON.parse(nextData);
      const raw  = json?.props?.pageProps?.search?.products
                || json?.props?.pageProps?.products
                || [];
      items = raw.slice(0, limit).map(p => {
        const price    = p.price || p.best_price || 0;
        const priceOld = p.original_price || null;
        const discount = calcDiscount(priceOld, price);
        return {
          id           : `mg_${p.id || p.sku || Math.random()}`,
          name         : p.title || p.name || '',
          category     : guessCategory(p.title || p.name || ''),
          marketplace  : 'magalu',
          image        : p.image || p.thumbnail || '',
          priceOld,
          priceCurrent : price,
          pixPrice     : p.pix_price || null,
          discount,
          installments : p.installment
            ? `${p.installment.count}x R$ ${brl(p.installment.value)}`
            : null,
          rating   : p.rating?.score || 4.5,
          reviews  : p.rating?.count || 0,
          badges   : [
            ...(p.free_shipping   ? ['frete'] : []),
            ...(discount >= 30    ? ['hot']   : []),
          ],
          url      : `https://www.magazineluiza.com.br${p.slug || p.url || ''}`,
          addedAt  : new Date().toISOString(),
        };
      });
    } catch { /* prossegue para tentativa 2 */ }
  }

  /* Tentativa 2: seletores HTML de produto */
  if (!items.length) {
    $('[data-testid="product-card-container"], li[class*="productCard"], [class*="sc-fzoiQi"]').each((_, el) => {
      if (items.length >= limit) return false;
      try {
        const name = $(el).find('[data-testid="product-title"], h2, h3').first().text().trim();
        const rawP = $(el).find('[data-testid="price-value"], [class*="price"]').first().text()
          .replace(/[^\d,]/g, '').replace(',', '.');
        const price = parseFloat(rawP) || 0;
        const img   = $(el).find('img').first().attr('src') || '';
        const href  = $(el).find('a').first().attr('href') || '';
        if (name && price > 0) {
          items.push({
            id           : `mg_${Date.now()}_${items.length}`,
            name,
            category     : guessCategory(name),
            marketplace  : 'magalu',
            image        : img,
            priceOld     : null,
            priceCurrent : price,
            pixPrice     : null,
            discount     : 0,
            installments : null,
            rating   : 4.5,
            reviews  : 0,
            badges   : [],
            url      : href.startsWith('http') ? href : `https://www.magazineluiza.com.br${href}`,
            addedAt  : new Date().toISOString(),
          });
        }
      } catch { /* ignora card com parsing falho */ }
    });
  }

  return items;
}

/* ══════════════════════════════════════════════════════════
   AMAZON BRASIL — scraping com Cheerio
   Nota: Amazon usa CloudFront. Pode exigir proxy rotativo
   ou resolução de desafio JS para requests frequentes.
   ══════════════════════════════════════════════════════════ */
async function searchAmazon(query, limit = 20) {
  const url = `https://www.amazon.com.br/s?k=${encodeURIComponent(query)}&ref=sr_pg_1`;

  const { data: html } = await http.get(url, {
    headers: {
      ...BROWSER_HEADERS,
      'Referer'             : 'https://www.amazon.com.br/',
      'sec-ch-ua'           : '"Chromium";v="124","Google Chrome";v="124","Not-A.Brand";v="99"',
      'sec-ch-ua-mobile'    : '?0',
      'sec-ch-ua-platform'  : '"Windows"',
      'Sec-Fetch-Dest'      : 'document',
      'Sec-Fetch-Mode'      : 'navigate',
      'Sec-Fetch-Site'      : 'same-origin',
    },
  });

  const $    = cheerio.load(html);
  const items = [];

  $('[data-component-type="s-search-result"]').each((_, el) => {
    if (items.length >= limit) return false;
    try {
      const name  = $(el).find('h2 span').first().text().trim();
      const asin  = $(el).attr('data-asin') || '';
      const img   = $(el).find('.s-image').attr('src') || '';
      const href  = $(el).find('h2 a').attr('href') || '';

      /* Preço atual */
      const whole = $(el).find('.a-price-whole').first().text().replace(/[^\d]/g, '');
      const frac  = $(el).find('.a-price-fraction').first().text().replace(/[^\d]/g, '') || '00';
      const price = whole ? parseFloat(`${whole}.${frac}`) : 0;

      /* Preço original (tachado) */
      const oldRaw = $(el).find('.a-text-strike').first().text()
        .replace(/[^\d,]/g, '').replace(',', '.');
      const priceOld = oldRaw ? parseFloat(oldRaw) : null;
      const discount = calcDiscount(priceOld, price);

      /* Avaliação */
      const starsText = $(el).find('.a-icon-alt').first().text();
      const rating = parseFloat(starsText) || 4.5;
      const reviewText = $(el).find('[aria-label*="avaliações"], [aria-label*="estrelas"]')
        .parent().next('span').text().replace(/[^\d]/g, '');
      const reviews = parseInt(reviewText) || 0;

      /* Frete grátis */
      const hasFreeShip = $(el).find('[aria-label*="FRETE GRÁTIS"], .a-color-success').length > 0;

      if (name && price > 0 && asin) {
        items.push({
          id           : `az_${asin}`,
          name,
          category     : guessCategory(name),
          marketplace  : 'amazon',
          image        : img,
          priceOld,
          priceCurrent : price,
          pixPrice     : null,
          discount,
          installments : null,
          rating,
          reviews,
          badges       : [
            ...(hasFreeShip    ? ['frete'] : []),
            ...(discount >= 30 ? ['hot']   : []),
          ],
          url     : href.startsWith('http') ? href : `https://www.amazon.com.br${href}`,
          addedAt : new Date().toISOString(),
        });
      }
    } catch { /* ignora produto com parsing falho */ }
  });

  return items;
}

/* ══════════════════════════════════════════════════════════
   ROUTES
   ══════════════════════════════════════════════════════════ */
app.get('/health', (_, res) => res.json({ ok: true, ts: Date.now() }));

/* ── Feed de deals do backend: Shopee flash + Magalu + Amazon ── */
/* ML NÃO está aqui — é buscado direto do browser no frontend.   */
app.get('/api/deals', async (req, res) => {
  try {
    /* Shopee flash deals + Amazon + Magalu em paralelo */
    const [shopeeItems, amazonItems, magaluItems] = await Promise.all([
      getShopeeFlashDeals(20).catch(e => { console.warn('[shopee flash]', e.message); return []; }),
      searchAmazon('ofertas do dia', 10).catch(e => { console.warn('[amazon deals]', e.message); return []; }),
      searchMagalu('promoção', 10).catch(e => { console.warn('[magalu deals]', e.message); return []; }),
    ]);

    const products = [...shopeeItems, ...amazonItems, ...magaluItems]
      .filter(Boolean)
      .sort((a, b) => b.discount - a.discount);

    res.json({ ok: true, count: products.length, products });
  } catch (err) {
    console.error('[/api/deals]', err.message);
    res.json({ ok: true, count: 0, products: [] }); /* nunca retorna erro pro browser */
  }
});

/* Rota individual por marketplace — ML removido (browser chama direto) */
const scrapers = {
  shopee : searchShopee,
  magalu : searchMagalu,
  amazon : searchAmazon,
};

for (const [name, fn] of Object.entries(scrapers)) {
  app.get(`/api/search/${name}`, async (req, res) => {
    const q     = (req.query.q || '').trim();
    const limit = Math.min(parseInt(req.query.limit) || 20, 50);
    if (!q) return res.status(400).json({ ok: false, error: 'Parâmetro q obrigatório' });
    try {
      const products = await fn(q, limit);
      res.json({ ok: true, marketplace: name, count: products.length, products });
    } catch (err) {
      /* 403/429 = site bloqueou o scraper — retorna 200 com array vazio
         para não aparecer como erro vermelho no F12 do browser. */
      const status = err.response?.status;
      if (status === 403 || status === 429) {
        console.warn(`[${name}] Bloqueado pelo site (HTTP ${status}) — retornando vazio`);
        return res.json({ ok: true, marketplace: name, count: 0, products: [],
          warning: `${name} temporariamente indisponível (${status})` });
      }
      console.error(`[${name}]`, err.message);
      res.status(502).json({ ok: false, marketplace: name, error: err.message });
    }
  });
}

/* Rota combinada — Shopee + Magalu + Amazon (ML é chamado direto pelo browser) */
app.get('/api/search', async (req, res) => {
  const q     = (req.query.q || '').trim();
  const limit = Math.min(parseInt(req.query.limit) || 20, 50);
  if (!q) return res.status(400).json({ ok: false, error: 'Parâmetro q obrigatório' });

  const results = await Promise.allSettled(
    Object.entries(scrapers).map(([, fn]) => fn(q, limit))
  );

  const products = [];
  const errors   = {};

  Object.keys(scrapers).forEach((name, i) => {
    const r = results[i];
    if (r.status === 'fulfilled') products.push(...(r.value || []));
    else { errors[name] = r.reason?.message || 'Erro desconhecido'; }
  });

  res.json({ ok: true, count: products.length, products, errors });
});

/* ══════════════════════════════════════════════════════════
   START
   ══════════════════════════════════════════════════════════ */
app.listen(PORT, () => {
  console.log(`\n🔥 PromoHunt rodando em http://localhost:${PORT}`);
  console.log(`   Abra http://localhost:${PORT} no navegador para usar o app.`);
  console.log('\n   API endpoints:');
  console.log(`   GET /health`);
  console.log(`   GET /api/search?q=samsung`);
  console.log(`   GET /api/search/mercadolivre?q=samsung`);
  console.log(`   GET /api/search/shopee?q=samsung`);
  console.log(`   GET /api/search/magalu?q=samsung`);
  console.log(`   GET /api/search/amazon?q=samsung\n`);
});
