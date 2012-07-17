param($installPath, $toolsPath, $package)

function Start-Benchmark {
  param (
    [string] $assembly
  )

  if($toolsPath -eq $null) {
    $toolsPath = Get-Script-Directory
  }

  $assemblyName = [IO.Path]::GetFileNameWithoutExtension($assembly)

  $filePath = (join-path $toolsPath "Benchmarque.Console.exe")

  $p = new-object System.Diagnostics.Process
  $p.StartInfo.Filename = $filePath
  $p.StartInfo.Arguments = $assemblyName
  $p.StartInfo.UseShellExecute = $False
  $p.Start();

  $p.WaitForExit();
}

function Get-Script-Directory {
  Split-Path $script:MyInvocation.MyCommand.Path
}

Export-ModuleMember Start-Benchmark

