'use strict';

// ===== STATE =====
const state = {
  apiKey: '',
  chatHistory: [],  // [{role:'user'|'assistant', content:'...'}]
};

// ===== UTILS =====
function getApiKey() {
  return state.apiKey || localStorage.getItem('ragApiKey') || '';
}

function setApiKey(key) {
  state.apiKey = key.trim();
  localStorage.setItem('ragApiKey', state.apiKey);
}

// HTTP headers accept only printable ASCII (0x20–0x7E).
// Strip anything outside that range so fetch() never throws.
function sanitizeHeaderValue(str) {
  return str.replace(/[^\x20-\x7E]/g, '');
}

function apiHeaders() {
  const key = sanitizeHeaderValue(getApiKey());
  const h = { 'Content-Type': 'application/json' };
  if (key) h['X-Api-Key'] = key;
  return h;
}

async function apiFetch(path, options = {}) {
  options.headers = { ...apiHeaders(), ...(options.headers || {}) };
  const res = await fetch(path, options);
  const text = await res.text();
  if (!text.trim()) {
    throw new Error(`Пустой ответ от сервера (HTTP ${res.status})`);
  }
  try {
    return JSON.parse(text);
  } catch {
    throw new Error(`Не JSON-ответ (HTTP ${res.status}): ${text.slice(0, 120)}`);
  }
}

function showLoading(visible) {
  document.getElementById('loadingOverlay').classList.toggle('visible', visible);
}

function formatBytes(bytes) {
  if (!bytes) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return (bytes / Math.pow(1024, i)).toFixed(1) + ' ' + units[i];
}

function formatDate(ms) {
  if (!ms) return '—';
  return new Date(ms).toLocaleString('ru-RU', { dateStyle: 'short', timeStyle: 'short' });
}

// ===== SIMPLE MARKDOWN RENDERER =====
function renderMarkdown(text) {
  if (!text) return '';
  // Escape HTML first
  let html = text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');

  // Code blocks (```)
  html = html.replace(/```(\w+)?\n?([\s\S]*?)```/g, (_, lang, code) =>
    `<pre><code>${code.trim()}</code></pre>`
  );

  // Inline code
  html = html.replace(/`([^`]+)`/g, '<code>$1</code>');

  // Headers
  html = html.replace(/^### (.+)$/gm, '<h3>$1</h3>');
  html = html.replace(/^## (.+)$/gm, '<h2>$1</h2>');
  html = html.replace(/^# (.+)$/gm, '<h1>$1</h1>');

  // Bold / italic
  html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
  html = html.replace(/\*([^*]+)\*/g, '<em>$1</em>');
  html = html.replace(/__([^_]+)__/g, '<strong>$1</strong>');
  html = html.replace(/_([^_]+)_/g, '<em>$1</em>');

  // Blockquotes
  html = html.replace(/^> (.+)$/gm, '<blockquote>$1</blockquote>');

  // Unordered lists (convert groups to <ul>)
  html = html.replace(/(^[*\-] .+$(\n[*\-] .+$)*)/gm, match => {
    const items = match.split('\n').map(l => `<li>${l.replace(/^[*\-] /, '')}</li>`).join('');
    return `<ul>${items}</ul>`;
  });

  // Ordered lists
  html = html.replace(/(^\d+\. .+$(\n\d+\. .+$)*)/gm, match => {
    const items = match.split('\n').map(l => `<li>${l.replace(/^\d+\. /, '')}</li>`).join('');
    return `<ol>${items}</ol>`;
  });

  // Paragraphs: split by double newlines, wrap non-block-elements in <p>
  const blockTags = /^<(h[1-6]|pre|ul|ol|blockquote)/;
  html = html
    .split(/\n{2,}/)
    .map(block => {
      block = block.trim();
      if (!block) return '';
      if (blockTags.test(block)) return block;
      return `<p>${block.replace(/\n/g, '<br>')}</p>`;
    })
    .join('\n');

  return html;
}

// ===== API KEY MANAGEMENT =====
function initApiKey() {
  const input = document.getElementById('apiKeyInput');
  const status = document.getElementById('apiKeyStatus');
  const saved = localStorage.getItem('ragApiKey') || '';

  if (saved) {
    input.value = saved;
    state.apiKey = saved;
    status.textContent = 'Сохранён';
  }

  document.getElementById('saveApiKey').addEventListener('click', () => {
    const raw = input.value.trim();

    // Bearer tokens must be printable ASCII only
    if (raw && /[^\x20-\x7E]/.test(raw)) {
      status.textContent = 'Ошибка: ключ содержит недопустимые символы (только ASCII)';
      status.style.color = 'var(--danger)';
      setTimeout(() => { status.textContent = ''; status.style.color = ''; }, 4000);
      return;
    }

    setApiKey(raw);
    status.textContent = raw ? 'Сохранён' : 'Очищен';
    status.style.color = '';
    setTimeout(() => status.textContent = '', 2500);
  });

  document.getElementById('toggleApiKey').addEventListener('click', () => {
    const isPass = input.type === 'password';
    input.type = isPass ? 'text' : 'password';
    const icon = document.getElementById('eyeIcon');
    icon.innerHTML = isPass
      ? `<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/>
         <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
         <line x1="1" y1="1" x2="23" y2="23"/>`
      : `<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
         <circle cx="12" cy="12" r="3"/>`;
  });

  input.addEventListener('keydown', e => {
    if (e.key === 'Enter') document.getElementById('saveApiKey').click();
  });
}

// ===== TABS =====
function initTabs() {
  document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const tabId = btn.dataset.tab;
      document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
      document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
      btn.classList.add('active');
      document.getElementById('tab-' + tabId).classList.add('active');
    });
  });
}

// ===== CHAT =====
function getChatSettings() {
  const key = document.getElementById('chatCollectionCustom').value.trim()
    || document.getElementById('chatCollection').value;

  const maxDocs = parseInt(document.getElementById('chatMaxDocs').value) || undefined;
  const maxTokens = parseInt(document.getElementById('chatMaxTokens').value) || undefined;
  const minSim = parseFloat(document.getElementById('chatMinSim').value);
  const temp = parseFloat(document.getElementById('chatTemp').value);
  const agentRole = document.getElementById('chatAgentRole').value.trim() || undefined;

  return {
    collectionKey: key,
    agentRole,
    maxDocuments: maxDocs > 0 ? maxDocs : undefined,
    maxTokens: maxTokens > 0 ? maxTokens : undefined,
    minSimilarity: minSim > 0 ? minSim : undefined,
    temperature: temp > 0 ? temp : undefined,
  };
}

function appendMessage(role, content) {
  const container = document.getElementById('chatMessages');

  // Remove welcome message on first real message
  const welcome = container.querySelector('.chat-welcome');
  if (welcome) welcome.remove();

  const isUser = role === 'user';
  const div = document.createElement('div');
  div.className = `chat-message ${isUser ? 'user' : 'assistant'}`;

  const avatar = document.createElement('div');
  avatar.className = 'message-avatar';
  avatar.textContent = isUser ? 'Вы' : 'AI';

  const bubble = document.createElement('div');
  bubble.className = 'message-bubble';

  if (isUser) {
    bubble.textContent = content;
  } else {
    bubble.innerHTML = renderMarkdown(content);
  }

  const meta = document.createElement('div');
  meta.className = 'message-meta';
  meta.textContent = new Date().toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });

  const inner = document.createElement('div');
  inner.style.display = 'flex';
  inner.style.flexDirection = 'column';
  inner.appendChild(bubble);
  inner.appendChild(meta);

  div.appendChild(avatar);
  div.appendChild(inner);
  container.appendChild(div);
  container.scrollTop = container.scrollHeight;

  return div;
}

function appendThinking() {
  const container = document.getElementById('chatMessages');
  const div = document.createElement('div');
  div.className = 'chat-message assistant';
  div.id = 'thinking-msg';

  const avatar = document.createElement('div');
  avatar.className = 'message-avatar';
  avatar.textContent = 'AI';

  const bubble = document.createElement('div');
  bubble.className = 'message-bubble thinking-bubble';
  bubble.innerHTML = '<span></span><span></span><span></span>';

  div.appendChild(avatar);
  div.appendChild(bubble);
  container.appendChild(div);
  container.scrollTop = container.scrollHeight;
  return div;
}

function removeThinking() {
  document.getElementById('thinking-msg')?.remove();
}

async function sendChatMessage() {
  const input = document.getElementById('chatInput');
  const text = input.value.trim();
  if (!text) return;

  const settings = getChatSettings();
  if (!settings.collectionKey) {
    alert('Укажите ключ коллекции в панели слева');
    return;
  }

  input.value = '';
  input.style.height = '';

  // Add user message to UI and history
  appendMessage('user', text);
  state.chatHistory.push({ role: 'user', content: text });

  // Build context_messages from history (all except the last user message)
  const ctxSize = parseInt(document.getElementById('chatCtxSize').value) || 6;
  const ctxMessages = state.chatHistory
    .slice(0, -1)           // exclude the just-added user msg
    .slice(-ctxSize);       // keep last N messages

  appendThinking();
  document.getElementById('sendChat').disabled = true;

  try {
    // Build body with snake_case keys to match backend JSON policy
    const body = {
      collection_key:   settings.collectionKey,
      message:          text,
      agent_role:       settings.agentRole,
      max_documents:    settings.maxDocuments,
      max_tokens:       settings.maxTokens,
      min_similarity:   settings.minSimilarity,
      temperature:      settings.temperature,
      context_messages: ctxMessages.length > 0 ? ctxMessages : undefined,
    };

    const data = await apiFetch('/api/rag/search', {
      method: 'POST',
      body: JSON.stringify(body),
    });

    removeThinking();

    if (data.status === 'ok') {
      const answer = data.data || '(пустой ответ)';
      appendMessage('assistant', answer);
      state.chatHistory.push({ role: 'assistant', content: answer });
    } else {
      const errMsg = data.errors || 'Неизвестная ошибка';
      appendMessage('assistant', `**Ошибка:** ${errMsg}`);
    }
  } catch (err) {
    removeThinking();
    appendMessage('assistant', `**Ошибка сети:** ${err.message}`);
  } finally {
    document.getElementById('sendChat').disabled = false;
    document.getElementById('chatInput').focus();
  }
}

function initChat() {
  const input = document.getElementById('chatInput');
  const sendBtn = document.getElementById('sendChat');

  sendBtn.addEventListener('click', sendChatMessage);

  input.addEventListener('keydown', e => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendChatMessage();
    }
  });

  // Auto-resize textarea
  input.addEventListener('input', () => {
    input.style.height = '';
    input.style.height = Math.min(input.scrollHeight, 200) + 'px';
  });

  document.getElementById('clearChat').addEventListener('click', () => {
    state.chatHistory = [];
    const container = document.getElementById('chatMessages');
    container.innerHTML = `
      <div class="chat-welcome">
        <div class="welcome-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M12 2L2 7l10 5 10-5-10-5z"/>
            <path d="M2 17l10 5 10-5"/>
            <path d="M2 12l10 5 10-5"/>
          </svg>
        </div>
        <p>Задайте вопрос по выбранной коллекции</p>
        <p class="welcome-hint">Укажите ключ коллекции в панели слева и введите вопрос ниже</p>
      </div>`;
  });

  // Slider value display
  const minSimSlider = document.getElementById('chatMinSim');
  const minSimVal = document.getElementById('chatMinSimVal');
  minSimSlider.addEventListener('input', () => {
    minSimVal.textContent = parseFloat(minSimSlider.value).toFixed(2);
  });

  const tempSlider = document.getElementById('chatTemp');
  const tempVal = document.getElementById('chatTempVal');
  tempSlider.addEventListener('input', () => {
    tempVal.textContent = parseFloat(tempSlider.value).toFixed(2);
  });

  const reloadBtn = document.getElementById('loadCollectionsForChat');
  reloadBtn.addEventListener('click', async () => {
    reloadBtn.classList.add('spinning');
    reloadBtn.disabled = true;
    await loadCollectionsIntoSelect('chatCollection');
    reloadBtn.classList.remove('spinning');
    reloadBtn.disabled = false;
  });
}

async function loadCollectionsIntoSelect(selectId) {
  try {
    const data = await apiFetch('/api/collections?limit=200');
    const select = document.getElementById(selectId);
    select.innerHTML = '<option value="">— выберите или введите —</option>';
    if (data.status === 'ok' && Array.isArray(data.data)) {
      data.data.forEach(col => {
        const opt = document.createElement('option');
        opt.value = col.key;
        opt.textContent = `${col.name} (${col.key})`;
        select.appendChild(opt);
      });
    }
  } catch (err) {
    console.error('Failed to load collections:', err);
  }
}

// ===== COLLECTIONS =====
function initCollections() {
  document.getElementById('loadCollections').addEventListener('click', loadCollections);
}

async function loadCollections() {
  const name = document.getElementById('colFilterName').value.trim();
  const key = document.getElementById('colFilterKey').value.trim();
  const orderBy = document.getElementById('colOrderBy').value;
  const orderDir = document.getElementById('colOrderDir').value;
  const page = document.getElementById('colPage').value;
  const limit = document.getElementById('colLimit').value;

  const params = new URLSearchParams();
  if (name) params.set('name', name);
  if (key) params.set('key', key);
  if (orderBy) params.set('order_by', orderBy);
  if (orderDir) params.set('order_dir', orderDir);
  if (page) params.set('page', page);
  if (limit) params.set('limit', limit);

  const container = document.getElementById('collectionsContainer');
  container.innerHTML = '<div class="empty-state">Загрузка...</div>';

  try {
    const data = await apiFetch('/api/collections?' + params.toString());

    if (data.status !== 'ok') {
      container.innerHTML = `<div class="error-banner">${data.errors || 'Ошибка загрузки коллекций'}</div>`;
      return;
    }

    const collections = data.data || [];

    if (collections.length === 0) {
      container.innerHTML = '<div class="empty-state">Коллекции не найдены</div>';
      return;
    }

    const table = document.createElement('table');
    table.className = 'collections-table';
    table.innerHTML = `
      <thead>
        <tr>
          <th>ID</th>
          <th>Ключ</th>
          <th>Название</th>
          <th>Описание</th>
          <th class="num-cell">Документов</th>
          <th class="num-cell">Чанков</th>
          <th class="num-cell">Размер</th>
          <th>Гибр.</th>
          <th>Реранк.</th>
          <th>Q.Exp.</th>
          <th>Создана</th>
        </tr>
      </thead>`;

    const tbody = document.createElement('tbody');
    collections.forEach(col => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${col.id}</td>
        <td><span class="col-key">${escHtml(col.key)}</span></td>
        <td title="${escHtml(col.name || '')}">${escHtml(col.name || '—')}</td>
        <td title="${escHtml(col.description || '')}">${escHtml(col.description || '—')}</td>
        <td class="num-cell">${(col.documents_count ?? '—').toLocaleString()}</td>
        <td class="num-cell">${(col.chunks_count ?? '—').toLocaleString()}</td>
        <td class="num-cell">${formatBytes(col.size)}</td>
        <td><span class="col-badge ${col.hybrid_search_enabled ? '' : 'off'}">${col.hybrid_search_enabled ? 'Да' : 'Нет'}</span></td>
        <td><span class="col-badge ${col.reranking_enabled ? '' : 'off'}">${col.reranking_enabled ? 'Да' : 'Нет'}</span></td>
        <td><span class="col-badge ${col.query_expansion_enabled ? '' : 'off'}">${col.query_expansion_enabled ? 'Да' : 'Нет'}</span></td>
        <td>${formatDate(col.created_at)}</td>`;
      tbody.appendChild(tr);
    });

    table.appendChild(tbody);
    container.innerHTML = '';
    container.appendChild(table);
  } catch (err) {
    container.innerHTML = `<div class="error-banner">Ошибка сети: ${escHtml(err.message)}</div>`;
  }
}

// ===== RAW SEARCH =====
function initRawSearch() {
  document.getElementById('runRawSearch').addEventListener('click', runRawSearch);
  document.getElementById('rawMessage').addEventListener('keydown', e => {
    if (e.key === 'Enter' && e.ctrlKey) runRawSearch();
  });
}

async function runRawSearch() {
  const collectionKey = document.getElementById('rawCollection').value.trim();
  const message = document.getElementById('rawMessage').value.trim();
  const maxDocs = parseInt(document.getElementById('rawMaxDocs').value) || undefined;
  const minSim = parseFloat(document.getElementById('rawMinSim').value);

  if (!collectionKey) { alert('Укажите ключ коллекции'); return; }
  if (!message) { alert('Введите поисковый запрос'); return; }

  const resultsDiv = document.getElementById('rawResults');
  resultsDiv.innerHTML = '<div class="empty-state">Выполняется поиск...</div>';

  try {
    const body = {
      collection_key: collectionKey,
      message,
      max_documents: maxDocs,
      min_similarity: minSim > 0 ? minSim : undefined,
    };

    const data = await apiFetch('/api/rag/raw-search', {
      method: 'POST',
      body: JSON.stringify(body),
    });

    if (data.status !== 'ok') {
      resultsDiv.innerHTML = `<div class="error-banner">${escHtml(data.errors || 'Ошибка поиска')}</div>`;
      return;
    }

    const chunks = data.data || [];

    if (chunks.length === 0) {
      resultsDiv.innerHTML = '<div class="empty-state">Чанки не найдены по данному запросу</div>';
      return;
    }

    resultsDiv.innerHTML = '';
    chunks.forEach((chunk, idx) => {
      const simPct = Math.round((chunk.similarity || 0) * 100);
      const card = document.createElement('div');
      card.className = 'chunk-card';
      card.innerHTML = `
        <div class="chunk-header">
          <div class="chunk-meta">
            <span class="chunk-index">#${idx + 1}</span>
            ${chunk.file_name
              ? `<span class="chunk-file" title="${escHtml(chunk.file_name)}">📄 ${escHtml(chunk.file_name)}</span>`
              : ''}
            <span class="chunk-file">ID: ${chunk.id} · Чанк ${chunk.index ?? '?'}</span>
          </div>
          <div class="chunk-similarity">
            <div class="similarity-bar">
              <div class="similarity-fill" style="width:${simPct}%"></div>
            </div>
            <span class="similarity-pct">${simPct}%</span>
          </div>
        </div>
        <div class="chunk-content">${escHtml(chunk.content || '')}</div>`;
      resultsDiv.appendChild(card);
    });
  } catch (err) {
    resultsDiv.innerHTML = `<div class="error-banner">Ошибка сети: ${escHtml(err.message)}</div>`;
  }
}

// ===== HELPERS =====
function escHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

// ===== THEME =====
function initTheme() {
  const DARK = 'dark';
  const html  = document.documentElement;
  const btn   = document.getElementById('themeToggle');
  const icon  = document.getElementById('themeIcon');

  const SUN_PATH  = '<circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>';
  const MOON_PATH = '<path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>';

  function applyTheme(dark) {
    html.setAttribute('data-theme', dark ? DARK : 'light');
    icon.innerHTML = dark ? SUN_PATH : MOON_PATH;
    btn.title = dark ? 'Светлая тема' : 'Тёмная тема';
  }

  const saved = localStorage.getItem('ragTheme') === DARK;
  applyTheme(saved);

  btn.addEventListener('click', () => {
    const isDark = html.getAttribute('data-theme') === DARK;
    localStorage.setItem('ragTheme', isDark ? 'light' : DARK);
    applyTheme(!isDark);
  });
}

// ===== INIT =====
document.addEventListener('DOMContentLoaded', () => {
  initApiKey();
  initTheme();
  initTabs();
  initChat();
  initCollections();
  initRawSearch();
});
