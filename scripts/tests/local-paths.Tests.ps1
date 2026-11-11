Describe 'Caminhos locais estaveis do GameLife' {
    BeforeAll {
        $script:scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'local-paths.ps1'
        . $script:scriptPath
    }

    It 'resolve LocalApplicationData fora de pacotes virtualizados' {
        $perfil = Join-Path $TestDrive 'Usuario'

        Get-GameLifeLocalApplicationDataPath -UserProfile $perfil |
            Should Be (Join-Path $perfil 'AppData\Local')
    }

    It 'respeita a Area de Trabalho configurada pelo Windows' {
        $desktop = Join-Path $TestDrive 'OneDrive\Desktop'

        Get-GameLifeDesktopPath -UserProfile $TestDrive -ConfiguredDesktop $desktop |
            Should Be $desktop
    }
}
