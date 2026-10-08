(() => {
    'use strict';
    function initialize() {
        const form = document.getElementById('baixa-pagamentos');
        if (!form) return;
        const checks = [...form.querySelectorAll('.demanda-pagamento:not(:disabled)')];
        const all = form.querySelector('#selecionar-todas');
        const money = value => (value / 100).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
        function update() {
            const selected = checks.filter(c => c.checked);
            const total = selected.reduce((sum, c) => sum + Number(c.dataset.centavos), 0);
            all.disabled = checks.length === 0;
            all.checked = checks.length > 0 && selected.length === checks.length;
            all.indeterminate = selected.length > 0 && selected.length < checks.length;
            form.querySelector('#pagar-selecionadas').disabled = selected.length === 0;
            form.querySelector('#confirmar-pagamentos').disabled = selected.length === 0;
            const summary = selected.length + ' demanda(s) selecionada(s) · Total: ' + money(total);
            form.querySelector('#resumo-selecao').textContent = summary;
            form.querySelector('#total-baixa').textContent = summary;
            form.querySelector('#lista-baixa').replaceChildren(...selected.map(c => {
                const item = document.createElement('li');
                item.textContent = c.dataset.titulo + ' — ' + money(Number(c.dataset.centavos));
                return item;
            }));
        }
        all.addEventListener('change', () => { checks.forEach(c => c.checked = all.checked); update(); });
        checks.forEach(c => c.addEventListener('change', update));
        form.addEventListener('submit', event => {
            if (!checks.some(c => c.checked)) event.preventDefault();
        });
        update();
    }
    document.addEventListener('app:navigated', initialize);
    initialize();
})();
