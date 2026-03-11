# Daily Dev Podcast — Build Progress

## Done

### Phase 1 — Azure Infrastructure ✅
All resources deployed to `myspace` RG:
- `stdevpodx36xhizqgqxak` — Storage account (Standard_LRS), `episodes` blob container, `Episodes` table
- `cog-devpod-prod-x36xhizqgqxak` — Speech Services S0 (TTS)
- `oai-devpod-prod-x36xhizqgqxak` — Azure OpenAI S0, GPT-4o deployment (10K TPM), East US
- `asp-devpod-prod` + `func-devpod-prod` — Consumption Function App, .NET 8 isolated, all app settings wired

Bicep files: `daily-dev-podcast-infra/`
- Deploy command: `az deployment group create --resource-group myspace --template-file main.bicep --parameters parameters/prod.bicepparam`

### Phase 2 — GitHub Repo + Pipeline ✅
- Repo: `https://github.com/Kensleyz/DevNews`
- Secret `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` added
- Pipeline: `.github/workflows/deploy-functions.yml` — triggers on push to `functions/**`
- Currently set to `workflow_dispatch` until Functions code is complete

### Phase 3 — Functions Code (In Progress)
Location: `functions/src/`

**Built & tested:**
- `Models/FeedItem.cs` — raw feed article
- `Models/Episode.cs` — Table Storage entity
- `Models/PodcastScript.cs` — AI script model
- `Services/FeedAggregatorService.cs` — fetches HN (21), Reddit (40), GitHub (20), Dev.to (85) = 166 items ✅ live tested
- `Services/FilterService.cs` — keyword filter + score ranking, cuts to top 25 ✅ builds

**Not built yet:**
- `Services/OpenAiScriptService.cs` — calls GPT-4o to generate 15-min podcast script
- `Services/TtsService.cs` — calls Azure Speech to convert script → MP3
- `Services/StorageService.cs` — uploads MP3 to Blob, writes metadata to Table
- `Functions/NightlyAggregatorFunction.cs` — timer trigger (00:00 UTC), orchestrates full pipeline
- `Functions/EpisodesApiFunction.cs` — HTTP trigger, returns episode list for mobile app

---

## What's Left

### Step 1 — OpenAiScriptService.cs
- Takes `List<FeedItem>` (filtered, ~25 items)
- Builds a prompt asking GPT-4o to write a 15-min podcast script
- Returns `PodcastScript` with content + estimated duration
- Uses `Azure.AI.OpenAI` SDK (already installed)
- Config: `AzureOpenAI__Endpoint`, `AzureOpenAI__Key`, `AzureOpenAI__DeploymentName`

### Step 2 — TtsService.cs
- Takes script text
- Calls Azure Cognitive Services Speech SDK
- Uses neural voice (e.g. `en-US-AndrewNeural`)
- Returns MP3 byte stream
- Config: `AzureSpeech__Key`, `AzureSpeech__Region`

### Step 3 — StorageService.cs
- `UploadEpisodeAsync(byte[] mp3, string date)` — uploads to `episodes` blob container
- `SaveEpisodeMetadataAsync(Episode episode)` — writes to `Episodes` table
- `GetEpisodesAsync()` — reads episode list for API
- Uses `Azure.Storage.Blobs` + `Azure.Data.Tables` (already installed)
- Config: `PodcastStorage__AccountName`, connection string from `AzureWebJobsStorage`

### Step 4 — NightlyAggregatorFunction.cs
- Timer trigger: `0 0 0 * * *` (00:00 UTC = 02:00 SAST)
- Orchestrates: FeedAggregator → Filter → OpenAI → TTS → Storage
- Writes episode status (`pending` → `ready` or `failed`)

### Step 5 — EpisodesApiFunction.cs
- HTTP GET trigger: `/api/episodes`
- Reads from Table Storage via StorageService
- Returns JSON list of episodes for the mobile app

### Step 6 — Wire up + local end-to-end test
- Update `local.settings.json` with real connection strings from Azure
- Run full pipeline locally: `func start`
- Verify MP3 appears in Blob Storage
- Verify episode row appears in Table Storage

### Step 7 — Push & deploy via pipeline
- Push `functions/` to GitHub → pipeline auto-deploys to `func-devpod-prod`
- Verify function appears in Azure Portal
- Manually trigger nightly function to confirm end-to-end works in Azure

### Phase 4 — Mobile App
- Scaffold React Native / Expo project in `mobile/`
- Episode list screen + audio player
- Calls `EpisodesApiFunction` HTTP endpoint

---

## Key Config Values (already in Function App settings)
| Setting | Value |
|---------|-------|
| `AzureOpenAI__Endpoint` | `https://oai-devpod-prod-x36xhizqgqxak.openai.azure.com/` |
| `AzureOpenAI__DeploymentName` | `gpt-4o` |
| `AzureSpeech__Region` | `southafricanorth` |
| `PodcastStorage__AccountName` | `stdevpodx36xhizqgqxak` |
| `PodcastStorage__EpisodeContainer` | `episodes` |
| `PodcastStorage__EpisodeTable` | `Episodes` |
| `NightlyJob__CronSchedule` | `0 0 0 * * *` |
