'use strict';

const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const path = require('path');

const app = express();

const PORT        = process.env.PORT        || 3000;
const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5000';

// ── Proxy all /api/* calls to the backend ───────────────────────────────────
// Mount at root with pathFilter so Express does NOT strip the /api prefix.
// If we used app.use('/api', proxy), Express would strip it → backend gets /collections → 404.
app.use(
  createProxyMiddleware({
    target: BACKEND_URL,
    changeOrigin: true,
    pathFilter: '/api/**',
    on: {
      error: (err, req, res) => {
        console.error('[proxy] error:', err.message);
        res.status(502).json({ status: 'error', errors: `Backend unavailable: ${err.message}` });
      },
    },
  })
);

// ── Serve static files ──────────────────────────────────────────────────────
app.use(express.static(path.join(__dirname, 'public')));

// ── SPA fallback ────────────────────────────────────────────────────────────
app.get('*', (_req, res) => {
  res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

// ── Start ────────────────────────────────────────────────────────────────────
app.listen(PORT, () => {
  console.log(`╔═══════════════════════════════════════════╗`);
  console.log(`║  RAG Client Frontend                      ║`);
  console.log(`║  http://localhost:${PORT}                     ║`);
  console.log(`║  /api/* → ${BACKEND_URL.padEnd(31)}║`);
  console.log(`╚═══════════════════════════════════════════╝`);
});
