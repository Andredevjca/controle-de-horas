(() => {
    const botao = document.getElementById('botaoSidebar');
    const fundo = document.getElementById('fundoMenu');
    try { if (localStorage.getItem('menu-recolhido') === 'true' && innerWidth > 991) document.body.classList.add('sidebar-collapsed'); } catch {}
    botao?.addEventListener('click', () => {
        if (innerWidth <= 991) document.body.classList.toggle('menu-aberto');
        else { document.body.classList.toggle('sidebar-collapsed'); try { localStorage.setItem('menu-recolhido', document.body.classList.contains('sidebar-collapsed')); } catch {} }
        botao.setAttribute('aria-expanded', String(innerWidth <= 991 ? document.body.classList.contains('menu-aberto') : !document.body.classList.contains('sidebar-collapsed')));
    });
    fundo?.addEventListener('click', () => document.body.classList.remove('menu-aberto'));
    let intervalos = [];
    function inicializarConteudo() {
        intervalos.forEach(clearInterval);
        intervalos = [];
    document.querySelectorAll('[data-segundos]').forEach(elemento => {
        const segundos = Number(elemento.dataset.segundos);
        const inicio = Date.now();
        const atualizar = () => {
            const total = Math.floor(segundos + (elemento.dataset.ativo === 'true' ? (Date.now() - inicio) / 1000 : 0));
            elemento.textContent = [Math.floor(total / 3600), Math.floor(total % 3600 / 60), total % 60].map(valor => String(valor).padStart(2, '0')).join(':');
        };
        atualizar(); intervalos.push(setInterval(atualizar, 1000));
    });
    }
    document.addEventListener('app:navigated', inicializarConteudo);
    inicializarConteudo();
})();
