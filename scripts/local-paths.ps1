function Get-GameLifeLocalApplicationDataPath {
    param([string]$UserProfile = $env:USERPROFILE)

    if ([string]::IsNullOrWhiteSpace($UserProfile)) {
        throw 'O perfil do usuário do Windows não foi encontrado.'
    }

    Join-Path $UserProfile 'AppData\Local'
}

function Get-GameLifeDesktopPath {
    param(
        [string]$UserProfile = $env:USERPROFILE,
        [string]$ConfiguredDesktop
    )

    if ([string]::IsNullOrWhiteSpace($ConfiguredDesktop)) {
        try {
            $ConfiguredDesktop = (Get-ItemProperty -LiteralPath `
                'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders').Desktop
        }
        catch {
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredDesktop)) {
        return [Environment]::ExpandEnvironmentVariables($ConfiguredDesktop)
    }

    if ([string]::IsNullOrWhiteSpace($UserProfile)) {
        throw 'A Área de Trabalho do Windows não foi encontrada.'
    }

    $oneDriveDesktop = Join-Path $UserProfile 'OneDrive\Desktop'
    if (Test-Path -LiteralPath $oneDriveDesktop) {
        return $oneDriveDesktop
    }

    Join-Path $UserProfile 'Desktop'
}
