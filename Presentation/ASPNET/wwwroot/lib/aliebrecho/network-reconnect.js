(() => {
    const overlay = document.getElementById('networkReconnect');
    const title = document.getElementById('networkReconnectTitle');
    const message = document.getElementById('networkReconnectMessage');
    const button = document.getElementById('networkReconnectButton');
    if (!overlay) return;

    let reloadScheduled = false;

    const show = (offline) => {
        overlay.classList.add('is-visible');
        overlay.setAttribute('aria-hidden', 'false');
        title.textContent = offline ? 'Você está sem internet' : 'Conexão instável';
        message.textContent = offline
            ? 'Assim que a conexão voltar, esta página será recarregada automaticamente.'
            : 'Não foi possível acessar o servidor. Verificando sua conexão...';
    };

    const reconnect = () => {
        if (reloadScheduled) return;
        reloadScheduled = true;
        show(false);
        title.textContent = 'Conexão restabelecida';
        message.textContent = 'Recarregando a página...';
        window.setTimeout(() => window.location.reload(), 900);
    };

    window.addEventListener('offline', () => show(true));
    window.addEventListener('online', reconnect);
    window.addEventListener('aliebrecho:network-error', () => show(!navigator.onLine));
    button?.addEventListener('click', () => window.location.reload());

    if (!navigator.onLine) show(true);
})();
