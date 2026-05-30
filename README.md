# CampusGo 🎓

CampusGo es una plataforma web desarrollada como proyecto final del curso de **Programación I**. Su objetivo principal es ofrecer una aplicación centralizada donde los estudiantes puedan organizar sus tareas universitarias y descubrir lugares o actividades cerca de su campus.

## Características Principales

Actualmente, el sistema cuenta con un flujo de autenticación completamente funcional:
- **Gestión de Usuarios:** Registro de nuevas cuentas, inicio de sesión y cierre de sesión.
- **Seguridad:** Autenticación basada en Cookies (`Cookie Authentication` en ASP.NET Core).
- **Rutas Protegidas:** Dashboard principal seguro (redirige al login si no hay una sesión activa).
    
### En Desarrollo
- Gestión de tareas y horarios.
- Recomendaciones de lugares cercanos.
- Módulo de eventos y tips para estudiantes.

## Tecnologías

El proyecto está construido sobre un stack robusto y moderno:
- **Backend:** C# y ASP.NET Core MVC.
- **Base de Datos:** SQLite gestionada a través de Entity Framework Core.
- **Frontend:** HTML5, CSS3 y Vistas Razor (`.cshtml`).

**Flujo de Autenticación:**
1. *Registro:* El formulario envía los datos al controlador, se validan y se guardan en SQLite.
2. *Login:* Al proporcionar credenciales correctas, el servidor genera una cookie encriptada con la información del usuario.
3. *Sesión:* Esta cookie se envía al navegador y otorga acceso a las rutas protegidas.

## Instalación y Ejecución

Sigue estos pasos para correr el proyecto localmente:

1. **Clonar el repositorio y acceder al directorio:**
   ```bash
   git clone <url-del-repositorio>
   ```

2. **Aplicar las migraciones (Crear la base de datos):**
   ```bash
   dotnet ef database update
   ```

3. **Ejecutar la aplicación:**
   ```bash
   dotnet run
   ```

4. **Abrir en el navegador:**
   Navega a la ruta indicada en la consola, por ejemplo `http://localhost:5018` o `http://localhost:5000`.
