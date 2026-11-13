param([string]$Filter, [string]$Name)
$u = 'D:\Unity Hub\6000.6.3f1\Editor\Unity.exe'
$proj = 'D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\MyriadCreation'
$log = Join-Path $proj "Logs\$Name.log"
$results = Join-Path $proj "Logs\$Name.xml"
if (Test-Path $results) { Remove-Item $results -Force }
$args = @('-batchmode','-nographics','-projectPath',$proj,'-runTests','-testPlatform','EditMode','-testFilter',$Filter,'-testResults',$results,'-logFile',$log)
$p = Start-Process -FilePath $u -ArgumentList $args -PassThru -WindowStyle Hidden
$p.WaitForExit(900000) | Out-Null
if (Test-Path $results) {
  [xml]$xml = Get-Content $results -Raw
  $run = $xml.'test-run'
  "total=$($run.total) passed=$($run.passed) failed=$($run.failed)"
  $xml.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
    "FAILED $($_.name): $($_.failure.message.'#cdata-section')"
  }
} else { "NO RESULTS" }
