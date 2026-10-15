const statusDot = document.getElementById('statusDot');
const statusText = document.getElementById('statusText');
const manualSection = document.getElementById('manualSection');
const infoSection = document.getElementById('infoSection');
const cellInput = document.getElementById('cellCode');
const printBtn = document.getElementById('printBtn');

async function updateStatus() {
  try {
    const response = await chrome.runtime.sendMessage({ type: 'GET_STATUS' });
    if (response?.connected) {
      statusDot.className = 'dot green';
      statusText.textContent = 'Подключено к PvzPrinter';
      manualSection.classList.remove('hidden');
      infoSection.classList.add('hidden');
    } else {
      statusDot.className = 'dot red';
      statusText.textContent = 'PvzPrinter не запущен';
      manualSection.classList.add('hidden');
      infoSection.classList.remove('hidden');
    }
  } catch {
    statusDot.className = 'dot red';
    statusText.textContent = 'Ошибка соединения';
    manualSection.classList.add('hidden');
    infoSection.classList.remove('hidden');
  }
}

// Печать
printBtn.addEventListener('click', async () => {
  const cellCode = cellInput.value.trim();
  if (!cellCode) return;

  printBtn.disabled = true;
  printBtn.textContent = 'Отправка...';

  const response = await chrome.runtime.sendMessage({ type: 'PRINT', cellCode });

  if (response?.ok) {
    printBtn.textContent = '✅ Отправлено!';
    cellInput.value = '';
    setTimeout(() => { printBtn.textContent = 'Печать'; printBtn.disabled = false; }, 1500);
  } else {
    printBtn.textContent = '❌ Ошибка';
    setTimeout(() => { printBtn.textContent = 'Печать'; updateStatus(); }, 1500);
  }
});

cellInput.addEventListener('keydown', (e) => {
  if (e.key === 'Enter' && !printBtn.disabled) printBtn.click();
});

updateStatus();
setInterval(updateStatus, 3000);