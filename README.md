
# 🎓 UniConnect — Plataforma de Eventos Universitarios USMP
> Aplicación web MVC en .NET 10 para centralizar eventos, actividades comunitarias y oportunidades para estudiantes de la USMP, proyecto grupal 

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-SQLite-003B57?logo=sqlite&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)
![Render](https://img.shields.io/badge/Deploy-Render-46E3B7?logo=render&logoColor=white)

---

## 🔗 Enlaces

| Recurso | URL |
|---|---|
| 📦 Repositorio GitHub | https://github.com/David-Clouds/UniConnect |
| 🎨 Diseño en Figma | https://www.figma.com/make/yGW4sb2DzXZM7zECkchGIt/UniConnect-web-app-design?t=JQlVb2r0avENCNDH-1 |


---

## 🛠️ Tecnologías

- **.NET 10** (ASP.NET Core MVC)
- **Entity Framework Core** + **SQLite**
- **Docker** (build multi-stage)
- **Git / GitHub** (ramas, Pull Requests, merges)
- **Render** (despliegue como Web Service)

---

## ✨ Funcionalidades

- ✅ Registro e inicio de sesión de estudiantes.
- ✅ Visualización, búsqueda y filtros de eventos (categoría, fecha, precio).
- ✅ Detalle de evento.
- ✅ Registro a eventos gratuitos.
- ✅ Compra simulada de tickets con generación de ticket digital + código QR.
- ✅ Sección **"Join Us"**: voluntariados, comunidades y causas solidarias dentro de la USMP.
- ✅ Publicación de eventos por organizadores.
- ✅ Panel de organizador (participantes, tickets vendidos, ingresos).
- ✅ Perfil del estudiante (mis eventos, mis tickets, mis voluntariados).

---

## 🗃️ Modelo de datos

**`Estudiante`**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | int | Identificador autogenerado |
| `Nombre` | string | Nombre del estudiante |
| `Correo` | string | Correo institucional |
| `Codigo` | string | Código de estudiante USMP |
| `PasswordHash` | string | Contraseña encriptada |

**`Organizador`**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | int | Identificador autogenerado |
| `Nombre` | string | Nombre del club/organizador |
| `Correo` | string | Correo de contacto |
| `Descripcion` | string? | Descripción opcional |

**`Evento`**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | int | Identificador autogenerado |
| `Titulo` | string | Nombre del evento |
| `Categoria` | string | Categoría del evento |
| `Fecha` | DateTime | Fecha y hora del evento |
| `Precio` | decimal | 0 si es gratuito |
| `Descripcion` | string? | Detalle adicional |
| `ImagenUrl` | string? | Imagen del evento |
| `OrganizadorId` | int | Relación con el organizador |

**`Ticket`**
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | int | Identificador autogenerado |
| `CodigoQR` | string | Código único del ticket |
| `FechaCompra` | DateTime | Fecha de generación |
| `EventoId` | int | Evento asociado |
| `EstudianteId` | int | Estudiante asociado |

**`Comunidad`** *(sección Join Us)*
| Campo | Tipo | Descripción |
|---|---|---|
| `Id` | int | Identificador autogenerado |
| `Nombre` | string | Nombre de la comunidad/causa |
| `Descripcion` | string | Detalle de la causa |
| `TipoApoyo` | string | Voluntariado, Donación, Colecta |
| `ImagenUrl` | string? | Imagen de la comunidad |

---

## 💻 Cómo correr localmente

```bash
cd UniConnect
dotnet restore
dotnet run --launch-profile http
```

Abre **http://localhost:5180/Cuenta/Login** (o el puerto que indique la consola).

### Demostración local en Development

Requiere el SDK de .NET 10. Ejecuta los comandos anteriores desde la carpeta que
contiene `UniConnect.csproj`. El perfil `http` establece `Development`; el arranque
aplica las migraciones y carga los datos iniciales automáticamente.

En una instalación de demostración sin estudiantes, abrir el login crea:

- Correo: **demo@usmp.pe**
- Contraseña de demostración: **Usmp2026!**

La cuenta recién creada se asigna a **DevClub USMP** únicamente si existe una sola
organización con ese nombre y no tiene responsable. Si no existe, se crea con sus
datos de demostración. Nunca recibe permisos de Administrador General.

Después del login puedes abrir **Mis Tickets** (`/Eventos/MisTickets`) y
**Panel Organizador** (`/Organizador/Panel`). Encontrarás tres tickets de demostración,
cada uno asociado a un evento distinto de DevClub USMP. Se reutilizan hasta tres
eventos existentes de esa organización y solo se crean los que falten para completar
tres; no se modifican sus fechas, precios ni otros datos. Si ya tenía más eventos,
se conservan todos y se toman los tres primeros por identificador para los tickets.

Las tarjetas y estadísticas se calculan a partir de estos registros: tres tickets
emitidos y un participante único en una instalación nueva. El número de eventos
próximos depende de las fechas de los eventos reutilizados. Los tickets son datos
de demostración, no acreditan pagos ni asistencia y no habilitan la generación de
QR. La cuenta, su asignación, los eventos que falten y los tickets se guardan en una
sola transacción.

Si ya existe cualquier estudiante, no se crea otra cuenta demo, no se cambian
contraseñas ni se conceden permisos, aunque la cuenta existente sea `demo@usmp.pe`.
El login tampoco añade eventos o tickets en esas bases existentes.
Tampoco se reemplazan responsables existentes ni se asignan organizaciones con
nombres ambiguos. Repetir la ejecución no vuelve a asignar la cuenta.

La inicialización del login comprueba que no haya migraciones pendientes y que
la migración de permisos esté aplicada. La factoría de diseño de EF usa SQLite
en memoria: `dotnet ef migrations list` sin una conexión explícita no describe
el archivo usado por la aplicación.

En **Production** (y cualquier entorno distinto de Development), abrir el login
no crea cuentas demo ni asignaciones. Las credenciales publicadas son solo para
demostraciones locales. No utilices una base de demostración con esas cuentas en
Production: cambiar de entorno no elimina cuentas ya creadas.

---

## 🚀 Despliegue en Render

| Configuración | Detalle |
|---|---|
| Runtime | Docker |
| Rama desplegada | `main` |
| Puerto | Variable de entorno `PORT` |
| Base de datos | SQLite (`uniconnect.db`); el repositorio incluye un archivo inicial, no publiques tu base local modificada |

---

## 🌳 Estructura de ramas (Git)
main
├── feature/login
├── feature/eventos
├── feature/joinus
└── feature/organizador


Cada integrante trabaja en su rama y abre un Pull Request hacia `main` para integrar cambios.
