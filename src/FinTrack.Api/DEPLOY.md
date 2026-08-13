# Example Cloud Run deploy (from fintrack-dotnet-app repo root)
#
# Prerequisites:
#   - gcloud auth + project set
#   - Artifact Registry repo (e.g. fintrack)
#   - Secrets: Jwt__SigningKey, MongoDb__ConnectionString
#   - Optional: Storage__Gcs__BucketName + runtime SA with storage.objectAdmin
#
# Build & push:
#   gcloud builds submit --tag REGION-docker.pkg.dev/PROJECT/fintrack/fintrack-api:latest \
#     -f src/FinTrack.Api/Dockerfile .
#
# Deploy:
#   gcloud run deploy fintrack-api \
#     --image REGION-docker.pkg.dev/PROJECT/fintrack/fintrack-api:latest \
#     --region us-central1 \
#     --allow-unauthenticated \
#     --set-env-vars "ASPNETCORE_ENVIRONMENT=Production,Storage__Provider=Gcs,Storage__Gcs__BucketName=YOUR_BUCKET,Cors__AllowedOrigins__0=https://YOUR_PROJECT.web.app" \
#     --set-secrets "Jwt__SigningKey=fintrack-jwt:latest,MongoDb__ConnectionString=fintrack-mongo:latest" \
#     --min-instances 0 \
#     --max-instances 2 \
#     --memory 512Mi
#
# Same-origin SPA: Firebase Hosting rewrites /api/** to this Cloud Run service
# (see fintrack-angular-app/firebase.json). Then Cors origins can stay empty for
# browser traffic; set them if you also call the Run URL directly from a browser.
