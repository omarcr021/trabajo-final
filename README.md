# CampusGo - Proyecto Final (Programación I)

CampusGo es una plataforma web "todo-en-uno" diseñada para mejorar la experiencia universitaria. Permite a los estudiantes organizar sus labores académicas y descubrir actividades o lugares alrededor de su campus.

## 🚀 Características Principales

Actualmente, el proyecto cuenta con el flujo de inicialización completamente funcional:

- **Sistema de Autenticación:** 
  - **Registro de Usuarios:** Creación de nuevas cuentas validando campos y disponibilidad del correo electrónico.
  - **Inicio de Sesión (Login):** Validado mediante Cookie Authentication en ASP.NET Core. Sólo los usuarios registrados pueden acceder al sistema interno.
  - **Cierre de Sesión (Logout):** Destrucción segura de la cookie de sesión.
- **Dashboard Protegido:** Un panel principal al que solo se puede ingresar si se cuenta con una sesión activa. Muestra el nombre real del usuario autenticado.
- **Base de Datos Local:** Utiliza SQLite para almacenar de forma ligera y rápida la información de los usuarios, gestionado a través de Entity Framework Core (Code-First).

Otras características proyectadas en la plataforma (en desarrollo):
- 📚 **Tareas:** Gestión y organización de pendientes universitarios.
- 🍕 **Lugares:** Recomendaciones de sitios alrededor de la universidad.
- 🎉 **Eventos & 💡 Tips:** Actividades y consejos para estudiantes.

## 🛠️ Tecnologías Utilizadas

- **Backend:** C# con ASP.NET Core MVC
- **Base de Datos:** SQLite
- **ORM:** Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`)
- **Autenticación:** ASP.NET Core Cookie Authentication
- **Frontend:** HTML5, CSS3 integrado con el motor de vistas Razor (`.cshtml`)

## ⚙️ Explicación del Flujo de Login y Base de Datos

1. **Modelos y DB:** Existe una entidad `Usuario` que representa la tabla en base de datos. Se utiliza un `AppDbContext` que hereda de `DbContext` para comunicarse con la base de datos `app.db` a través de SQLite.
2. **Registro:** Al completar el formulario en `/Account/Register`, se enviará una petición HTTP POST al `AccountController`, que validará la información y guardará el registro en SQLite usando Entity Framework.
3. **Autenticación:** Al enviar credenciales correctas en `/Account/Login`, el servidor genera un entorno de "Claims" (información en memoria del usuario como Nombre e Id). Se crea una cookie encriptada que se envía al navegador del estudiante.
4. **Protección de Rutas:** El acceso a la vista `/Account/Dashboard` está protegido con el atributo `[Authorize]`. Si un usuario intenta entrar sin sesión (sin la cookie válida), ASP.NET Core lo redirigirá automáticamente a la pantalla de Login.

## 🏁 Cómo ejecutar el proyecto

Clona el proyecto y abre tu terminal o consola de comandos. Sigue estos pasos:

1. Ingresa a la carpeta principal del código fuente:
   ```bash
   cd trabajo-final
   ```

2. (Opcional) Si la base de datos (`app.db`) no existe, las migraciones ya están configuradas. Puedes aplicarlas con:
   ```bash
   dotnet ef database update
   ```

3. Compila y ejecuta la aplicación:
   ```bash
   dotnet run
   ```

4. Abre tu navegador web y dirígete a la URL que indica la terminal (por lo general `http://localhost:5000` o `https://localhost:5001`).
