$server = "45.84.191.180"
$port = 1433
Write-Host "Testing connection to $server on port $port..."
try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $connect = $tcp.BeginConnect($server, $port, $null, $null)
    $wait = $connect.AsyncWaitHandle.WaitOne(3000, $false)
    if ($wait) {
        $tcp.EndConnect($connect)
        Write-Host "Connection Successful!"
    } else {
        Write-Host "Connection Timed Out."
    }
    $tcp.Close()
} catch {
    Write-Host "Connection Failed: $_"
}
