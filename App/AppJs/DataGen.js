let currentSessionId = null;
let progressInterval = null;
let isGenerating = false;

function generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0;
        const v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function addLog(message, type = 'INFO') {
    const logContainer = document.getElementById('logContainer');
    const now = new Date().toLocaleTimeString();
    const logEntry = document.createElement('div');
    logEntry.className = 'log-entry';
    logEntry.innerHTML = `<span class="log-time">[${type}]</span><span>${message}</span>`;
    logContainer.appendChild(logEntry);
    logContainer.scrollTop = logContainer.scrollHeight;
}

function updateProgress(data) {
    if (!data) return;

    const progressBar = document.getElementById('progressBar');
    const progressText = document.getElementById('progressText');
    const progressStatus = document.getElementById('progressStatus');

    // Update progress bar
    progressBar.style.width = data.percentage + '%';
    progressText.textContent = data.percentage + '%';

    // Update stats
    document.getElementById('statProcessed').textContent = data.processedItems?.toLocaleString() || '0';
    document.getElementById('statTotal').textContent = data.totalItems?.toLocaleString() || '0';
    document.getElementById('statSpeed').textContent = data.itemsPerSecond || '0';
    document.getElementById('statElapsed').textContent = data.elapsedTime || '00:00:00';
    document.getElementById('statRemaining').textContent = data.estimatedTimeRemaining || '00:00:00';

    // Update status
    if (data.isCompleted) {
        progressStatus.className = 'progress-status status-completed';
        progressStatus.textContent = 'HOÀN THÀNH';
        clearInterval(progressInterval);
        isGenerating = false;

        // Enable buttons
        document.getElementById('btnGenerateCategories').disabled = false;
        document.getElementById('btnGenerateNews').disabled = false;
        document.getElementById('btnStop').disabled = true;

        addLog(data.message || 'Hoàn thành!', 'SUCCESS');
    } else {
        addLog(`Tiến trình: ${data.percentage}% - ${data.message}`, 'INFO');
    }
}

function startProgressTracking(sessionId, title) {
    currentSessionId = sessionId;
    isGenerating = true;

    const progressContainer = document.getElementById('progressContainer');
    const progressTitle = document.getElementById('progressTitle');
    const progressStatus = document.getElementById('progressStatus');

    progressContainer.classList.add('active');
    progressTitle.textContent = title;
    progressStatus.className = 'progress-status status-running';
    progressStatus.textContent = 'ĐANG CHẠY';

    // Disable generation buttons
    document.getElementById('btnGenerateCategories').disabled = true;
    document.getElementById('btnGenerateNews').disabled = true;
    document.getElementById('btnStop').disabled = false;

    addLog(`Bắt đầu ${title} - Session: ${sessionId}`, 'INFO');

    // Start polling for progress
    progressInterval = setInterval(() => {
        if (currentSessionId) {
            fetch(`/DataGenerator/GetProgress?sessionId=${currentSessionId}`)
                .then(response => response.json())
                .then(data => updateProgress(data))
                .catch(error => {
                    console.error('Error fetching progress:', error);
                    addLog('Lỗi khi lấy tiến trình: ' + error.message, 'ERROR');
                });
        }
    }, 1000);
}

function generateCategories() {
    const count = parseInt(document.getElementById('categoryCount').value);

    if (!count || count < 1 || count > 1000000) {
        alert('Vui lòng nhập số lượng hợp lệ (1 - 1,000,000)');
        return;
    }

    if (isGenerating) {
        alert('Đang có quá trình tạo dữ liệu khác. Vui lòng đợi hoàn thành.');
        return;
    }

    const sessionId = generateGUID();

    fetch('/DataGenerator/GenerateCategories', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ count: count, sessionId: sessionId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                startProgressTracking(sessionId, `Tạo ${count.toLocaleString()} danh mục`);
            } else {
                alert('Lỗi: ' + data.message);
                addLog('Lỗi khi tạo danh mục: ' + data.message, 'ERROR');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Lỗi kết nối: ' + error.message);
            addLog('Lỗi kết nối: ' + error.message, 'ERROR');
        });
}

function generateNews() {
    const count = parseInt(document.getElementById('newsCount').value);

    if (!count || count < 1 || count > 100000000) {
        alert('Vui lòng nhập số lượng hợp lệ (1 - 100,000,000)');
        return;
    }

    if (isGenerating) {
        alert('Đang có quá trình tạo dữ liệu khác. Vui lòng đợi hoàn thành.');
        return;
    }

    const sessionId = generateGUID();

    fetch('/DataGenerator/GenerateNews', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ count: count, sessionId: sessionId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                startProgressTracking(sessionId, `Tạo ${count.toLocaleString()} tin tức`);
            } else {
                alert('Lỗi: ' + data.message);
                addLog('Lỗi khi tạo tin tức: ' + data.message, 'ERROR');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Lỗi kết nối: ' + error.message);
            addLog('Lỗi kết nối: ' + error.message, 'ERROR');
        });
}

function stopGeneration() {
    if (confirm('Bạn có chắc chắn muốn dừng quá trình tạo dữ liệu?')) {
        clearInterval(progressInterval);
        isGenerating = false;
        currentSessionId = null;

        // Enable buttons
        document.getElementById('btnGenerateCategories').disabled = false;
        document.getElementById('btnGenerateNews').disabled = false;
        document.getElementById('btnStop').disabled = true;

        // Update status
        const progressStatus = document.getElementById('progressStatus');
        progressStatus.className = 'progress-status status-error';
        progressStatus.textContent = 'ĐÃ DỪNG';

        addLog('Quá trình tạo dữ liệu đã được dừng bởi người dùng', 'WARNING');
    }
}

function clearProgress() {
    if (isGenerating && !confirm('Quá trình đang chạy. Bạn có chắc chắn muốn xóa tiến trình?')) {
        return;
    }

    clearInterval(progressInterval);

    if (currentSessionId) {
        fetch('/DataGenerator/ClearProgress', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ sessionId: currentSessionId })
        })
            .then(response => response.json())
            .then(data => console.log('Progress cleared'));
    }

    // Reset UI
    document.getElementById('progressContainer').classList.remove('active');
    document.getElementById('progressBar').style.width = '0%';
    document.getElementById('progressText').textContent = '0%';
    document.getElementById('logContainer').innerHTML = '<div class="log-entry"><span class="log-time">[INFO]</span><span>Sẵn sàng để bắt đầu...</span></div>';

    // Reset stats
    document.getElementById('statProcessed').textContent = '0';
    document.getElementById('statTotal').textContent = '0';
    document.getElementById('statSpeed').textContent = '0';
    document.getElementById('statElapsed').textContent = '00:00:00';
    document.getElementById('statRemaining').textContent = '00:00:00';

    // Enable buttons
    document.getElementById('btnGenerateCategories').disabled = false;
    document.getElementById('btnGenerateNews').disabled = false;
    document.getElementById('btnStop').disabled = true;

    currentSessionId = null;
    isGenerating = false;
}

// Format numbers with locale
document.getElementById('categoryCount').addEventListener('input', function (e) {
    const value = parseInt(e.target.value);
    if (value) {
        e.target.setAttribute('data-formatted', value.toLocaleString());
    }
});

document.getElementById('newsCount').addEventListener('input', function (e) {
    const value = parseInt(e.target.value);
    if (value) {
        e.target.setAttribute('data-formatted', value.toLocaleString());
    }
});

// Initialize
document.addEventListener('DOMContentLoaded', function () {
    addLog('Hệ thống Data Generator đã sẵn sàng', 'INFO');
});