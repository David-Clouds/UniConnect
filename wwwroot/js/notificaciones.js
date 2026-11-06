// Notificaciones en tiempo real con PieHost (WebSocket).
// El servidor publica "ticket-emitido" en el canal del organizador y en el de administración;
// aquí se reciben, se muestran en la campana y se avisa al Panel Organizador.
(() => {
    const raiz = document.querySelector('.notif[data-piehost-cluster]');
    if (!raiz || typeof WebSocket === 'undefined') return;

    const cluster = raiz.dataset.piehostCluster || '';
    const apiKey = raiz.dataset.piehostKey || '';
    const canales = (raiz.dataset.piehostCanales || '').split(',').filter(Boolean);
    if (!/^[a-z0-9-]{1,63}(\.[a-z0-9-]{1,63}){0,3}$/.test(cluster) || !apiKey || canales.length === 0) return;

    const boton = raiz.querySelector('.notif-btn');
    const panel = raiz.querySelector('.notif-panel');
    const lista = raiz.querySelector('.notif-list');
    const contador = raiz.querySelector('.notif-count');
    const estado = raiz.querySelector('.notif-estado');
    const toasts = document.querySelector('.toast-region');

    const formato = new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' });
    const vistos = new Set();
    let pendientes = 0;
    let conectados = 0;

    const crear = (etiqueta, clase, texto) => {
        const elemento = document.createElement(etiqueta);
        if (clase) elemento.className = clase;
        if (texto !== undefined) elemento.textContent = texto;
        return elemento;
    };

    function actualizarEstado() {
        const enVivo = conectados > 0;
        raiz.classList.toggle('notif-en-vivo', enVivo);
        if (estado) estado.textContent = enVivo ? '● En vivo' : 'Reconectando…';
    }

    function actualizarContador() {
        contador.textContent = pendientes > 9 ? '9+' : String(pendientes);
        contador.hidden = pendientes === 0;
        boton.setAttribute('aria-label', pendientes > 0 ? `Notificaciones: ${pendientes} nuevas` : 'Notificaciones');
    }

    function mostrarToast(titulo, detalle) {
        if (!toasts) return;
        const toast = crear('div', 'toast');
        toast.append(crear('strong', '', titulo), crear('span', '', detalle));
        toasts.append(toast);
        setTimeout(() => toast.classList.add('toast-saliendo'), 4500);
        setTimeout(() => toast.remove(), 5000);
    }

    function recibirTicket(datos) {
        const clave = datos.ticketId ? String(datos.ticketId) : null;
        if (clave) {
            if (vistos.has(clave)) return; // el mismo ticket puede llegar por dos canales
            vistos.add(clave);
        }

        const precio = Number(datos.precio) > 0 ? formato.format(Number(datos.precio)) : 'Gratis';
        const titulo = '🎟️ Nuevo ticket';
        const detalle = `${datos.titulo || 'Evento'} · ${precio} · ${datos.totalTicketsEvento ?? '?'} en total`;

        lista.querySelector('.notif-empty')?.remove();
        const item = crear('li', 'notif-item');
        const hora = crear('time', 'notif-hora', new Date().toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit' }));
        item.append(crear('strong', '', titulo), crear('span', '', detalle), hora);
        lista.prepend(item);
        while (lista.children.length > 8) lista.lastElementChild.remove();

        if (panel.hidden) {
            pendientes += 1;
            actualizarContador();
        }
        mostrarToast(titulo, detalle);
        document.dispatchEvent(new CustomEvent('uniconnect:ticket-emitido', { detail: datos }));
    }

    function conectar(canal, intento) {
        const url = `wss://${cluster}.piesocket.com/v3/${encodeURIComponent(canal)}` +
            `?api_key=${encodeURIComponent(apiKey)}&notify_self=1`;
        const socket = new WebSocket(url);
        let abierto = false;

        socket.addEventListener('open', () => {
            abierto = true;
            intento = 0;
            conectados += 1;
            actualizarEstado();
        });

        socket.addEventListener('message', (evento) => {
            let mensaje;
            try { mensaje = JSON.parse(evento.data); } catch { return; }
            if (mensaje && mensaje.event === 'ticket-emitido' && mensaje.data) recibirTicket(mensaje.data);
        });

        socket.addEventListener('close', () => {
            if (abierto) conectados -= 1;
            actualizarEstado();
            const espera = Math.min(30000, 2000 * 2 ** intento);
            setTimeout(() => conectar(canal, intento + 1), espera);
        });
    }

    boton.addEventListener('click', () => {
        const abrir = panel.hidden;
        panel.hidden = !abrir;
        boton.setAttribute('aria-expanded', String(abrir));
        if (abrir) {
            pendientes = 0;
            actualizarContador();
        }
    });

    document.addEventListener('click', (evento) => {
        if (!panel.hidden && !evento.target.closest('.notif')) {
            panel.hidden = true;
            boton.setAttribute('aria-expanded', 'false');
        }
    });

    document.addEventListener('keydown', (evento) => {
        if (evento.key === 'Escape' && !panel.hidden) {
            panel.hidden = true;
            boton.setAttribute('aria-expanded', 'false');
            boton.focus();
        }
    });

    canales.forEach((canal) => conectar(canal, 0));
})();
