const statusDot = document.getElementById('statusDot');
const statusText = document.getElementById('statusText');
const cellInput = document.getElementById('cellCode');
const printBtn = document.getElementById('printBtn');

// Обновляем статус подключения
async function updateStatus() {
  const response = await chrome.runtime.sendMessage({ type: 'GET_STATUS' });
  if (response.connected) {
    statusDot.className = 'dot green';
    statusText.textContent = 'Подключено к PvzPrinter';
    printBtn.disabled = false;
  } else {
    statusDot.className = 'dot red';
    statusText.textContent = 'PvzPrinter не запущен';
    printBtn.disabled = true;
  }
}

// Печать
printBtn.addEventListener('click', async () => {
  const cellCode = cellInput.value.trim();
  if (!cellCode) return;

  printBtn.disabled = true;
  printBtn.textContent = 'Отправка...';

  const response = await chrome.runtime.sendMessage({
    type: 'PRINT',
    cellCode
  });

  if (response.ok) {
    printBtn.textContent = '✅ Отправлено!';
    cellInput.value = '';
    setTimeout(() => {
      printBtn.textContent = 'Печать';
      printBtn.disabled = false;
    }, 1500);
  } else {
    printBtn.textContent = '❌ Ошибка';
    setTimeout(() => {
      printBtn.textContent = 'Печать';
      updateStatus();
    }, 1500);
  }
});

// Enter для печати
cellInput.addEventListener('keydown', (e) => {
  if (e.key === 'Enter' && !printBtn.disabled) {
    printBtn.click();
  }
});

// Обновляем статус каждые 3 секунды
updateStatus();
setInterval(updateStatus, 3000);