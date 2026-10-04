(() => {
    'use strict';
    let timer;
    let generation = 0;
    function initialize() {
        clearInterval(timer);
        const version = ++generation;
        const root = document.getElementById('whatsapp-relatorios');
        if (!root) return;
        const find = id => root.querySelector(`#${id}`);
        const status = find('wa-status'), error = find('wa-erro'), qr = find('wa-qr');
        const client = find('wa-cliente'), type = find('wa-tipo'), send = find('wa-enviar');
        let connected = false, busy = false, sending = false;
        const configured = root.dataset.configurado === 'true';
        const token = root.querySelector('#wa-conexao input[name="__RequestVerificationToken"]').value;
        function buttons() {
            send.disabled = !connected || !client.value || sending;
            find('wa-conectar').disabled = busy || !configured || connected;
            find('wa-atualizar').disabled = busy || !configured;
            find('wa-desconectar').disabled = busy;
            find('wa-desconectar').hidden = !connected;
        }
        function preview() {
            find('wa-previa').value = client.selectedOptions[0]?.dataset.mensagem || '';
            const link = find('wa-download');
            const url = new URL(link.href);
            url.pathname = `/Relatorios/${type.value === 'excel' ? 'Excel' : 'Pdf'}`;
            link.href = url.href;
            buttons();
        }
        function show(account) {
            connected = account.conectado === true;
            status.textContent = connected ? `WhatsApp conectado${account.numero ? ' · ' + account.numero.split('@')[0] : ''}` : 'WhatsApp desconectado';
            if (connected) { qr.replaceChildren(); clearInterval(timer); }
            else if (account.qrcode) {
                qr.replaceChildren();
                const value = account.qrcode;
                if (/^(data:image\/png;base64,)?[A-Za-z0-9+/=\r\n]+$/.test(value) && value.length > 100) {
                    const img = document.createElement('img');
                    img.src = value.startsWith('data:') ? value : `data:image/png;base64,${value}`;
                    img.alt = 'QR Code para conectar seu WhatsApp';
                    img.style.maxWidth = '100%'; img.width = 260; img.height = 260;
                    qr.append(img);
                } else if (value.length < 100) qr.textContent = `Código de pareamento: ${value}`;
                else qr.textContent = 'QR Code indisponível. Gere um novo código.';
            }
            buttons();
        }
        async function request(action, post = false) {
            if (busy) return;
            busy = true; error.textContent = ''; buttons();
            try {
                const body = post ? new FormData() : undefined;
                if (body) body.set('__RequestVerificationToken', token);
                const response = await fetch(`/WhatsApp/${action}`, { method: post ? 'POST' : 'GET', body, credentials: 'same-origin', cache: 'no-store' });
                if (version !== generation) return;
                if (!response.headers.get('content-type')?.includes('application/json')) throw new Error('Sua sessão expirou. Atualize a página e entre novamente.');
                const account = await response.json();
                if (!response.ok) throw new Error(account.erro || 'Não foi possível completar a operação.');
                show(account);
                if (action === 'Desconectar') { qr.replaceChildren(); clearInterval(timer); }
                if (action === 'Conectar' && !connected) {
                    if (!account.qrcode) qr.textContent = 'QR Code ainda indisponível. Aguarde e clique em gerar QR Code novamente.';
                    clearInterval(timer);
                    let polls = 0;
                    timer = setInterval(() => {
                        if (++polls > 24) { clearInterval(timer); qr.replaceChildren(); status.textContent = 'O QR Code pode ter expirado. Gere um novo código.'; return; }
                        request('Status');
                    }, 5000);
                }
            } catch (e) {
                if (version !== generation) return;
                connected = false;
                status.textContent = 'Conexão não confirmada';
                error.textContent = e.message;
            } finally { busy = false; if (version === generation) buttons(); }
        }
        client.addEventListener('change', preview);
        type.addEventListener('change', preview);
        find('wa-conectar').addEventListener('click', () => request('Conectar', true));
        find('wa-atualizar').addEventListener('click', () => request('Status'));
        find('wa-desconectar').addEventListener('click', () => request('Desconectar', true));
        find('wa-envio').addEventListener('submit', event => {
            if (sending || !connected || !client.value) { event.preventDefault(); return; }
            sending = true;
            send.textContent = 'Registrando e enviando…';
            // The persisted request key also prevents retries from sending the same document twice.
            setTimeout(buttons, 0);
        });
        preview();
        if (configured) request('Status');
    }
    document.addEventListener('app:navigated', initialize);
    initialize();
})();
