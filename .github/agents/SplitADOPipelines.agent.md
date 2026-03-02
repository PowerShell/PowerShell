---
name: SplitADOPipelines
description: This agent will implement and restructure the repository's existing ADO pipelines into Official and NonOfficial pipelines.
tools: ['vscode', 'execute', 'read', 'agent', 'edit', 'search', 'todo']
---

This agent will implement and restructure the repository's existing ADO pipelines into Official and NonOfficial pipelines. 

A repository will have under the ./pipelines directory a series of yaml files that define the ADO pipelines for the repository.

First confirm if the pipelines are using a toggle switch for Official and NonOfficial. This will look something like this

```yaml
parameters:
  - name: templateFile
    value: ${{ iif ( parameters.OfficialBuild, 'v2/OneBranch.Official.CrossPlat.yml@onebranchTemplates', 'v2/OneBranch.NonOfficial.CrossPlat.yml@onebranchTemplates' ) }}
```

Followed by:

```yaml
extends:
  template: ${{ variables.templateFile }}
```

This is an indicator that this work needs to be done. This toggle switch is no longer allowed and the templates need to be hard coded.

## Refactoring Steps

### Step 1: Extract Shared Templates

For each pipeline file that uses the toggle switch pattern (e.g., `PowerShell-Packages.yml`):

1. Create a `./pipelines/templates` directory if it doesn't exist
2. Extract the **variables section** into `./pipelines/templates/PowerShell-Packages-Variables.yml`
3. Extract the **stages section** into `./pipelines/templates/PowerShell-Packages-Stages.yml`

**IMPORTANT**: Only extract the `variables:` and `stages:` sections. All other sections (parameters, resources, extends, etc.) remain in the pipeline files.

### Step 2: Create Official Pipeline (In-Place Refactoring)

The original toggle-based file becomes the Official pipeline:

1. **Keep the file in its original location** (e.g., `./pipelines/PowerShell-Packages.yml` stays where it is)
2. Remove the toggle switch parameter (`templateFile` parameter)
3. Hard-code the Official template reference:
   ```yaml
   extends:
     template: v2/OneBranch.Official.CrossPlat.yml@onebranchTemplates
   ```
4. Replace the `variables:` section with a template reference:
   ```yaml
   variables:
     - template: templates/PowerShell-Packages-Variables.yml
   ```
5. Replace the `stages:` section with a template reference:
   ```yaml
   stages:
     - template: templates/PowerShell-Packages-Stages.yml
   ```

### Step 3: Create NonOfficial Pipeline

1. Create `./pipelines/NonOfficial` directory if it doesn't exist
2. Create the NonOfficial pipeline file (e.g., `./pipelines/NonOfficial/PowerShell-Packages-NonOfficial.yml`)
3. Copy the structure from the refactored Official pipeline
4. Hard-code the NonOfficial template reference:
   ```yaml
   extends:
     template: v2/OneBranch.NonOfficial.CrossPlat.yml@onebranchTemplates
   ```
5. Reference the same shared templates:
   ```yaml
   variables:
     - template: ../templates/PowerShell-Packages-Variables.yml
   
   stages:
     - template: ../templates/PowerShell-Packages-Stages.yml
   ```

**Note**: The NonOfficial pipeline uses `../templates/` because it's one directory deeper than the Official pipeline.

### Step 4: Link NonOfficial Pipelines to NonOfficial Dependencies

After creating NonOfficial pipelines, ensure they consume artifacts from other **NonOfficial** pipelines, not Official ones.

1. **Check the `resources:` section** in each NonOfficial pipeline for `pipelines:` dependencies
2. **Identify Official pipeline references** that need to be changed to NonOfficial
3. **Update the `source:` field** to point to the NonOfficial version

**Example Problem:** NonOfficial pipeline pointing to Official dependency
```yaml
resources:
  pipelines:
    - pipeline: CoOrdinatedBuildPipeline
      source: 'PowerShell-Coordinated Binaries-Official'  # ❌ Wrong - Official!
```

**Solution:** Update to NonOfficial dependency
```yaml
resources:
  pipelines:
    - pipeline: CoOrdinatedBuildPipeline
      source: 'PowerShell-Coordinated Binaries-NonOfficial'  # ✅ Correct - NonOfficial!
```

**IMPORTANT**: The `source:` field must match the **exact ADO pipeline definition name** as it appears in Azure DevOps, not necessarily the file name.

### Step 5: Configure Release Environment Parameters (NonAzure Only)

**This step only applies if the pipeline uses `category: NonAzure` in the release configuration.**

If you detect this pattern in the original pipeline:

```yaml
extends:
  template: v2/OneBranch.Official.CrossPlat.yml@onebranchTemplates  # or NonOfficial
  parameters:
    release:
      category: NonAzure
```

Then you must configure the `ob_release_environment` parameter when referencing the stages template.

#### Official Pipeline Configuration

In the Official pipeline (e.g., `./pipelines/PowerShell-Packages.yml`):

```yaml
stages:
  - template: templates/PowerShell-Packages-Stages.yml
    parameters:
      ob_release_environment: Production
```

#### NonOfficial Pipeline Configuration

In the NonOfficial pipeline (e.g., `./pipelines/NonOfficial/PowerShell-Packages-NonOfficial.yml`):

```yaml
stages:
  - template: ../templates/PowerShell-Packages-Stages.yml
    parameters:
      ob_release_environment: Test
```

#### Update Stages Template to Accept Parameter

The extracted stages template (e.g., `./pipelines/templates/PowerShell-Packages-Stages.yml`) must declare the parameter at the top:

```yaml
parameters:
  - name: ob_release_environment
    type: string

stages:
  # ... rest of stages configuration using ${{ parameters.ob_release_environment }}
```

**IMPORTANT**: 
- Only configure this for pipelines with `category: NonAzure`
- Official pipelines always use `ob_release_environment: Production`
- NonOfficial pipelines always use `ob_release_environment: Test`
- The stages template must accept this parameter and use it in the appropriate stage configurations
