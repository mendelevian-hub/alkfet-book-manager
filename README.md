# alkfet-book-manager
GDE Alkfet.tech. beadandó


Egy egyszerű, konténerizált (Docker) “Book Manager” alkalmazás **Angular + ASP.NET + MongoDB** alapon, CI/CD-vel (**GitHub Actions + GHCR + ArgoCD**) és Kubernetes telepítéssel (kind).

---

## Architecture

- **WebUI (Angular + Nginx)**  
  - Felület: `/books` (lista + lapozás), `/books/new` és `/books/:id` (create/edit/delete)
  - Nginx proxy: `/api/*` → **WebApi**
- **WebApi (ASP.NET)**  
  - Publikus REST: `/api/books`
  - Továbbít: **DataAccessApi**
- **DataAccessApi (ASP.NET + MongoDB.Driver)**  
  - CRUD: `/books`
  - MongoDB-t használ
- **McpService (ASP.NET + MCP)**  
  - MCP endpoint: `/mcp`
  - Toolok: könyvek listázása / létrehozása / törlése
- **MongoDB** (Helm chart)