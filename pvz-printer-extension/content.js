// Content Script для Ozon PVZ — автопечать ячеек при сканировании
console.log('[PvzPrinter] Content script loaded on:', window.location.href);

const CELL_REGEX = /\b(\d{1,4}-\d{1,3})\b/;
const PRINTED_ATTR = 'data-pvz-printed';
const printedCells = new Set();

function checkForNewCells() {
  const elements = document.querySelectorAll('span, div, td, p, a');

  for (const el of elements) {
    if (el.hasAttribute(PRINTED_ATTR)) continue;

    const text = el.textContent?.trim();
    if (!text || text.length > 20) continue;

    const match = text.match(CELL_REGEX);
    if (!match) continue;

    const cellCode = match[1];

    // Защита от дублей в рамках сессии
    if (printedCells.has(cellCode)) {
      el.setAttribute(PRINTED_ATTR, 'true');
      continue;
    }

    el.setAttribute(PRINTED_ATTR, 'true');
    printedCells.add(cellCode);

    console.log(`[PvzPrinter] Auto-print: ${cellCode}`);
    chrome.runtime.sendMessage({ type: 'PRINT', cellCode }, (response) => {
      if (response?.ok) {
        console.log(`[PvzPrinter] OK: ${cellCode}`);
      } else {
        console.warn(`[PvzPrinter] FAILED: ${cellCode}`);
      }
    });
  }
}

// Первый проход + наблюдатель за SPA-обновлениями
checkForNewCells();

const observer = new MutationObserver(() => checkForNewCells());
observer.observe(document.body, { childList: true, subtree: true });