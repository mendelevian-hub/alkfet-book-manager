# Developer Guide – alkfet-book-manager

Ez a dokumentum fejlesztőknek szól: hogyan futtasd lokálisan, hogyan buildeld a konténereket, és hogyan deployold lokális Kubernetesre (kind), illetve ArgoCD-vel.

## 1) Komponensek
- **WebUI** (Angular) – felület
- **WebApi** (ASP.NET) – publikus backend API (`/api/books`)
- **DataAccessApi** (ASP.NET) – adat-hozzáférés API MongoDB-vel (`/books`)
- **McpService** (ASP.NET MCP) – MCP endpoint (`/mcp`)
- **MongoDB** – adatbázis (K8s-ben Helm chartból)

## 2) Repo mappastruktúra (lényeg)

Megj: A repository jelenleg nem teljesen egységes elnevezést használ: a backend projektek a `src/` mappában, míg a frontend az `apps/webui/` mappában található. Ez főként a projekt kialakulásából adódik, nem külön architekturális döntésből.

- Backend: `src/`
- Frontend: `apps/webui/`
- K8s manifestek: `deployment/prod/`
- ArgoCD app: `deployment/argocd/application.yaml`

## 3) Lokális futtatás (Kubernetes nélkül)

### 3.1 Előfeltételek

- Visual Studio (a .NET projektekhez)
- Node.js + npm (Angularhoz)
- Docker Desktop (Mongohoz)

### 3.2 MongoDB indítás Dockerrel

```powershell
docker run --rm -d --name mongo -p 27017:27017 mongo:8
```
Ellenőrzés:

```powershell
docker ps
```

### 3.3 DataAccessApi indítás (Visual Studio)

1. Startup Project: `DataAccessApi`

2. F5

Config: `src/DataAccessApi/appsettings.json` (Mongo rész):

- `Mongo:ConnectionString = mongodb://localhost:27017`

- `Mongo:Database = alkfet`

A lokális fejlesztői port a jelenlegi beállítások alapján: 5033

Teszt:

```powershell
curl.exe http://localhost:5033/health
curl.exe "http://localhost:5033/books?page=1&pageSize=10"
```

### 3.4 WebApi indítás (Visual Studio)

1. Startup Project: `WebApi`

2. F5

Config: `src/WebApi/appsettings.json`:

- `DataAccess:BaseUrl = http://localhost:5033`

A lokális fejlesztői port a jelenlegi beállítások alapján: 5204

Teszt:

```powershell
curl.exe "http://localhost:5024/api/books?page=1&pageSize=10"
```

### 3.5 McpService indítás (Visual Studio)

1. Startup Project: McpService

2. F5

Config: src/McpService/appsettings.json:

- DataAccess:BaseUrl = http://localhost:5033

- Mcp:AuthKey fejlesztői környezetben lehet üres

A lokális fejlesztői port a jelenlegi beállítások alapján: 5111

Teszt:

```
curl.exe http://localhost:5111/health
```

### 3.6 WebUI indítás (Angular dev server)
```
cd apps/webui
npm install
npm start
```
UI:

`http://localhost:4200`

Fejlesztéshez proxy (WebApi felé):

Megj: A frontend fejlesztés közben Angular dev proxy konfigurációt használ. Ennek célja, hogy a WebUI-ból érkező /api kérések a lokálisan futó WebApi felé legyenek továbbítva, így a frontendben nem kell fix backend URL-t használni. Ez azt jelenti, hogy a frontendben elég relatív /api/... útvonalakat használni, és a dev szerver ezeket továbbítja a backend felé.

`apps/webui/proxy.conf.json` target -> `http://localhost:5204`

## 4) Docker image-ek
### 4.1 Dockerfile-ok

`src/DataAccessApi/Dockerfile`

`src/WebApi/Dockerfile`

`src/McpService/Dockerfile`

`apps/webui/Dockerfile`

### 4.2 Lokális build teszt

Repo gyökeréből:
```
docker build -f src/DataAccessApi/Dockerfile -t test-dataaccess .
docker build -f src/WebApi/Dockerfile -t test-webapi .
docker build -f src/McpService/Dockerfile -t test-mcp .
docker build -f apps/webui/Dockerfile -t test-webui .
```

## 5) CI (GitHub Actions) – build + push GHCR-be

Workflow: `.github/workflows/ci.yml`

Feladata:

- build + push mind a 4 image-et GHCR-be

- tagek: `latest` + `${{ github.sha }}`

- GitOps “bump”: frissíti a `deployment/prod/kustomization.yaml` image tagjeit SHA-ra, commitol és pushol

GitHub Actions permissions:

Ha a workflow jogosultsági hibával futna, érdemes ellenőrizni a repository Settings → Actions → General → Workflow permissions beállítását is. Ajánlott:

- Repo → Settings → Actions → General → Workflow permissions:

- Read and write permissions

GHCR:

- A GHCR package-eket Public-ra érdemes állítani, ha a klaszter imagePullSecret nélkül húzza le az image-eket.

## 6) Lokális Kubernetes deploy (Docker Desktop Kubernetes + Helm + kustomize)

### 6.1 Kubernetes bekapcsolása Docker Desktopban

A projekt lokális Kubernetes futtatásához a Docker Desktop beépített Kubernetes támogatását használjuk. Ez egyszerűbb fejlesztői környezetet ad, mint egy külön kind cluster, és jobban illeszkedik ahhoz, hogy a MongoDB futtatásához is Docker Desktopot használunk.

Lépések:

1. Nyisd meg a Docker Desktopot.
2. Kapcsold be a Kubernetes támogatást.
3. Ellenőrizd, hogy a klaszter elérhető:

```
kubectl get nodes
```

### 6.2 MongoDB telepítés Helm-mel

```
kubectl create namespace alkfet

helm install alkfet-mongodb oci://registry-1.docker.io/cloudpirates/mongodb `
  -n alkfet `
  --set fullnameOverride=alkfet-mongodb `
  --set auth.enabled=false `
  --set persistence.enabled=false
  ```

Megjegyzés:

Az auth.enabled=false és persistence.enabled=false beállítások a gyors, lokális fejlesztői/demó környezet egyszerűsítését szolgálják. Ezek nem production jellegű beállítások, hanem eldobható lokális futtatásra készültek.

Ellenőrzés:

```
kubectl -n alkfet get pods
kubectl -n alkfet get svc
```

### 6.3 Alkalmazás telepítés kustomize-zal

```
kubectl apply -k deployment/prod
kubectl -n alkfet get pods
kubectl -n alkfet get svc
```

Elérés:

- WebUI: http://localhost:30080

- WebApi teszt:
```
curl.exe "http://localhost:30081/api/books?page=1&pageSize=10"
```
Hibakeresés:
```
kubectl -n alkfet get pods
kubectl -n alkfet describe pod <POD>
kubectl -n alkfet logs deploy/alkfet-webapi
kubectl -n alkfet logs deploy/alkfet-dataaccess
```

## 7) CD (ArgoCD) – GitOps deploy
### 7.1 ArgoCD telepítés
```
kubectl create namespace argocd
kubectl apply -n argocd --server-side -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
kubectl -n argocd get pods
```

### 7.2 ArgoCD UI elérés (port-forward)

Nyiss egy terminált és hagyd futni:
```
kubectl port-forward svc/argocd-server -n argocd 8080:443
```
UI:

- https://localhost:8080 (self-signed figyelmeztetés elfogadható lokális környezetben)

Admin jelszó (PowerShell base64 decode):
```
$pwdB64 = kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}"
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($pwdB64))
```

### 7.3 Application felvétele

Fájl: `deployment/argocd/application.yaml`

Apply:
```
kubectl apply -f deployment/argocd/application.yaml
kubectl -n argocd get applications
```
Kényszer refresh (ha kell):
```
kubectl -n argocd annotate application alkfet-book-manager argocd.argoproj.io/refresh=hard --overwrite
```
## 8) Release flow

1. Push a `main` branchre

2. CI: build+push GHCR + bumpolja a `deployment/prod/kustomization.yaml` tageket a commit SHA-ra

3. ArgoCD auto-sync -> új podok SHA image taggel futnak

Ellenőrzés:

```
kubectl -n alkfet get deploy alkfet-webapi -o jsonpath="{.spec.template.spec.containers[0].image}`n"
```