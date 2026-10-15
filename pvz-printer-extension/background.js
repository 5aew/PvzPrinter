const WS_URL = 'ws://localhost:8765';
let ws = null;

function connect() {
  if (ws && ws.readyState === WebSocket.OPEN) return;

  try {
    ws = new WebSocket(WS_URL);
    ws.onopen = () => console.log('[PvzPrinter] Connected');
    ws.onclose = () => setTimeout(connect, 3000);
    ws.onerror = () => {};
  } catch {
    setTimeout(connect, 3000);
  }
}

chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
  if (msg.type === 'PRINT') {
    const ok = ws && ws.readyState === WebSocket.OPEN;
    if (ok) {
      ws.send(JSON.stringify({ cellCode: msg.cellCode }));
      console.log(`[PvzPrinter] Print: ${msg.cellCode}`);
    } else {
      console.warn('[PvzPrinter] Not connected');
    }
    sendResponse({ ok });
    return false;
  }

  if (msg.type === 'GET_STATUS') {
    sendResponse({ connected: ws && ws.readyState === WebSocket.OPEN });
    return false;
  }
});

connect();