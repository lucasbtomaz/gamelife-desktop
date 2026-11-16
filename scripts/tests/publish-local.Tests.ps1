Describe 'Publicação local do monorepositório' {
    BeforeAll {
        $script:scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'publish-local.ps1'
        . $script:scriptPath -LibraryOnly
    }

    It 'resolve todos os caminhos dentro da raiz informada' {
        $root = Join-Path $TestDrive 'Gamelife'
        $paths = Get-GameLifePublishPaths -ProjectRoot $root

        $paths.WebRoot | Should Be (Join-Path $root 'src\GameLife.Web')
        $paths.SolutionPath | Should Be (Join-Path $root 'GameLife.slnx')
        $paths.DesktopProject | Should Be (Join-Path $root 'src\GameLife.Desktop\GameLife.Desktop.csproj')
        $paths.OutputPath | Should Be (Join-Path $root 'artifacts\GameLife')
        $paths.PublishedExecutablePath | Should Be (Join-Path $root 'artifacts\GameLife\GameLife.exe')
    }

    It 'cria o atalho apontando direto para a publicacao como o FinBot' {
        $root = Join-Path $TestDrive 'Gamelife'
        $paths = Get-GameLifePublishPaths -ProjectRoot $root
        $steps = @(Get-GameLifePublishSteps -Paths $paths)
        $shortcut = $steps | Where-Object Name -eq 'create shortcut'

        $shortcut.ExecutablePath | Should Be $paths.PublishedExecutablePath
    }

    It 'executa build e publicação na ordem obrigatória' {
        $calls = [Collections.Generic.List[string]]::new()
        $runner = { param($step) $calls.Add($step.Name); return 0 }.GetNewClosure()

        Invoke-GameLifePublish -Paths ([pscustomobject]@{
            WebRoot = 'web'; SolutionPath = 'GameLife.slnx';
            DesktopProject = 'desktop.csproj'; OutputPath = 'artifacts'
        }) -StepRunner $runner

        ($calls -join '|') | Should Be 'database snapshot before|npm ci|npm test|npm run build|dotnet restore|dotnet test|dotnet publish|create shortcut|database snapshot after'
    }

    It 'interrompe antes do dotnet quando o build do React falha' {
        $calls = [Collections.Generic.List[string]]::new()
        $runner = {
            param($step)
            $calls.Add($step.Name)
            if ($step.Name -eq 'npm run build') { return 1 }
            return 0
        }.GetNewClosure()

        $exception = $null
        try {
            Invoke-GameLifePublish -Paths ([pscustomobject]@{
                WebRoot = 'web'; SolutionPath = 'GameLife.slnx';
                DesktopProject = 'desktop.csproj'; OutputPath = 'artifacts'
            }) -StepRunner $runner
        }
        catch {
            $exception = $_
        }

        $exception | Should Not BeNullOrEmpty
        $exception.Exception.Message | Should Match 'npm run build'
        ($calls -join '|') | Should Be 'database snapshot before|npm ci|npm test|npm run build'
    }

    It 'separa a saída textual do código de saída da etapa' {
        $step = [pscustomobject]@{
            Name = 'comando com saída'
            WorkingDirectory = $TestDrive
            Command = { Write-Output 'mensagem normal'; $global:LASTEXITCODE = 0 }
        }

        Invoke-GameLifePublishStep -Step $step | Should Be 0
    }
}
