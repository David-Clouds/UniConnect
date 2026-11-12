// Chatbot "Uni": ventana flotante que conversa con /api/chatbot.
// La conversación se guarda en sessionStorage para que no se pierda al cambiar de página.
(() => {
    const raiz = document.querySelector('.chatbot');
    if (!raiz) return;

    const endpoint = raiz.dataset.endpoint;
    const base = (raiz.dataset.base || '/').replace(/\/$/, '');
    const boton = raiz.querySelector('.chatbot-boton');
    const ventana = raiz.querySelector('.chatbot-ventana');
    const lista = raiz.querySelector('.chatbot-mensajes');
    const sugerencias = raiz.querySelector('.chatbot-sugerencias');
    const form = raiz.querySelector('.chatbot-form');
    const input = form.querySelector('input');
    const enviar = form.querySelector('.chatbot-enviar');

    const CLAVE = 'uniconnect-chatbot';
    const BIENVENIDA = '¡Hola! Soy Uni 👋 Puedo recomendarte eventos, explicarte cómo conseguir tus tickets o contarte sobre los voluntariados de Join Us. ¿En qué te ayudo?';
    const MAX_HISTORIAL = 12;

    const leer = (clave, defecto) => {
        try { return JSON.parse(sessionStorage.getItem(clave)) ?? defecto; } catch { return defecto; }
    };
    const guardar = () => {
        try { sessionStorage.setItem(CLAVE, JSON.stringify(historial)); } catch { /* modo privado */ }
    };

    let historial = leer(CLAVE, []);
    if (!Array.isArray(historial)) historial = [];
    let enviando = false;

    // Convierte rutas internas (/Eventos/Detalle/5, /Comunidad, ...) en enlaces, sin usar innerHTML.
    const RUTAS = /(\/(?:Eventos\/Detalle\/\d+|Comunidad\/Detalle\/\d+|Eventos\/MisTickets|Organizador\/Panel|Eventos|Comunidad|Perfil))(?![\w/])/g;
    const pintarTexto = (elemento, texto) => {
        let ultimo = 0;
        for (const coincidencia of texto.matchAll(RUTAS)) {
            elemento.append(texto.slice(ultimo, coincidencia.index));
            const enlace = document.createElement('a');
            enlace.href = base + coincidencia[1];
            enlace.textContent = coincidencia[1];
            elemento.append(enlace);
            ultimo = coincidencia.index + coincidencia[0].length;
        }
        elemento.append(texto.slice(ultimo));
    };

    const agregar = (rol, texto, clase) => {
        const li = document.createElement('li');
        li.className = `chatbot-msg ${clase || (rol === 'user' ? 'chatbot-msg-user' : 'chatbot-msg-bot')}`;
        if (rol === 'user') li.textContent = texto; else pintarTexto(li, texto);
        lista.append(li);
        lista.scrollTop = lista.scrollHeight;
        return li;
    };

    const renderizar = () => {
        lista.replaceChildren();
        agregar('assistant', BIENVENIDA);
        for (const m of historial) agregar(m.rol, m.texto);
        sugerencias.hidden = historial.length > 0;
    };

    const abrir = (abierto) => {
        ventana.hidden = !abierto;
        boton.setAttribute('aria-expanded', String(abierto));
        boton.querySelector('.chatbot-boton-icono').textContent = abierto ? '✕' : '💬';
        boton.setAttribute('aria-label', abierto ? 'Cerrar asistente virtual' : 'Abrir asistente virtual');
        if (abierto) { lista.scrollTop = lista.scrollHeight; input.focus(); }
    };

    const preguntar = async (texto) => {
        texto = texto.trim();
        if (!texto || enviando) return;
        enviando = true;
        enviar.disabled = true;
        sugerencias.hidden = true;

        historial.push({ rol: 'user', texto });
        historial = historial.slice(-MAX_HISTORIAL);
        guardar();
        agregar('user', texto);

        const escribiendo = agregar('assistant', '');
        escribiendo.setAttribute('aria-label', 'Uni está escribiendo');
        const puntos = document.createElement('span');
        puntos.className = 'chatbot-escribiendo';
        puntos.append(document.createElement('span'), document.createElement('span'), document.createElement('span'));
        escribiendo.append(puntos);

        try {
            const respuesta = await fetch(endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ mensajes: historial })
            });
            escribiendo.remove();

            if (respuesta.status === 429) {
                agregar('assistant', 'Estás enviando muchos mensajes seguidos. Espera un minuto e inténtalo de nuevo. ⏳', 'chatbot-msg chatbot-msg-error');
                historial.pop(); guardar();
                return;
            }
            if (!respuesta.ok) throw new Error(String(respuesta.status));

            const datos = await respuesta.json();
            historial.push({ rol: 'assistant', texto: datos.respuesta });
            guardar();
            agregar('assistant', datos.respuesta);
        } catch {
            escribiendo.remove();
            historial.pop(); guardar();
            agregar('assistant', 'No pude conectarme en este momento. Inténtalo otra vez en unos segundos.', 'chatbot-msg chatbot-msg-error');
        } finally {
            enviando = false;
            enviar.disabled = false;
            input.focus();
        }
    };

    boton.addEventListener('click', () => abrir(ventana.hidden));
    raiz.querySelector('.chatbot-cerrar').addEventListener('click', () => { abrir(false); boton.focus(); });
    raiz.querySelector('.chatbot-limpiar').addEventListener('click', () => {
        historial = [];
        guardar();
        renderizar();
        input.focus();
    });
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && !ventana.hidden) { abrir(false); boton.focus(); }
    });
    sugerencias.addEventListener('click', (e) => {
        if (e.target instanceof HTMLButtonElement) preguntar(e.target.textContent);
    });
    form.addEventListener('submit', (e) => {
        e.preventDefault();
        const texto = input.value;
        input.value = '';
        preguntar(texto);
    });

    renderizar();
})();
