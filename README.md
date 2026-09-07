## Introduction
In the rapidly evolving **landscape of technology education**, _Microsoft Technical Trainers (MTTs)_ and their students face the challenge of keeping up with complex architectures and technologies. Our innovative research project leverages the power of Generative AI (GPT-4o) to transform the way trainers and students interact with technical diagrams and concepts. By seamlessly integrating AI with traditional teaching methods, we aim to enhance the learning experience, making it more interactive, efficient, and impactful.

[![Hits](https://hits.seeyoufarm.com/api/count/incr/badge.svg?url=https%3A%2F%2Fgithub.com%2Fghoshdipanjan%2FScribbleToAzureRiches&count_bg=%2379C83D&title_bg=%23555555&icon=&icon_color=%23E7E7E7&title=visits&edge_flat=false)](https://hits.seeyoufarm.com)

## What It's About
Our project is **designed to assist MTTs and students by converting handwritten whiteboard diagrams(Hand written notes) into detailed, analyzable content**. When a trainer sketches an architecture involving components like virtual machines, web apps, and SQL databases, our application reads and interprets these diagrams. It:
> :bulb: Identifies the technologies discussed<br>
> :bulb: Provides in-depth architectural details, and <br>
> :bulb: Offers citations for further reading. 
Additionally, <br>
> :bulb: The application generates templates that can be used to create resources in Azure, streamlining the process for both trainers and students. <br><br>Ultimately, 
**this tool can directly create the resources depicted in the diagrams within the trainer's Azure subscription**, _bridging the gap between conceptual understanding and practical implementation._

## How This Tool Helps MTTs and Students
* This tool is a game-changer for MTTs and students, aligning with modern andragogical research that emphasizes active, learner-centered approaches. According to Knowles' Adult Learning Theory, adults learn best when they are actively involved in the learning process and can relate new knowledge to their existing experiences [The Andragogy approach: knowles, Research.com](https://research.com/education/the-andragogy-approach) 
* Our application supports this by providing an interactive platform where learners can visualize and engage with complex technical concepts in real-time.

* Recent studies highlight the importance of experiential learning and problem-based learning in adult education [International Journal of Multidisciplinary Perspectives in Higher Education](https://ojed.org/jimphe) By transforming static diagrams into dynamic, interactive content, our tool fosters a deeper understanding and retention of technical knowledge. It also supports the development of critical thinking and problem-solving skills, which are essential for success in the tech industry.

## Immerse and experience
Incorporating cutting-edge AI technology into the classroom, our project not only enhances the teaching capabilities of MTTs but also empowers students to grasp complex topics more effectively. By providing detailed architectural insights, ready-to-use templates, and direct resource creation in Azure, this tool bridges the gap between theory and practice. It is an indispensable asset for any technical training program, ensuring that both trainers and students stay ahead in the ever-evolving world of technology.

<img style="border:3px solid #ddd;border-radius:4px;" src="https://github.com/user-attachments/assets/a9ab862b-914e-498a-941e-5138a23b9b25" width="600" height="450">

Well this tech. takes us to a whole new level in terms of using the tech. in human learning experience and a "Win-Win for us, let's immerse into this podcast and a discussion on the reaserch and the tool, how we do it, how AI assisted learning like this is real time and what are our possibilites!

## Podcast

<a href="https://scribbletorich-bzfrayc8gvcccsfz.australiaeast-01.azurewebsites.net/Home/Index?podcast=true" target="_blank"><img src="https://github.com/user-attachments/assets/5ccc90cc-17a9-4bcf-94a2-3cd19844efc5" width="800" height="250"></a>

## CI/CD — Deploying to Azure Web App for Containers

The repository ships a GitHub Actions workflow (`.github/workflows/deploy.yml`) that:

1. Builds the `Dockerfile` into a container image and pushes it to **GitHub Container Registry (GHCR)**.
2. Logs in to Azure using **OpenID Connect (OIDC)** — no long-lived credentials are stored as secrets.
3. Creates (if missing) and configures the Azure resources listed below, then deploys the new image.

### Azure resources created by the workflow

| Resource | Value |
|---|---|
| Resource group | `rg-prod` |
| App Service plan | `my-webapp-prod` (Linux, B1) |
| Web app name | `scribble-azure-<repository_id>` |
| Region | `eastus` |

### Required GitHub secrets

Set these in **Settings → Secrets and variables → Actions** of the repository:

| Secret | Description |
|---|---|
| `AZURE_CLIENT_ID` | Application (client) ID of the Azure AD app registration used for federated OIDC login |
| `AZURE_TENANT_ID` | Directory (tenant) ID of your Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID to deploy resources into |
| `GHCR_TOKEN` | **Optional.** GitHub Personal Access Token (classic) with `read:packages` scope — only required when the GHCR package visibility is **private**. If you [make the package public](https://docs.github.com/en/packages/learn-github-packages/configuring-a-packages-access-control-and-visibility), no additional secret is needed. |

### Configuring Azure federated credentials for GitHub OIDC

1. **Create an App Registration** in [Azure Entra ID (Azure AD)](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps):
   - Note the **Application (client) ID** → set as `AZURE_CLIENT_ID`.
   - Note the **Directory (tenant) ID** → set as `AZURE_TENANT_ID`.

2. **Add a federated credential** on the App Registration:
   - Open the app → **Certificates & secrets** → **Federated credentials** → **Add credential**.
   - Scenario: `GitHub Actions deploying Azure resources`.
   - Fill in:
     - **Organization**: `ghoshdipanjan`
     - **Repository**: `ScribbleToAzureRiches`
     - **Entity type**: `Branch`
     - **Branch**: `main`
   - Save the credential.

3. **Assign the Contributor role** to the app registration on your subscription (or resource group):
   ```bash
   az role assignment create \
     --assignee "<AZURE_CLIENT_ID>" \
     --role Contributor \
     --scope "/subscriptions/<AZURE_SUBSCRIPTION_ID>"
   ```

4. Set `AZURE_SUBSCRIPTION_ID` to your Azure subscription ID.

5. **Optional — GHCR_TOKEN:** If the container image on GHCR is private, create a GitHub PAT with `read:packages` scope and set it as the `GHCR_TOKEN` secret so the Azure Web App can pull the image. Alternatively, [make the GHCR package public](https://docs.github.com/en/packages/learn-github-packages/configuring-a-packages-access-control-and-visibility) to skip this requirement entirely.

Once all secrets are configured, push to `main` (or trigger the workflow manually via **Actions → workflow_dispatch**) to deploy.
