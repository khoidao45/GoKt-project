# Deploy Gokt To Azure VM With Jenkins

This guide uses Azure VM + Docker Compose as the deployment target, and Jenkins as CI/CD.

## 1. Azure Resources

- Create one Linux VM (Ubuntu 22.04 is recommended).
- Create one Azure Container Registry (ACR).
- Open inbound ports on VM NSG:
  - `22` for SSH
  - `80` for frontend
  - `8080` for API (optional if only internal)

## 2. VM Bootstrap (one time)

Run on the VM:

```bash
sudo apt-get update
sudo apt-get install -y docker.io docker-compose-plugin
sudo usermod -aG docker $USER
newgrp docker

sudo mkdir -p /opt/gokt
sudo chown -R $USER:$USER /opt/gokt
```

If VM needs to pull from private ACR directly:

```bash
az login
az acr login --name <your-acr-name>
```

## 3. Jenkins Agent Requirements

Jenkins executor that runs pipeline must have:

- `dotnet` SDK 8
- `node` 22 + npm
- `docker`
- `az` CLI
- `ssh`/`scp`

## 4. Jenkins Credentials

Create these credentials in Jenkins (IDs must match `Jenkinsfile`):

- `AZURE_CLIENT_ID` (Secret text)
- `AZURE_CLIENT_SECRET` (Secret text)
- `AZURE_TENANT_ID` (Secret text)
- `AZURE_SUBSCRIPTION_ID` (Secret text)
- `AZURE_VM_SSH_KEY` (SSH username with private key)
- `AZURE_VM_HOST` (Secret text, example: `20.1.2.3`)
- `JWT_SECRET` (Secret text)
- `APP_BASE_URL` (Secret text, example: `http://<vm-public-ip>:8080`)

## 5. ACR Name

Current `Jenkinsfile` default:

- `ACR_NAME=goktacr`
- `ACR_LOGIN_SERVER=goktacr.azurecr.io`

Update these in `Jenkinsfile` if your ACR uses a different name.

## 6. Deployment Files

- `docker-compose.prod.yml` is used on VM at `/opt/gokt/docker-compose.prod.yml`.
- Pipeline writes `/opt/gokt/.env` on VM each deploy with image tags and runtime secrets.

## 7. Pipeline Flow

For each run:

1. Checkout code.
2. `dotnet test Gokt.sln`.
3. Build frontend (`npm ci`, `npm run build`).
4. Login Azure with service principal.
5. Build and push images to ACR:
   - `gokt-api:<BUILD_NUMBER>`
   - `gokt-worker:<BUILD_NUMBER>`
   - `gokt-frontend:<BUILD_NUMBER>`
6. Copy compose file + env file to VM.
7. Run `docker compose pull` and `docker compose up -d`.

## 8. Rollback

On VM, set previous image tags in `/opt/gokt/.env` then run:

```bash
cd /opt/gokt
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

## 9. First Run Checklist

- Verify VM can be reached by SSH from Jenkins.
- Verify Jenkins can run Docker build and push.
- Verify ACR push permissions of service principal (`AcrPush`).
- Verify app env vars are valid (`JWT_SECRET`, `APP_BASE_URL`, DB secrets).
- Verify API and frontend are reachable:
  - `http://<vm-ip>/`
  - `http://<vm-ip>:8080/swagger` (if enabled)