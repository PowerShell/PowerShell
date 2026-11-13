---
name: SplitADOPipelines
description: This agent will implement and restructure the repository's existing ADO pipelines into Official and NonOfficial pipelines.
tools: ['vscode', 'execute', 'read', 'agent', 'edit', 'search', 'todo']
---

This agent will implement and restructure the repository's existing ADO pipelines into Official and NonOfficial pipelines. 

A repository will have under the .pipelines directory a series of yaml files that define the ADO pipelines for the repository.

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

## Template Reference Convention (MUST follow)

All `- template:` references to files **inside this repo** must use the **absolute** form anchored at the repo root, with the `@self` suffix:

```yaml
- template: /.pipelines/templates/<path>/<file>.yml@self
```

Do **not** use relative paths such as `templates/...`, `../templates/...`, or bare filenames. Rationale:

- Absolute paths resolve identically regardless of where the referring file lives, so moving a pipeline file between directories (for example, into `.pipelines/NonOfficial/`) does not silently break includes.
- Relative paths are resolved by Azure DevOps against the directory of the referring file, which has caused real outages in this repo when a relative include was composed into a nonexistent nested path like `.pipelines/templates/stages/.pipelines/templates/...`.
- The majority of existing includes already use the absolute form; keeping new work consistent reduces review burden.

The only acceptable non-absolute references are to external repositories resolved via the `resources.repositories` block, for example `v2/OneBranch.Official.CrossPlat.yml@onebranchTemplates`.

## Refactoring Steps

### Step 1: Extract Shared Templates

For each pipeline file that uses the toggle switch pattern (e.g., `PowerShell-Packages-Official.yml`):

1. Create the `.pipelines/templates/variables` and `.pipelines/templates/stages` directories if they don't exist
2. Extract the **variables section** into `.pipelines/templates/variables/PowerShell-Packages-Variables.yml`
3. Extract the **stages section** into `.pipelines/templates/stages/PowerShell-Packages-Stages.yml`

**IMPORTANT**: Only extract the `variables:` and `stages:` sections. All other sections (parameters, resources, extends, etc.) remain in the pipeline files.

### Step 2: Create Official Pipeline (In-Place Refactoring)

The original toggle-based file becomes the Official pipeline:

1. **Keep the file in its original location** (e.g., `.pipelines/PowerShell-Packages-Official.yml` stays where it is)
2. Remove the toggle switch parameter (`templateFile` parameter)
3. Hard-code the Official template reference:
   ```yaml
   extends:
     template: v2/OneBranch.Official.CrossPlat.yml@onebranchTemplates
   ```
4. Replace the `variables:` section with a template reference:
   ```yaml
   variables:
     - template: /.pipelines/templates/variables/PowerShell-Packages-Variables.yml@self
   ```
5. Replace the `stages:` section with a template reference:
   ```yaml
   stages:
     - template: /.pipelines/templates/stages/PowerShell-Packages-Stages.yml@self
   ```

### Step 3: Create NonOfficial Pipeline

1. Create `.pipelines/NonOfficial` directory if it doesn't exist
2. Create the NonOfficial pipeline file (e.g., `.pipelines/NonOfficial/PowerShell-Packages-NonOfficial.yml`)
3. Copy the structure from the refactored Official pipeline
4. Hard-code the NonOfficial template reference:
   ```yaml
   extends:
     template: v2/OneBranch.NonOfficial.CrossPlat.yml@onebranchTemplates
   ```
5. Reference the same shared templates:
   ```yaml
   variables:
     - template: /.pipelines/templates/variables/PowerShell-Packages-Variables.yml@self
   
   stages:
     - template: /.pipelines/templates/stages/PowerShell-Packages-Stages.yml@self
   ```

**Note**: Always use **absolute** template paths of the form `/.pipelines/templates/...@self`. Do not use relative paths like `templates/...` or `../templates/...`. Absolute paths are anchored at the repo root and resolve consistently from any referring file, preventing breakage when files are moved between directories.

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

In the Official pipeline (e.g., `.pipelines/PowerShell-Packages-Official.yml`):

```yaml
stages:
  - template: /.pipelines/templates/stages/PowerShell-Packages-Stages.yml@self
    parameters:
      ob_release_environment: Production
```

#### NonOfficial Pipeline Configuration

In the NonOfficial pipeline (e.g., `.pipelines/NonOfficial/PowerShell-Packages-NonOfficial.yml`):

```yaml
stages:
  - template: /.pipelines/templates/stages/PowerShell-Packages-Stages.yml@self
    parameters:
      ob_release_environment: Test
```

#### Update Stages Template to Accept Parameter

The extracted stages template (e.g., `.pipelines/templates/stages/PowerShell-Packages-Stages.yml`) must declare the parameter at the top:

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
