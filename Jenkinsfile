pipeline {
  agent any

  options {
    timestamps()
    disableConcurrentBuilds()
  }

  environment {
    ACR_NAME = 'goktacr'
    ACR_LOGIN_SERVER = 'goktacr.azurecr.io'

    IMAGE_TAG = "${env.BUILD_NUMBER}"
    API_IMAGE = "${env.ACR_LOGIN_SERVER}/gokt-api:${env.IMAGE_TAG}"
    WORKER_IMAGE = "${env.ACR_LOGIN_SERVER}/gokt-worker:${env.IMAGE_TAG}"
    FRONTEND_IMAGE = "${env.ACR_LOGIN_SERVER}/gokt-frontend:${env.IMAGE_TAG}"

    DEPLOY_PATH = '/opt/gokt'
    FRONTEND_API_BASE_URL = 'http://20.205.29.208:8080/api/v1'
  }

  stages {
    stage('Checkout') {
      steps {
        checkout scm
      }
    }

    stage('Test .NET') {
      environment {
        DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = 'true'
      }
      steps {
        sh 'dotnet test Gokt.sln --configuration Release --nologo'
      }
    }

    stage('Build Frontend') {
      steps {
        dir('frontend') {
          sh 'npm ci'
          sh 'npm run build'
        }
      }
    }

    stage('Azure Login + ACR Login') {
      steps {
        withCredentials([
          string(credentialsId: 'AZURE_CLIENT_ID', variable: 'AZURE_CLIENT_ID'),
          string(credentialsId: 'AZURE_CLIENT_SECRET', variable: 'AZURE_CLIENT_SECRET'),
          string(credentialsId: 'AZURE_TENANT_ID', variable: 'AZURE_TENANT_ID'),
          string(credentialsId: 'AZURE_SUBSCRIPTION_ID', variable: 'AZURE_SUBSCRIPTION_ID')
        ]) {
          sh '''
            az login --service-principal -u "$AZURE_CLIENT_ID" -p "$AZURE_CLIENT_SECRET" --tenant "$AZURE_TENANT_ID"
            az account set --subscription "$AZURE_SUBSCRIPTION_ID"
            az acr login --name "$ACR_NAME"
          '''
        }
      }
    }

    stage('Build + Push Docker Images') {
      steps {
        sh '''
          docker build -f Gokt/Dockerfile -t "$API_IMAGE" .
          docker build -f src/Gokt.MatchingWorker/Dockerfile -t "$WORKER_IMAGE" .
          docker build -f frontend/Dockerfile -t "$FRONTEND_IMAGE" \
            --build-arg VITE_API_BASE_URL="$FRONTEND_API_BASE_URL" frontend

          docker push "$API_IMAGE"
          docker push "$WORKER_IMAGE"
          docker push "$FRONTEND_IMAGE"
        '''
      }
    }

    stage('Deploy To Azure VM') {
      steps {
        withCredentials([
          sshUserPrivateKey(credentialsId: 'AZURE_VM_SSH_KEY', keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER'),
          string(credentialsId: 'AZURE_VM_HOST', variable: 'VM_HOST'),
          string(credentialsId: 'JWT_SECRET', variable: 'JWT_SECRET'),
          string(credentialsId: 'APP_BASE_URL', variable: 'APP_BASE_URL')
        ]) {
          sh '''
            cat > .env.deploy <<EOF
API_IMAGE=$API_IMAGE
WORKER_IMAGE=$WORKER_IMAGE
FRONTEND_IMAGE=$FRONTEND_IMAGE
JWT_SECRET=$JWT_SECRET
APP_BASE_URL=$APP_BASE_URL
POSTGRES_DB=gokt
POSTGRES_USER=gokt
POSTGRES_PASSWORD=gokt
EOF

            ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$SSH_USER@$VM_HOST" "mkdir -p $DEPLOY_PATH"
            scp -i "$SSH_KEY" -o StrictHostKeyChecking=no docker-compose.prod.yml "$SSH_USER@$VM_HOST:$DEPLOY_PATH/docker-compose.prod.yml"
            scp -i "$SSH_KEY" -o StrictHostKeyChecking=no .env.deploy "$SSH_USER@$VM_HOST:$DEPLOY_PATH/.env"

            ACR_TOKEN=$(az acr login --name "$ACR_NAME" --expose-token --output tsv --query accessToken)
            echo "$ACR_TOKEN" | ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$SSH_USER@$VM_HOST" \
              "docker login $ACR_LOGIN_SERVER -u 00000000-0000-0000-0000-000000000000 --password-stdin && cd $DEPLOY_PATH && docker compose -f docker-compose.prod.yml pull && docker compose -f docker-compose.prod.yml up -d --remove-orphans && docker image prune -f"

            rm -f .env.deploy
          '''
        }
      }
    }
  }

  post {
    always {
      sh 'docker logout "$ACR_LOGIN_SERVER" || true'
      cleanWs()
    }
  }
}