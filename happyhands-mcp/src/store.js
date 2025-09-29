import { readFile, writeFile } from 'node:fs/promises';

const FILE = process.env.DATA_FILE || './nelrock-mcp.data.json';
let state = { 
  appointments: [], 
  messages: [], 
  cases: [],
  storm_intel: [],
  estimates: []
};
let loaded = false;

async function load() {
  if (loaded) return;
  try {
    const txt = await readFile(FILE, 'utf8');
    state = JSON.parse(txt);
  } catch {
    // first run: initialize
    await writeFile(FILE, JSON.stringify(state, null, 2), 'utf8');
  }
  loaded = true;
}

async function save() {
  await writeFile(FILE, JSON.stringify(state, null, 2), 'utf8');
}

export const db = {
  async add(collection, obj) {
    await load();
    state[collection].push(obj);
    await save();
    return obj;
  },
  async update(collection, id, patch) {
    await load();
    const idx = state[collection].findIndex(x => x.id === id);
    if (idx === -1) throw new Error(`Not found: ${collection}/${id}`);
    state[collection][idx] = { ...state[collection][idx], ...patch };
    await save();
    return state[collection][idx];
  },
  async list(collection, pred = () => true) {
    await load();
    return state[collection].filter(pred);
  },
  async get(collection, id) {
    await load();
    return state[collection].find(x => x.id === id) || null;
  }
};
