// Buscador de la barra superior: resultados mientras escribes (Algolia, o la base de datos como respaldo).
(() => {
    const input = document.getElementById('buscador-input');
    const panel = document.getElementById('buscador-resultados');
    if (!input || !panel) return;

    const endpoint = input.dataset.endpoint || '/api/buscar';
    let temporizador = null;
    let peticion = null;
    let opciones = [];
    let activa = -1;

    const crear = (etiqueta, clase, texto) => {
        const elemento = document.createElement(etiqueta);
        if (clase) elemento.className = clase;
        if (texto !== undefined) elemento.textContent = texto;
        return elemento;
    };

    // Solo rutas internas de la app ("/Eventos/..."), nunca otro dominio.
    const esRutaInterna = (url) => typeof url === 'string' && url.startsWith('/') && !url.startsWith('//');
    const esImagenSegura = (url) => typeof url === 'string' && url.startsWith('https://');

    function abrir() {
        panel.hidden = false;
        input.setAttribute('aria-expanded', 'true');
    }

    function cerrar() {
        panel.hidden = true;
        input.setAttribute('aria-expanded', 'false');
        input.removeAttribute('aria-activedescendant');
        activa = -1;
    }

    function marcar(indice) {
        opciones.forEach((opcion, i) => opcion.setAttribute('aria-selected', i === indice ? 'true' : 'false'));
        activa = indice;
        if (indice >= 0 && opciones[indice]) {
            input.setAttribute('aria-activedescendant', opciones[indice].id);
            opciones[indice].scrollIntoView({ block: 'nearest' });
        } else {
            input.removeAttribute('aria-activedescendant');
        }
    }

    function mostrarMensaje(texto) {
        opciones = [];
        panel.replaceChildren(crear('p', 'buscador-vacio', texto));
        abrir();
    }

    function mostrar(datos, consulta) {
        const resultados = Array.isArray(datos.resultados) ? datos.resultados : [];
        opciones = [];
        activa = -1;

        if (resultados.length === 0) {
            mostrarMensaje(`Sin resultados para “${consulta}”.`);
            return;
        }

        const fragmento = document.createDocumentFragment();
        resultados.forEach((resultado, i) => {
            if (!esRutaInterna(resultado.url)) return;

            const opcion = crear('a', 'buscador-item');
            opcion.id = `buscador-opcion-${i}`;
            opcion.href = resultado.url;
            opcion.setAttribute('role', 'option');
            opcion.setAttribute('aria-selected', 'false');
            opcion.tabIndex = -1;

            if (esImagenSegura(resultado.imagenUrl)) {
                const imagen = crear('img', 'buscador-img');
                imagen.src = resultado.imagenUrl;
                imagen.alt = '';
                imagen.loading = 'lazy';
                imagen.referrerPolicy = 'no-referrer';
                opcion.append(imagen);
            } else {
                opcion.append(crear('span', 'buscador-img buscador-img-vacia', resultado.tipo === 'Join Us' ? '🤝' : '📅'));
            }

            const textos = crear('span', 'buscador-textos');
            textos.append(
                crear('span', 'buscador-titulo', resultado.titulo || ''),
                crear('span', 'buscador-detalle', resultado.detalle || ''));
            opcion.append(textos);
            opcion.append(crear('span', resultado.tipo === 'Join Us' ? 'buscador-tipo buscador-tipo-teal' : 'buscador-tipo', resultado.tipo || 'Evento'));

            fragmento.append(opcion);
            opciones.push(opcion);
        });

        const pie = crear('p', 'buscador-fuente', datos.fuente === 'Algolia' ? 'Búsqueda con Algolia' : 'Búsqueda en la base de datos');
        panel.replaceChildren(fragmento, pie);
        abrir();
    }

    async function buscar(consulta) {
        if (peticion) peticion.abort();
        peticion = new AbortController();
        try {
            const respuesta = await fetch(`${endpoint}?q=${encodeURIComponent(consulta)}`, {
                headers: { Accept: 'application/json' },
                signal: peticion.signal
            });
            if (!respuesta.ok) throw new Error(`HTTP ${respuesta.status}`);
            const datos = await respuesta.json();
            if (input.value.trim() === consulta) mostrar(datos, consulta);
        } catch (error) {
            if (error.name !== 'AbortError') mostrarMensaje('No se pudo buscar en este momento.');
        }
    }

    input.addEventListener('input', () => {
        clearTimeout(temporizador);
        const consulta = input.value.trim();
        if (consulta.length < 2) {
            if (peticion) peticion.abort();
            cerrar();
            return;
        }
        temporizador = setTimeout(() => buscar(consulta), 200);
    });

    input.addEventListener('keydown', (evento) => {
        if (evento.key === 'Escape') {
            cerrar();
            return;
        }
        if (panel.hidden || opciones.length === 0) return;

        if (evento.key === 'ArrowDown') {
            evento.preventDefault();
            marcar((activa + 1) % opciones.length);
        } else if (evento.key === 'ArrowUp') {
            evento.preventDefault();
            marcar(activa <= 0 ? opciones.length - 1 : activa - 1);
        } else if (evento.key === 'Enter') {
            evento.preventDefault();
            window.location.href = opciones[Math.max(activa, 0)].href;
        }
    });

    input.addEventListener('focus', () => {
        if (opciones.length > 0 && input.value.trim().length >= 2) abrir();
    });

    document.addEventListener('click', (evento) => {
        if (!evento.target.closest('.buscador')) cerrar();
    });
})();
