param($installPath, $toolsPath, $package)

function Start-Benchmark {
  param (
    [string] $assembly
  )

  if($toolsPath -eq $null) {
    $toolsPath = Get-Script-Directory
  }

  $filePath = (join-path $toolsPath "Benchmarque.Console.exe")

  $p = new-object System.Diagnostics.Process
  $p.StartInfo.Filename = $filePath
  $p.StartInfo.Arguments = $assembly
  $p.StartInfo.UseShellExecute = $False
  $p.StartInfo.CreateNoWindow = $True
  $p.StartInfo.WorkingDirectory = $(get-location)
  $p.StartInfo.RedirectStandardOutput = $True
  $p.Start();

  $success = $p.WaitForExit();

  [string] $out = $p.StandardOutput.ReadToEnd();

  ($out)
}

function Get-Script-Directory {
  Split-Path $script:MyInvocation.MyCommand.Path
}

Export-ModuleMember Start-Benchmark

