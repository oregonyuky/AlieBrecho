(function () {
    const input = document.getElementById('heroImageInput');
    const preview = document.getElementById('heroPreview');
    const empty = document.getElementById('heroEmpty');
    const saveButton = document.getElementById('saveHeroImage');
    const status = document.getElementById('siteSettingsStatus');

    function showPreview(url) {
        if (!url) return;
        preview.src = url;
        preview.hidden = false;
        empty.hidden = true;
    }

    input.addEventListener('change', function () {
        const file = input.files && input.files[0];
        if (file) showPreview(URL.createObjectURL(file));
    });

    async function loadSettings() {
        try {
            const response = await AxiosManager.get('/SiteSettings', {});
            const imageUrl = response?.data?.content?.heroImageUrl;
            if (imageUrl) showPreview(imageUrl);
        } catch (error) {
            status.textContent = 'Não foi possível carregar a configuração atual.';
            status.className = 'site-settings__status text-danger';
        }
    }

    saveButton.addEventListener('click', async function () {
        const file = input.files && input.files[0];
        if (!file) {
            status.textContent = 'Selecione uma imagem antes de salvar.';
            status.className = 'site-settings__status text-danger';
            return;
        }

        saveButton.disabled = true;
        status.textContent = 'Enviando imagem...';
        status.className = 'site-settings__status text-muted';
        try {
            const formData = new FormData();
            formData.append('file', file);
            const upload = await AxiosManager.post('/FileImage/UploadImage', formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });
            const imageName = upload?.data?.content?.imageName;
            if (!imageName) throw new Error('Upload sem nome de imagem.');

            const saved = await AxiosManager.post('/SiteSettings', { heroImageName: imageName });
            showPreview(saved?.data?.content?.heroImageUrl);
            status.textContent = 'Imagem da página inicial salva com sucesso.';
            status.className = 'site-settings__status text-success';
        } catch (error) {
            status.textContent = 'Não foi possível salvar a imagem.';
            status.className = 'site-settings__status text-danger';
        } finally {
            saveButton.disabled = false;
        }
    });

    loadSettings();
})();
