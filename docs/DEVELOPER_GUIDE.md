# Developer Guide – alkfet-book-manager

Ez a dokumentum fejlesztőknek szól: hogyan futtasd lokálisan, hogyan buildeld a konténereket, és hogyan deployold lokális Kubernetesre (kind), illetve ArgoCD-vel.

## 1) Komponensek
- **WebUI** (Angular) – felület
- **WebApi** (ASP.NET) – publikus backend API (`/api/books`)
- **DataAccessApi** (ASP.NET) – adat-hozzáférés API MongoDB-vel (`/books`)
- **McpService** (ASP.NET MCP) – MCP endpoint (`/mcp`)
- **MongoDB** – adatbázis (K8s-ben Helm chartból)

## 2) Repo mappastruktúra (lényeg)
- Backend: `src/`
- Frontend: `apps/webui/`
- K8s manifestek: `deployment/prod/`
- ArgoCD app: `deployment/argocd/application.yaml`
- kind config: `kind-config.yaml`

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

1. Startup Project: DataAccessApi

2. F5

Config: src/DataAccessApi/appsettings.json (Mongo rész):

- Mongo:ConnectionString = mongodb://localhost:27017

- Mongo:Database = alkfet

Teszt (a <DATAACCESS_PORT> a VS Outputban látszik, pl. 5033):

```powershell
curl.exe http://localhost:<DATAACCESS_PORT>/health
curl.exe "http://localhost:<DATAACCESS_PORT>/books?page=1&pageSize=10"
```

### 3.4 WebApi indítás (Visual Studio)

1. Startup Project: WebApi

2. F5

Config: src/WebApi/appsettings.json:

- DataAccess:BaseUrl = http://localhost:<DATAACCESS_PORT>

Teszt (a <WEBAPI_PORT> a VS Outputban látszik, pl. 5204):

```powershell
curl.exe "http://localhost:<WEBAPI_PORT>/api/books?page=1&pageSize=10"
```

### 3.5 McpService indítás (Visual Studio)

1. Startup Project: McpService

2. F5

Config: src/McpService/appsettings.json:

- DataAccess:BaseUrl = http://localhost:<DATAACCESS_PORT>

Mcp:AuthKey (dev-ben lehet üres)

Teszt (a <MCP_PORT> a VS Outputban látszik):

```
curl.exe http://localhost:<MCP_PORT>/health
```

### 3.6 WebUI indítás (Angular dev server)
```
cd apps/webui
npm install
npm start
```
UI:

http://localhost:4200

Fejlesztéshez proxy (WebApi felé):

apps/webui/proxy.conf.json target -> http://localhost:<WEBAPI_PORT>

## 4) Docker image-ek
### 4.1 Dockerfile-ok

src/DataAccessApi/Dockerfile

src/WebApi/Dockerfile

src/McpService/Dockerfile

apps/webui/Dockerfile

### 4.2 Lokális build teszt

Repo gyökeréből:
```
docker build -f src/DataAccessApi/Dockerfile -t test-dataaccess .
docker build -f src/WebApi/Dockerfile -t test-webapi .
docker build -f src/McpService/Dockerfile -t test-mcp .
docker build -f apps/webui/Dockerfile -t test-webui .
```

## 5) CI (GitHub Actions) – build + push GHCR-be

Workflow: .github/workflows/ci.yml

Feladata:

- build + push mind a 4 image-et GHCR-be

- tagek: latest + ${{ github.sha }}

- GitOps “bump”: frissíti a deployment/prod/kustomization.yaml image tagjeit SHA-ra, commitol és pushol

Fontos GitHub beállítás:

- Repo → Settings → Actions → General → Workflow permissions:

- Read and write permissions

GHCR:

- A GHCR package-eket Public-ra kell állítani, különben a K8s secret nélkül nem tudja lehúzni.

## 6) Lokális Kubernetes deploy (kind + Helm + kustomize)

### 6.1 kind cluster létrehozás

A kind-config.yaml port mapping:

- WebUI: host 30080

- WebApi: host 30081

Cluster létrehozás:
```
kind create cluster --name alkfet --config kind-config.yaml
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

- https://localhost:8080 (self-signed figyelmeztetés oké)

Admin jelszó (PowerShell base64 decode):
```
$pwdB64 = kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}"
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($pwdB64))
```

### 7.3 Application felvétele

Fájl: deployment/argocd/application.yaml

Apply:
```
kubectl apply -f deployment/argocd/application.yaml
kubectl -n argocd get applications
```
Kényszer refresh (ha kell):
```
```
kubectl -n argocd annotate application alkfet-book-manager argocd.argoproj.io/refresh=hard --overwrite

## 8) Release flow

1. Push a main branchre

2. CI: build+push GHCR + bumpolja a deployment/prod/kustomization.yaml tageket a commit SHA-ra

3. ArgoCD auto-sync -> új podok SHA image taggel futnak

Ellenőrzés:

```
kubectl -n alkfet get deploy alkfet-webapi -o jsonpath="{.spec.template.spec.containers[0].image}`n"
```