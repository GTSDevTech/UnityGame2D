# Contributing Guide (Unity + Git)

Este proyecto usa Unity y Git para trabajo en equipo. Para evitar conflictos y roturas de referencias, sigue estas normas.

---

## 1) Requisitos
- **Unity:** usar la versión exacta indicada en `ProjectSettings/ProjectVersion.txt`
- **Git LFS:** obligatorio para assets pesados (arte/audio/3D)
  - Instalar Git LFS y ejecutar:
    ```bash
    git lfs install
    ```
- **Assets:** 
    - No subir archivos enormes “porque sí”. Si un asset es pesado, debe ir por LFS.
    - No renombrar/mover assets en bloque sin avisar (rompe referencias).

---

## 2) Estructura del repo (qué se versiona)
Se sube a Git:
- `Assets/` (incluye `.meta`)
- `Packages/`
- `ProjectSettings/`
- `.gitignore`, `.gitattributes`, `README.md`

NO se sube:
- `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`

> Nota: Los `.meta` **SIEMPRE** se commitean. Si faltan, Unity rompe referencias.

---

## 3) Configuración obligatoria en Unity 
En Unity:
- `Edit > Project Settings > Editor`
  - **Version Control:** `Visible Meta Files`
  - **Asset Serialization:** `Force Text`

---

## 4) Flujo de trabajo con ramas
No se trabaja directo en `main`.

Ramas:
- `main` → estable / entregas
- `develop` → integración continua
- `feature/<nombre>` → nuevas funcionalidades
- `fix/<nombre>` → arreglos

Ejemplo:
```bash
git checkout develop
git pull
git checkout -b feature/player-jump
