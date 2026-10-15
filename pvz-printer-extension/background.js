const WS_URL = 'ws://localhost:8765';
let ws = null;
let reconnectTimer = null;

function connect() {
  if (ws && ws.readyState === WebSocket.OPEN) return;

  try {
    ws = new WebSocket(WS_URL);

    ws.onopen = () => {
      console.log('[PvzPrinter] Connected to local server');
      chrome.storage.local.set({ connected: true });
    };

    ws.onclose = () => {
      console.log('[PvzPrinter] Disconnected, reconnecting in 3s...');
      chrome.storage.local.set({ connected: false });
      reconnectTimer = setTimeout(connect, 3000);
    };

    ws.onerror = (err) => {
      console.error('[PvzPrinter] WebSocket error:', err);
    };

    ws.onmessage = (event) => {
      console.log('[PvzPrinter] Received:', event.data);
    };
  } catch (e) {
    console.error('[PvzPrinter] Connection failed:', e);
    reconnectTimer = setTimeout(connect, 3000);
  }
}

// Отправка команды печати
function printLabel(cellCode) {
  if (!ws || ws.readyState !== WebSocket.OPEN) {
    console.warn('[PvzPrinter] Not connected, cannot print');
    return Promise.resolve(false);
  }

  const message = JSON.stringify({ cellCode });
  ws.send(message);
  console.log(`[PvzPrinter] Sent print command: ${cellCode}`);
  return Promise.resolve(true);
}

// Слушаем сообщения от popup и content script
chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.type === 'PRINT') {
    printLabel(msg.cellCode).then(ok => sendResponse({ ok }));
    return true; // async response
  }

  if (msg.type === 'GET_STATUS') {
    const connected = ws && ws.readyState === WebSocket.OPEN;
    sendResponse({ connected });
    return false;
  }
});

// Подключаемся при старте
connect();