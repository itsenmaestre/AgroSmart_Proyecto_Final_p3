# 🌱 AgroSmart – Plataforma Integral de Gestión Agrícola  
**Proyecto Final P3 – AgroSmart_Proyecto_Final_p3**

AgroSmart es un sistema de gestión agrícola desarrollado en **C# (.NET Framework + WPF)**, que busca modernizar los procesos de cultivo, cosecha, insumos, tareas, liquidaciones y seguimiento de personal.  
Integra una arquitectura en capas, base de datos Oracle, automatización con bots de Telegram, y módulos adicionales (API, servicios, lógica, interfaz). El propósito es ofrecer una solución completa, escalable y modular para la gestión agronómica.

##  Arquitectura general del proyecto

- **ENTITIES**: Contiene todas las clases de datos / DTOs (Usuario, Cultivo, Cosecha, Tarea, Empleado, Insumo, Liquidaciones, etc.).  
- **DAL (Data Access Layer)**: Maneja la comunicación con Oracle Database — repositorios, consultas, inserciones, actualizaciones, eliminaciones.  
- **BLL (Business Logic Layer)**: Encapsula la lógica de negocio sobre las entidades: servicios CRUD, validaciones, reglas, procesos.  
- **GUI (Interfaz WPF)**: Aplicación de escritorio con ventanas y vistas para Administrador y Empleado. Maneja login, registro, gestión completa del sistema, interfaz amigable.  
- **Bots / Servicios / API**: Incluye:  
  - Bot de Telegram para notificaciones, consultas y alertas.  
  - Servicio/API (por ejemplo módulo `IAAgroSmart`) para futuras integraciones.  
  - Automatizaciones auxiliares (scripts, conectividad, etc.).  

Esta estructura modular permite mantenimiento, escalabilidad y separación clara de responsabilidades.

##  Características principales

- Gestión completa de **cultivos, cosechas, insumos**  
- Gestión de **usuarios, empleados, tareas, asignaciones**  
- Sistema de **liquidaciones y gastos**  
- Interfaz moderna y organizada usando **WPF + XAML**  
- **Compatibilidad con Oracle Database**  
- **Bots de Telegram** para notificaciones y consultas remotas  
- Estructura limpia y modular (Entidad → Datos → Lógica → UI)  
- Código separado por proyectos: BLL, DAL, ENTITY, GUI, Bots, Servicios  
- `.gitignore` configurado, sin archivos innecesarios  
- Proyecto inicial limpio: ideal para mantenimiento o entrega académica  

##  Estructura de carpetas (a nivel alto)

Cada carpeta representa un módulo del sistema, lo cual ayuda a mantener el proyecto organizado, modular y fácil de navegar.

## 🚀 Cómo ejecutar el proyecto localmente

### Requisitos
- Visual Studio 2022  
- .NET Framework 4.8 compatible  
- Oracle Database (local o remota)  
- Credenciales de conexión a Oracle configuradas en los archivos `app.config` correspondientes  
- Dependencias restauradas (NuGet)  

### Pasos
1. Clona el repositorio:
   ```bash
   git clone https://github.com/itsenmaestre/AgroSmart_Proyecto_Final_p3.git






