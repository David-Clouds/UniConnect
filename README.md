
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
dotnet ef database update
dotnet run
```

Abre **http://localhost:5000/** (o el puerto que indique la consola).

---

## 🚀 Despliegue en Render

| Configuración | Detalle |
|---|---|
| Runtime | Docker |
| Rama desplegada | `main` |
| Puerto | Variable de entorno `PORT` |
| Base de datos | SQLite (`uniconnect.db`), no versionada en Git |

---

## 🌳 Estructura de ramas (Git)
main
├── feature/login
├── feature/eventos
├── feature/joinus
└── feature/organizador


Cada integrante trabaja en su rama y abre un Pull Request hacia `main` para integrar cambios.